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
        Title = T("Appearance");
        Loc.Aplicar(this);
        Width = 620;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        SetResourceReference(IconProperty, "IconoVentana");

        var panel = new StackPanel { Margin = new Thickness(26, 22, 26, 22) };
        panel.Children.Add(new TextBlock { Text = T("Appearance"), FontSize = 20, FontWeight = FontWeights.SemiBold,
                                           Margin = new Thickness(0, 0, 0, 14) });

        var modos = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
        foreach (var (clave, texto) in new[]
        {
            (Tema.Auto, T("AsWindows")), (Tema.Claro, T("Light")), (Tema.Oscuro, T("Dark")),
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

        panel.Children.Add(Secundario(T("Colour"), new Thickness(0, 0, 0, 6)));
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
            void Elegir()
            {
                Ajustes.Datos.Color = clave;
                Ajustes.Guardar();
                Tema.Aplicar();
                Marcar();
            }
            azulejo.MouseLeftButtonUp += (_, _) => Elegir();
            // Reachable with the keyboard (Tab, then Enter or Space) and named for screen readers.
            azulejo.Focusable = true;
            KeyboardNavigation.SetIsTabStop(azulejo, true);
            System.Windows.Automation.AutomationProperties.SetName(azulejo, c.Nombre);
            azulejo.KeyDown += (_, e) =>
            {
                if (e.Key is Key.Enter or Key.Space) { Elegir(); e.Handled = true; }
            };
            azulejos.Add((clave, azulejo));
            rejilla.Children.Add(azulejo);
        }
        panel.Children.Add(rejilla);
        panel.Children.Add(Secundario(T("ColourUse"), new Thickness(0, 0, 0, 0)));

        var cerrar = new Button { Content = T("Done"), Padding = new Thickness(18, 6, 18, 6), IsDefault = true, IsCancel = true,
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
