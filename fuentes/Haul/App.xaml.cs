//
//  App.xaml.cs
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
using Haul.Core;

namespace Haul;

public partial class App : Application
{
    // Test option (not shown to users): "--captura <file.png>" draws a window into a PNG and exits, without
    // writing the settings; "--ventana main|tema|acerca|aviso" picks the window, and "--idioma en|es",
    // "--tema light|dark" and "--color <key>" override the stored language, appearance and colour.
    public static bool Captura { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Ajustes.Cargar();
        string? Opcion(string nombre)
        {
            int i = Array.IndexOf(e.Args, nombre);
            return i >= 0 && i + 1 < e.Args.Length ? e.Args[i + 1] : null;
        }
        var archivo = Opcion("--captura");
        Captura = archivo != null;
        if (Captura)
        {
            Ajustes.SoloLectura = true;
            if (Opcion("--idioma") is string idioma) Ajustes.Datos.Idioma = idioma;
            if (Opcion("--tema") is string tema) Ajustes.Datos.Apariencia = tema;
            if (Opcion("--color") is string color) Ajustes.Datos.Color = color;
        }
        Loc.Iniciar();
        Tema.Aplicar();
        if (Captura)
        {
            Window w = Opcion("--ventana") switch
            {
                "tema" => new TemaVentana(),
                "acerca" => AcercaDe.Crear(),
                "aviso" => AcercaDe.Aviso(new Version(Versiones.Actual.Major, Versiones.Actual.Minor + 1, 0)),
                _ => new MainWindow(Array.Empty<string>()),
            };
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            w.ContentRendered += (_, _) => Dispatcher.BeginInvoke(() => { GuardarCaptura(w, archivo!); Shutdown(); },
                                                                  System.Windows.Threading.DispatcherPriority.ContextIdle);
            w.Show();
            return;
        }
        var ventana = new MainWindow(e.Args);
        MainWindow = ventana;
        ventana.Show();
    }

    private static void GuardarCaptura(Window w, string archivo)
    {
        var contenido = (FrameworkElement)w.Content;
        double escala = System.Windows.Media.VisualTreeHelper.GetDpi(w).DpiScaleX;
        // The content with its margins; the Fluent window has a see-through backdrop, so the image gets the
        // theme's solid background behind it.
        double ancho = contenido.ActualWidth + contenido.Margin.Left + contenido.Margin.Right;
        double alto = contenido.ActualHeight + contenido.Margin.Top + contenido.Margin.Bottom;
        var bmp = new System.Windows.Media.Imaging.RenderTargetBitmap((int)(ancho * escala), (int)(alto * escala),
                                                                      96 * escala, 96 * escala, System.Windows.Media.PixelFormats.Pbgra32);
        var fondo = new System.Windows.Media.DrawingVisual();
        using (var dc = fondo.RenderOpen())
        {
            var brocha = w.TryFindResource("ApplicationBackgroundBrush") as System.Windows.Media.Brush
                         ?? new System.Windows.Media.SolidColorBrush(Tema.EsOscuro ? System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20)
                                                                                   : System.Windows.Media.Color.FromRgb(0xF3, 0xF3, 0xF3));
            dc.DrawRectangle(brocha, null, new Rect(0, 0, ancho, alto));
        }
        bmp.Render(fondo);
        bmp.Render(contenido);
        var png = new System.Windows.Media.Imaging.PngBitmapEncoder();
        png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
        using var f = System.IO.File.Create(archivo);
        png.Save(f);
    }
}

