//
//  MainWindow.Actualizacion.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: November 20, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

using System.Diagnostics;
using System.IO;
using System.Windows;
using Haul.Core;
using static Haul.Core.Loc;

namespace Haul;

// Updates that install themselves, for Haul and for yt-dlp.
//
// Haul: "Check for updates" in the ⓘ window, and once a day by itself. When GitHub has a newer release, one
// question (Update now / What's new / Later), and then everything happens alone: the release's SHA256SUMS.txt must
// carry the author's signature (release.sig, ReleaseSignature.cs; without a valid one nothing is downloaded),
// haul-setup.exe is downloaded and checked against that list (a file that does not match is deleted, never run); if a download
// is running, Haul asks whether to stop it now (it comes back from its partial files) or to wait for the queue to
// end; the queue is saved, the setup program runs in silence (/quiet) keeping the installed options, waits for this
// Haul to be gone (/HAULWAITPID) and opens Haul again (/HAULRELAUNCH), which brings the queue back. Windows asks
// for permission, because Haul is installed for all users.
//
// yt-dlp: "Update yt-dlp" in the same window, and once a day by itself (MotorYtDlp). A running download is never
// touched: it finishes with the copy it started with.
public partial class MainWindow
{
    // Set from the start of a check for a new Haul until it ends: the question, the download, the wait for the queue.
    private bool actualizando;
    private string? estadoPie;
    // The verified setup program, held open without write or delete sharing from its last check until Haul exits, so
    // nothing can replace it between that check and its start.
    private FileStream? instaladorBloqueado;

    // A short note after the version line at the bottom of the window ("Downloading yt-dlp… 40 %"); null clears it.
    private void MostrarEstado(string? texto)
    {
        estadoPie = texto;
        ActualizarPie();
    }

    private void ActualizarPie()
    {
        var pie = T(Herramientas.YtDlpActualizado ? "FooterUpdated" : "FooterBundled", Herramientas.VersionHaul, Herramientas.VersionYtDlp);
        Pie.Text = estadoPie != null ? pie + " · " + estadoPie : pie;
    }

    // A little after the start: the result of an update that just ran, then the daily checks.
    private async void Comprobaciones()
    {
        DespuesDeActualizar();
        await Task.Delay(TimeSpan.FromSeconds(8));
        if (IsVisible) await ActualizarYtDlpAsync(interactiva: false);
        await Task.Delay(TimeSpan.FromSeconds(8));
        if (IsVisible) await BuscarActualizacionAsync(interactiva: false);
    }

    private static bool Toca(DateTime ultima) => DateTime.UtcNow - ultima >= TimeSpan.FromHours(24) || ultima > DateTime.UtcNow;

    // ---- Haul ----

    public async Task BuscarActualizacionAsync(bool interactiva)
    {
        if (actualizando)
        {
            // "Check for updates" pressed while a check, a download or the wait for the queue is under way.
            if (interactiva) Avisar(T("UpdateInProgress"));
            return;
        }
        if (!interactiva && (!Ajustes.Datos.BuscarVersiones || !Toca(Ajustes.Datos.UltimaBusqueda))) return;
        actualizando = true;
        try
        {
            Ajustes.Datos.UltimaBusqueda = DateTime.UtcNow;   // even without network: at most one try a day
            Ajustes.Guardar();
            Actualizacion.Lanzamiento l;
            try
            {
                if (interactiva) MostrarEstado(T("AskingGitHub"));
                l = await Versiones.UltimoAsync();
            }
            catch (Exception ex)
            {
                MostrarEstado(null);
                if (interactiva) Avisar(T("GitHubUnreachable", ex.Message), MessageBoxImage.Warning);
                return;
            }
            MostrarEstado(null);
            if (!Versiones.EsNueva(l) || Versiones.DeEtiqueta(l.Etiqueta) is not { } nueva)
            {
                if (interactiva) Avisar(T("LatestHaul", Versiones.Actual));
                return;
            }
            if (!IsVisible) return;
            var archivos = Versiones.Archivos(l);
            int r = AcercaDe.PreguntarVersion(this, nueva, archivos != null);
            if (r == AcercaDe.Novedades) Herramientas.Abrir(l.Pagina ?? Versiones.Pagina);
            else if (r == AcercaDe.Ahora && archivos is { } a) await InstalarAsync(nueva, a.instalador, a.sumas, a.firma);
        }
        finally
        {
            actualizando = false;
        }
    }

    private async Task InstalarAsync(Version nueva, string urlInstalador, string urlSumas, string urlFirma)
    {
        var instalador = Path.Combine(Versiones.CarpetaDescarga, Versiones.Instalador);
        try
        {
            var progreso = new Progress<int>(p => MostrarEstado(p >= 0 ? T("DownloadingHaulPercent", nueva, p) : T("DownloadingHaul", nueva)));
            var hash = await Actualizacion.DescargarVerificadoAsync(urlInstalador, urlSumas, Versiones.Instalador, instalador, progreso, urlFirma);

            if (cola.HayTrabajo)
            {
                int r = Pregunta.Hacer(this, T("QueueBusyTitle"), T("QueueBusyText"),
                                       [T("Cancel"), T("WhenQueueEnds"), T("UpdateNow")], 2);
                if (r == 0) { Terminar(null); return; }
                if (r == 1)
                {
                    MostrarEstado(T("InstallsWhenQueueEnds", nueva));
                    while (cola.HayTrabajo)
                    {
                        await Task.Delay(1000);
                        if (!IsLoaded) return;   // closed meanwhile: nothing to install
                    }
                }
            }

            // The file is checked again right before it runs, and stays open without write or delete sharing until Haul
            // exits: nothing can change it between this check and the start of the setup program.
            instaladorBloqueado = Actualizacion.AbrirVerificado(instalador, hash)
                                  ?? throw new InvalidDataException(T("SetupChanged"));
            MostrarEstado(T("InstallingHaul", nueva));
            Directory.CreateDirectory(Ajustes.Carpeta);
            File.WriteAllText(Versiones.ArchivoPendiente, nueva.ToString());
            var psi = new ProcessStartInfo(instalador) { UseShellExecute = true };
            foreach (var a in new[] { "/quiet", "/norestart", "/HAULWAITPID=" + Environment.ProcessId, "/HAULRELAUNCH=1" })
                psi.ArgumentList.Add(a);
            Process.Start(psi);
            // Closing saves the queue (Cola.Cerrar) and stops yt-dlp; the setup program waits for this process to end.
            Close();
        }
        catch (Exception ex)
        {
            instaladorBloqueado?.Dispose();
            instaladorBloqueado = null;
            try { File.Delete(Versiones.ArchivoPendiente); } catch { /* not written */ }
            Terminar(T("HaulNotUpdated", ex.Message));
        }
    }

    private void Terminar(string? error)
    {
        MostrarEstado(null);
        if (error != null) Avisar(error, MessageBoxImage.Warning);
    }

    // The setup program opened Haul again (or Haul was opened by hand after an update): says whether the version it
    // was going to install is the one running now.
    private void DespuesDeActualizar()
    {
        string? texto;
        try
        {
            if (!File.Exists(Versiones.ArchivoPendiente)) return;
            texto = File.ReadAllText(Versiones.ArchivoPendiente).Trim();
            File.Delete(Versiones.ArchivoPendiente);
        }
        catch { return; }
        if (Versiones.DeEtiqueta(texto) is not { } esperada) return;
        if (Versiones.Actual >= esperada)
            MostrarEstado(T("HaulUpdatedTo", Versiones.Actual));
        else
            Avisar(T("HaulNotInstalled", esperada, Versiones.Actual), MessageBoxImage.Warning);
    }

    // ---- yt-dlp ----

    // Updates yt-dlp if a newer release exists. Returns a sentence about the result for the ⓘ window; the daily
    // check (not interactive) only leaves a note at the bottom of the window, and only when something changed.
    public async Task<string?> ActualizarYtDlpAsync(bool interactiva)
    {
        if (!interactiva && (!Ajustes.Datos.ActualizarYtDlp || !Toca(Ajustes.Datos.UltimaBusquedaYtDlp))) return null;
        Ajustes.Datos.UltimaBusquedaYtDlp = DateTime.UtcNow;
        Ajustes.Guardar();
        try
        {
            var progreso = new Progress<int>(p => MostrarEstado(p >= 0 ? T("DownloadingYtDlpPercent", p) : T("DownloadingYtDlp")));
            var (nueva, version) = await MotorYtDlp.ActualizarAsync(progreso);
            MostrarEstado(nueva ? T("YtDlpUpdatedShort", version) : null);
            return T(nueva ? "YtDlpUpdated" : "YtDlpLatest", version);
        }
        catch (Exception ex)
        {
            MostrarEstado(null);
            return T("YtDlpNotUpdated", ex.Message, Herramientas.VersionYtDlp);
        }
    }

    private void Avisar(string texto, MessageBoxImage icono = MessageBoxImage.Information) =>
        MessageBox.Show(this, texto, "Haul", MessageBoxButton.OK, icono);
}
