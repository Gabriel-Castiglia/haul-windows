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
using System.Windows.Media.Imaging;
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

        var panel = new StackPanel { Margin = new Thickness(28, 24, 28, 24) };

        var cabecera = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
        cabecera.Children.Add(new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/Recursos/icono-256.png")),
            Width = 72, Height = 72,
        });
        var nombre = new StackPanel { Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        nombre.Children.Add(new TextBlock { Text = "Haul", FontSize = 26, FontWeight = FontWeights.Bold });
        nombre.Children.Add(Secundario(tr($"Versión {Herramientas.VersionHaul} para Windows · yt-dlp {Herramientas.VersionYtDlp}",
                                          $"Version {Herramientas.VersionHaul} for Windows · yt-dlp {Herramientas.VersionYtDlp}")));
        cabecera.Children.Add(nombre);
        panel.Children.Add(cabecera);

        panel.Children.Add(Titulo(tr("El nombre", "The name")));
        panel.Children.Add(Parrafo(tr(
            "«Haul» es una palabra inglesa: quiere decir acarrear algo pesado y también la carga que uno se trae de "
            + "vuelta, como la pesca de un día. Eso hace la app: se trae a casa videos y música de YouTube y de casi "
            + "cualquier sitio.",
            "To haul is to carry something heavy, and a haul is the load you bring back, like a day's catch. That is "
            + "what the app does: it brings video and music home from YouTube and almost any other site.")));

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

        var botones = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                                       Margin = new Thickness(0, 22, 0, 0) };
        var cerrar = new Button { Content = tr("Cerrar", "Close"), Padding = new Thickness(14, 6, 14, 6),
                                  IsDefault = true, IsCancel = true };
        if (Application.Current.TryFindResource("AccentButtonStyle") is Style s) cerrar.Style = s;
        cerrar.Click += (_, _) => Close();
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
}
