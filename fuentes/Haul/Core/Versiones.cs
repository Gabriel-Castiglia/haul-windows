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

using System.IO;

namespace Haul.Core;

// The releases of Haul for Windows on GitHub. Nothing is sent but the request itself (with "Haul/<version>" as the
// program name). Each release carries haul-setup.exe, SHA256SUMS.txt and release.sig, the author's signature of that
// list (ReleaseSignature.cs); the update installs that setup program only after checking the signature and then the
// setup against the list (MainWindow.Actualizacion.cs). A release without them is only offered as a page.
public static class Versiones
{
    public const string Repositorio = "Gabriel-Castiglia/haul-windows";
    public const string Pagina = "https://github.com/" + Repositorio + "/releases/latest";
    public const string Instalador = "haul-setup.exe";
    public const string Sumas = "SHA256SUMS.txt";

    // The running version, as major.minor.patch.
    public static Version Actual => Version.TryParse(Herramientas.VersionHaul, out var v)
        ? new Version(v.Major, v.Minor, Math.Max(0, v.Build)) : new Version(0, 0, 0);

    // A tag as major.minor.patch ("v1.7.0" → 1.7.0); null if it is not a version.
    public static Version? DeEtiqueta(string etiqueta) =>
        Actualizacion.Normalizar(etiqueta) is { } v ? new Version(v.Major, v.Minor, v.Build) : null;

    // The latest release; throws if GitHub cannot be reached.
    public static Task<Actualizacion.Lanzamiento> UltimoAsync() => Actualizacion.UltimoAsync(Repositorio);

    // Whether a release is newer than the running Haul.
    public static bool EsNueva(Actualizacion.Lanzamiento l) => Actualizacion.EsMasNueva(l.Etiqueta, Actual.ToString());

    // The addresses of the setup program, of the checksum list and of its signature, when the release has all three.
    public static (string instalador, string sumas, string firma)? Archivos(Actualizacion.Lanzamiento l) =>
        l.Archivo(Instalador) is { } i && l.Archivo(Sumas) is { } s && l.Archivo(ReleaseSignature.AssetName) is { } f ? (i, s, f) : null;

    // Where the setup program is downloaded before running it.
    public static string CarpetaDescarga => Path.Combine(Path.GetTempPath(), "HaulUpdate");

    // The version an update was going to install, written just before the setup program runs, so that the Haul
    // reopened by the setup can tell whether it worked. It lives with the settings.
    public static string ArchivoPendiente => Path.Combine(Ajustes.Carpeta, "update-pending.txt");
}
