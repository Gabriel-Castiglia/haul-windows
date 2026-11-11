//
//  AcercaDe.cs
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
using System.Windows.Documents;
using System.Windows.Media;
using Haul.Core;
using static Haul.Core.Loc;

namespace Haul;

// The About window: version, the meaning of the name, author, license and repository.
public sealed class AcercaDe : Window
{
    public const string Repositorio = "https://github.com/Gabriel-Castiglia/haul-windows";
    public const string RepositorioMac = "https://github.com/Gabriel-Castiglia/haul";

    private AcercaDe()
    {
        Title = tr("Acerca de Haul", "About Haul");
        Width = 460;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        SetResourceReference(IconProperty, "IconoVentana");

        var panel = new StackPanel { Margin = new Thickness(28, 24, 28, 24) };

        var cabecera = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
        var icono = new Image { Width = 72, Height = 72 };
        icono.SetResourceReference(Image.SourceProperty, "IconoHaul");
        RenderOptions.SetBitmapScalingMode(icono, BitmapScalingMode.HighQuality);
        cabecera.Children.Add(icono);
        var nombre = new StackPanel { Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        nombre.Children.Add(new TextBlock { Text = "Haul", FontSize = 26, FontWeight = FontWeights.Bold });
        nombre.Children.Add(Secundario(tr($"Versión {Herramientas.VersionHaul} para Windows · yt-dlp {Herramientas.VersionYtDlp}",
                                          $"Version {Herramientas.VersionHaul} for Windows · yt-dlp {Herramientas.VersionYtDlp}")));
        cabecera.Children.Add(nombre);
        panel.Children.Add(cabecera);

        panel.Children.Add(Titulo(tr("El nombre", "The name")));
        panel.Children.Add(Parrafo(tr(
            "«Haul» es una palabra inglesa: quiere decir acarrear algo pesado y también la carga que uno se trae de "
            + "vuelta, como la pesca de un día. Eso hace la app, con yt-dlp de motor: se trae "
            + "la carga a casa.",
            "To haul is to carry something heavy, and a haul is the load you bring back, like a day's catch. That is "
            + "what the app does: with yt-dlp as its engine, it brings the load home.")));

        panel.Children.Add(Secundario(tr(
            "Ícono: el águila que trae una liebre del grabado «Águila que le trae una liebre a una joven», del Monogramista B "
            + "(Italia, siglo XVII), Rijksmuseum, dominio público.",
            "Icon: the eagle carrying a hare from the engraving \"Eagle bringing a young woman a hare\" by the Monogrammist B "
            + "(Italy, 17th century), Rijksmuseum, public domain.")));

        panel.Children.Add(Titulo(tr("Autor", "Author")));
        panel.Children.Add(Parrafo("Gabriel Castiglia · © 2026"));

        panel.Children.Add(Titulo(tr("Licencia", "License")));
        panel.Children.Add(Parrafo(tr(
            "Software libre bajo la Licencia Pública General de GNU, versión 2 (GPL-2.0-only), la misma del núcleo "
            + "Linux. La copia incluida de yt-dlp conserva sus propias licencias (Unlicense y las de sus componentes).",
            "Free software under the GNU General Public License, version 2 (GPL-2.0-only), the same license as the "
            + "Linux kernel. The bundled copy of yt-dlp keeps its own licenses (the Unlicense and those of its components).")));

        panel.Children.Add(Titulo(tr("Código fuente", "Source code")));
        panel.Children.Add(Enlace(Repositorio));
        var mac = Secundario(tr("Versión para macOS:", "macOS version:"));
        mac.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(mac);
        panel.Children.Add(Enlace(RepositorioMac));

        panel.Children.Add(Titulo(tr("Versiones nuevas", "New versions")));
        var avisar = new CheckBox
        {
            Content = new TextBlock
            {
                Text = tr("Avisar cuando haya una versión nueva (se consulta GitHub una vez al día)",
                          "Tell me when there is a new version (GitHub is asked once a day)"),
                TextWrapping = TextWrapping.Wrap,
            },
            IsChecked = Ajustes.Datos.BuscarVersiones,
        };
        avisar.Click += (_, _) => { Ajustes.Datos.BuscarVersiones = avisar.IsChecked == true; Ajustes.Guardar(); };
        panel.Children.Add(avisar);
        var buscar = new Button { Content = tr("Buscar versión nueva", "Check for a new version"), Padding = new Thickness(12, 5, 12, 5) };
        var estado = Secundario("");
        estado.VerticalAlignment = VerticalAlignment.Center;
        estado.Margin = new Thickness(0, 6, 0, 0);
        buscar.Click += async (_, _) =>
        {
            buscar.IsEnabled = false;
            estado.Text = tr("Consultando GitHub…", "Asking GitHub…");
            try
            {
                var ultima = await Versiones.UltimaAsync();
                if (ultima != null && ultima > Versiones.Actual)
                {
                    estado.Text = tr($"Está la versión {ultima}.", $"Version {ultima} is out.");
                    AvisarVersion(this, ultima);
                }
                else estado.Text = tr("Tenés la última versión.", "You have the latest version.");
            }
            catch
            {
                estado.Text = tr("No se pudo consultar GitHub.", "GitHub could not be reached.");
            }
            buscar.IsEnabled = true;
        };
        panel.Children.Add(estado);

        // The check on request sits at the left of the bottom row, Close at the right.
        var botones = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 18, 0, 0) };
        DockPanel.SetDock(buscar, Dock.Left);
        botones.Children.Add(buscar);
        var cerrar = new Button { Content = tr("Cerrar", "Close"), Padding = new Thickness(14, 6, 14, 6),
                                  IsDefault = true, IsCancel = true };
        if (Application.Current.TryFindResource("AccentButtonStyle") is Style s) cerrar.Style = s;
        cerrar.Click += (_, _) => Close();
        DockPanel.SetDock(cerrar, Dock.Right);
        botones.Children.Add(cerrar);
        panel.Children.Add(botones);

        Content = panel;
    }

    private static TextBlock Titulo(string t) =>
        new() { Text = t, FontWeight = FontWeights.SemiBold, FontSize = 14, Margin = new Thickness(0, 12, 0, 4) };

    private static TextBlock Parrafo(string t) => new() { Text = t, TextWrapping = TextWrapping.Wrap, LineHeight = 20 };

    private static TextBlock Secundario(string t)
    {
        var b = new TextBlock { Text = t, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        b.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        return b;
    }

    private static TextBlock Enlace(string url)
    {
        var h = new Hyperlink(new Run(url.Replace("https://", ""))) { NavigateUri = new Uri(url) };
        h.SetResourceReference(TextElement.ForegroundProperty, "AcentoBrush");
        h.RequestNavigate += (_, e) => Herramientas.Abrir(e.Uri.AbsoluteUri);
        return new TextBlock(h);
    }

    public static void Mostrar(Window duenio) => new AcercaDe { Owner = duenio }.ShowDialog();

    // For the test screenshot.
    public static Window Crear() => new AcercaDe();

    // Says that a newer version is out and offers its download page.
    public static void AvisarVersion(Window duenio, Version nueva)
    {
        var w = Aviso(nueva);
        w.Owner = duenio;
        w.ShowDialog();
    }

    public static Window Aviso(Version nueva)
    {
        var w = new Window
        {
            Title = "Haul", Width = 440, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false,
        };
        w.SetResourceReference(IconProperty, "IconoVentana");
        var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };
        var cabecera = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        var icono = new Image { Width = 48, Height = 48, Margin = new Thickness(0, 0, 14, 0) };
        icono.SetResourceReference(Image.SourceProperty, "IconoHaul");
        RenderOptions.SetBitmapScalingMode(icono, BitmapScalingMode.HighQuality);
        cabecera.Children.Add(icono);
        cabecera.Children.Add(new TextBlock
        {
            Text = tr("Hay una versión nueva de Haul", "A new version of Haul is out"), FontSize = 17, FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap,
        });
        panel.Children.Add(cabecera);
        panel.Children.Add(Parrafo(tr($"Haul {nueva} está en GitHub; tenés la {Versiones.Actual}. La página de descarga tiene el instalador y lo que cambió.",
                                      $"Haul {nueva} is on GitHub; you have {Versiones.Actual}. The download page has the setup and what changed.")));
        var botones = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                                       Margin = new Thickness(0, 18, 0, 0) };
        var luego = new Button { Content = tr("Ahora no", "Not now"), Padding = new Thickness(14, 6, 14, 6), IsCancel = true,
                                 Margin = new Thickness(0, 0, 8, 0) };
        luego.Click += (_, _) => w.Close();
        var ir = new Button { Content = tr("Ir a la descarga", "Go to the download"), Padding = new Thickness(14, 6, 14, 6), IsDefault = true };
        if (Application.Current.TryFindResource("AccentButtonStyle") is Style s) ir.Style = s;
        ir.Click += (_, _) => { Herramientas.Abrir(Versiones.Pagina); w.Close(); };
        botones.Children.Add(luego);
        botones.Children.Add(ir);
        panel.Children.Add(botones);
        w.Content = panel;
        return w;
    }
}
