//
//  Versiones.cs
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

using System.Net.Http;
using System.Text.Json;

namespace Haul.Core;

// Asks GitHub for the latest published version of Haul for Windows. Nothing is sent but the request itself (with
// "Haul/<version>" as the program name), and nothing is downloaded or installed: the user decides whether to open
// the download page.
public static class Versiones
{
    private const string Api = "https://api.github.com/repos/Gabriel-Castiglia/haul-windows/releases/latest";
    public const string Pagina = "https://github.com/Gabriel-Castiglia/haul-windows/releases/latest";

    // The running version, as major.minor.patch.
    public static Version Actual => Version.TryParse(Herramientas.VersionHaul, out var v)
        ? new Version(v.Major, v.Minor, Math.Max(0, v.Build)) : new Version(0, 0, 0);

    // The latest version on GitHub when it is newer than this one; null if it is not, or if GitHub did not answer.
    public static async Task<Version?> MasNuevaAsync()
    {
        try
        {
            var ultima = await UltimaAsync();
            return ultima != null && ultima > Actual ? ultima : null;
        }
        catch
        {
            return null;
        }
    }

    // The latest published version; throws if GitHub cannot be reached.
    public static async Task<Version?> UltimaAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Haul/" + Actual);
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        using var doc = JsonDocument.Parse(await http.GetStringAsync(Api));
        var tag = doc.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
        return Version.TryParse(tag, out var v) ? new Version(v.Major, v.Minor, Math.Max(0, v.Build)) : null;
    }
}
