//
//  Tema.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: October 20, 2026 (light, dark or as Windows and twelve colours: November 11, 2026)
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Haul.Core;

// An accent colour: the tile of the icon (icono/accents/haul_<key>.ico, drawn by scripts/make_icon.py with the
// same keys, the deeper tone as the tile) and the highlight colour of the window. The deeper tone is used on a
// light window and under white text; the brighter one for highlights on a dark window.
public sealed record ColorTema(string Clave, string Es, string En, Color Hondo, Color Vivo)
{
    public string Nombre => Loc.tr(Es, En);
}

// Light, dark or as Windows (the Fluent theme of WPF), and the accent colour, which also colours the icon of the
// window and of the taskbar button. The brushes go into the application resources; every DynamicResource follows.
public static class Tema
{
    public const string Auto = "auto", Claro = "light", Oscuro = "dark";

    public static readonly ColorTema[] Colores =
    {
        new("ember", "Naranja", "Orange", Rgb(0xD9652F), Rgb(0xF08A5D)),       // the default
        new("gold", "Dorado", "Gold", Rgb(0x9A6F00), Rgb(0xE2B84C)),
        new("purple", "Morado", "Purple", Rgb(0x5B2A86), Rgb(0xB392E0)),
        new("sky", "Celeste", "Sky blue", Rgb(0x1A8CD8), Rgb(0x5CB8F0)),
        new("red", "Rojo", "Red", Rgb(0xC8102E), Rgb(0xF2545B)),
        new("yellow", "Amarillo", "Yellow", Rgb(0xB88A00), Rgb(0xF5C842)),
        new("green", "Verde", "Green", Rgb(0x1E8A43), Rgb(0x4CC27A)),
        new("teal", "Verde azulado", "Teal", Rgb(0x00796B), Rgb(0x4DB6AC)),
        new("blue", "Azul", "Blue", Rgb(0x1F5FD1), Rgb(0x6E9BFF)),
        new("pink", "Rosa", "Pink", Rgb(0xC2185B), Rgb(0xF06292)),
        new("black", "Negro", "Black", Rgb(0x1D1D1F), Rgb(0xC7C7CC)),
        new("white", "Blanco", "White", Rgb(0x8E8E93), Rgb(0xF5F5F7)),
    };

    public static ColorTema Obtener(string? clave) => Colores.FirstOrDefault(c => c.Clave == clave) ?? Colores[0];

    public static bool EsOscuro { get; private set; } = true;

    // Raised after the appearance or the colour changes.
    public static event Action? Cambio;

    // Applies the appearance and the colour stored in the settings.
    public static void Aplicar()
    {
        var datos = Ajustes.Datos;
        var app = Application.Current;
        app.ThemeMode = datos.Apariencia switch { Claro => ThemeMode.Light, Oscuro => ThemeMode.Dark, _ => ThemeMode.System };
        EsOscuro = datos.Apariencia == Oscuro || (datos.Apariencia != Claro && SistemaOscuro());

        var color = Obtener(datos.Color);
        var vivo = EsOscuro ? color.Vivo : color.Hondo;
        var hondo = color.Hondo;
        var r = app.Resources;
        r["AcentoColor"] = vivo;
        r["AcentoBrush"] = Congelado(new SolidColorBrush(vivo));
        r["AcentoSuaveBrush"] = Congelado(new SolidColorBrush(Color.FromArgb(0x40, vivo.R, vivo.G, vivo.B)));
        r["DegradeBrush"] = Congelado(new LinearGradientBrush(hondo, Mezclar(hondo, Colors.Black, 0.35), new Point(0, 0), new Point(1, 1)));
        r["TextoSobreAcentoBrush"] = Congelado(new SolidColorBrush(SobreColor(hondo)));

        // The Fluent controls (check boxes, progress bars, the accent button) take the chosen colour instead of the
        // accent of Windows.
        r["SystemAccentColor"] = hondo;
        r["SystemAccentColorLight1"] = Mezclar(hondo, Colors.White, 0.2);
        r["SystemAccentColorLight2"] = Mezclar(hondo, Colors.White, 0.4);
        r["SystemAccentColorLight3"] = Mezclar(hondo, Colors.White, 0.6);
        r["SystemAccentColorDark1"] = Mezclar(hondo, Colors.Black, 0.2);
        r["SystemAccentColorDark2"] = Mezclar(hondo, Colors.Black, 0.4);
        r["SystemAccentColorDark3"] = Mezclar(hondo, Colors.Black, 0.6);
        var relleno = EsOscuro ? Mezclar(hondo, Colors.White, 0.4) : hondo;
        r["AccentFillColorDefaultBrush"] = Congelado(new SolidColorBrush(relleno));
        r["AccentFillColorSecondaryBrush"] = Congelado(new SolidColorBrush(Color.FromArgb(0xE6, relleno.R, relleno.G, relleno.B)));
        r["AccentFillColorTertiaryBrush"] = Congelado(new SolidColorBrush(Color.FromArgb(0xCC, relleno.R, relleno.G, relleno.B)));
        r["AccentTextFillColorPrimaryBrush"] = Congelado(new SolidColorBrush(vivo));
        r["AccentTextFillColorSecondaryBrush"] = Congelado(new SolidColorBrush(vivo));
        r["AccentTextFillColorTertiaryBrush"] = Congelado(new SolidColorBrush(vivo));
        r["TextOnAccentFillColorPrimaryBrush"] = Congelado(new SolidColorBrush(SobreColor(relleno)));
        // The Fluent styles read the accent through the keys of SystemColors; the application resources win over them.
        foreach (var (clave, brocha, c) in new (object, object, Color)[]
        {
            (SystemColors.AccentColorKey, SystemColors.AccentColorBrushKey, hondo),
            (SystemColors.AccentColorLight1Key, SystemColors.AccentColorLight1BrushKey, Mezclar(hondo, Colors.White, 0.2)),
            (SystemColors.AccentColorLight2Key, SystemColors.AccentColorLight2BrushKey, Mezclar(hondo, Colors.White, 0.4)),
            (SystemColors.AccentColorLight3Key, SystemColors.AccentColorLight3BrushKey, Mezclar(hondo, Colors.White, 0.6)),
            (SystemColors.AccentColorDark1Key, SystemColors.AccentColorDark1BrushKey, Mezclar(hondo, Colors.Black, 0.2)),
            (SystemColors.AccentColorDark2Key, SystemColors.AccentColorDark2BrushKey, Mezclar(hondo, Colors.Black, 0.4)),
            (SystemColors.AccentColorDark3Key, SystemColors.AccentColorDark3BrushKey, Mezclar(hondo, Colors.Black, 0.6)),
        })
        {
            r[clave] = c;
            r[brocha] = Congelado(new SolidColorBrush(c));
        }

        r["IconoHaul"] = Icono(color.Clave, 256);
        r["IconoVentana"] = IconoVentana(color.Clave);
        Cambio?.Invoke();
    }

    // Windows "app mode": true when apps are set to dark (Settings > Personalization > Colors).
    public static bool SistemaOscuro()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return k?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch
        {
            return true;
        }
    }

    private static Uri Ruta(string clave) => new($"pack://application:,,,/Recursos/accents/haul_{Obtener(clave).Clave}.ico");

    // One frame of the icon in a colour, the nearest to the size asked for.
    public static ImageSource? Icono(string clave, int tamanio)
    {
        try
        {
            var dec = BitmapDecoder.Create(Ruta(clave), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            return dec.Frames.OrderBy(f => Math.Abs(f.PixelWidth - tamanio)).First();
        }
        catch
        {
            return null;
        }
    }

    // The whole icon (all sizes), for the window and its taskbar button.
    private static ImageSource? IconoVentana(string clave)
    {
        try
        {
            return BitmapFrame.Create(Ruta(clave), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }
        catch
        {
            return null;
        }
    }

    // Text on a fill of colour c: nearly black on pale tones, white otherwise.
    private static Color SobreColor(Color c) =>
        (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255 > 0.6 ? Rgb(0x111111) : Colors.White;

    private static Color Rgb(uint v) => Color.FromRgb((byte)(v >> 16), (byte)(v >> 8), (byte)v);

    private static Color Mezclar(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

    private static T Congelado<T>(T f) where T : Freezable
    {
        f.Freeze();
        return f;
    }
}
