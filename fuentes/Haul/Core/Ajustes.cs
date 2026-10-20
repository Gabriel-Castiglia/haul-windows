//
//  Ajustes.cs
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

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Haul.Core;

public enum Idioma { En, Es }

// Interface language. English is the default on first launch; the choice is remembered.
public static class Loc
{
    private static Idioma actual = Idioma.En;

    // Raised after the language changes, so every visible text can be refreshed.
    public static event Action? Cambio;

    public static Idioma Actual
    {
        get => actual;
        set
        {
            if (actual == value) return;
            actual = value;
            Ajustes.Datos.Idioma = value == Idioma.Es ? "es" : "en";
            Ajustes.Guardar();
            Cambio?.Invoke();
        }
    }

    public static void Iniciar() =>
        actual = Ajustes.Datos.Idioma == "es" ? Idioma.Es : Idioma.En;

    // Returns the text for the current interface language.
    public static string tr(string es, string en) => actual == Idioma.Es ? es : en;
}

// Preferences stored in %APPDATA%\Haul\settings.json. It can be deleted to reset everything.
public static class Ajustes
{
    public sealed class Valores
    {
        public string Idioma { get; set; } = "en";
        public string? Carpeta { get; set; }
        public string Formato { get; set; } = "video";
        public double? Izquierda { get; set; }
        public double? Arriba { get; set; }
        public double? Ancho { get; set; }
        public double? Alto { get; set; }
        public bool Maximizada { get; set; }
    }

    public static readonly string Carpeta =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Haul");
    private static readonly string Archivo = Path.Combine(Carpeta, "settings.json");

    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Valores Datos { get; private set; } = new();

    public static void Cargar()
    {
        try
        {
            if (File.Exists(Archivo))
                Datos = JsonSerializer.Deserialize<Valores>(File.ReadAllText(Archivo), Opciones) ?? new();
        }
        catch
        {
            // A damaged file is replaced by the defaults on the next save.
            Datos = new();
        }
    }

    public static void Guardar()
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Archivo, JsonSerializer.Serialize(Datos, Opciones));
        }
        catch
        {
            // Preferences are a convenience; failing to save them must not stop the app.
        }
    }
}
