//
//  Loc.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: November 9, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

namespace Haul;

public enum UiLanguage { En, Es, Ja }

/// <summary>
/// Interface language of the setup window. It starts as the language of an installed Haul, or Spanish or Japanese on
/// a Windows in that language, or English (SetupApplication.InitialLanguage); the choice goes to the MSI, which
/// stores it for the app. Every visible text goes through <see cref="T(string)"/>, which reads it from the table of
/// the current language (Setup.SetupTexts).
/// </summary>
internal static class Loc
{
    private static UiLanguage current = UiLanguage.En;

    /// <summary>Raised after the language changes, so open windows can rebuild their texts.</summary>
    public static event Action Changed;

    public static UiLanguage Current
    {
        get => current;
        set
        {
            if (current == value) return;
            current = value;
            Changed?.Invoke();
        }
    }

    /// <summary>Configuration code of a language ("en", "es" or "ja").</summary>
    public static string Code(UiLanguage l) => l == UiLanguage.Es ? "es" : l == UiLanguage.Ja ? "ja" : "en";

    /// <summary>Language for a configuration code; anything unknown means English.</summary>
    public static UiLanguage FromCode(string code) =>
        string.Equals(code, "es", StringComparison.OrdinalIgnoreCase) ? UiLanguage.Es
        : string.Equals(code, "ja", StringComparison.OrdinalIgnoreCase) ? UiLanguage.Ja : UiLanguage.En;

    private static IReadOnlyDictionary<string, string> Table =>
        current == UiLanguage.Es ? Setup.SetupTexts.Es : current == UiLanguage.Ja ? Setup.SetupTexts.Ja : Setup.SetupTexts.En;

    /// <summary>Returns the text for the current interface language; a missing key falls back to English.</summary>
    public static string T(string key) =>
        Table.TryGetValue(key, out var t) || Setup.SetupTexts.En.TryGetValue(key, out t) ? t : key;

    /// <summary>The same, with "{0}", "{1}"… filled in.</summary>
    public static string T(string key, params object[] values) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, T(key), values);
}
