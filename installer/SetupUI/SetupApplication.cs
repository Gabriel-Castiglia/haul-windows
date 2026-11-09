//
//  SetupApplication.cs
//  Haul setup: the bootstrapper application that drives the WiX engine.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: November 9, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Win32;
using WixToolset.BootstrapperApplicationApi;

namespace Haul.Setup;

/// <summary>What the setup can do, as offered on its first page.</summary>
internal enum SetupState { Install, Upgrade, Maintenance, Uninstall, Newer }

/// <summary>
/// Bootstrapper application: talks to the WiX engine (detect, plan, apply) and drives the setup window.
/// The engine calls back on its own thread; everything that touches the window goes through the dispatcher.
/// Without a full user interface (/quiet, /passive, or an upgrade removing this version) it works on its own.
/// </summary>
internal sealed class SetupApplication : BootstrapperApplication
{
    /// <summary>Id of the MSI package in the bundle (Bundle.wxs).</summary>
    private const string PackageId = "HaulMsi";
    /// <summary>Windows Installer error 1602: the user canceled.</summary>
    private const int UserCanceled = unchecked((int)0x80070642);

    public IBootstrapperCommand Command { get; private set; }
    public IEngine Engine => engine;
    public string Version { get; private set; }
    /// <summary>Version of an older Haul that this setup will replace, if any.</summary>
    public string OlderVersion { get; private set; }
    /// <summary>Version of a newer Haul already installed, if any.</summary>
    public string NewerVersion { get; private set; }
    public bool Installed { get; private set; }
    public LaunchAction Planned { get; private set; }
    public string LastError { get; private set; }
    public bool Canceled { get; set; }

    private Dispatcher dispatcher;
    private SetupWindow window;
    private IntPtr windowHandle;
    private int exitCode;

    protected override void OnCreate(CreateEventArgs args)
    {
        base.OnCreate(args);
        Command = args.Command;
    }

    protected override void Run()
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        Version = engine.GetVariableVersion("WixBundleVersion");
        Loc.Current = InitialLanguage();

        DetectRelatedBundle += OnDetectRelatedBundle;
        DetectPackageComplete += OnDetectPackageComplete;
        DetectComplete += OnDetectComplete;
        PlanComplete += OnPlanComplete;
        Progress += (s, e) => { Report(e.OverallPercentage); e.Cancel = Canceled; };
        ExecuteProgress += (s, e) => { Report(e.OverallPercentage); e.Cancel = Canceled; };
        Error += OnError;
        ApplyComplete += OnApplyComplete;

        // The window always exists, because the engine needs its handle to apply; it is shown only with a
        // full or passive interface.
        window = new SetupWindow(this);
        window.Closed += (s, e) => dispatcher.InvokeShutdown();
        windowHandle = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
        if (HasWindow) window.Show();
        engine.Detect();
        Dispatcher.Run();
        engine.Quit(exitCode);
    }

    /// <summary>Full or passive display: a window is shown (passive shows progress only).</summary>
    private bool HasWindow => Command.Display == Display.Full || Command.Display == Display.Passive;
    private bool Interactive => Command.Display == Display.Full;

    /// <summary>
    /// The language of an installed Haul when there is one (uninstall, repair), otherwise the language of
    /// Windows if it is Spanish, otherwise English. The user can change it in the window at any time.
    /// </summary>
    private static UiLanguage InitialLanguage()
    {
        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"Software\Haul");
            if (key?.GetValue("Language") is string code && code.Length > 0) return Loc.FromCode(code);
        }
        catch { }
        return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es" ? UiLanguage.Es : UiLanguage.En;
    }

    // ---------------- Detect ----------------
    private void OnDetectRelatedBundle(object sender, DetectRelatedBundleEventArgs e)
    {
        if (e.RelationType != RelationType.Upgrade) return;
        int cmp = engine.CompareVersions(e.Version, Version);
        if (cmp > 0) NewerVersion = e.Version;
        else if (cmp < 0) OlderVersion = e.Version;
    }

    private void OnDetectPackageComplete(object sender, DetectPackageCompleteEventArgs e)
    {
        if (e.PackageId == PackageId && e.State == PackageState.Present) Installed = true;
    }

    private void OnDetectComplete(object sender, DetectCompleteEventArgs e)
    {
        if (e.Status < 0)
        {
            Finish(e.Status);
            return;
        }
        // Without a full interface the requested action goes ahead at once (e.g. an upgrade removing this version).
        if (!Interactive) { Plan(Command.Action); return; }
        SetupState state =
            Command.Action == LaunchAction.Uninstall ? SetupState.Uninstall
            : NewerVersion != null ? SetupState.Newer
            : Installed ? SetupState.Maintenance
            : OlderVersion != null ? SetupState.Upgrade
            : SetupState.Install;
        dispatcher.BeginInvoke(new Action(() => window.ShowStart(state)));
    }

    // ---------------- Plan and apply ----------------
    /// <summary>Starts an install, repair or uninstall; the chosen language goes to the MSI (see Package.wxs).</summary>
    public void Plan(LaunchAction action)
    {
        Planned = action;
        engine.SetVariableString("HaulLanguage", Loc.Code(Loc.Current), false);
        if (HasWindow) dispatcher.BeginInvoke(new Action(() => window.ShowProgress(action)));
        // Closing Haul can take a few seconds: it is done off the window's thread.
        System.Threading.Tasks.Task.Run(() =>
        {
            CloseRunningHaul(InstallFolder);
            if (action != LaunchAction.Uninstall) RemoveScriptInstall();
            try { engine.Plan(action); }
            catch (Exception ex)
            {
                engine.Log(LogLevel.Error, "Haul setup: plan could not start: " + ex.Message);
                LastError = ex.Message;
                Finish(ex.HResult < 0 ? ex.HResult : unchecked((int)0x80004005));
            }
        });
    }

    // ---------------- Closing a running Haul ----------------
    private const uint WM_CLOSE = 0x0010;
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Closes the Haul of this user that runs from the install folder, before its files are replaced or
    /// removed. The MSI also closes it (util:CloseApplication), but only after removing files, which on an
    /// uninstall would leave the executable in use until a restart. Haul quits when one of its windows
    /// is asked to close; a version that does not do so is ended after a few seconds.
    /// </summary>
    private void CloseRunningHaul(string installFolder)
    {
        string folder;
        try { folder = Path.GetFullPath(installFolder).TrimEnd('\\') + "\\"; } catch { return; }
        foreach (var p in Process.GetProcessesByName("Haul"))
        {
            using (p)
            {
                try
                {
                    string exe = p.MainModule?.FileName;
                    if (exe == null || !exe.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) continue;
                    EnumWindows((h, l) =>
                    {
                        GetWindowThreadProcessId(h, out uint pid);
                        if (pid == (uint)p.Id) PostMessage(h, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        return true;
                    }, IntPtr.Zero);
                    if (!p.WaitForExit(8000))
                    {
                        engine.Log(LogLevel.Standard, "Haul setup: Haul did not close in 8 s; ending it.");
                        p.Kill();
                        p.WaitForExit(3000);
                    }
                    else engine.Log(LogLevel.Standard, "Haul setup: closed the running Haul.");
                }
                catch (Exception ex) { engine.Log(LogLevel.Standard, "Haul setup: could not close Haul: " + ex.Message); }
            }
        }
    }

    private void OnPlanComplete(object sender, PlanCompleteEventArgs e)
    {
        if (e.Status < 0) { Finish(e.Status); return; }
        try { engine.Apply(windowHandle); }
        catch (Exception ex)
        {
            // Without this, a failed call would leave the setup waiting forever.
            engine.Log(LogLevel.Error, "Haul setup: apply could not start: " + ex.Message);
            LastError = ex.Message;
            Finish(ex.HResult < 0 ? ex.HResult : unchecked((int)0x80004005));
        }
    }

    private void Report(int percent)
    {
        if (HasWindow) dispatcher.BeginInvoke(new Action(() => window.SetProgress(percent)));
    }

    private void OnError(object sender, WixToolset.BootstrapperApplicationApi.ErrorEventArgs e)
    {
        // The message is shown on the final page; the engine's recommended answer is kept.
        if (e.ErrorCode != 1602 && !string.IsNullOrWhiteSpace(e.ErrorMessage)) LastError = e.ErrorMessage.Trim();
        engine.Log(LogLevel.Standard, "Haul setup: error " + e.ErrorCode + ": " + e.ErrorMessage);
    }

    private void OnApplyComplete(object sender, ApplyCompleteEventArgs e)
    {
        bool restart = e.Restart == ApplyRestart.RestartRequired;
        if (e.Status < 0 || Canceled || Planned == LaunchAction.Uninstall)
        {
            Finish(e.Status, restart);
            return;
        }
        // Haul is in place; yt-dlp still needs FFmpeg and Deno, which come from winget.
        int status = e.Status;
        System.Threading.Tasks.Task.Run(() =>
        {
            if (HasWindow) dispatcher.BeginInvoke(new Action(() => window.ShowTools()));
            InstallTools();
            Finish(status, restart);
        });
    }

    // ---------------- FFmpeg and Deno ----------------
    /// <summary>winget packages that yt-dlp needs: FFmpeg joins and converts, Deno solves JavaScript challenges.</summary>
    private static readonly string[] Tools = { "yt-dlp.FFmpeg", "DenoLand.Deno" };

    /// <summary>Set when FFmpeg or Deno could not be installed; shown on the final page.</summary>
    public string ToolsNote { get; private set; }

    /// <summary>
    /// Installs FFmpeg and Deno with winget, or upgrades them if they are already there. It runs as the user who
    /// opened the setup, as winget expects. A failure here does not undo Haul; the final page says what is missing.
    /// </summary>
    private void InstallTools()
    {
        string winget = FindWinget();
        if (winget == null)
        {
            ToolsNote = Loc.tr("No se encontró winget. Instala «Instalador de aplicación» desde Microsoft Store y vuelve a ejecutar este programa para instalar FFmpeg y Deno.",
                               "winget was not found. Install \"App Installer\" from the Microsoft Store and run this setup again to install FFmpeg and Deno.");
            engine.Log(LogLevel.Standard, "Haul setup: winget not found.");
            return;
        }
        var failed = new List<string>();
        foreach (string id in Tools)
        {
            string list = Run(winget, $"list --id {id} --exact --accept-source-agreements --disable-interactivity", out _);
            if (list.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // An exit code here usually means there was nothing to upgrade.
                Run(winget, $"upgrade --id {id} --exact --silent --accept-package-agreements --accept-source-agreements --disable-interactivity", out int code);
                engine.Log(LogLevel.Standard, $"Haul setup: winget upgrade {id}: {code}");
            }
            else
            {
                Run(winget, $"install --id {id} --exact --silent --accept-package-agreements --accept-source-agreements --disable-interactivity", out int code);
                engine.Log(LogLevel.Standard, $"Haul setup: winget install {id}: {code}");
                if (code != 0) failed.Add(id.Split('.')[1]);
            }
        }
        if (failed.Count > 0)
            ToolsNote = string.Format(Loc.tr("winget no pudo instalar {0}. Haul lo necesita para descargar: vuelve a ejecutar este programa más tarde.",
                                             "winget could not install {0}. Haul needs it to download: run this setup again later."), string.Join(", ", failed));
    }

    private static string FindWinget()
    {
        string alias = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\winget.exe");
        if (File.Exists(alias)) return alias;
        foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
        {
            try { string p = Path.Combine(dir.Trim(), "winget.exe"); if (File.Exists(p)) return p; } catch { }
        }
        return null;
    }

    /// <summary>Runs a command without a window and returns its output; gives up after ten minutes.</summary>
    private string Run(string exe, string arguments, out int exitCode)
    {
        exitCode = -1;
        try
        {
            var info = new ProcessStartInfo(exe, arguments)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };
            using (var p = Process.Start(info))
            {
                var output = p.StandardOutput.ReadToEndAsync();
                p.StandardError.ReadToEndAsync();
                if (!p.WaitForExit(600000)) { try { p.Kill(); } catch { } return ""; }
                exitCode = p.ExitCode;
                return output.Result;
            }
        }
        catch (Exception ex)
        {
            engine.Log(LogLevel.Standard, "Haul setup: could not run winget: " + ex.Message);
            return "";
        }
    }

    // ---------------- The 1.4 script installation ----------------
    /// <summary>
    /// Removes an installation made by the scripts of Haul 1.4 and earlier (per user, in
    /// %LOCALAPPDATA%\Programs\Haul), so only one Haul remains. Its settings in %APPDATA%\Haul are kept.
    /// </summary>
    private void RemoveScriptInstall()
    {
        const string key = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Haul";
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Haul");
        string shortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Haul.lnk");
        try
        {
            bool found = Directory.Exists(folder) || File.Exists(shortcut);
            using (var k = Registry.CurrentUser.OpenSubKey(key)) found |= k != null;
            if (!found) return;
            CloseRunningHaul(folder);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            if (File.Exists(shortcut)) File.Delete(shortcut);
            Registry.CurrentUser.DeleteSubKeyTree(key, false);
            engine.Log(LogLevel.Standard, "Haul setup: removed the script installation in " + folder);
        }
        catch (Exception ex) { engine.Log(LogLevel.Standard, "Haul setup: could not remove the script installation: " + ex.Message); }
    }

    /// <summary>Ends the operation: shows the result page or, without a full interface, closes.</summary>
    private void Finish(int status, bool restart = false)
    {
        exitCode = status;
        bool canceled = Canceled || status == UserCanceled;
        if (!Interactive)
        {
            dispatcher.BeginInvoke(new Action(() => dispatcher.InvokeShutdown()));
            return;
        }
        dispatcher.BeginInvoke(new Action(() => window.ShowResult(status >= 0, canceled, restart)));
    }

    /// <summary>Called when the user closes the window before anything was done.</summary>
    public void CancelBeforeStart() => exitCode = UserCanceled;

    /// <summary>Path of the setup log, for the "View log" button.</summary>
    public string LogPath => engine.ContainsVariable("WixBundleLog") ? engine.GetVariableString("WixBundleLog") : null;

    /// <summary>Where Haul is (or will be) installed.</summary>
    public string InstallFolder => engine.FormatString(@"[ProgramFiles64Folder]Haul");
}
