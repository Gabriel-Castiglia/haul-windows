//
//  Herramientas.cs
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

using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace Haul.Core;

// Locates the bundled yt-dlp and the external tools it needs (ffmpeg and deno).
public static class Herramientas
{
    // yt-dlp ships next to Haul.exe, pinned to the version it was tested with.
    public static readonly string RutaYtDlp = Path.Combine(AppContext.BaseDirectory, "yt-dlp.exe");

    public static readonly string VersionYtDlp =
        typeof(Herramientas).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "YtDlpVersion")?.Value ?? "?";

    public static readonly string VersionHaul =
        typeof(Herramientas).Assembly.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "?";

    // An app started from the Start menu inherits the PATH Explorer had when it started, which
    // misses tools installed afterwards with winget. The current PATH is rebuilt from the
    // registry instead, plus winget's folder of command aliases.
    public static string RutasDeBusqueda { get; } = ArmarPath();

    public static string? Ffmpeg { get; } = Buscar("ffmpeg.exe");
    public static string? Deno { get; } = Buscar("deno.exe");

    public static bool YtDlpPresente => File.Exists(RutaYtDlp);
    public static bool Completo => YtDlpPresente && Ffmpeg != null && Deno != null;

    private static string ArmarPath()
    {
        static string Leer(RegistryKey raiz, string sub)
        {
            using var k = raiz.OpenSubKey(sub);
            var valor = k?.GetValue("Path", "", RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "";
            return Environment.ExpandEnvironmentVariables(valor);
        }
        var partes = new List<string>();
        void Sumar(string lista)
        {
            foreach (var p in lista.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (!partes.Contains(p, StringComparer.OrdinalIgnoreCase)) partes.Add(p);
        }
        Sumar(Leer(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment"));
        Sumar(Leer(Registry.CurrentUser, "Environment"));
        Sumar(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                           "Microsoft", "WinGet", "Links"));
        Sumar(Environment.GetEnvironmentVariable("PATH") ?? "");
        return string.Join(';', partes);
    }

    private static string? Buscar(string programa)
    {
        foreach (var carpeta in RutasDeBusqueda.Split(';'))
        {
            try
            {
                var p = Path.Combine(carpeta, programa);
                if (File.Exists(p)) return p;
            }
            catch (ArgumentException)
            {
                // A malformed PATH entry is skipped.
            }
        }
        return null;
    }

    // Prepares a yt-dlp process with hidden console, UTF-8 output and the rebuilt PATH.
    public static ProcessStartInfo Preparar(IEnumerable<string> argumentos, string? carpeta = null)
    {
        var psi = new ProcessStartInfo(RutaYtDlp)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };
        if (carpeta != null) psi.WorkingDirectory = carpeta;
        psi.Environment["PATH"] = RutasDeBusqueda;
        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        foreach (var a in argumentos) psi.ArgumentList.Add(a);
        return psi;
    }

    // Opens a folder or a file with its default program.
    public static void Abrir(string ruta)
    {
        try { Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true }); }
        catch { /* nothing to do if the user removed it */ }
    }

    // Opens File Explorer with the file selected.
    public static void MostrarEnExplorador(string archivo)
    {
        try { Process.Start("explorer.exe", $"/select,\"{archivo}\""); }
        catch { /* idem */ }
    }
}
