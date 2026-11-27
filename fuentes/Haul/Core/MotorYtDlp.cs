//
//  MotorYtDlp.cs
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

using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Haul.Core;

// Newer copies of yt-dlp, downloaded by Haul itself from the official releases on GitHub.
//
// The copy installed with Haul lives in Program Files, which a user cannot write to, so every newer copy goes to
// %LOCALAPPDATA%\Haul\yt-dlp\<version>\yt-dlp.exe, one folder per version. A copy is kept only after its SHA-256
// matches the release's SHA2-256SUMS and it answers its own version number. It is downloaded and checked in
// "<version>.download", and only then that folder is renamed to "<version>": a folder with a version for its name is
// one that passed every check, so no separate mark is written (earlier versions wrote a verified.txt that was never
// read; it is ignored, and removed with its folder). Downloads already running keep the copy they started with (its
// folder is not touched while Haul is open); the next ones use the new copy. Older folders are removed when Haul starts. The copy installed with
// Haul stays as it is and is used again whenever no newer verified copy exists.
public static partial class MotorYtDlp
{
    public const string Repositorio = "yt-dlp/yt-dlp";
    public const string Ejecutable = "yt-dlp.exe";
    public const string Sumas = "SHA2-256SUMS";
    private const string Temporal = ".download";

    // Where the downloaded copies go; the self-test points it to a folder of its own.
    public static string Raiz { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Haul", "yt-dlp");

    private static readonly SemaphoreSlim uno = new(1, 1);

    // yt-dlp's tags are dates with dots ("2026.09.30", sometimes with a fourth number); only those make a folder.
    [GeneratedRegex(@"^\d{4}\.\d{1,2}\.\d{1,2}(\.\d+)?$")]
    private static partial Regex EtiquetaValida();

    public static bool EsEtiqueta(string? etiqueta) => etiqueta != null && EtiquetaValida().IsMatch(etiqueta);

    // The newest verified copy in "raiz" (a folder named after its version, with its yt-dlp.exe) that is newer than the
    // version installed with Haul; null if there is none. Unfinished downloads end in ".download" and never qualify.
    public static (string ruta, string version)? Elegir(string raiz, string incluida)
    {
        (string ruta, string version)? mejor = null;
        string referencia = incluida;
        try
        {
            if (!Directory.Exists(raiz)) return null;
            foreach (var carpeta in Directory.GetDirectories(raiz))
            {
                var nombre = Path.GetFileName(carpeta);
                var exe = Path.Combine(carpeta, Ejecutable);
                if (!EsEtiqueta(nombre) || !File.Exists(exe) || !Actualizacion.EsMasNueva(nombre, referencia)) continue;
                mejor = (exe, nombre);
                referencia = nombre;
            }
        }
        catch { /* an unreadable folder: the copy installed with Haul */ }
        return mejor;
    }

    // Removes every folder in "raiz" except the one of the copy in use ("conservar", or none), including downloads
    // that never finished. A folder still in use stays until the next time.
    public static void Limpiar(string raiz, string? conservar)
    {
        try
        {
            if (!Directory.Exists(raiz)) return;
            var dejar = conservar != null ? Path.GetFullPath(Path.GetDirectoryName(conservar)!) : null;
            foreach (var carpeta in Directory.GetDirectories(raiz))
            {
                if (dejar != null && string.Equals(Path.GetFullPath(carpeta), dejar, StringComparison.OrdinalIgnoreCase)) continue;
                // A folder that can only be removed in part still holds its yt-dlp.exe because that file is in use,
                // and so unchanged; without it the folder is never chosen.
                try { Directory.Delete(carpeta, true); }
                catch { /* in use: next start */ }
            }
        }
        catch { /* nothing to clean */ }
    }

    // At start: uses the newest verified copy, if any, and removes the others.
    public static void Iniciar()
    {
        var elegida = Elegir(Raiz, Herramientas.VersionYtDlpIncluido);
        if (elegida is { } e) Herramientas.UsarYtDlp(e.ruta, e.version);
        Limpiar(Raiz, elegida?.ruta);
    }

    // Asks GitHub for yt-dlp's latest release and, if it is newer than the copy in use, downloads it, checks it and
    // starts using it. Returns whether there was a new version and the version in use afterwards. Throws with a
    // message for the user if something fails; the copy in use stays as it was.
    public static async Task<(bool nueva, string version)> ActualizarAsync(IProgress<int>? progreso = null)
    {
        await uno.WaitAsync();
        try
        {
            var l = await Actualizacion.UltimoAsync(Repositorio);
            if (!Actualizacion.EsMasNueva(l.Etiqueta, Herramientas.VersionYtDlp)) return (false, Herramientas.VersionYtDlp);
            if (!EsEtiqueta(l.Etiqueta))
                throw new InvalidDataException(Loc.T("YtDlpOddTag", l.Etiqueta));
            var urlExe = l.Archivo(Ejecutable);
            var urlSumas = l.Archivo(Sumas);
            if (urlExe == null || urlSumas == null)
                throw new InvalidDataException(Loc.T("YtDlpNoFiles", l.Etiqueta, Ejecutable, Sumas));
            var instalada = await InstalarAsync(Raiz, l.Etiqueta, urlExe, urlSumas, progreso);
            Herramientas.UsarYtDlp(instalada, l.Etiqueta);
            return (true, l.Etiqueta);
        }
        finally
        {
            uno.Release();
        }
    }

    // Downloads and checks one version into its own folder; returns the path of its yt-dlp.exe.
    private static async Task<string> InstalarAsync(string raiz, string etiqueta, string urlExe, string urlSumas, IProgress<int>? progreso)
    {
        var temporal = Path.Combine(raiz, etiqueta + Temporal);
        if (Directory.Exists(temporal)) Directory.Delete(temporal, true);
        var exe = Path.Combine(temporal, Ejecutable);
        try
        {
            await Actualizacion.DescargarVerificadoAsync(urlExe, urlSumas, Ejecutable, exe, progreso);
            // The checked file is run once: it must answer the version it was published as.
            var dice = await VersionDeAsync(exe);
            if (Actualizacion.Normalizar(dice) != Actualizacion.Normalizar(etiqueta))
                throw new InvalidDataException(Loc.T("YtDlpWrongVersion", dice, etiqueta));
            // Renamed only now: from here on the folder counts as verified.
            var final = Path.Combine(raiz, etiqueta);
            if (Directory.Exists(final)) Directory.Delete(final, true);
            Directory.Move(temporal, final);
            return Path.Combine(final, Ejecutable);
        }
        catch
        {
            try { if (Directory.Exists(temporal)) Directory.Delete(temporal, true); } catch { /* next start */ }
            throw;
        }
    }

    // What "yt-dlp --version" prints, or null if it does not answer within a minute.
    public static async Task<string?> VersionDeAsync(string exe)
    {
        // No configuration file and no plugin is read (see Herramientas.Preparar).
        var psi = new ProcessStartInfo(exe, "--ignore-config --no-plugin-dirs --version")
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        using var p = Process.Start(psi);
        if (p == null) return null;
        var salida = p.StandardOutput.ReadToEndAsync();
        _ = p.StandardError.ReadToEndAsync();
        using var limite = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        try { await p.WaitForExitAsync(limite.Token); }
        catch (OperationCanceledException)
        {
            try { p.Kill(true); } catch { /* already gone */ }
            return null;
        }
        return (await salida).Trim();
    }
}
