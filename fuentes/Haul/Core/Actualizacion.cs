//
//  Actualizacion.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: November 20, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace Haul.Core;

// What Haul and yt-dlp share to update themselves from a GitHub release: the latest release and its files, the
// comparison of version numbers, the hash of one file in a checksum list, and a download that is checked against
// that hash before anything else can use it. A file that does not match is deleted and never run.
public static class Actualizacion
{
    // A published release: its tag ("v1.7.0", "2026.09.30"), its page and its files (name → download address).
    public sealed record Lanzamiento(string Etiqueta, string? Pagina, IReadOnlyDictionary<string, string> Archivos)
    {
        public string? Archivo(string nombre) =>
            Archivos.FirstOrDefault(a => string.Equals(a.Key, nombre, StringComparison.OrdinalIgnoreCase)).Value;
    }

    // A version number with its missing parts as zero ("1.7" → 1.7.0.0), so "1.7" and "1.7.0" are the same
    // version; null if the text is not a version. A leading "v" is ignored.
    public static Version? Normalizar(string? texto)
    {
        if (!Version.TryParse((texto ?? "").Trim().TrimStart('v', 'V'), out var v)) return null;
        return new Version(v.Major, v.Minor, Math.Max(0, v.Build), Math.Max(0, v.Revision));
    }

    // Whether a published tag is newer than the version in use (the IsNewer of the other apps of the author).
    // Anything that is not a version number is never newer.
    public static bool EsMasNueva(string? etiqueta, string? mia) =>
        Normalizar(etiqueta) is { } a && Normalizar(mia) is { } b && a > b;

    // The hash of one file in a checksum list, one "hash  name" or "hash *name" per line (the format of
    // sha256sum, used by Haul's SHA256SUMS.txt and yt-dlp's SHA2-256SUMS); null if the file is not listed.
    // The HashFor of the other apps of the author.
    public static string? HashDe(string sumas, string nombre)
    {
        foreach (var crudo in sumas.Split('\n'))
        {
            var linea = crudo.Trim();
            int espacio = linea.IndexOfAny([' ', '\t']);
            if (espacio != 64 || !linea[..64].All(Uri.IsHexDigit)) continue;
            var archivo = linea[espacio..].Trim().TrimStart('*');
            if (string.Equals(archivo, nombre, StringComparison.OrdinalIgnoreCase)) return linea[..64].ToLowerInvariant();
        }
        return null;
    }

    // Reads the answer of GitHub's "latest release" API.
    public static Lanzamiento Leer(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var raiz = doc.RootElement;
        var etiqueta = raiz.GetProperty("tag_name").GetString() ?? "";
        var pagina = raiz.TryGetProperty("html_url", out var h) && h.ValueKind == JsonValueKind.String ? h.GetString() : null;
        var archivos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (raiz.TryGetProperty("assets", out var lista) && lista.ValueKind == JsonValueKind.Array)
            foreach (var a in lista.EnumerateArray())
                if (a.TryGetProperty("name", out var n) && n.GetString() is { Length: > 0 } nombre
                    && a.TryGetProperty("browser_download_url", out var u) && u.GetString() is { Length: > 0 } url)
                    archivos[nombre] = url;
        return new Lanzamiento(etiqueta, pagina, archivos);
    }

    public static HttpClient Cliente(TimeSpan espera)
    {
        var http = new HttpClient { Timeout = espera };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Haul/" + Herramientas.VersionHaul);
        return http;
    }

    // The latest release of a GitHub repository ("owner/name"); throws if GitHub cannot be reached.
    public static async Task<Lanzamiento> UltimoAsync(string repositorio)
    {
        using var http = Cliente(TimeSpan.FromSeconds(15));
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return Leer(await http.GetStringAsync($"https://api.github.com/repos/{repositorio}/releases/latest"));
    }

    // The SHA-256 of a file, in lower-case hexadecimal.
    public static string Sha256(string ruta)
    {
        using var f = File.OpenRead(ruta);
        return Convert.ToHexString(SHA256.HashData(f)).ToLowerInvariant();
    }

    // Keeps the file only if its hash is the expected one; otherwise deletes it. Returns whether it matched.
    public static bool Verificar(string ruta, string esperado)
    {
        bool bien;
        try { bien = string.Equals(Sha256(ruta), esperado, StringComparison.OrdinalIgnoreCase); }
        catch { bien = false; }
        if (!bien) try { File.Delete(ruta); } catch { /* already gone */ }
        return bien;
    }

    // Downloads one file of a release to "ruta" after reading its hash from the release's checksum list, and
    // checks it. Throws, leaving no file behind, if the list does not name it or the hash does not match.
    // "progreso" receives the percentage, or -1 when the size is unknown. Returns the expected hash, so the file can be
    // checked again right before it is used. "urlFirma" (Haul's own releases; yt-dlp has none): the author's signature of
    // the list, checked before anything is downloaded; a list it does not validate throws.
    public static async Task<string> DescargarVerificadoAsync(string url, string urlSumas, string nombre, string ruta, IProgress<int>? progreso,
                                                              string? urlFirma = null)
    {
        using var http = Cliente(TimeSpan.FromMinutes(15));
        var bytesSumas = await http.GetByteArrayAsync(urlSumas);
        if (urlFirma != null && !ReleaseSignature.Valid(bytesSumas, await http.GetStringAsync(urlFirma)))
            throw new InvalidDataException(Loc.tr("La firma de esta versión no es válida: no se bajó ni se instaló nada. Se puede bajar a mano desde su página.",
                                                  "The signature of this release is not valid: nothing was downloaded or installed. It can be downloaded by hand from its page."));
        var esperado = HashDe(System.Text.Encoding.UTF8.GetString(bytesSumas), nombre)
                       ?? throw new InvalidDataException(Loc.tr($"La lista de sumas de comprobación no nombra {nombre}.",
                                                                $"The checksum list does not name {nombre}."));
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        try
        {
            await BajarAsync(http, url, ruta, progreso);
        }
        catch
        {
            try { File.Delete(ruta); } catch { /* nothing was written */ }
            throw;
        }
        if (!Verificar(ruta, esperado))
            throw new InvalidDataException(Loc.tr($"{nombre} no coincide con su suma de comprobación: se borró sin ejecutarlo.",
                                                  $"{nombre} does not match its checksum: it was deleted without running it."));
        return esperado;
    }

    private static async Task BajarAsync(HttpClient http, string url, string ruta, IProgress<int>? progreso)
    {
        using (var respuesta = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        {
            respuesta.EnsureSuccessStatusCode();
            long total = respuesta.Content.Headers.ContentLength ?? -1, hecho = 0;
            await using var entrada = await respuesta.Content.ReadAsStreamAsync();
            await using var salida = new FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[81920];
            int n, ultimo = -2;
            while ((n = await entrada.ReadAsync(buffer)) > 0)
            {
                await salida.WriteAsync(buffer.AsMemory(0, n));
                hecho += n;
                int pct = total > 0 ? (int)(hecho * 100 / total) : -1;
                if (pct != ultimo) { ultimo = pct; progreso?.Report(pct); }
            }
        }
    }
}
