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
// question (Update now / What's new / Later), and then everything happens alone: haul-setup.exe is downloaded and
// checked against the release's SHA256SUMS.txt (a file that does not match is deleted, never run); if a download
// is running, Haul asks whether to stop it now (it comes back from its partial files) or to wait for the queue to
// end; the queue is saved, the setup program runs in silence (/quiet) keeping the installed options, waits for this
// Haul to be gone (/HAULWAITPID) and opens Haul again (/HAULRELAUNCH), which brings the queue back. Windows asks
// for permission, because Haul is installed for all users.
//
// yt-dlp: "Update yt-dlp" in the same window, and once a day by itself (MotorYtDlp). A running download is never
// touched: it finishes with the copy it started with.
public partial class MainWindow
{
    private bool actualizando;
    private string? estadoPie;

    // A short note after the version line at the bottom of the window ("Downloading yt-dlp… 40 %"); null clears it.
    private void MostrarEstado(string? texto)
    {
        estadoPie = texto;
        ActualizarPie();
    }

    private void ActualizarPie()
    {
        var pie = Herramientas.YtDlpActualizado
            ? tr($"Haul {Herramientas.VersionHaul} · Motor: yt-dlp {Herramientas.VersionYtDlp}, actualizado por Haul",
                 $"Haul {Herramientas.VersionHaul} · Engine: yt-dlp {Herramientas.VersionYtDlp}, updated by Haul")
            : tr($"Haul {Herramientas.VersionHaul} · Motor: yt-dlp {Herramientas.VersionYtDlp}, incluido en la app",
                 $"Haul {Herramientas.VersionHaul} · Engine: yt-dlp {Herramientas.VersionYtDlp}, bundled with the app");
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
        if (actualizando) return;
        if (!interactiva && (!Ajustes.Datos.BuscarVersiones || !Toca(Ajustes.Datos.UltimaBusqueda))) return;
        Ajustes.Datos.UltimaBusqueda = DateTime.UtcNow;   // even without network: at most one try a day
        Ajustes.Guardar();
        Actualizacion.Lanzamiento l;
        try
        {
            if (interactiva) MostrarEstado(tr("Consultando GitHub…", "Asking GitHub…"));
            l = await Versiones.UltimoAsync();
        }
        catch (Exception ex)
        {
            MostrarEstado(null);
            if (interactiva) Avisar(tr("No se pudo consultar GitHub: ", "GitHub could not be reached: ") + ex.Message, MessageBoxImage.Warning);
            return;
        }
        MostrarEstado(null);
        if (!Versiones.EsNueva(l) || Versiones.DeEtiqueta(l.Etiqueta) is not { } nueva)
        {
            if (interactiva) Avisar(tr($"Tenés la última versión de Haul ({Versiones.Actual}).", $"You have the latest version of Haul ({Versiones.Actual})."));
            return;
        }
        if (!IsVisible) return;
        var archivos = Versiones.Archivos(l);
        int r = AcercaDe.PreguntarVersion(this, nueva, archivos != null);
        if (r == AcercaDe.Novedades) Herramientas.Abrir(l.Pagina ?? Versiones.Pagina);
        else if (r == AcercaDe.Ahora && archivos is { } a) await InstalarAsync(nueva, a.instalador, a.sumas);
    }

    private async Task InstalarAsync(Version nueva, string urlInstalador, string urlSumas)
    {
        actualizando = true;
        var instalador = Path.Combine(Versiones.CarpetaDescarga, Versiones.Instalador);
        try
        {
            var progreso = new Progress<int>(p => MostrarEstado(p >= 0
                ? tr($"Descargando Haul {nueva}… {p} %", $"Downloading Haul {nueva}… {p} %")
                : tr($"Descargando Haul {nueva}…", $"Downloading Haul {nueva}…")));
            var hash = await Actualizacion.DescargarVerificadoAsync(urlInstalador, urlSumas, Versiones.Instalador, instalador, progreso);

            if (cola.HayTrabajo)
            {
                int r = Pregunta.Hacer(this, tr("Hay descargas en la cola", "There are downloads in the queue"),
                    tr("Si actualizás ahora, la descarga en curso se detiene y sigue sola desde donde quedó cuando Haul vuelva a "
                       + "abrirse, con el resto de la cola. También podés esperar a que la cola termine.",
                       "If you update now, the running download stops and carries on from where it was when Haul opens again, "
                       + "with the rest of the queue. You can also wait for the queue to finish."),
                    [tr("Cancelar", "Cancel"), tr("Cuando termine la cola", "When the queue ends"), tr("Actualizar ahora", "Update now")], 2);
                if (r == 0) { Terminar(null); return; }
                if (r == 1)
                {
                    MostrarEstado(tr($"Haul {nueva} se instala cuando termine la cola", $"Haul {nueva} installs when the queue ends"));
                    while (cola.HayTrabajo)
                    {
                        await Task.Delay(1000);
                        if (!IsLoaded) return;   // closed meanwhile: nothing to install
                    }
                }
            }

            // The file is checked again right before it runs.
            if (!Actualizacion.Verificar(instalador, hash))
                throw new InvalidDataException(tr("El instalador cambió después de descargarlo: se borró sin ejecutarlo.",
                                                  "The setup program changed after it was downloaded: it was deleted without running it."));
            MostrarEstado(tr($"Instalando Haul {nueva}…", $"Installing Haul {nueva}…"));
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
            try { File.Delete(Versiones.ArchivoPendiente); } catch { /* not written */ }
            Terminar(tr("No se pudo actualizar Haul: ", "Haul could not be updated: ") + ex.Message);
        }
    }

    private void Terminar(string? error)
    {
        actualizando = false;
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
            MostrarEstado(tr($"Haul se actualizó a la versión {Versiones.Actual}", $"Haul was updated to version {Versiones.Actual}"));
        else
            Avisar(tr($"Haul {esperada} no se instaló (quizás no se dio el permiso de Windows). Sigue la versión {Versiones.Actual}; "
                      + "podés probar de nuevo desde ⓘ › Buscar actualizaciones.",
                      $"Haul {esperada} was not installed (perhaps Windows' permission was not given). Version {Versiones.Actual} "
                      + "stays; you can try again from ⓘ › Check for updates."), MessageBoxImage.Warning);
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
            var progreso = new Progress<int>(p => MostrarEstado(p >= 0
                ? tr($"Descargando yt-dlp… {p} %", $"Downloading yt-dlp… {p} %") : tr("Descargando yt-dlp…", "Downloading yt-dlp…")));
            var (nueva, version) = await MotorYtDlp.ActualizarAsync(progreso);
            MostrarEstado(nueva ? tr($"yt-dlp actualizado a {version}", $"yt-dlp updated to {version}") : null);
            return nueva
                ? tr($"yt-dlp se actualizó a la versión {version}. Una descarga que ya estaba en curso termina con la versión anterior; las siguientes usan la nueva.",
                     $"yt-dlp was updated to version {version}. A download already running finishes with the previous version; the next ones use the new one.")
                : tr($"yt-dlp ya está en la última versión ({version}).", $"yt-dlp is already at the latest version ({version}).");
        }
        catch (Exception ex)
        {
            MostrarEstado(null);
            return tr($"No se pudo actualizar yt-dlp: {ex.Message} Se sigue usando la versión {Herramientas.VersionYtDlp}.",
                      $"yt-dlp could not be updated: {ex.Message} Version {Herramientas.VersionYtDlp} is still in use.");
        }
    }

    private void Avisar(string texto, MessageBoxImage icono = MessageBoxImage.Information) =>
        MessageBox.Show(this, texto, "Haul", MessageBoxButton.OK, icono);
}
