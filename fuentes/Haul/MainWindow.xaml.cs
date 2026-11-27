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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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
        BotonOpciones.IsChecked = Ajustes.Datos.OpcionesAbiertas;
        MostrarOpciones();
        cola.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(Cola.ListaEntera) or nameof(Cola.Instrumentales) or nameof(Cola.LimpiarSola)) MostrarOpciones();
        };
        // A window too low for the queue: the panel above it scrolls with the queue (see Acomodar).
        SizeChanged += (_, _) => Acomodar();
        PanelSuperior.SizeChanged += (_, _) => Acomodar();
        // The picture for the README shows the whole example queue.
        if (App.Captura) Height = 720;
        AplicarTextos();
        Loc.Cambio += AplicarTextos;

        Loaded += (_, _) => { Arrancar(); if (!App.Captura) Comprobaciones(); };
        Activated += (_, _) => { if (!App.Captura) cola.TomarDelPortapapeles(); };
        Closing += (_, _) => { if (App.Captura) return; GuardarVentana(); cola.Cerrar(); };
        Herramientas.CambioYtDlp += () => Dispatcher.BeginInvoke(ActualizarPie);
        SourceInitialized += (_, _) =>
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(Mensajes);
    }

    private void Arrancar()
    {
        if (App.Captura) { cola.Ejemplo(); return; }
        // The queue left when Haul last closed comes back first; a download that was running starts again from
        // its partial files.
        if (!cola.Abrir(ColaGuardada.Archivo))
            MostrarEstado(T("QueueDamaged"));
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

    // "As Windows": follows a change between light and dark made in Windows Settings while the app is open.
    private IntPtr Mensajes(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_SETTINGCHANGE = 0x001A;
        if (msg == WM_SETTINGCHANGE && Ajustes.Datos.Apariencia == Tema.Auto && Tema.SistemaOscuro() != Tema.EsOscuro) Tema.Aplicar();
        return IntPtr.Zero;
    }

    private void Tema_Click(object sender, RoutedEventArgs e) => TemaVentana.Mostrar(this);

    // ---- Room for the queue ----

    // The queue should always show at least two rows. When the window is too low for that with the panel above it
    // fixed (a small screen, a large Windows scale), the panel moves into the scrolling area and scrolls away with
    // the queue; when there is room again it goes back. The two thresholds keep it from flickering at the edge.
    private const double MinimoCola = 190, HolguraCola = 30;

    private void Acomodar()
    {
        if (Content is not FrameworkElement raiz || !IsLoaded) return;
        double libre = raiz.ActualHeight - Cabecera.ActualHeight - Cabecera.Margin.Top - Cabecera.Margin.Bottom - PanelSuperior.ActualHeight;
        bool movido = ZonaMovil.Content == PanelSuperior;
        if (!movido && libre < MinimoCola)
        {
            ZonaFija.Content = null;
            PanelSuperior.Margin = new Thickness(0, 0, 0, 6);
            ZonaMovil.Content = PanelSuperior;
        }
        else if (movido && libre >= MinimoCola + HolguraCola)
        {
            ZonaMovil.Content = null;
            PanelSuperior.Margin = new Thickness(20, 0, 20, 0);
            ZonaFija.Content = PanelSuperior;
        }
    }

    // ---- Options panel ----

    private void Opciones_Cambio(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        Ajustes.Datos.OpcionesAbiertas = BotonOpciones.IsChecked == true;
        Ajustes.Guardar();
        MostrarOpciones();
    }

    // Folded, the line lists the options that are on, so nothing is hidden from view.
    private void MostrarOpciones()
    {
        bool abierto = BotonOpciones.IsChecked == true;
        PanelOpciones.Visibility = abierto ? Visibility.Visible : Visibility.Collapsed;
        FlechaOpciones.Text = abierto ? "\uE70D" : "\uE76C";
        var activas = new List<string>();
        if (!abierto)
        {
            if (cola.ListaEntera) activas.Add(T("OnWholePlaylists"));
            if (cola.Instrumentales) activas.Add(T("OnInstrumentals"));
            if (cola.LimpiarSola) activas.Add(T("OnClearFinished"));
        }
        TextoOpciones.Text = T("Options") + (activas.Count > 0 ? ": " + string.Join(" · ", activas) : "");
        System.Windows.Automation.AutomationProperties.SetName(BotonOpciones, T("Options"));
    }


    private void AplicarTextos()
    {
        Loc.Aplicar(this);
        SelectorIdioma.SelectedIndex = (int)Actual;
        SelectorIdioma.ToolTip = T("Language");
        BotonAcercaDe.ToolTip = T("AboutHaul");
        BotonTema.ToolTip = T("AppearanceAndColour");
        System.Windows.Automation.AutomationProperties.SetName(BotonTema, (string)BotonTema.ToolTip);
        System.Windows.Automation.AutomationProperties.SetName(BotonAcercaDe, (string)BotonAcercaDe.ToolTip);
        Sugerencia.Text = T("LinksHint");
        BotonBorrar.Content = T("Clear");
        TextoPegar.Text = T("Paste");
        TituloVideo.Text = Formatos.Titulo(Formato.Video);
        DetalleVideo.Text = Formatos.Detalle(Formato.Video);
        TituloAudio.Text = Formatos.Titulo(Formato.Audio);
        DetalleAudio.Text = Formatos.Detalle(Formato.Audio);
        OpcionVideo.IsChecked = cola.Formato == Formato.Video;
        OpcionAudio.IsChecked = cola.Formato == Formato.Audio;
        TextoGuardarEn.Text = T("SaveTo");
        BotonCambiar.Content = T("Change");
        BotonAbrirCarpeta.ToolTip = T("OpenFolder");
        System.Windows.Automation.AutomationProperties.SetName(BotonAbrirCarpeta, (string)BotonAbrirCarpeta.ToolTip);
        System.Windows.Automation.AutomationProperties.SetName(SelectorIdioma, T("Language"));
        TituloCola.Text = T("Queue");
        BotonCancelarTodo.Content = T("CancelAll");
        BotonPausarTodo.Content = T("PauseAll");
        BotonSeguirTodo.Content = T("ResumeAll");
        BotonReintentarTodas.Content = T("RetryAll");
        OpcionListaEntera.Content = T("WholePlaylists");
        OpcionListaEntera.ToolTip = T("WholePlaylistsHelp");
        OpcionLimpiar.Content = T("ClearFinished");
        OpcionLimpiar.ToolTip = T("ClearFinishedHelp");
        OpcionInstrumentales.Content = T("Instrumentals");
        MostrarOpciones();
        OpcionInstrumentales.ToolTip = T("InstrumentalsHelp");
        ActualizarPie();

        var faltan = new List<string>();
        if (!Herramientas.YtDlpPresente) faltan.Add("yt-dlp.exe");
        if (Herramientas.Ffmpeg == null) faltan.Add("ffmpeg");
        if (Herramientas.Deno == null) faltan.Add("deno");
        // The pictures for the README never show what this computer lacks.
        AvisoFaltantes.Visibility = faltan.Count > 0 && !App.Captura ? Visibility.Visible : Visibility.Collapsed;
        TextoFaltantes.Text = T("Missing", string.Join(", ", faltan));
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
        if (IsLoaded && SelectorIdioma.SelectedIndex >= 0) Loc.Actual = (Idioma)SelectorIdioma.SelectedIndex;
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
        var dialogo = new OpenFolderDialog { InitialDirectory = cola.Destino, Title = T("SaveTo") };
        if (dialogo.ShowDialog(this) == true) cola.Destino = dialogo.FolderName;
    }

    private void AbrirCarpeta_Click(object sender, RoutedEventArgs e) => Herramientas.Abrir(cola.Destino);

    private void Descargar_Click(object sender, RoutedEventArgs e)
    {
        if (cola.PuedeDescargar) cola.Agregar();
    }

    private void CancelarTodo_Click(object sender, RoutedEventArgs e) => cola.CancelarTodo();
    private void PausarTodo_Click(object sender, RoutedEventArgs e) => cola.PausarTodo();
    private void SeguirTodo_Click(object sender, RoutedEventArgs e) => cola.ReanudarTodo();
    private void ReintentarTodas_Click(object sender, RoutedEventArgs e) => cola.ReintentarFallidas();

    private static Elemento? Fila(object sender) => (sender as FrameworkElement)?.DataContext as Elemento;

    private void CancelarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) cola.Cancelar(f); }
    private void QuitarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) cola.Quitar(f); }
    private void ReintentarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) cola.Reintentar(f); }
    private void PausarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) cola.Pausar(f); }
    private void ReanudarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) cola.Reanudar(f); }
    private void AbrirFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender)?.Archivo is { } a) Herramientas.Abrir(a); }
    private void MostrarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender)?.Archivo is { } a) Herramientas.MostrarEnExplorador(a); }

    // ---- Moving rows with the keyboard: Alt+Up and Alt+Down, or the row's menu (Shift+F10 or the menu key) ----

    private void Fila_KeyDown(object sender, KeyEventArgs e)
    {
        // With Alt held, WPF reports the arrow as Key.System and the arrow itself in SystemKey.
        if (Keyboard.Modifiers != ModifierKeys.Alt || e.Key != Key.System || Fila(sender) is not { } fila) return;
        int paso = e.SystemKey switch { Key.Up => -1, Key.Down => 1, _ => 0 };
        if (paso == 0) return;
        e.Handled = true;
        Desplazar(fila, paso);
    }

    private void SubirFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) Desplazar(f, -1); }
    private void BajarFila_Click(object sender, RoutedEventArgs e) { if (Fila(sender) is { } f) Desplazar(f, 1); }

    // The menu offers only the moves that are possible from where the row is.
    private void Fila_MenuAbriendo(object sender, ContextMenuEventArgs e)
    {
        if (sender is not FrameworkElement { ContextMenu: { } menu } || Fila(sender) is not { } fila) return;
        int i = cola.Elementos.IndexOf(fila);
        if (menu.Items[0] is MenuItem subir) subir.IsEnabled = i > 0;
        if (menu.Items[1] is MenuItem bajar) bajar.IsEnabled = i >= 0 && i < cola.Elementos.Count - 1;
    }

    // Moves the row and keeps the keyboard focus on it, so it can be moved again at once.
    private void Desplazar(Elemento fila, int paso)
    {
        if (!cola.Desplazar(fila, paso)) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (ListaCola.ItemContainerGenerator.ContainerFromItem(fila) is ContentPresenter c
                && VisualTreeHelper.GetChildrenCount(c) > 0 && VisualTreeHelper.GetChild(c, 0) is Border b)
            {
                b.Focus();
                b.BringIntoView();
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    // ---- Dragging rows to reorder the queue ----

    private Point? inicioArrastre;

    private void Fila_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // A press on one of the row's buttons is a click, not the start of a drag.
        inicioArrastre = DentroDeBoton(e.OriginalSource as DependencyObject) ? null : e.GetPosition(this);
    }

    private void Fila_MouseMove(object sender, MouseEventArgs e)
    {
        if (inicioArrastre is not { } inicio || e.LeftButton != MouseButtonState.Pressed || Fila(sender) is not { } fila) return;
        var d = e.GetPosition(this) - inicio;
        if (Math.Abs(d.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(d.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        inicioArrastre = null;
        DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(typeof(Elemento), fila), DragDropEffects.Move);
    }

    // The dragged row takes the place of the one under the pointer as it moves.
    private void Fila_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(Elemento)) is Elemento arrastrada && Fila(sender) is { } destino)
        {
            cola.Mover(arrastrada, destino);
            e.Effects = DragDropEffects.Move;
        }
        else e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void Fila_Drop(object sender, DragEventArgs e) => e.Handled = true;

    // While a row is dragged near the top or bottom edge of the queue, the queue scrolls.
    private void Desplazador_DragOver(object sender, DragEventArgs e)
    {
        const double borde = 40, paso = 18;
        var y = e.GetPosition(Desplazador).Y;
        if (y < borde) Desplazador.ScrollToVerticalOffset(Desplazador.VerticalOffset - paso);
        else if (y > Desplazador.ActualHeight - borde) Desplazador.ScrollToVerticalOffset(Desplazador.VerticalOffset + paso);
    }

    private static bool DentroDeBoton(DependencyObject? o)
    {
        for (; o != null; o = o is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(o) : LogicalTreeHelper.GetParent(o))
            if (o is ButtonBase) return true;
        return false;
    }
}
