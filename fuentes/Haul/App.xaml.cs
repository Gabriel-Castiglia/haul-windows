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
    // The pictures use the default settings, an example queue and no clipboard, so they show nothing of the
    // computer they were drawn on.
    public static bool Captura { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        string? Opcion(string nombre)
        {
            int i = Array.IndexOf(e.Args, nombre);
            return i >= 0 && i + 1 < e.Args.Length ? e.Args[i + 1] : null;
        }

        // Test option (not shown to users): "--self-test <log>" checks the saved queue, the version logic and the
        // checksums with throw-away data in a temporary folder, writes the results to <log> and exits with the
        // number of failures. No window is shown and the user's settings and queue are never read or written.
        // "--red" adds the checks that need GitHub.
        if (Opcion("--self-test") is string registro)
        {
            Ajustes.SoloLectura = true;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(PruebaPropia.Ejecutar(registro, e.Args.Contains("--red")));
            return;
        }

        // Test option (not shown to users): "--datos <folder>" keeps the settings, the queue and the copies of yt-dlp
        // in that folder instead of the user's, to try Haul with throw-away data.
        if (Opcion("--datos") is string datos)
        {
            Ajustes.Carpeta = System.IO.Path.Combine(System.IO.Path.GetFullPath(datos), "config");
            MotorYtDlp.Raiz = System.IO.Path.Combine(System.IO.Path.GetFullPath(datos), "yt-dlp");
        }

        var archivo = Opcion("--captura");
        Captura = archivo != null;
        Ajustes.Cargar(desdeArchivo: !Captura);
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
            // "--tamano 640x576" sets the window's size (in device-independent pixels), to see how it fits small screens.
            if (Opcion("--tamano")?.Split('x') is [var ancho, var alto]
                && double.TryParse(ancho, System.Globalization.CultureInfo.InvariantCulture, out var an)
                && double.TryParse(alto, System.Globalization.CultureInfo.InvariantCulture, out var al))
            {
                w.Width = an;
                w.Height = al;
            }
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            w.ContentRendered += (_, _) => Dispatcher.BeginInvoke(() => { GuardarCaptura(w, archivo!); Shutdown(); },
                                                                  System.Windows.Threading.DispatcherPriority.ContextIdle);
            w.Show();
            return;
        }
        MotorYtDlp.Iniciar();
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

