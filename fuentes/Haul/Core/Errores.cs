//
//  Errores.cs
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

using System.Globalization;
using static Haul.Core.Loc;

namespace Haul.Core;

public static class Errores
{
    // Normalizes a yt-dlp progress field; unknown values are returned empty.
    public static string Limpio(string s)
    {
        var t = s.Trim();
        return t.Contains("Unknown") || t == "NA" ? "" : t;
    }

    public static string Duracion(string s)
    {
        if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return "";
        int seg = (int)d, h = seg / 3600, m = seg % 3600 / 60, r = seg % 60;
        return h > 0 ? $"{h}:{m:00}:{r:00}" : $"{m}:{r:00}";
    }

    // Strips the prefixes from a yt-dlp error line.
    // Example: "ERROR: [youtube] abc123: Video unavailable" -> "Video unavailable".
    public static string Mensaje(string linea)
    {
        var t = linea["ERROR:".Length..].Trim();
        if (t.StartsWith('[') && t.IndexOf(']') is var cierre and > 0)
        {
            t = t[(cierre + 1)..].Trim();
            int dos = t.IndexOf(": ", StringComparison.Ordinal);
            if (dos > 0 && !t[..dos].Contains(' ')) t = t[(dos + 2)..];
        }
        return t;
    }

    // Friendly wording for the most common yt-dlp errors, in the current interface language.
    // Errors are stored as yt-dlp reports them and translated at display time, so they follow
    // language changes; anything not listed is shown unchanged.
    public static string Traducir(string mensaje)
    {
        (string patron, string clave)[] conocidos =
        [
            ("unavailable", "ErrUnavailable"),
            ("Private video", "ErrPrivate"),
            ("Sign in to confirm your age", "ErrAge"),
            ("Unsupported URL", "ErrUnsupported"),
            ("is not a valid URL", "ErrInvalidUrl"),
            ("Unable to download webpage", "ErrWebpage"),
            ("Requested format is not available", "ErrFormat"),
            ("Sign in to confirm you", "ErrBot"),
            ("does not have a videos tab", "ErrNoVideosTab"),
            ("The playlist does not exist", "ErrNoPlaylist"),
        ];
        foreach (var (patron, clave) in conocidos)
            if (mensaje.Contains(patron, StringComparison.OrdinalIgnoreCase))
                return T(clave);
        return mensaje;
    }
}
