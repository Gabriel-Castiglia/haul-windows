//
//  Pregunta.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: November 20, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Haul;

// A small question window with Haul's icon, a title, a paragraph and a row of buttons; the answer is the index of
// the button pressed, or the "cancel" index if the window is closed.
public sealed class Pregunta : Window
{
    public int Respuesta { get; private set; }

    private Pregunta(string titulo, string texto, string[] botones, int predeterminado, int cancelar)
    {
        Respuesta = cancelar;
        Title = "Haul";
        Width = 460;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        SetResourceReference(IconProperty, "IconoVentana");

        var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };
        var cabecera = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        var icono = new Image { Width = 48, Height = 48, Margin = new Thickness(0, 0, 14, 0) };
        icono.SetResourceReference(Image.SourceProperty, "IconoHaul");
        RenderOptions.SetBitmapScalingMode(icono, BitmapScalingMode.HighQuality);
        cabecera.Children.Add(icono);
        cabecera.Children.Add(new TextBlock
        {
            Text = titulo, FontSize = 17, FontWeight = FontWeights.SemiBold, MaxWidth = 330,
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap,
        });
        panel.Children.Add(cabecera);
        panel.Children.Add(new TextBlock { Text = texto, TextWrapping = TextWrapping.Wrap, LineHeight = 20 });

        var fila = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        for (int i = 0; i < botones.Length; i++)
        {
            int indice = i;
            var b = new Button
            {
                Content = botones[i], Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(8, 0, 0, 0),
                IsDefault = i == predeterminado, IsCancel = i == cancelar,
            };
            if (i == predeterminado && Application.Current.TryFindResource("AccentButtonStyle") is Style s) b.Style = s;
            b.Click += (_, _) => { Respuesta = indice; Close(); };
            fila.Children.Add(b);
        }
        panel.Children.Add(fila);
        Content = panel;
    }

    // Shows the question and returns the index of the button pressed ("cancelar" if the window was closed).
    public static int Hacer(Window? duenio, string titulo, string texto, string[] botones, int predeterminado, int cancelar = 0)
    {
        var w = new Pregunta(titulo, texto, botones, predeterminado, cancelar);
        if (duenio is { IsVisible: true }) w.Owner = duenio;
        else w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        w.ShowDialog();
        return w.Respuesta;
    }

    // For the test screenshot.
    public static Window Crear(string titulo, string texto, string[] botones, int predeterminado, int cancelar = 0) =>
        new Pregunta(titulo, texto, botones, predeterminado, cancelar);
}
