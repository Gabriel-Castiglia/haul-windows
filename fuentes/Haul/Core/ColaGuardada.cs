//
//  ColaGuardada.cs
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Haul.Core;

// One row of the queue as it is kept between sessions.
public sealed class FilaGuardada
{
    public string Enlace { get; set; } = "";
    public string Formato { get; set; } = "video";
    public string Destino { get; set; } = "";
    public string? Prefijo { get; set; }
    public string? NombreLista { get; set; }
    public string? Posicion { get; set; }
    public bool EsLectura { get; set; }
    public bool ListaEntera { get; set; }
    // EnCola, Detenida (paused by the user), Pausada (an instrumental held back) or Fallo.
    public string Etapa { get; set; } = nameof(Core.Etapa.EnCola);
    public string? Fallo { get; set; }
    public string? Titulo { get; set; }
    public string? Autor { get; set; }
    public string? Duracion { get; set; }
    public string? Miniatura { get; set; }
    public bool Instrumental { get; set; }
    public bool Forzar { get; set; }
    public double Progreso { get; set; }
    // The files of an unfinished download, so cancelling it later still removes only what it created.
    public string? BaseArchivo { get; set; }
    public List<string>? Previos { get; set; }
}

// What the queue keeps between sessions: its rows in order, the links still waiting to be added, and whether the
// whole queue was paused.
public sealed class EstadoCola
{
    public int Version { get; set; } = 1;
    public bool EnPausa { get; set; }
    public List<FilaGuardada> Filas { get; set; } = [];
    public List<string> Enlaces { get; set; } = [];

    [JsonIgnore] public bool Vacio => Filas.Count == 0 && Enlaces.Count == 0 && !EnPausa;
}

// %APPDATA%\Haul\queue.json: the queue that comes back when Haul opens again. It is written to a temporary file
// that then replaces the old one, so a crash or a power cut leaves either the old queue or the new one, never half
// of each. A damaged file is set aside as queue.damaged.json (never lost) and Haul starts with an empty queue.
public static class ColaGuardada
{
    public static string Archivo => Path.Combine(Ajustes.Carpeta, "queue.json");

    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // Only web links come back; anything else in the file is left out.
    private static bool EsEnlace(string? s) =>
        Uri.TryCreate(s, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    public static string Serializar(EstadoCola estado) => JsonSerializer.Serialize(estado, Opciones);

    // Writes the queue; an empty queue removes the file. Throws if the folder cannot be written.
    public static void Guardar(EstadoCola estado, string archivo)
    {
        if (estado.Vacio)
        {
            if (File.Exists(archivo)) File.Delete(archivo);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);
        var temporal = archivo + ".tmp";
        using (var f = new FileStream(temporal, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var w = new StreamWriter(f, new System.Text.UTF8Encoding(false)))
        {
            w.Write(Serializar(estado));
            w.Flush();
            f.Flush(flushToDisk: true);
        }
        File.Move(temporal, archivo, overwrite: true);
    }

    // Reads the queue. A missing file is an empty queue; a damaged one too, and "danado" says so after the file was
    // renamed to <name>.damaged.json.
    public static EstadoCola Cargar(string archivo, out bool danado)
    {
        danado = false;
        try { File.Delete(archivo + ".tmp"); } catch { /* left by a save that did not finish */ }
        if (!File.Exists(archivo)) return new EstadoCola();
        try
        {
            var estado = JsonSerializer.Deserialize<EstadoCola>(File.ReadAllText(archivo), Opciones)
                         ?? throw new JsonException("null");
            estado.Filas = (estado.Filas ?? []).Where(f => f != null && EsEnlace(f.Enlace)).ToList();
            estado.Enlaces = (estado.Enlaces ?? []).Where(EsEnlace).ToList();
            return estado;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException or ArgumentException)
        {
            danado = true;
            try { File.Move(archivo, Path.ChangeExtension(archivo, ".damaged.json"), overwrite: true); } catch { /* left in place */ }
            return new EstadoCola();
        }
    }
}
