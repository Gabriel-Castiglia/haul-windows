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

public enum Etapa { EnCola, Leyendo, Preparando, Bajando, Procesando, Lista, Cancelada, Fallo }

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

    public void Reiniciar()
    {
        baseArchivo = null;
        previos.Clear();
        Fallo = "";
        Progreso = 0;
        velocidad = restante = "";
        parte = 1;
        Archivo = null;
        Etapa = Etapa.EnCola;
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
                    etapa = valor >= 100 ? Etapa.Procesando : Etapa.Bajando;
                }
                velocidad = Errores.Limpio(c[2]);
                restante = Errores.Limpio(c[3]);
                AvisarTodo();
                break;
            case "ARCHIVO" when c.Length >= 2:
                Archivo = c[1].Trim();
                break;
            default:
                if (linea.StartsWith("ERROR:")) Fallar(Errores.Mensaje(linea));
                break;
        }
    }

    public void Terminar() { Progreso = 1; Etapa = Etapa.Lista; }

    private void Anotar(string nombre)
    {
        var p = System.IO.Path.IsPathRooted(nombre) ? nombre : System.IO.Path.Combine(Destino, nombre);
        baseArchivo = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(p) ?? Destino,
                                             System.IO.Path.GetFileNameWithoutExtension(p));
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

    // ---- Values shown by the row ----

    public string Titulo => titulo ?? Enlace;
    public ImageSource? Imagen => imagen;
    public bool SinImagen => imagen == null;

    public bool Activa => etapa is Etapa.Leyendo or Etapa.Preparando or Etapa.Bajando or Etapa.Procesando;
    public bool Terminada => etapa is Etapa.Lista or Etapa.Cancelada or Etapa.Fallo;

    public bool MostrarBarra => etapa is Etapa.Bajando or Etapa.Procesando;
    public bool MostrarEspera => etapa is Etapa.Preparando or Etapa.Leyendo;
    public bool EsError => etapa == Etapa.Fallo;
    public double Opacidad => etapa == Etapa.Cancelada ? 0.5 : 1;

    public bool PuedeQuitar => etapa is Etapa.EnCola or Etapa.Cancelada or Etapa.Fallo;
    public bool PuedeCancelar => Activa;
    public bool PuedeReintentar => etapa is Etapa.Cancelada or Etapa.Fallo;
    public bool PuedeAbrir => etapa == Etapa.Lista && Archivo != null;
    public bool ListoSinArchivo => etapa == Etapa.Lista && Archivo == null;

    public string AyudaQuitar => etapa == Etapa.EnCola ? tr("Sacar de la cola", "Remove from queue")
                                                       : tr("Sacar de la lista", "Remove from list");
    public string AyudaCancelar => etapa == Etapa.Leyendo ? tr("Dejar de leer la lista", "Stop reading the playlist")
                                                          : tr("Cancelar esta descarga", "Cancel this download");
    public string AyudaReintentar => tr("Probar de nuevo", "Try again");
    public string AyudaMostrar => tr("Mostrar en el Explorador", "Show in File Explorer");
    public string AyudaAbrir => tr("Abrir", "Open");

    // Playlist rows show where they belong, e.g. "Mix 2026 · 3/12".
    public string? Ubicacion => NombreLista == null ? null : $"{NombreLista} · {Posicion}";
    public bool TieneUbicacion => NombreLista != null;

    public string TextoDeEstado
    {
        get
        {
            switch (etapa)
            {
                case Etapa.EnCola: return tr("En cola", "Queued") + " · " + Formatos.Titulo(Formato);
                case Etapa.Leyendo: return tr("Leyendo la lista…", "Reading the playlist…");
                case Etapa.Preparando: return tr("Preparando…", "Preparing…");
                case Etapa.Procesando:
                    return Formato == Formato.Audio ? tr("Convirtiendo a MP3…", "Converting to MP3…")
                                                    : tr("Uniendo imagen y sonido…", "Merging picture and sound…");
                case Etapa.Bajando:
                    var partes = new List<string> { $"{(int)(Progreso * 100)} %" };
                    if (velocidad != "") partes.Add(velocidad);
                    if (restante != "") partes.Add(tr($"quedan {restante}", $"{restante} left"));
                    if (Formato == Formato.Video && parte > 1) partes.Add(tr("sonido", "sound"));
                    return string.Join(" · ", partes);
                case Etapa.Lista:
                    var datos = new[] { autor, duracion }.Where(x => !string.IsNullOrEmpty(x));
                    return datos.Any() ? string.Join(" · ", datos) : tr("Listo", "Done");
                case Etapa.Cancelada: return tr("Cancelada", "Cancelled");
                default: return Errores.Traducir(Fallo);
            }
        }
    }

    // YouTube offers WebP thumbnails; the JPEG of the same picture is requested instead, which
    // Windows decodes everywhere.
    private ImageSource? Cargar(string url)
    {
        try
        {
            if (url.Contains("ytimg.com") && url.Contains("vi_webp"))
                url = url.Replace("/vi_webp/", "/vi/").Replace(".webp", ".jpg");
            var b = new BitmapImage();
            b.BeginInit();
            b.UriSource = new Uri(url);
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
