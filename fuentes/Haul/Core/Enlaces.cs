//
//  Enlaces.cs
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

using System.Text.RegularExpressions;

namespace Haul.Core;

// What a link points to, as far as playlists are concerned.
public enum Clase
{
    Video,          // a single video, or any link from a site other than YouTube
    Lista,          // a YouTube playlist or channel page: every video becomes a queue row
    VideoEnLista,   // a YouTube video opened from a playlist: the user chooses
}

public static partial class Enlaces
{
    [GeneratedRegex(@"https?://[^\s<>""'`{}|\\^]+", RegexOptions.IgnoreCase)]
    private static partial Regex Patron();

    // Extracts http(s) links from free text, without duplicates and in order of appearance.
    public static List<string> En(string texto)
    {
        var vistos = new HashSet<string>(StringComparer.Ordinal);
        var resultado = new List<string>();
        foreach (Match m in Patron().Matches(texto))
        {
            var s = Recortar(m.Value);
            if (Uri.TryCreate(s, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https")
                && !string.IsNullOrEmpty(u.Host) && vistos.Add(s))
                resultado.Add(s);
        }
        return resultado;
    }

    // Drops punctuation that ends a sentence rather than the link, and a closing bracket
    // that has no opening one inside the link, as in "(see https://example.com/a)".
    private static string Recortar(string s)
    {
        while (s.Length > 0)
        {
            char c = s[^1];
            if (".,;:!?'\"".Contains(c)) { s = s[..^1]; continue; }
            if (c == ')' && s.Count(x => x == '(') < s.Count(x => x == ')')) { s = s[..^1]; continue; }
            if (c == ']' && s.Count(x => x == '[') < s.Count(x => x == ']')) { s = s[..^1]; continue; }
            break;
        }
        return s;
    }

    // Classifies a link. Only YouTube is inspected: links to other sites download as they are,
    // one video each, as in version 1.0.
    public static Clase Clasificar(string enlace)
    {
        if (!Uri.TryCreate(enlace, UriKind.Absolute, out var u)) return Clase.Video;
        var host = u.Host.ToLowerInvariant();
        bool youtube = host == "youtu.be" || host == "youtube.com" || host.EndsWith(".youtube.com");
        if (!youtube) return Clase.Video;

        var consulta = System.Web.HttpUtility.ParseQueryString(u.Query);
        bool tieneLista = !string.IsNullOrEmpty(consulta["list"]);
        bool tieneVideo = !string.IsNullOrEmpty(consulta["v"]) || host == "youtu.be"
                          || u.AbsolutePath.StartsWith("/shorts/") || u.AbsolutePath.StartsWith("/live/");
        var ruta = u.AbsolutePath.TrimEnd('/');

        if (tieneLista && tieneVideo) return Clase.VideoEnLista;
        if (tieneLista || ruta == "/playlist") return Clase.Lista;
        // Channel pages (/@name, /@name/videos, /channel/…, /c/…, /user/…) are lists of videos too.
        if (ruta.StartsWith("/@") || ruta.StartsWith("/channel/") || ruta.StartsWith("/c/") || ruta.StartsWith("/user/"))
            return Clase.Lista;
        return Clase.Video;
    }
}
