//
//  Acento.cs
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
using System.Windows.Media;
using Microsoft.Win32;

namespace Haul.Core;

// The palette derives from the Windows accent color (Settings > Personalization > Colors), so the
// interface follows any accent change without restarting, as the macOS version does.
public static class Acento
{
    public static Color Leer()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (k?.GetValue("AccentColor") is int abgr)
            {
                // Stored as 0xAABBGGRR.
                uint v = unchecked((uint)abgr);
                return Color.FromRgb((byte)(v & 0xFF), (byte)((v >> 8) & 0xFF), (byte)((v >> 16) & 0xFF));
            }
        }
        catch
        {
            // Falls back to the default below.
        }
        return Color.FromRgb(0x1E, 0x8E, 0x45);
    }

    // Writes the accent brushes into the application resources; every DynamicResource follows.
    public static void Aplicar()
    {
        var c = Leer();
        var oscuro = Mezclar(c, Colors.Black, 0.35);
        var r = Application.Current.Resources;
        r["AcentoColor"] = c;
        r["AcentoBrush"] = Congelado(new SolidColorBrush(c));
        r["AcentoSuaveBrush"] = Congelado(new SolidColorBrush(Color.FromArgb(0x40, c.R, c.G, c.B)));
        r["DegradeBrush"] = Congelado(new LinearGradientBrush(c, oscuro, new Point(0, 0), new Point(1, 1)));
    }

    private static Color Mezclar(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

    private static T Congelado<T>(T f) where T : Freezable
    {
        f.Freeze();
        return f;
    }
}
