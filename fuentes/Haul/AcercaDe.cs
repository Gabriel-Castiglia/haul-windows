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
        Title = T("AboutHaul");
        Loc.Aplicar(this);
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
        nombre.Children.Add(Secundario(T("VersionLine", Herramientas.VersionHaul, Herramientas.VersionYtDlp)));
        cabecera.Children.Add(nombre);
        panel.Children.Add(cabecera);

        panel.Children.Add(Titulo(T("TheName")));
        panel.Children.Add(Parrafo(T("NameText")));

        panel.Children.Add(Secundario(T("IconText")));

        panel.Children.Add(Titulo(T("Author")));
        panel.Children.Add(Parrafo("Gabriel Castiglia · © 2026"));

        panel.Children.Add(Titulo(T("License")));
        panel.Children.Add(Parrafo(T("LicenseText")));

        panel.Children.Add(Titulo(T("SourceCode")));
        panel.Children.Add(Enlace(Repositorio));
        var mac = Secundario(T("MacVersion"));
        mac.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(mac);
        panel.Children.Add(Enlace(RepositorioMac));

        panel.Children.Add(Titulo(T("HaulUpdates")));
        var avisar = new CheckBox
        {
            Content = new TextBlock
            {
                Text = T("CheckDaily"),
                TextWrapping = TextWrapping.Wrap,
            },
            IsChecked = Ajustes.Datos.BuscarVersiones,
        };
        avisar.Click += (_, _) => { Ajustes.Datos.BuscarVersiones = avisar.IsChecked == true; Ajustes.Guardar(); };
        panel.Children.Add(avisar);
        var buscar = Boton(T("CheckForUpdates"));
        // The check runs in the main window, which downloads, closes and reopens Haul if the answer is yes.
        buscar.Click += (_, _) =>
        {
            var principal = Owner as MainWindow;
            Close();
            if (principal != null) _ = principal.BuscarActualizacionAsync(interactiva: true);
        };
        panel.Children.Add(buscar);

        panel.Children.Add(Titulo(T("EngineYtDlp")));
        var motor = Secundario(TextoMotor());
        panel.Children.Add(motor);
        var solo = new CheckBox
        {
            Content = new TextBlock
            {
                Text = T("UpdateYtDlpDaily"),
                TextWrapping = TextWrapping.Wrap,
            },
            IsChecked = Ajustes.Datos.ActualizarYtDlp,
            Margin = new Thickness(0, 6, 0, 0),
        };
        solo.Click += (_, _) => { Ajustes.Datos.ActualizarYtDlp = solo.IsChecked == true; Ajustes.Guardar(); };
        panel.Children.Add(solo);
        var actualizar = Boton(T("UpdateYtDlp"));
        var estado = Secundario("");
        estado.Margin = new Thickness(0, 6, 0, 0);
        actualizar.Click += async (_, _) =>
        {
            if (Owner is not MainWindow principal) return;
            actualizar.IsEnabled = false;
            estado.Text = T("AskingGitHub");
            estado.Text = await principal.ActualizarYtDlpAsync(interactiva: true) ?? "";
            motor.Text = TextoMotor();
            actualizar.IsEnabled = true;
        };
        panel.Children.Add(actualizar);
        panel.Children.Add(estado);

        var cerrar = new Button { Content = T("Close"), Padding = new Thickness(14, 6, 14, 6),
                                  IsDefault = true, IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right,
                                  Margin = new Thickness(0, 18, 0, 0) };
        if (Application.Current.TryFindResource("AccentButtonStyle") is Style s) cerrar.Style = s;
        cerrar.Click += (_, _) => Close();
        panel.Children.Add(cerrar);
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

    private static Button Boton(string texto) =>
        new() { Content = texto, Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };

    private static string TextoMotor() => Herramientas.YtDlpActualizado
        ? T("EngineDownloaded", Herramientas.VersionYtDlp, Herramientas.VersionYtDlpIncluido)
        : T("EngineBundled", Herramientas.VersionYtDlp);

    // The answers to the question about a new version, in the order of its buttons.
    public const int Luego = 0, Novedades = 1, Ahora = 2;

    // Says that a newer version is out. With its setup program in the release it offers to install it now; without
    // it, only the page.
    public static int PreguntarVersion(Window duenio, Version nueva, bool instalable) =>
        Pregunta.Hacer(duenio, TituloVersion(), TextoVersion(nueva, instalable), BotonesVersion(instalable), instalable ? Ahora : Novedades);

    // For the test screenshot.
    public static Window Aviso(Version nueva) => Pregunta.Crear(TituloVersion(), TextoVersion(nueva, true), BotonesVersion(true), Ahora);

    private static string TituloVersion() => T("NewVersionTitle");

    private static string TextoVersion(Version nueva, bool instalable) =>
        T(instalable ? "NewVersionInstallable" : "NewVersionPage", nueva, Versiones.Actual);

    private static string[] BotonesVersion(bool instalable) => instalable
        ? [T("Later"), T("WhatsNew"), T("UpdateNow")]
        : [T("Later"), T("WhatsNew")];
}
