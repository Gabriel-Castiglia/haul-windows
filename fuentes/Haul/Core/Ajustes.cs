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

public enum Idioma { En, Es, Ja }

// Interface language. On first launch it is the language chosen in the setup window or, without one, the language of
// Windows when it is Spanish or Japanese (English otherwise); the choice is remembered. The texts live in Textos.cs.
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
            Ajustes.Datos.Idioma = Codigo(value);
            Ajustes.Guardar();
            Cambio?.Invoke();
        }
    }

    public static void Iniciar() => actual = DesdeCodigo(Ajustes.Datos.Idioma);

    // Settings code of a language ("en", "es" or "ja"); anything unknown means English.
    public static string Codigo(Idioma i) => i switch { Idioma.Es => "es", Idioma.Ja => "ja", _ => "en" };
    public static Idioma DesdeCodigo(string? codigo) => codigo?.Trim().ToLowerInvariant() switch
    {
        "es" => Idioma.Es, "ja" => Idioma.Ja, _ => Idioma.En,
    };

    // The language of Windows' interface, when Haul speaks it; null otherwise.
    public static string? CodigoDeWindows()
    {
        var c = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return c is "es" or "ja" ? c : null;
    }

    public static IReadOnlyDictionary<string, string> Tabla(Idioma i) => i switch
    {
        Idioma.Es => Textos.Es, Idioma.Ja => Textos.Ja, _ => Textos.En,
    };

    // The text for the current interface language; a key missing from it falls back to English, then to the key.
    public static string T(string clave) =>
        Tabla(actual).TryGetValue(clave, out var t) || Textos.En.TryGetValue(clave, out t) ? t : clave;

    // The same, with "{0}", "{1}"… filled in.
    public static string T(string clave, params object?[] valores) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, T(clave), valores);

    // The language and the fonts of a window: Japanese text uses Windows' Japanese interface fonts, and the language
    // tag picks Japanese glyph shapes and line breaking.
    public static void Aplicar(System.Windows.FrameworkElement ventana)
    {
        ventana.Language = System.Windows.Markup.XmlLanguage.GetLanguage(actual switch
        {
            Idioma.Es => "es", Idioma.Ja => "ja-JP", _ => "en-US",
        });
        if (actual == Idioma.Ja)
            ventana.SetValue(System.Windows.Documents.TextElement.FontFamilyProperty,
                             new System.Windows.Media.FontFamily("Yu Gothic UI, Meiryo UI, Segoe UI"));
        else
            ventana.ClearValue(System.Windows.Documents.TextElement.FontFamilyProperty);
    }
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
        public bool LimpiarSola { get; set; }
        public bool Instrumentales { get; set; }
        public bool ListaEntera { get; set; } = true;
        // The options under the Download button: folded (the default, more room for the queue) or open.
        public bool OpcionesAbiertas { get; set; }
        // "auto" (as Windows), "light" or "dark". A settings file from before 1.6 has none and keeps the dark
        // window it had; a new installation follows Windows.
        public string? Apariencia { get; set; }
        // Accent colour: the key of one of Tema.Colores.
        public string? Color { get; set; }
        // Once a day Haul asks GitHub whether there is a newer version and, if there is, offers to install it.
        public bool BuscarVersiones { get; set; } = true;
        public DateTime UltimaBusqueda { get; set; }
        // Once a day Haul asks GitHub for yt-dlp's latest release and, if it is newer, installs it by itself.
        public bool ActualizarYtDlp { get; set; } = true;
        public DateTime UltimaBusquedaYtDlp { get; set; }
    }

    // The test option --datos points it to a throw-away folder (App.xaml.cs).
    public static string Carpeta { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Haul");
    private static string Archivo => Path.Combine(Carpeta, "settings.json");

    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Valores Datos { get; private set; } = new();

    // Without "desdeArchivo" (the test pictures) the defaults are used and nothing is read.
    public static void Cargar(bool desdeArchivo = true)
    {
        try
        {
            if (!desdeArchivo) Datos = new();
            else if (File.Exists(Archivo))
            {
                Datos = JsonSerializer.Deserialize<Valores>(File.ReadAllText(Archivo), Opciones) ?? new();
                Datos.Apariencia ??= "dark";
            }
            else
            {
                // First start: the language chosen in the setup window or, without a setup, the language of Windows.
                var elegido = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Haul")?.GetValue("Language") as string;
                Datos.Idioma = Loc.Codigo(Loc.DesdeCodigo(string.IsNullOrWhiteSpace(elegido) ? Loc.CodigoDeWindows() : elegido));
            }
        }
        catch
        {
            // A damaged file is replaced by the defaults on the next save.
            Datos = new();
        }
        Datos.Apariencia = Datos.Apariencia is "light" or "dark" ? Datos.Apariencia : "auto";
        Datos.Color = Tema.Obtener(Datos.Color).Clave;
    }

    // A test screenshot never writes the settings.
    public static bool SoloLectura { get; set; }

    public static void Guardar()
    {
        if (SoloLectura) return;
        try
        {
            Directory.CreateDirectory(Carpeta);
            // A temporary file replaces the old one, so a crash never leaves half a settings file.
            File.WriteAllText(Archivo + ".tmp", JsonSerializer.Serialize(Datos, Opciones));
            File.Move(Archivo + ".tmp", Archivo, overwrite: true);
        }
        catch
        {
            // Preferences are a convenience; failing to save them must not stop the app.
        }
    }
}
