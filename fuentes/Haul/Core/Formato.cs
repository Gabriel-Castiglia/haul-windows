//
//  Formato.cs
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

using static Haul.Core.Loc;

namespace Haul.Core;

public enum Formato { Video, Audio }

public static class Formatos
{
    public static string Titulo(Formato f) => f == Formato.Video ? "Video" : tr("Solo audio", "Audio only");

    public static string Detalle(Formato f) =>
        f == Formato.Video ? tr("MP4 · mejor calidad", "MP4 · best quality")
                           : tr("MP3 · máxima calidad", "MP3 · highest quality");

    // yt-dlp arguments for each format, identical to the macOS version.
    public static string[] Argumentos(Formato f) => f switch
    {
        // H.264 + AAC is preferred because every Windows player handles it; without this
        // preference YouTube serves AV1 + Opus.
        Formato.Video => ["-f", "bv*+ba/b", "-S", "vcodec:h264,res,acodec:aac", "--merge-output-format", "mp4"],
        _ => ["-x", "--audio-format", "mp3", "--audio-quality", "0"],
    };

    public static Formato Desde(string? s) => s == "audio" ? Formato.Audio : Formato.Video;
    public static string Clave(Formato f) => f == Formato.Audio ? "audio" : "video";
}
