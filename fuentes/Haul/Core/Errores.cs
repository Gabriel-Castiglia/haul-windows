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
        (string patron, string es, string en)[] conocidos =
        [
            ("unavailable", "El video no está disponible (borrado, privado o bloqueado).",
                            "The video is unavailable (removed, private or blocked)."),
            ("Private video", "Es un video privado.", "This is a private video."),
            ("Sign in to confirm your age", "Pide confirmar la edad con una cuenta: no se puede descargar así.",
                                            "Age confirmation with an account is required; it cannot be downloaded."),
            ("Unsupported URL", "Ese sitio no está soportado por yt-dlp.", "That site is not supported by yt-dlp."),
            ("is not a valid URL", "Eso no parece un enlace válido.", "That does not look like a valid link."),
            ("Unable to download webpage", "No se pudo abrir la página: revisá la conexión.",
                                           "The page could not be opened; check the connection."),
            ("Requested format is not available", "Ese formato no está disponible para este video.",
                                                  "That format is not available for this video."),
            ("Sign in to confirm you", "YouTube pide verificar que no sos un robot: instalá la versión más nueva de Haul.",
                                       "YouTube asks to confirm you are not a bot; install the latest version of Haul."),
            ("does not have a videos tab", "Ese canal no tiene videos para bajar.", "That channel has no videos to download."),
            ("The playlist does not exist", "La lista no existe o es privada.", "The playlist does not exist or is private."),
        ];
        foreach (var (patron, es, en) in conocidos)
            if (mensaje.Contains(patron, StringComparison.OrdinalIgnoreCase))
                return tr(es, en);
        return mensaje;
    }
}
