//
//  Filtro.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: October 21, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

namespace Haul.Core;

// Checks made on every video before anything is downloaded: instrumentals wait unless the user
// allows them, and a few uploads are dropped without a trace.
public static class Filtro
{
    private const string Video = "J0vUqvfHQeQ";
    private const string Titulo = "Unreleased";
    private static readonly string[] Canales = ["ahdukejk", "UCYEUeTgPLy8MtY8iXja3AFQ"];

    public static bool EsInstrumental(string? titulo) =>
        titulo != null && titulo.Contains("instrumental", StringComparison.OrdinalIgnoreCase);

    // fuentes: channel, channel id, uploader and uploader id, as yt-dlp reports them.
    public static bool Descartado(string? id, string? titulo, params string?[] fuentes) =>
        id == Video
        || string.Equals(titulo?.Trim(), Titulo, StringComparison.OrdinalIgnoreCase)
        || fuentes.Any(f => f != null && Canales.Contains(f.Trim().TrimStart('@'), StringComparer.OrdinalIgnoreCase));

    // The same checks as a yt-dlp --match-filters expression, so a held-back video is never
    // downloaded. The "?" lets a video without that field through.
    public static string Expresion(bool instrumentales)
    {
        var partes = new List<string>
        {
            $"id!=?{Video}",
            $"title!~=?(?i)^\\s*{Titulo}\\s*$",
            $"channel!~=?(?i)^{Canales[0]}$",
            $"uploader!~=?(?i)^{Canales[0]}$",
            $"uploader_id!~=?(?i)^@?{Canales[0]}$",
            $"channel_id!=?{Canales[1]}",
        };
        if (!instrumentales) partes.Add("title!~=?(?i)instrumental");
        return string.Join(" & ", partes);
    }
}
