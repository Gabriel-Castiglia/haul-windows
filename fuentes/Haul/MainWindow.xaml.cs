//
//  MainWindow.xaml.cs
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

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Haul.Core;
using Microsoft.Win32;
using static Haul.Core.Loc;

namespace Haul;

public partial class MainWindow : Window
{
    private readonly Cola cola = new();
    private readonly string[] argumentos;

    public MainWindow(string[] args)
    {
        argumentos = args;
        InitializeComponent();
        DataContext = cola;
        // A question card is scrolled into view when it appears.
        cola.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Cola.HayPregunta) && cola.HayPregunta)
                Dispatcher.BeginInvoke(() => TarjetaPregunta.BringIntoView(), System.Windows.Threading.DispatcherPriority.Background);
        };

        RestaurarVentana();
        AplicarTextos();
        Loc.Cambio += AplicarTextos;

        // When links are added, scroll to the queue to show progress.
        cola.Elementos.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                Dispatcher.BeginInvoke(() => SeccionCola.BringIntoView(), System.Windows.Threading.DispatcherPriority.Background);
        };

        Loaded += (_, _) => Arrancar();
        Activated += (_, _) => cola.TomarDelPortapapeles();
        Closing += (_, _) => GuardarVentana();
        SourceInitialized += (_, _) =>
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(Mensajes);
    }

    private void Arrancar()
    {
        // Launching with links from a terminal or a shortcut:
        //   Haul.exe --descargar URL [URL…]
        int i = Array.IndexOf(argumentos, "--descargar");
        if (i >= 0)
        {
            cola.Texto = string.Join("\n", argumentos.Skip(i + 1));
            cola.Agregar();
        }
        else
        {
            cola.TomarDelPortapapeles();
        }
    }

    // Follows accent color and theme changes made in Windows Settings while the app is open.
    private IntPtr Mensajes(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_SETTINGCHANGE = 0x001A, WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;
        if (msg is WM_SETTINGCHANGE or WM_DWMCOLORIZATIONCOLORCHANGED) Acento.Aplicar();
        return IntPtr.Zero;
    }

    private void AplicarTextos()
    {
        Lema.Text = tr("Videos y música de YouTube y casi cualquier sitio", "Video and music from YouTube and almost any site");
        SelectorIdioma.SelectedIndex = Actual == Idioma.Es ? 1 : 0;
        SelectorIdioma.ToolTip = tr("Idioma", "Language");
        BotonAcercaDe.ToolTip = tr("Acerca de Haul", "About Haul");
        System.Windows.Automation.AutomationProperties.SetName(BotonAcercaDe, (string)BotonAcercaDe.ToolTip);
        Sugerencia.Text = tr("Pegá uno o varios enlaces, uno por línea", "Paste one or more links, one per line");
        BotonBorrar.Content = tr("Borrar", "Clear");
        TextoPegar.Text = tr("Pegar", "Paste");
        TituloVideo.Text = Formatos.Titulo(Formato.Video);
        DetalleVideo.Text = Formatos.Detalle(Formato.Video);
        TituloAudio.Text = Formatos.Titulo(Formato.Audio);
        DetalleAudio.Text = Formatos.Detalle(Formato.Audio);
        OpcionVideo.IsChecked = cola.Formato == Formato.Video;
        OpcionAudio.IsChecked = cola.Formato == Formato.Audio;
        TextoGuardarEn.Text = tr("Guardar en", "Save to");
        BotonCambiar.Content = tr("Cambiar…", "Change…");
        BotonAbrirCarpeta.ToolTip = tr("Abrir la carpeta en el Explorador", "Open the folder in File Explorer");
        System.Windows.Automation.AutomationProperties.SetName(BotonAbrirCarpeta, (string)BotonAbrirCarpeta.ToolTip);
        System.Windows.Automation.AutomationProperties.SetName(SelectorIdioma, tr("Idioma", "Language"));
        TituloCola.Text = tr("Cola", "Queue");
        BotonCancelarTodo.Content = tr("Cancelar todo", "Cancel all");
        BotonLimpiar.Content = tr("Limpiar terminadas", "Clear finished");
        Detalles.Header = tr("Detalles técnicos", "Technical details");
        Pie.Text = tr($"Haul {Herramientas.VersionHaul} · Motor: yt-dlp {Herramientas.VersionYtDlp}, incluido en la app",
                      $"Haul {Herramientas.VersionHaul} · Engine: yt-dlp {Herramientas.VersionYtDlp}, bundled with the app");

        var faltan = new List<string>();
        if (!Herramientas.YtDlpPresente) faltan.Add("yt-dlp.exe");
        if (Herramientas.Ffmpeg == null) faltan.Add("ffmpeg");
        if (Herramientas.Deno == null) faltan.Add("deno");
        AvisoFaltantes.Visibility = faltan.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        TextoFaltantes.Text = tr(
            $"Falta {string.Join(", ", faltan)}. Volvé a instalar Haul: powershell -ExecutionPolicy Bypass -File scripts\\instalar.ps1",
            $"{string.Join(", ", faltan)} missing. Reinstall Haul: powershell -ExecutionPolicy Bypass -File scripts\\instalar.ps1");
    }

    // ---- Window size and position, kept between sessions ----

    private void RestaurarVentana()
    {
        var d = Ajustes.Datos;
        if (d.Ancho is double w && d.Alto is double h && d.Izquierda is double x && d.Arriba is double y)
        {
            var area = SystemParameters.VirtualScreenWidth;
            var alto = SystemParameters.VirtualScreenHeight;
            // A position saved on a monitor that is no longer connected is ignored.
            if (x >= SystemParameters.VirtualScreenLeft - 50 && y >= SystemParameters.VirtualScreenTop - 50
                && x + 100 < SystemParameters.VirtualScreenLeft + area && y + 100 < SystemParameters.VirtualScreenTop + alto)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = x; Top = y;
            }
            Width = Math.Max(MinWidth, w);
            Height = Math.Max(MinHeight, h);
        }
        else
        {
            Height = Math.Min(800, SystemParameters.WorkArea.Height - 40);
        }
        if (d.Maximizada) WindowState = WindowState.Maximized;
    }

    private void GuardarVentana()
    {
        var r = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        var d = Ajustes.Datos;
        d.Izquierda = r.Left; d.Arriba = r.Top; d.Ancho = r.Width; d.Alto = r.Height;
        d.Maximizada = WindowState == WindowState.Maximized;
        Ajustes.Guardar();
    }

    // ---- Controls ----

    private void Idioma_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) Loc.Actual = SelectorIdioma.SelectedIndex == 1 ? Idioma.Es : Idioma.En;
    }

    private void AcercaDe_Click(object sender, RoutedEventArgs e) => AcercaDe.Mostrar(this);

    private void PreguntaPrincipal_Click(object sender, RoutedEventArgs e)
    {
        if (cola.HaySecundario) cola.ResponderLista(true); else cola.ResponderGrande(true);
    }
    private void PreguntaSecundaria_Click(object sender, RoutedEventArgs e) => cola.ResponderLista(false);
    private void PreguntaCancelar_Click(object sender, RoutedEventArgs e)
    {
        if (cola.HaySecundario) cola.ResponderLista(null); else cola.ResponderGrande(false);
    }

    private void Pegar_Click(object sender, RoutedEventArgs e) => cola.TomarDelPortapapeles(forzar: true);
    private void Borrar_Click(object sender, RoutedEventArgs e) => cola.Texto = "";

    private void Formato_Checked(object sender, RoutedEventArgs e) =>
        cola.Formato = sender == OpcionAudio ? Formato.Audio : Formato.Video;

    private void Cambiar_Click(object sender, RoutedEventArgs e)
    {
        var dialogo = new OpenFolderDialog { InitialDirectory = cola.Destino, Title = tr("Guardar en", "Save to") };
        if (dialogo.ShowDialog(this) == true) cola.Destino = dialogo.FolderName;
    }

    private void AbrirCarpeta_Click(object sender, RoutedEventArgs e) => Herramientas.Abrir(cola.Destino);

    private void Descargar_Click(object sender, RoutedEventArgs e)
    {
        if (cola.PuedeDescargar) cola.Agregar();
    }

    private void CancelarTodo_Click(object sender, RoutedEventArgs e) => cola.CancelarTodo();
    private void Limpiar_Click(object sender, RoutedEventArgs e) => cola.LimpiarTerminados();

    private static Elemento? Fila(object sender) => (sender as FrameworkElement)?.DataContext as Elemento;

    private void CancelarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) cola.Cancelar(f); }
    private void QuitarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) cola.Quitar(f); }
    private void ReintentarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) cola.Reintentar(f); }
    private void AbrirFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender)?.Archivo is { } a) Herramientas.Abrir(a); }
    private void MostrarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender)?.Archivo is { } a) Herramientas.MostrarEnExplorador(a); }

    // Keeps the technical log scrolled to its last line.
    private void Registro_TextChanged(object sender, TextChangedEventArgs e) => CampoRegistro.ScrollToEnd();
}
