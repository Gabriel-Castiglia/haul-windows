//
//  Elemento.cs
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

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Haul.Core.Loc;

namespace Haul.Core;

// Esperando: a failed attempt waits before the next retry. Pausada: an instrumental held back.
// Detenida: paused by the user; its partial files are kept so the download resumes.
public enum Etapa { EnCola, Leyendo, Preparando, Bajando, Procesando, Esperando, Lista, Cancelada, Fallo, Pausada, Detenida }

public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Avisar([CallerMemberName] string? nombre = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));

    // Refreshes every binding of the object, used after a language change.
    public void AvisarTodo() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}

// One row of the queue: a download, or a playlist whose contents are being read.
public sealed class Elemento : Observable
{
    public string Enlace { get; }
    public Formato Formato { get; }
    public string Destino { get; }          // folder where the file is saved

    // Playlist rows: number prefix ("03"), playlist name and position ("3/12").
    public string? Prefijo { get; init; }
    public string? NombreLista { get; init; }
    public string? Posicion { get; init; }

    // A placeholder row that reads a playlist and is replaced by its videos.
    public bool EsLectura { get; init; }
    public bool ListaEntera { get; init; }   // for VideoEnLista links: read the whole playlist

    public Elemento(string enlace, Formato formato, string destino)
    {
        Enlace = enlace;
        Formato = formato;
        Destino = destino;
    }

    private Etapa etapa = Etapa.EnCola;
    public Etapa Etapa
    {
        get => etapa;
        set { etapa = value; AvisarTodo(); CambioDeEtapa?.Invoke(); }
    }

    // Lets the queue refresh its summary and buttons on every stage change.
    public Action? CambioDeEtapa;

    public string Fallo { get; private set; } = "";
    public void Fallar(string mensaje) { Fallo = mensaje; Etapa = Etapa.Fallo; }

    // A failed download or playlist read is tried again this many times, two seconds apart.
    public const int MaxReintentos = 4;
    public int Reintentos { get; set; }

    // Set from the metadata yt-dlp prints before downloading (or from the playlist entry).
    public bool Instrumental { get; set; }
    public bool Descartado { get; private set; }
    // The user asked for this instrumental anyway.
    public bool Forzar { get; set; }

    private string? titulo, autor, duracion;
    private string? miniatura;
    private ImageSource? imagen;

    public void FijarInfo(string? t, string? a, string? d, string? m)
    {
        if (!string.IsNullOrWhiteSpace(t)) titulo = t;
        autor = string.IsNullOrWhiteSpace(a) || a == "NA" ? autor : a;
        duracion = string.IsNullOrWhiteSpace(d) ? duracion : d;
        if (!string.IsNullOrWhiteSpace(m) && m != "NA" && m != miniatura)
        {
            miniatura = m;
            imagen = Cargar(m);
        }
        AvisarTodo();
    }

    public double Progreso { get; private set; }
    private string velocidad = "", restante = "";
    private int parte = 1;
    public string? Archivo { get; private set; }

    // Base name of this download's files (without extension) and the files with that base that
    // already existed when it started, so a cancelled download removes only what it created.
    private string? baseArchivo;
    private HashSet<string> previos = new(StringComparer.OrdinalIgnoreCase);

    // A retry pressed by the user starts over; an automatic one keeps the partial files, which
    // yt-dlp resumes, and the list of files that existed before the first attempt.
    public void Reiniciar(bool automatico = false)
    {
        if (!automatico)
        {
            baseArchivo = null;
            previos.Clear();
            Reintentos = 0;
        }
        Fallo = "";
        Progreso = 0;
        velocidad = restante = "";
        parte = 1;
        Archivo = null;
        Etapa = Etapa.EnCola;
    }

    // A pause keeps the progress, the partial files and the list of files that existed before,
    // so resuming continues where it stopped and cancelling still removes only what it created.
    public void Pausar(Etapa nueva)
    {
        Fallo = "";
        velocidad = restante = "";
        Etapa = nueva;
    }

    // Reads one line of yt-dlp output, printed in the custom format requested by the queue.
    public void Procesar(string linea)
    {
        var c = linea.Split('\t');
        switch (c[0])
        {
            case "INFO" when c.Length >= 5:
                FijarInfo(c[1], c[2], Errores.Duracion(c[3]), c[4]);
                if (c.Length >= 6 && c[5].Trim() is { Length: > 0 } nombre && nombre != "NA") Anotar(nombre);
                break;
            case "AVANCE" when c.Length >= 4:
                var numero = c[1].Trim().Replace("%", "");
                if (double.TryParse(numero, NumberStyles.Float, CultureInfo.InvariantCulture, out var valor))
                {
                    // In video mode yt-dlp downloads picture and sound separately; a drop in the
                    // percentage marks the start of the second track.
                    if (valor / 100 < Progreso - 0.5) parte++;
                    Progreso = valor / 100;
                    // Only a running download moves between these stages: a line still queued after the row
                    // failed, or was paused or cancelled, must not bring it back.
                    if (etapa is Etapa.Preparando or Etapa.Bajando or Etapa.Procesando)
                        etapa = valor >= 100 ? Etapa.Procesando : Etapa.Bajando;
                }
                velocidad = Errores.Limpio(c[2]);
                restante = Errores.Limpio(c[3]);
                AvisarTodo();
                break;
            case "META" when c.Length >= 7:
                FijarInfo(c[6], c[2] != "NA" ? c[2] : c[4], null, c.Length >= 8 ? c[7] : null);
                Instrumental = Filtro.EsInstrumental(c[6]);
                Descartado = Filtro.Descartado(c[1], c[6], c[2], c[3], c[4], c[5]);
                break;
            case "ARCHIVO" when c.Length >= 2:
                Archivo = c[1].Trim();
                break;
            default:
                // The error is kept but the row does not turn red yet: yt-dlp may still be running (merging, or
                // with a later error), and a failed row could be removed or retried meanwhile. The queue decides
                // when yt-dlp exits whether to try again or to mark it failed (Cola.Terminar).
                if (linea.StartsWith("ERROR:"))
                {
                    Fallo = Errores.Mensaje(linea);
                    AvisarTodo();
                }
                break;
        }
    }

    public void Terminar() { Progreso = 1; Etapa = Etapa.Lista; }

    // ---- Kept between sessions (ColaGuardada) ----

    // The row as it is kept when Haul closes: a download that was running comes back queued, with its partial
    // files, and a playlist being read is read again. Finished and cancelled rows are not kept (null).
    public FilaGuardada? Guardar()
    {
        Etapa? queda = etapa switch
        {
            Etapa.EnCola or Etapa.Leyendo or Etapa.Preparando or Etapa.Bajando or Etapa.Procesando or Etapa.Esperando => Etapa.EnCola,
            Etapa.Detenida or Etapa.Pausada or Etapa.Fallo => etapa,
            _ => null,
        };
        if (queda is not { } e) return null;
        return new FilaGuardada
        {
            Enlace = Enlace, Formato = Formatos.Clave(Formato), Destino = Destino,
            Prefijo = Prefijo, NombreLista = NombreLista, Posicion = Posicion,
            EsLectura = EsLectura, ListaEntera = ListaEntera,
            Etapa = e.ToString(), Fallo = e == Etapa.Fallo ? Fallo : null,
            Titulo = titulo, Autor = autor, Duracion = duracion, Miniatura = miniatura,
            Instrumental = Instrumental, Forzar = Forzar,
            Progreso = e == Etapa.Detenida ? Progreso : 0,
            BaseArchivo = baseArchivo, Previos = baseArchivo != null ? previos.ToList() : null,
        };
    }

    // A row kept by Guardar. The thumbnail is fetched again.
    public static Elemento Restaurar(FilaGuardada f)
    {
        var e = new Elemento(f.Enlace, Formatos.Desde(f.Formato), string.IsNullOrWhiteSpace(f.Destino) ? Cola.CarpetaDescargas() : f.Destino)
        {
            Prefijo = f.Prefijo, NombreLista = f.NombreLista, Posicion = f.Posicion,
            EsLectura = f.EsLectura, ListaEntera = f.ListaEntera,
        };
        e.etapa = Enum.TryParse<Etapa>(f.Etapa, out var etapa) && etapa is Etapa.Detenida or Etapa.Pausada or Etapa.Fallo
            ? etapa : Etapa.EnCola;
        if (e.etapa == Etapa.Fallo) e.Fallo = f.Fallo ?? "";
        e.Instrumental = f.Instrumental;
        e.Forzar = f.Forzar;
        e.Progreso = e.etapa == Etapa.Detenida ? Math.Clamp(f.Progreso, 0, 1) : 0;
        e.baseArchivo = string.IsNullOrWhiteSpace(f.BaseArchivo) ? null : f.BaseArchivo;
        e.previos = new HashSet<string>(f.Previos ?? [], StringComparer.OrdinalIgnoreCase);
        e.titulo = string.IsNullOrWhiteSpace(f.Titulo) ? null : f.Titulo;
        e.autor = f.Autor;
        e.duracion = f.Duracion;
        if (!string.IsNullOrWhiteSpace(f.Miniatura))
        {
            e.miniatura = f.Miniatura;
            e.imagen = e.Cargar(f.Miniatura);
        }
        return e;
    }

    private void Anotar(string nombre)
    {
        var p = System.IO.Path.IsPathRooted(nombre) ? nombre : System.IO.Path.Combine(Destino, nombre);
        var b = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(p) ?? Destino,
                                       System.IO.Path.GetFileNameWithoutExtension(p));
        if (string.Equals(b, baseArchivo, StringComparison.OrdinalIgnoreCase)) return;   // a retry of the same file
        baseArchivo = b;
        previos = new HashSet<string>(Archivos(), StringComparer.OrdinalIgnoreCase);
    }

    // Every file named "<base>.<anything>": the final file, the separate picture and sound
    // tracks (".f137.mp4"), partial files (".part", ".part-Frag3", ".ytdl") and ".temp" files.
    private IEnumerable<string> Archivos()
    {
        if (baseArchivo == null) return [];
        try
        {
            var carpeta = System.IO.Path.GetDirectoryName(baseArchivo)!;
            return System.IO.Directory.Exists(carpeta)
                ? System.IO.Directory.GetFiles(carpeta, System.IO.Path.GetFileName(baseArchivo) + ".*") : [];
        }
        catch { return []; }
    }

    // Deletes what a cancelled download left behind. Files that existed before it started, such
    // as an earlier download of the same video in another format, are never touched.
    public void BorrarRestos()
    {
        foreach (var f in Archivos().Where(f => !previos.Contains(f)))
        {
            try { System.IO.File.Delete(f); } catch { /* still in use or already gone */ }
        }
        baseArchivo = null;
    }

    // After a successful download, removes what yt-dlp could not delete by itself, such as the
    // original audio after converting to MP3 while a file sync client held it
    // open. The final file and files that existed before are kept; a locked file is tried again.
    public async void BorrarIntermedios()
    {
        if (Archivo == null || baseArchivo == null) return;
        var final = System.IO.Path.GetFullPath(Archivo);
        for (int intento = 0; intento < 5; intento++)
        {
            var quedan = Archivos().Where(f => !previos.Contains(f)
                                               && !string.Equals(System.IO.Path.GetFullPath(f), final, StringComparison.OrdinalIgnoreCase))
                                   .ToList();
            if (quedan.Count == 0) return;
            foreach (var f in quedan)
            {
                try { System.IO.File.Delete(f); } catch { /* still open: next round */ }
            }
            await Task.Delay(3000);
        }
    }

    // ---- Values shown by the row ----

    public string Titulo => titulo ?? Enlace;
    public ImageSource? Imagen => imagen;
    public bool SinImagen => imagen == null;

    public bool Activa => etapa is Etapa.Leyendo or Etapa.Preparando or Etapa.Bajando or Etapa.Procesando or Etapa.Esperando;
    public bool Terminada => etapa is Etapa.Lista or Etapa.Cancelada or Etapa.Fallo;

    public bool MostrarBarra => etapa is Etapa.Bajando or Etapa.Procesando;
    public bool MostrarEspera => etapa is Etapa.Preparando or Etapa.Leyendo or Etapa.Esperando;
    public bool EsError => etapa == Etapa.Fallo;
    public double Opacidad => etapa switch { Etapa.Cancelada => 0.5, Etapa.Pausada or Etapa.Detenida => 0.6, _ => 1 };

    public bool PuedeQuitar => etapa is Etapa.EnCola or Etapa.Cancelada or Etapa.Fallo or Etapa.Pausada or Etapa.Detenida;
    public bool PuedeCancelar => Activa;
    public bool PuedePausar => !EsLectura && etapa is Etapa.EnCola or Etapa.Preparando or Etapa.Bajando or Etapa.Procesando or Etapa.Esperando;
    public bool PuedeReanudar => etapa == Etapa.Detenida;
    public bool PuedeReintentar => etapa is Etapa.Cancelada or Etapa.Fallo or Etapa.Pausada;
    public string IconoReintentar => etapa == Etapa.Pausada ? "\uE896" : "\uE72C";
    public bool PuedeAbrir => etapa == Etapa.Lista && Archivo != null;
    public bool ListoSinArchivo => etapa == Etapa.Lista && Archivo == null;

    public string AyudaQuitar => T(etapa == Etapa.EnCola ? "RemoveFromQueue" : "RemoveFromList");
    public string AyudaCancelar => T(etapa == Etapa.Leyendo ? "StopReading" : "CancelDownload");
    public string AyudaReintentar => T(etapa == Etapa.Pausada ? "DownloadAnyway" : "TryAgain");
    public string AyudaPausar => T("Pause");
    public string AyudaReanudar => T("Resume");
    public string AyudaMostrar => T("ShowInExplorer");
    public string AyudaAbrir => T("Open");
    public string AyudaSubir => T("MoveUp");
    public string AyudaBajar => T("MoveDown");

    // Playlist rows show where they belong, e.g. "Mix 2026 · 3/12".
    public string? Ubicacion => NombreLista == null ? null : $"{NombreLista} · {Posicion}";
    public bool TieneUbicacion => NombreLista != null;

    public string TextoDeEstado
    {
        get
        {
            // While retrying, the attempt is shown first: "Retry 2/4 · 37 % · …".
            var intento = Reintentos > 0 ? T("RetryOf", Reintentos, MaxReintentos) + " · " : "";
            switch (etapa)
            {
                case Etapa.EnCola: return T("Queued") + " · " + Formatos.Titulo(Formato);
                case Etapa.Leyendo: return intento + T("ReadingPlaylist");
                case Etapa.Preparando: return intento + T("Preparing");
                case Etapa.Esperando: return T("WaitingRetry", Reintentos, MaxReintentos);
                case Etapa.Pausada: return T("HeldInstrumental");
                case Etapa.Detenida: return T("Paused") + (Progreso > 0 ? $" · {(int)(Progreso * 100)} %" : "");
                case Etapa.Procesando: return intento + T(Formato == Formato.Audio ? "ConvertingMp3" : "Merging");
                case Etapa.Bajando:
                    var partes = new List<string> { $"{(int)(Progreso * 100)} %" };
                    if (velocidad != "") partes.Add(velocidad);
                    if (restante != "") partes.Add(T("TimeLeft", restante));
                    if (Formato == Formato.Video && parte > 1) partes.Add(T("Sound"));
                    return intento + string.Join(" · ", partes);
                case Etapa.Lista:
                    var datos = new[] { autor, duracion }.Where(x => !string.IsNullOrEmpty(x));
                    return datos.Any() ? string.Join(" · ", datos) : T("Done");
                case Etapa.Cancelada: return T("Cancelled");
                default:
                    return Errores.Traducir(Fallo) + (Reintentos > 0 ? " · " + T("Retries", Reintentos) : "");
            }
        }
    }

    // YouTube offers WebP thumbnails; the JPEG of the same picture is requested instead, which
    // Windows decodes everywhere. Only web addresses are fetched: a thumbnail given by a site (or kept in the
    // saved queue) as a local or network path (file://, \\server\share) is ignored.
    private ImageSource? Cargar(string url)
    {
        try
        {
            if (url.Contains("ytimg.com") && url.Contains("vi_webp"))
                url = url.Replace("/vi_webp/", "/vi/").Replace(".webp", ".jpg");
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return null;
            var b = new BitmapImage();
            b.BeginInit();
            b.UriSource = uri;
            b.DecodePixelWidth = 224;
            b.CacheOption = BitmapCacheOption.OnLoad;
            // If the picture cannot be fetched, the row goes back to the placeholder.
            b.DownloadFailed += (_, _) => { if (imagen == b) { imagen = null; AvisarTodo(); } };
            b.EndInit();
            return b;
        }
        catch
        {
            return null;
        }
    }
}
