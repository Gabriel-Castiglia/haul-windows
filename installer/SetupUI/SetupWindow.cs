//
//  SetupWindow.cs
//  Haul setup: the setup window, in English, Spanish and Japanese.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: November 9, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WixToolset.BootstrapperApplicationApi;
using static Haul.Loc;

namespace Haul.Setup;

/// <summary>
/// The setup window: a header with the icon, the version and the language menu, one page at a time
/// (start, progress, result) and the buttons of that page. A language change redraws the current page.
/// The window also works without an engine (app == null) to render the pages for the manuals.
/// </summary>
internal sealed class SetupWindow : Window
{
    private const string LicenseUrl = "https://www.gnu.org/licenses/old-licenses/gpl-2.0.html";

    private readonly SetupApplication app;
    private readonly ContentControl page = new();
    private readonly StackPanel buttons = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly ProgressBar bar = new() { Height = 14, Minimum = 0, Maximum = 100, Margin = new Thickness(0, 14, 0, 0) };
    private Action redraw;
    private TextBlock subtitle;

    // Values used by the pages; filled from the engine or, for a preview, by hand.
    public string Version = "", OlderVersion, NewerVersion, InstallFolder = @"C:\Program Files\Haul", ErrorText;
    public bool Applying { get; private set; }

    public SetupWindow(SetupApplication app)
    {
        this.app = app;
        if (app != null)
        {
            Version = app.Version;
            OlderVersion = app.OlderVersion;
            NewerVersion = app.NewerVersion;
            InstallFolder = app.InstallFolder;
        }
        Width = 560;
        Height = 360;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xF7, 0xF9));
        FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
        FontSize = 13;
        try { Icon = BitmapFrame.Create(new Uri("pack://application:,,,/haul.ico")); } catch { }

        var root = new DockPanel { Margin = new Thickness(28, 22, 28, 20) };
        var header = BuildHeader();
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);
        root.Children.Add(page);
        Content = root;

        Loc.Changed += Redraw;
        Closed += (s, e) => Loc.Changed -= Redraw;
        Closing += OnClosing;
        ShowLoading();
    }

    private void Redraw()
    {
        // Japanese text uses Windows' Japanese interface fonts; the language tag picks Japanese glyph shapes and
        // line breaking.
        Language = System.Windows.Markup.XmlLanguage.GetLanguage(Loc.Current == UiLanguage.Ja ? "ja-JP" : Loc.Current == UiLanguage.Es ? "es" : "en-US");
        FontFamily = new FontFamily(Loc.Current == UiLanguage.Ja ? "Yu Gothic UI, Meiryo UI, Segoe UI" : "Segoe UI Variable Text, Segoe UI");
        Title = T("Title");
        if (subtitle != null) subtitle.Text = T("Subtitle", Version);
        redraw?.Invoke();
    }

    private FrameworkElement BuildHeader()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        try
        {
            grid.Children.Add(new Image
            {
                // The largest image of the icon; the first one is 16 pixels and would look blurred.
                Source = BitmapDecoder.Create(new Uri("pack://application:,,,/haul.ico"), BitmapCreateOptions.None, BitmapCacheOption.OnLoad)
                    .Frames.OrderByDescending(f => f.PixelWidth).First(),
                Width = 48, Height = 48, Margin = new Thickness(0, 0, 14, 0),
            });
        }
        catch { /* the window still works without the icon */ }
        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titles.Children.Add(new TextBlock { Text = "Haul", FontSize = 24, FontWeight = FontWeights.SemiBold });
        subtitle = new TextBlock { Foreground = Brushes.DimGray };
        titles.Children.Add(subtitle);
        Grid.SetColumn(titles, 1);
        grid.Children.Add(titles);

        // The language menu: its label carries the three languages, so it can be found from any of them.
        var language = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        var label = new TextBlock { FontSize = 11.5, Foreground = Brushes.DimGray, Margin = new Thickness(0, 0, 0, 3) };
        label.Inlines.Add(new Run("Idioma / Language / "));
        label.Inlines.Add(new Run("言語") { Language = System.Windows.Markup.XmlLanguage.GetLanguage("ja-JP"), FontFamily = new FontFamily("Yu Gothic UI, Meiryo UI") });
        language.Children.Add(label);
        var combo = new ComboBox { Width = 120 };
        combo.Items.Add("English");
        combo.Items.Add("Español");
        combo.Items.Add(new ComboBoxItem
        {
            Content = "日本語", Language = System.Windows.Markup.XmlLanguage.GetLanguage("ja-JP"), FontFamily = new FontFamily("Yu Gothic UI, Meiryo UI"),
        });
        combo.SelectedIndex = (int)Loc.Current;
        combo.SelectionChanged += (s, e) => { if (combo.SelectedIndex >= 0) Loc.Current = (UiLanguage)combo.SelectedIndex; };
        language.Children.Add(combo);
        Grid.SetColumn(language, 2);
        grid.Children.Add(language);

        return grid;
    }

    // ---------------- Pages ----------------
    private void Show(Action build)
    {
        redraw = build;
        Redraw();
    }

    public void ShowLoading() => Show(() =>
    {
        Body(T("Checking"));
        Buttons();
    });

    public void ShowStart(SetupState state)
    {
        switch (state)
        {
            case SetupState.Install:
                Show(() =>
                {
                    Body(T("InstallIn"), InstallFolder, ToolsLine, LicenseLine());
                    Buttons((T("Install"), () => app?.Plan(LaunchAction.Install), true), CancelButton());
                });
                break;
            case SetupState.Upgrade:
                Show(() =>
                {
                    Body(T("UpgradeText", OlderVersion, Version), null, ToolsLine, LicenseLine());
                    Buttons((T("Update"), () => app?.Plan(LaunchAction.Install), true), CancelButton());
                });
                break;
            case SetupState.Maintenance:
                Show(() =>
                {
                    Body(T("MaintenanceText", Version), null, T("MaintenanceNote"));
                    Buttons((T("Repair"), () => app?.Plan(LaunchAction.Repair), false),
                            (T("Uninstall"), () => app?.Plan(LaunchAction.Uninstall), false),
                            CloseButton(true));
                });
                break;
            case SetupState.Uninstall:
                Show(() =>
                {
                    Body(T("UninstallText"), null, T("UninstallNote"));
                    Buttons((T("Uninstall"), () => app?.Plan(LaunchAction.Uninstall), true), CancelButton());
                });
                break;
            case SetupState.Newer:
                Show(() =>
                {
                    Body(T("NewerText", NewerVersion));
                    Buttons(CloseButton(true));
                });
                break;
        }
    }

    private static string ToolsLine => T("ToolsLine");

    public void ShowProgress(LaunchAction action)
    {
        Applying = true;
        Show(() =>
        {
            string text = action switch
            {
                LaunchAction.Uninstall => T("Uninstalling"),
                LaunchAction.Repair => T("Repairing"),
                _ => T(OlderVersion != null ? "Updating" : "Installing"),
            };
            var panel = BodyPanel(text);
            if (bar.Parent is Panel old) old.Children.Remove(bar);
            panel.Children.Add(bar);
            page.Content = panel;
            Buttons((T("Cancel"), () => { if (app != null) app.Canceled = true; }, false));
        });
    }

    /// <summary>Set while winget installs FFmpeg and Deno: Haul is already installed, so there is nothing to roll back.</summary>
    private bool installingTools;

    /// <summary>Progress page while winget installs or upgrades FFmpeg and Deno; it cannot be canceled.</summary>
    public void ShowTools() => Show(() =>
    {
        installingTools = true;
        var panel = BodyPanel(T("ToolsProgress"));
        panel.Children.Add(new TextBlock
        {
            Text = T("ToolsWhy"),
            Foreground = Brushes.DimGray, Margin = new Thickness(0, 10, 0, 10), TextWrapping = TextWrapping.Wrap,
        });
        if (bar.Parent is Panel old) old.Children.Remove(bar);
        bar.IsIndeterminate = true;
        panel.Children.Add(bar);
        page.Content = panel;
        Buttons();
    });

    public void SetProgress(int percent) => bar.Value = Math.Max(bar.Value, Math.Min(100, Math.Max(0, percent)));

    public void ShowResult(bool ok, bool canceled, bool restart)
    {
        Applying = false;
        installingTools = false;
        var action = app?.Planned ?? LaunchAction.Install;
        Show(() =>
        {
            if (canceled)
            {
                Body(T("Canceled"));
                Buttons(CloseButton(true));
                return;
            }
            if (!ok)
            {
                Body(T("Failed"), null, ErrorText ?? app?.LastError);
                Buttons((T("ViewLog"), OpenLog, false), CloseButton(true));
                return;
            }
            string done = action switch
            {
                LaunchAction.Uninstall => T("Uninstalled"),
                LaunchAction.Repair => T("Repaired"),
                _ => T("Installed"),
            };
            bar.IsIndeterminate = false;
            string note = restart ? T("Restart") : null;
            if (app?.ToolsNote != null) note = note == null ? app.ToolsNote : note + "\n\n" + app.ToolsNote;
            Body(done, null, note);
            if (action == LaunchAction.Install)
                Buttons((T("OpenHaul"), LaunchHaul, true), CloseButton(false));
            else
                Buttons(CloseButton(true));
        });
    }

    // ---------------- Building blocks ----------------
    private StackPanel BodyPanel(string text)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 14 });
        return panel;
    }

    /// <summary>Page body: a main text, an optional highlighted line, an optional note and an optional extra block.</summary>
    private void Body(string text, string highlight = null, string note = null, UIElement extra = null)
    {
        var panel = BodyPanel(text);
        if (highlight != null)
            panel.Children.Add(new TextBlock { Text = highlight, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap });
        if (note != null)
            panel.Children.Add(new TextBlock { Text = note, Foreground = Brushes.DimGray, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap });
        if (extra != null) panel.Children.Add(extra);
        page.Content = panel;
    }

    private UIElement LicenseLine()
    {
        var link = new Hyperlink(new Run(T("LicenseName")))
        {
            NavigateUri = new Uri(LicenseUrl),
        };
        link.RequestNavigate += (s, e) => Open(LicenseUrl);
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DimGray, Margin = new Thickness(0, 16, 0, 0) };
        text.Inlines.Add(new Run(T("FreeBefore")));
        text.Inlines.Add(link);
        text.Inlines.Add(new Run(T("FreeAfter")));
        return text;
    }

    private void Buttons(params (string text, Action action, bool isDefault)[] list)
    {
        buttons.Children.Clear();
        foreach (var (text, action, isDefault) in list)
        {
            var b = new Button { Content = text, MinWidth = 96, Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(8, 0, 0, 0), IsDefault = isDefault };
            b.Click += (s, e) => action();
            buttons.Children.Add(b);
        }
    }

    private (string, Action, bool) CancelButton() => (T("Cancel"), Close, false);
    private (string, Action, bool) CloseButton(bool isDefault) => (T("Close"), Close, isDefault);

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // During the FFmpeg and Deno step the window stays open: canceling there would only mark the setup as
        // canceled ("the computer was left as it was") although Haul is already installed.
        if (installingTools && app != null)
        {
            e.Cancel = true;
            return;
        }
        if (!Applying || app == null)
        {
            if (app != null && app.Planned == LaunchAction.Unknown) app.CancelBeforeStart();
            return;
        }
        // While installing, closing the window means canceling; the engine rolls the changes back.
        e.Cancel = true;
        var answer = MessageBox.Show(this, T("CancelQuestion"),
                                     "Haul", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes && app != null) app.Canceled = true;
    }

    private void LaunchHaul()
    {
        Open(Path.Combine(InstallFolder, "Haul.exe"));
        Close();
    }

    private void OpenLog()
    {
        string log = app?.LogPath;
        if (!string.IsNullOrEmpty(log)) Open(log);
    }

    private static void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); } catch { }
    }
}
