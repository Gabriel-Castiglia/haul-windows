//
//  Cola.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: October 20, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using static Haul.Core.Loc;

namespace Haul.Core;

// The download queue: links go in, one download runs at a time, playlists are expanded into
// one row per video.
public sealed partial class Cola : Observable
{
    // Lists longer than this ask for confirmation before filling the queue.
    public const int ListaGrande = 200;

    public ObservableCollection<Elemento> Elementos { get; } = [];

    // Questions shown as a card in the window. While one is open, the remaining links wait.
    private string? preguntaLista;                   // a video opened from a playlist
    private (Elemento fila, string nombre, List<(string enlace, string? titulo, string? autor, string? duracion, string? miniatura)> videos)? esperaGrande;
    private readonly Queue<string> pendientes = new();

    private readonly Dispatcher ui = Dispatcher.CurrentDispatcher;
    private Process? proceso;
    private Elemento? actual;
    private readonly Dictionary<Elemento, Process> lecturas = [];
    private readonly HashSet<Elemento> canceladas = [];
    private uint ultimoPortapapeles;
    private readonly StringBuilder registro = new();

    public Cola()
    {
        // Restores the folder and format from the previous session. If the saved folder no
        // longer exists, Downloads is used.
        var guardada = Ajustes.Datos.Carpeta;
        destino = guardada != null && Directory.Exists(guardada) ? guardada : CarpetaDescargas();
        formato = Formatos.Desde(Ajustes.Datos.Formato);
        Loc.Cambio += () => { AvisarTodo(); foreach (var e in Elementos) e.AvisarTodo(); };
    }

    public static string CarpetaDescargas()
    {
        // The Downloads known folder, which the user may have moved to another drive.
        try
        {
            if (SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, IntPtr.Zero, out var p) == 0)
            {
                var ruta = Marshal.PtrToStringUni(p);
                Marshal.FreeCoTaskMem(p);
                if (!string.IsNullOrEmpty(ruta)) return ruta;
            }
        }
        catch { /* fallback below */ }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    // ---- State bound by the window ----

    private string texto = "";
    public string Texto
    {
        get => texto;
        set { texto = value; Avisar(); Avisar(nameof(Detectados)); Avisar(nameof(Contador));
              Avisar(nameof(TituloDelBoton)); Avisar(nameof(PuedeDescargar)); Avisar(nameof(TextoVacio)); }
    }
    public bool TextoVacio => texto.Length == 0;

    private Formato formato;
    public Formato Formato
    {
        get => formato;
        set { formato = value; Ajustes.Datos.Formato = Formatos.Clave(value); Ajustes.Guardar(); Avisar(); }
    }

    private string destino;
    public string Destino
    {
        get => destino;
        set { destino = value; Ajustes.Datos.Carpeta = value; Ajustes.Guardar(); Avisar(); Avisar(nameof(NombreDestino)); }
    }

    public string NombreDestino =>
        string.Equals(destino.TrimEnd('\\'), CarpetaDescargas().TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
            ? tr("Descargas", "Downloads") : Path.GetFileName(destino.TrimEnd('\\')) is { Length: > 0 } n ? n : destino;

    public List<string> Detectados => Enlaces.En(texto);
    public bool Ocupada => actual != null || lecturas.Count > 0;
    public bool HayTerminados => Elementos.Any(e => e.Terminada);
    public bool HayElementos => Elementos.Count > 0;
    public bool PuedeDescargar => Detectados.Count > 0 && Herramientas.YtDlpPresente;

    public string Registro => registro.ToString();
    public bool HayRegistro => registro.Length > 0;

    public string Resumen
    {
        get
        {
            var hechas = Elementos.Count(e => e.Etapa == Etapa.Lista);
            var fallos = Elementos.Count(e => e.Etapa == Etapa.Fallo);
            // A playlist still being read is not counted yet; one that failed is.
            var total = Elementos.Count(e => !(e.EsLectura && e.Activa));
            var t = tr($"{hechas} de {total} listas", $"{hechas} of {total} done");
            if (fallos > 0) t += " · " + tr($"{fallos} con error", $"{fallos} failed");
            return t;
        }
    }

    public string Contador
    {
        get
        {
            var n = Detectados.Count;
            return n switch
            {
                0 => texto.Length == 0 ? tr("Ctrl+Enter para descargar", "Ctrl+Enter to download")
                                       : tr("No veo ningún enlace", "No links found"),
                1 => tr("1 enlace · Ctrl+Enter para descargar", "1 link · Ctrl+Enter to download"),
                _ => tr($"{n} enlaces · Ctrl+Enter para descargar", $"{n} links · Ctrl+Enter to download"),
            };
        }
    }

    public string TituloDelBoton
    {
        get
        {
            var n = Detectados.Count;
            if (Ocupada && n > 0)
                return n == 1 ? tr("Agregar a la cola", "Add to queue") : tr($"Agregar {n} a la cola", $"Add {n} to queue");
            return n > 1 ? tr($"Descargar {n} enlaces", $"Download {n} links") : tr("Descargar", "Download");
        }
    }

    private void Refrescar()
    {
        foreach (var p in new[] { nameof(Ocupada), nameof(HayTerminados), nameof(HayElementos), nameof(Resumen),
                                  nameof(TituloDelBoton) })
            Avisar(p);
    }

    private void Anotar(string linea)
    {
        registro.Append(linea).Append('\n');
        // Keeps the technical log to a reasonable size during long playlists.
        if (registro.Length > 400_000) registro.Remove(0, registro.Length - 300_000);
        Avisar(nameof(Registro));
        Avisar(nameof(HayRegistro));
    }

    // ---- Clipboard ----

    // Adds the links found on the clipboard. In automatic mode it only acts when the field is
    // empty and the clipboard changed since the last read, so processed links are not re-added.
    public void TomarDelPortapapeles(bool forzar = false)
    {
        uint numero = GetClipboardSequenceNumber();
        if (!forzar && (texto.Length > 0 || numero == ultimoPortapapeles)) return;
        ultimoPortapapeles = numero;
        string contenido;
        try { contenido = Clipboard.ContainsText() ? Clipboard.GetText() : ""; }
        catch (COMException) { return; }   // another program holds the clipboard open
        var yaEstan = Detectados.ToHashSet();
        var nuevos = Enlaces.En(contenido).Where(x => !yaEstan.Contains(x)).ToList();
        if (nuevos.Count == 0) return;
        Texto = (texto.Length == 0 || texto.EndsWith('\n') ? texto : texto + "\n") + string.Join("\n", nuevos) + "\n";
    }

    // ---- Queue operations ----

    public void Agregar()
    {
        var nuevos = Detectados;
        if (nuevos.Count == 0) return;
        Texto = "";
        foreach (var enlace in nuevos) pendientes.Enqueue(enlace);
        ProcesarPendientes();
    }

    // Adds the waiting links in order; stops at a video opened from a playlist until the user answers.
    private void ProcesarPendientes()
    {
        while (preguntaLista == null && pendientes.Count > 0)
        {
            var enlace = pendientes.Dequeue();
            switch (Enlaces.Clasificar(enlace))
            {
                case Clase.Lista: Leer(new Elemento(enlace, formato, destino) { EsLectura = true, ListaEntera = true }); break;
                case Clase.VideoEnLista: preguntaLista = enlace; AvisarPregunta(); break;
                default: Sumar(new Elemento(enlace, formato, destino)); break;
            }
        }
        Siguiente();
    }

    // Answer to the playlist question: the whole playlist (true), only the video (false) or nothing (null).
    public void ResponderLista(bool? entera)
    {
        if (preguntaLista is not { } enlace) return;
        preguntaLista = null;
        AvisarPregunta();
        if (entera == true) Leer(new Elemento(enlace, formato, destino) { EsLectura = true, ListaEntera = true });
        else if (entera == false) Sumar(new Elemento(enlace, formato, destino));
        ProcesarPendientes();
    }

    public void ResponderGrande(bool agregar)
    {
        if (esperaGrande is not { } espera) return;
        esperaGrande = null;
        AvisarPregunta();
        if (agregar) Expandir(espera.fila, espera.nombre, espera.videos);
        else { espera.fila.Etapa = Etapa.Cancelada; Refrescar(); }
    }

    // ---- The question card ----

    public bool HayPregunta => preguntaLista != null || esperaGrande != null;
    public bool CampoHabilitado => !HayPregunta;
    public string IconoPregunta => preguntaLista != null ? "\uE8FD" : "\uE8F1";
    public string TituloPregunta => preguntaLista != null
        ? tr("Este video está dentro de una lista", "This video is part of a playlist")
        : tr("Una lista grande", "A large playlist");
    public string TextoPregunta => preguntaLista != null
        ? tr("Podés bajar solo este video o la lista entera. Con la lista entera, cada video entra a la cola por "
             + "separado y se guarda en una carpeta con el nombre de la lista, numerado en orden.",
             "You can download just this video or the whole playlist. With the whole playlist, each video joins "
             + "the queue on its own and is saved in a folder named after the playlist, in order.")
        : esperaGrande is { } e
            ? tr($"«{NombreVisible(e.nombre)}» tiene {e.videos.Count} videos. ¿Los agrego todos a la cola?",
                 $"\"{NombreVisible(e.nombre)}\" has {e.videos.Count} videos. Add them all to the queue?")
            : "";
    public string? DetallePregunta => preguntaLista;
    public bool HayDetallePregunta => preguntaLista != null;
    public string TextoPrincipal => preguntaLista != null ? tr("La lista entera", "The whole playlist")
        : tr($"Agregar {esperaGrande?.videos.Count}", $"Add {esperaGrande?.videos.Count}");
    public string TextoSecundario => tr("Solo este video", "Only this video");
    public bool HaySecundario => preguntaLista != null;
    public string TextoCancelar => tr("Cancelar", "Cancel");

    private static string NombreVisible(string n) => string.IsNullOrWhiteSpace(n) ? tr("Lista", "Playlist") : n;

    private void AvisarPregunta()
    {
        foreach (var p in new[] { nameof(HayPregunta), nameof(CampoHabilitado), nameof(IconoPregunta), nameof(TituloPregunta),
                                  nameof(TextoPregunta), nameof(DetallePregunta), nameof(HayDetallePregunta),
                                  nameof(TextoPrincipal), nameof(TextoSecundario), nameof(HaySecundario) })
            Avisar(p);
    }

    private void Sumar(Elemento e, int? posicion = null)
    {
        e.CambioDeEtapa = Refrescar;
        if (posicion is int i && i <= Elementos.Count) Elementos.Insert(i, e); else Elementos.Add(e);
        Refrescar();
    }

    public void Quitar(Elemento e)
    {
        if (e.Activa) return;
        Elementos.Remove(e);
        Refrescar();
    }

    public void Cancelar(Elemento e)
    {
        if (lecturas.TryGetValue(e, out var p)) { canceladas.Add(e); Matar(p); }
        else if (e == actual && proceso != null) { canceladas.Add(e); Matar(proceso); }
        else if (e.Etapa == Etapa.EnCola) e.Etapa = Etapa.Cancelada;
    }

    public void CancelarTodo()
    {
        foreach (var e in Elementos.Where(x => x.Etapa == Etapa.EnCola)) e.Etapa = Etapa.Cancelada;
        foreach (var (e, p) in lecturas.ToList()) { canceladas.Add(e); Matar(p); }
        if (actual != null && proceso != null) { canceladas.Add(actual); Matar(proceso); }
    }

    public void Reintentar(Elemento e)
    {
        if (e.EsLectura) { e.Reiniciar(); Leer(e, yaEnLaCola: true); return; }
        e.Reiniciar();
        Siguiente();
    }

    public void LimpiarTerminados()
    {
        foreach (var e in Elementos.Where(x => x.Terminada).ToList()) Elementos.Remove(e);
        Refrescar();
    }

    // yt-dlp starts ffmpeg as a child process, so the whole tree is stopped.
    private static void Matar(Process p)
    {
        try { p.Kill(entireProcessTree: true); } catch { /* already finished */ }
    }

    // ---- Downloads ----

    // Processes the queue serially: the next download starts when the previous one ends.
    private void Siguiente()
    {
        if (actual != null) return;
        var e = Elementos.FirstOrDefault(x => x.Etapa == Etapa.EnCola && !x.EsLectura);
        if (e == null) { Refrescar(); return; }

        try { Directory.CreateDirectory(e.Destino); }
        catch (Exception ex) { e.Fallar(tr("No se pudo crear la carpeta: ", "Could not create the folder: ") + ex.Message); Siguiente(); return; }

        var plantilla = e.Prefijo != null ? $"{e.Prefijo} - %(title)s.%(ext)s" : "%(title)s.%(ext)s";
        // Output is requested in a custom, tab-separated format:
        //   INFO     video metadata and file name, before downloading (-O before_dl)
        //   AVANCE   percentage, speed and time remaining (--progress-template)
        //   ARCHIVO  final path, after merging or converting (-O after_move)
        var args = Formatos.Argumentos(e.Formato).Concat(new[]
        {
            "--newline", "--color", "no_color", "--no-playlist", "--encoding", "utf-8",
            "-o", plantilla,
            "--no-simulate", "--progress",
            "--progress-template",
            "download:AVANCE\t%(progress._percent_str)s\t%(progress._speed_str)s\t%(progress._eta_str)s",
            "-O", "before_dl:INFO\t%(title)s\t%(uploader)s\t%(duration)s\t%(thumbnail)s\t%(filename)s",
            "-O", "after_move:ARCHIVO\t%(filepath)s",
        }).ToList();
        if (Herramientas.Ffmpeg != null) { args.Add("--ffmpeg-location"); args.Add(Herramientas.Ffmpeg); }
        args.Add("--");
        args.Add(e.Enlace);

        var p = new Process { StartInfo = Herramientas.Preparar(args, e.Destino), EnableRaisingEvents = true };
        p.OutputDataReceived += (_, d) => { if (d.Data != null) ui.BeginInvoke(() => { Anotar(d.Data); e.Procesar(d.Data); }); };
        p.ErrorDataReceived += (_, d) => { if (d.Data != null) ui.BeginInvoke(() => { Anotar(d.Data); e.Procesar(d.Data); }); };
        p.Exited += (_, _) => ui.BeginInvoke(() => Terminar(e, p));

        Anotar($"── {e.Enlace} ──");
        e.Etapa = Etapa.Preparando;
        actual = e;
        try
        {
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            proceso = p;
        }
        catch (Exception ex)
        {
            actual = null;
            e.Fallar(tr("No se pudo arrancar yt-dlp: ", "Could not start yt-dlp: ") + ex.Message);
            Siguiente();
        }
        Refrescar();
    }

    private void Terminar(Elemento e, Process p)
    {
        // Exited can fire before the last output lines are delivered; waiting drains them.
        p.WaitForExit();
        int codigo = p.ExitCode;
        p.Dispose();
        // Lines still queued on the dispatcher are processed before deciding the result.
        ui.BeginInvoke(DispatcherPriority.Background, () =>
        {
            proceso = null;
            actual = null;
            if (canceladas.Remove(e)) { e.BorrarRestos(); e.Etapa = Etapa.Cancelada; }
            else if (codigo == 0) e.Terminar();
            else if (e.Etapa != Etapa.Fallo)
                e.Fallar(tr($"yt-dlp terminó con el código {codigo}. El detalle está en «Detalles técnicos».",
                            $"yt-dlp exited with code {codigo}. See \"Technical details\" for more."));
            Siguiente();
        });
    }

    // ---- Playlists ----

    // Reads a playlist with yt-dlp (--flat-playlist, no download) and replaces the placeholder
    // row with one row per video, saved in a subfolder named after the playlist and numbered in
    // playlist order.
    private void Leer(Elemento fila, bool yaEnLaCola = false)
    {
        if (!yaEnLaCola) Sumar(fila);
        fila.FijarInfo(null, null, null, null);
        fila.Etapa = Etapa.Leyendo;

        var args = new List<string> { "--flat-playlist", "--yes-playlist", "-J", "--no-warnings", "--encoding", "utf-8",
                                      "--", fila.Enlace };
        var p = new Process { StartInfo = Herramientas.Preparar(args), EnableRaisingEvents = true };
        var salida = new StringBuilder();
        var error = new StringBuilder();
        p.OutputDataReceived += (_, d) => { if (d.Data != null) lock (salida) salida.Append(d.Data).Append('\n'); };
        p.ErrorDataReceived += (_, d) => { if (d.Data != null) lock (error) error.Append(d.Data).Append('\n'); };
        p.Exited += (_, _) =>
        {
            p.WaitForExit();
            int codigo = p.ExitCode;
            p.Dispose();
            ui.BeginInvoke(() => Leida(fila, codigo, salida.ToString(), error.ToString()));
        };

        Anotar($"── {fila.Enlace} ({tr("lista", "playlist")}) ──");
        try
        {
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            lecturas[fila] = p;
        }
        catch (Exception ex)
        {
            fila.Fallar(tr("No se pudo arrancar yt-dlp: ", "Could not start yt-dlp: ") + ex.Message);
        }
        Refrescar();
    }

    private void Leida(Elemento fila, int codigo, string json, string error)
    {
        lecturas.Remove(fila);
        foreach (var l in error.Split('\n', StringSplitOptions.RemoveEmptyEntries)) Anotar(l);
        if (canceladas.Remove(fila)) { fila.Etapa = Etapa.Cancelada; Refrescar(); return; }

        if (codigo != 0)
        {
            var linea = error.Split('\n').LastOrDefault(l => l.StartsWith("ERROR:"));
            fila.Fallar(linea != null ? Errores.Mensaje(linea) : tr($"yt-dlp terminó con el código {codigo}.", $"yt-dlp exited with code {codigo}."));
            Refrescar();
            return;
        }

        List<(string enlace, string? titulo, string? autor, string? duracion, string? miniatura)> videos = [];
        string nombre;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var raiz = doc.RootElement;
            if (Cadena(raiz, "_type") != "playlist" || !raiz.TryGetProperty("entries", out var entradas))
            {
                // Not a playlist after all (yt-dlp resolved it to a single video).
                Elementos.Remove(fila);
                Sumar(new Elemento(fila.Enlace, fila.Formato, fila.Destino));
                Siguiente();
                return;
            }
            nombre = Cadena(raiz, "title") ?? "";
            foreach (var v in entradas.EnumerateArray())
            {
                var enlace = Cadena(v, "url") ?? Cadena(v, "webpage_url");
                if (enlace == null && Cadena(v, "id") is { } id) enlace = "https://www.youtube.com/watch?v=" + id;
                if (enlace == null) continue;
                if (!enlace.StartsWith("http") && Cadena(v, "id") is { } id2) enlace = "https://www.youtube.com/watch?v=" + id2;
                string? mini = null;
                if (v.TryGetProperty("thumbnails", out var ts) && ts.ValueKind == JsonValueKind.Array && ts.GetArrayLength() > 0)
                    mini = Cadena(ts[ts.GetArrayLength() - 1], "url");
                string? dur = v.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number
                    ? Errores.Duracion(d.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture)) : null;
                videos.Add((enlace, Cadena(v, "title"), Cadena(v, "channel") ?? Cadena(v, "uploader"), dur, mini));
            }
        }
        catch (JsonException ex)
        {
            fila.Fallar(tr("No se pudo leer la lista: ", "Could not read the playlist: ") + ex.Message);
            Refrescar();
            return;
        }

        if (videos.Count == 0)
        {
            fila.Fallar(tr("La lista está vacía.", "The playlist is empty."));
            Refrescar();
            return;
        }
        if (videos.Count > ListaGrande)
        {
            esperaGrande = (fila, nombre, videos);
            AvisarPregunta();
            return;
        }
        Expandir(fila, nombre, videos);
    }

    private void Expandir(Elemento fila, string nombre,
                          List<(string enlace, string? titulo, string? autor, string? duracion, string? miniatura)> videos)
    {
        var carpeta = Path.Combine(fila.Destino, NombreDeCarpeta(nombre));
        int ancho = Math.Max(2, videos.Count.ToString().Length);
        int lugar = Elementos.IndexOf(fila);
        Elementos.Remove(fila);
        for (int i = 0; i < videos.Count; i++)
        {
            var (enlace, titulo, autor, duracion, miniatura) = videos[i];
            var e = new Elemento(enlace, fila.Formato, carpeta)
            {
                Prefijo = (i + 1).ToString().PadLeft(ancho, '0'),
                NombreLista = string.IsNullOrWhiteSpace(nombre) ? tr("Lista", "Playlist") : nombre,
                Posicion = $"{i + 1}/{videos.Count}",
            };
            e.FijarInfo(titulo, autor, duracion, miniatura);
            Sumar(e, lugar < 0 ? null : lugar + i);
        }
        Siguiente();
    }

    private static string? Cadena(JsonElement e, string propiedad) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(propiedad, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    // A playlist title made safe as a Windows folder name.
    public static string NombreDeCarpeta(string titulo)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var t = new string(titulo.Select(c => invalidos.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.', ' ');
        if (t.Length > 120) t = t[..120].TrimEnd('.', ' ');
        string[] reservados = ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "LPT1", "LPT2", "LPT3"];
        if (t.Length == 0 || reservados.Contains(t.ToUpperInvariant())) t = tr("Lista", "Playlist") + (t.Length > 0 ? " " + t : "");
        return t;
    }

    [LibraryImport("user32.dll")]
    private static partial uint GetClipboardSequenceNumber();

    [LibraryImport("shell32.dll")]
    private static partial int SHGetKnownFolderPath(in Guid rfid, uint flags, IntPtr token, out IntPtr ruta);
}
