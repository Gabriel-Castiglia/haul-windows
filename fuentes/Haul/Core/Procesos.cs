//
//  Procesos.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: November 27, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Haul.Core;

// A yt-dlp process with its output read line by line, inside a Windows job object together with every process it
// starts (ffmpeg, and the second yt-dlp.exe of its one-file launcher). The end of the process is waited for off the
// UI thread: once it has exited, the end of both outputs gets a few seconds more, and if a process it left behind
// still holds them open, the whole job is stopped. Before, the queue waited for that end on the UI thread with no
// limit, and such a leftover process froze the window.
public sealed partial class ProcesoVigilado : IDisposable
{
    // How long the outputs may stay open after the process has exited.
    public static TimeSpan Gracia { get; set; } = TimeSpan.FromSeconds(10);

    public Process Proceso { get; }
    private readonly TaskCompletionSource salio = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource finSalida = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource finError = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IntPtr trabajo;

    // "salida" receives every line of standard output and "error" every line of standard error ("salida" too when
    // it is null), on a thread of the process reader.
    public ProcesoVigilado(ProcessStartInfo inicio, Action<string> salida, Action<string>? error = null)
    {
        error ??= salida;
        Proceso = new Process { StartInfo = inicio, EnableRaisingEvents = true };
        Proceso.OutputDataReceived += (_, d) => { if (d.Data == null) finSalida.TrySetResult(); else salida(d.Data); };
        Proceso.ErrorDataReceived += (_, d) => { if (d.Data == null) finError.TrySetResult(); else error(d.Data); };
        // Process.WaitForExit and WaitForExitAsync also wait for the end of the outputs, with no limit: the exit is
        // taken from this event instead.
        Proceso.Exited += (_, _) => salio.TrySetResult();
    }

    // Starts the process and its readers. Throws if it cannot start.
    public void Iniciar()
    {
        Proceso.Start();
        trabajo = CreateJobObjectW(IntPtr.Zero, IntPtr.Zero);
        // Without a job (very unlikely) the process still runs; only the stop after the grace time is lost.
        if (trabajo != IntPtr.Zero && !AssignProcessToJobObject(trabajo, Proceso.Handle))
        {
            CloseHandle(trabajo);
            trabajo = IntPtr.Zero;
        }
        Proceso.BeginOutputReadLine();
        Proceso.BeginErrorReadLine();
    }

    // Waits, off the calling thread, for the process to exit and for both outputs to end; returns the exit code and
    // whether the job had to be stopped because the outputs stayed open past the grace time.
    public async Task<(int codigo, bool detenido)> EsperarAsync()
    {
        await salio.Task.ConfigureAwait(false);
        bool detenido = false;
        try { await Task.WhenAll(finSalida.Task, finError.Task).WaitAsync(Gracia).ConfigureAwait(false); }
        catch (TimeoutException)
        {
            detenido = true;
            Detener();
        }
        return (Proceso.ExitCode, detenido);
    }

    // Stops the process and everything it started.
    public void Detener()
    {
        if (trabajo != IntPtr.Zero) TerminateJobObject(trabajo, 1);
        try { Proceso.Kill(entireProcessTree: true); } catch { /* already finished */ }
    }

    public void Dispose()
    {
        if (trabajo != IntPtr.Zero) { CloseHandle(trabajo); trabajo = IntPtr.Zero; }
        Proceso.Dispose();
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr CreateJobObjectW(IntPtr atributos, IntPtr nombre);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(IntPtr trabajo, IntPtr proceso);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TerminateJobObject(IntPtr trabajo, uint codigo);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr h);
}
