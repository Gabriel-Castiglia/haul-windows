//
//  TemaVentana.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: November 11, 2026
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
using Haul.Core;
using static Haul.Core.Loc;

namespace Haul;

// The appearance window: light, dark or as Windows, and the twelve colours. Every change applies at once.
public sealed class TemaVentana : Window
{
    private readonly List<(string Clave, Border Azulejo)> azulejos = new();

    public TemaVentana()
    {
        Title = tr("Apariencia", "Appearance");
        Width = 620;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        SetResourceReference(IconProperty, "IconoVentana");

        var panel = new StackPanel { Margin = new Thickness(26, 22, 26, 22) };
        panel.Children.Add(new TextBlock { Text = tr("Apariencia", "Appearance"), FontSize = 20, FontWeight = FontWeights.SemiBold,
                                           Margin = new Thickness(0, 0, 0, 14) });

        var modos = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
        foreach (var (clave, texto) in new[]
        {
            (Tema.Auto, tr("Como Windows", "As Windows")), (Tema.Claro, tr("Claro", "Light")), (Tema.Oscuro, tr("Oscuro", "Dark")),
        })
        {
            var b = new RadioButton { Content = texto, GroupName = "apariencia", IsChecked = Ajustes.Datos.Apariencia == clave,
                                      Margin = new Thickness(0, 0, 22, 0) };
            b.Checked += (_, _) =>
            {
                if (!b.IsLoaded) return;
                Ajustes.Datos.Apariencia = clave;
                Ajustes.Guardar();
                Tema.Aplicar();
            };
            modos.Children.Add(b);
        }
        panel.Children.Add(modos);

        panel.Children.Add(Secundario(tr("Color", "Colour"), new Thickness(0, 0, 0, 6)));
        var rejilla = new UniformGrid { Columns = 6, Margin = new Thickness(-4, 0, -4, 8) };
        foreach (var c in Tema.Colores)
        {
            var imagen = new Image { Width = 44, Height = 44, Source = Tema.Icono(c.Clave, 64), Margin = new Thickness(0, 2, 0, 4) };
            RenderOptions.SetBitmapScalingMode(imagen, BitmapScalingMode.HighQuality);
            var nombre = new TextBlock { Text = c.Nombre, FontSize = 11.5, TextAlignment = TextAlignment.Center,
                                         TextWrapping = TextWrapping.Wrap, Height = 32 };
            var azulejo = new Border
            {
                Padding = new Thickness(2, 6, 2, 4), Margin = new Thickness(4), CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(2), Cursor = Cursors.Hand, ToolTip = c.Nombre, Background = Brushes.Transparent,
                Child = new StackPanel { Children = { imagen, nombre } },
            };
            var clave = c.Clave;
            azulejo.MouseLeftButtonUp += (_, _) =>
            {
                Ajustes.Datos.Color = clave;
                Ajustes.Guardar();
                Tema.Aplicar();
                Marcar();
            };
            azulejos.Add((clave, azulejo));
            rejilla.Children.Add(azulejo);
        }
        panel.Children.Add(rejilla);
        panel.Children.Add(Secundario(tr("El color se usa en el ícono de la ventana y de la barra de tareas, en los botones y en lo seleccionado.",
                                         "The colour is used for the icon of the window and of the taskbar button, the buttons and selected items."),
                                      new Thickness(0, 0, 0, 0)));

        var cerrar = new Button { Content = tr("Listo", "Done"), Padding = new Thickness(18, 6, 18, 6), IsDefault = true, IsCancel = true,
                                  HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        if (Application.Current.TryFindResource("AccentButtonStyle") is Style s) cerrar.Style = s;
        cerrar.Click += (_, _) => Close();
        panel.Children.Add(cerrar);

        Content = panel;
        Marcar();
    }

    private void Marcar()
    {
        foreach (var (clave, azulejo) in azulejos)
        {
            if (clave == Ajustes.Datos.Color)
            {
                azulejo.SetResourceReference(Border.BorderBrushProperty, "AcentoBrush");
                azulejo.SetResourceReference(Border.BackgroundProperty, "AcentoSuaveBrush");
            }
            else
            {
                azulejo.BorderBrush = Brushes.Transparent;
                azulejo.Background = Brushes.Transparent;
            }
        }
    }

    private static TextBlock Secundario(string t, Thickness margen)
    {
        var b = new TextBlock { Text = t, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = margen };
        b.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        return b;
    }

    public static void Mostrar(Window duenio) => new TemaVentana { Owner = duenio }.ShowDialog();
}
