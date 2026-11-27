//
//  PruebaPropia.cs
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

using System.IO;
using System.Text;
using Haul.Core;

namespace Haul;

// Haul.exe --self-test <log> [--red]: checks the interface texts of Haul and of its setup window (three tables with
// the same keys and placeholders), the saved queue (save, load, damaged file), moving rows, the version logic of Haul
// and of yt-dlp, the checksums and the release signature, playlist folder names, the arguments every yt-dlp
// process starts with and the end of a process that leaves another one behind, with throw-away data in a temporary folder that is removed at the end. No
// window is shown; the user's settings, queue and copies of yt-dlp are never read or written (the settings are
// read-only and every path points into the temporary folder). The exit code is the number of failures. "--red"
// adds the checks that ask GitHub for the latest releases and update yt-dlp for real, into the temporary folder;
// without it nothing goes to the network.
public static class PruebaPropia
{
    private static readonly StringBuilder registro = new();
    private static int fallas, pasadas;

    public static int Ejecutar(string archivoRegistro, bool red)
    {
        var raiz = Path.Combine(Path.GetTempPath(), "haul-self-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(raiz);
        // Belt and braces: whatever runs here, the user's folders are out of reach.
        Ajustes.Carpeta = Path.Combine(raiz, "config");
        MotorYtDlp.Raiz = Path.Combine(raiz, "yt-dlp-in-use");
        Linea($"Haul {Herramientas.VersionHaul} self-test, {DateTime.Now:yyyy-MM-dd HH:mm:ss}; yt-dlp included: {Herramientas.VersionYtDlpIncluido}");
        try
        {
            Bloque("Interface texts (Haul)", () => Tablas(Textos.En, Textos.Es, Textos.Ja));
            Bloque("Interface texts (setup window)", () => Tablas(Haul.Setup.SetupTexts.En, Haul.Setup.SetupTexts.Es, Haul.Setup.SetupTexts.Ja));
            Bloque("Interface language", Idiomas);
            Bloque("Versions (IsNewer)", Versiones_);
            Bloque("Checksums (HashFor)", Sumas_);
            Bloque("Release of Haul", LanzamientoHaul);
            Bloque("Copies of yt-dlp", () => CopiasYtDlp(Path.Combine(raiz, "yt-dlp")));
            Bloque("Verified file", () => Verificado(Path.Combine(raiz, "verificar")));
            Bloque("Saved queue: file", () => ArchivoCola(Path.Combine(raiz, "cola")));
            Bloque("Saved queue: rows", () => FilasCola(Path.Combine(raiz, "filas")));
            Bloque("Folder names and yt-dlp arguments", NombresYArgumentos);
            Bloque("Rows: failing, moving, error colour", Filas);
            Bloque("Process that leaves another one behind", Rezagado);
            if (red) Bloque("GitHub (network)", () => Red(Path.Combine(raiz, "red")));
            else Linea("SKIP GitHub (network): run with --red to include it");
        }
        catch (Exception ex)
        {
            Fallo("unexpected: " + ex);
        }
        finally
        {
            try { Directory.Delete(raiz, true); } catch (Exception ex) { Fallo("temporary folder not removed: " + ex.Message); }
            Comprobar("temporary folder removed", !Directory.Exists(raiz));
        }
        Linea($"RESULT: {pasadas} passed, {fallas} failed");
        try { File.WriteAllText(archivoRegistro, registro.ToString()); } catch { /* the exit code still tells */ }
        return fallas;
    }

    // ---- Checks ----

    // The three tables of a program: the same keys, no empty text, and for every key the same placeholders.
    private static void Tablas(IReadOnlyDictionary<string, string> en, IReadOnlyDictionary<string, string> es, IReadOnlyDictionary<string, string> ja)
    {
        static string Faltan(IReadOnlyDictionary<string, string> a, IReadOnlyDictionary<string, string> b) =>
            string.Join(", ", a.Keys.Where(k => !b.ContainsKey(k)));
        Comprobar($"{en.Count} texts in English, none empty", en.Count > 0 && en.Values.All(t => t.Length > 0));
        foreach (var (nombre, otra) in new[] { ("Spanish", es), ("Japanese", ja) })
        {
            var faltan = Faltan(en, otra);
            var sobran = Faltan(otra, en);
            Comprobar($"{nombre}: the same keys as English" + (faltan.Length > 0 ? "; missing: " + faltan : "") + (sobran.Length > 0 ? "; extra: " + sobran : ""),
                      faltan.Length == 0 && sobran.Length == 0);
            var distintos = en.Keys.Where(k => otra.TryGetValue(k, out var t) && Marcadores(t) != Marcadores(en[k])).ToList();
            Comprobar($"{nombre}: the same placeholders as English" + (distintos.Count > 0 ? "; differ: " + string.Join(", ", distintos) : ""),
                      distintos.Count == 0);
            Comprobar($"{nombre}: no empty text", otra.Values.All(t => t.Length > 0));
        }
        var malos = en.Concat(es).Concat(ja).Where(p => !FormatoValido(p.Value)).Select(p => p.Key).Distinct().ToList();
        Comprobar("every text is a valid format string" + (malos.Count > 0 ? "; not: " + string.Join(", ", malos) : ""), malos.Count == 0);
    }

    // The placeholders of a text, sorted ("{0}{1}").
    private static string Marcadores(string t) =>
        string.Concat(System.Text.RegularExpressions.Regex.Matches(t, @"\{\d+(?:[,:][^}]*)?\}").Select(m => m.Value).Order());

    private static bool FormatoValido(string t)
    {
        try { _ = string.Format(t, "a", "b", "c", "d"); return true; }
        catch (FormatException) { return false; }
    }

    private static void Idiomas()
    {
        var antes = Loc.Actual;
        try
        {
            Comprobar("codes: en, es, ja; anything else is English", Loc.DesdeCodigo("ja") == Idioma.Ja && Loc.DesdeCodigo("ES") == Idioma.Es
                      && Loc.DesdeCodigo("en") == Idioma.En && Loc.DesdeCodigo("fr") == Idioma.En && Loc.DesdeCodigo(null) == Idioma.En
                      && Loc.Codigo(Idioma.Ja) == "ja" && Loc.Codigo(Idioma.Es) == "es" && Loc.Codigo(Idioma.En) == "en");
            Loc.Actual = Idioma.Ja;
            Comprobar("Japanese texts", Loc.T("Queue") == "キュー" && Loc.T("SummaryDone", 2, 5) == "5 件中 2 件完了");
            Loc.Actual = Idioma.Es;
            Comprobar("Spanish texts", Loc.T("Queue") == "Cola" && Loc.T("SummaryDone", 2, 5) == "2 de 5 listas");
            Loc.Actual = Idioma.En;
            Comprobar("English texts", Loc.T("Queue") == "Queue" && Loc.T("SummaryDone", 2, 5) == "2 of 5 done");
            Comprobar("an unknown key shows itself", Loc.T("No such key") == "No such key");
        }
        finally { Loc.Actual = antes; }
    }

    private static void Filas()
    {
        // An error line on the last attempt leaves the row running: it turns red only when yt-dlp exits, so it cannot
        // be removed or retried while yt-dlp still runs.
        var e = new Elemento("https://www.example.com/watch?v=x", Formato.Video, Path.GetTempPath()) { Reintentos = Elemento.MaxReintentos };
        e.Etapa = Etapa.Bajando;
        e.Procesar("ERROR: [site] x: Video unavailable");
        Comprobar("an error line does not end a running row", e.Etapa == Etapa.Bajando && e.Fallo == "Video unavailable"
                  && !e.PuedeQuitar && !e.PuedeReintentar && e.PuedeCancelar);

        var cola = new Cola();
        var carpeta = Path.GetTempPath();
        cola.Restaurar(new EstadoCola { Filas = [new() { Enlace = "https://www.example.com/watch?v=1", Destino = carpeta },
                                                 new() { Enlace = "https://www.example.com/watch?v=2", Destino = carpeta },
                                                 new() { Enlace = "https://www.example.com/watch?v=3", Destino = carpeta }] }, arrancar: false);
        var f = cola.Elementos;
        string Orden() => string.Concat(f.Select(x => x.Enlace[^1]));
        var primera = f[0];
        Comprobar("Alt+Down: one place down", cola.Desplazar(primera, 1) && Orden() == "213");
        Comprobar("Alt+Up: one place up", cola.Desplazar(primera, -1) && Orden() == "123");
        Comprobar("nothing moves past either end", !cola.Desplazar(primera, -1) && !cola.Desplazar(f[2], 1) && Orden() == "123");

        // The red of a failed row against the cards and the window, light and dark (WCAG AA for text: 4.5:1).
        double c1 = Tema.Contraste(Tema.ErrorClaro, 0xFFFFFF), c2 = Tema.Contraste(Tema.ErrorClaro, 0xF3F3F3), c3 = Tema.Contraste(Tema.ErrorClaro, 0xFBFBFB);
        Comprobar($"error red, light window: {c1:0.0}:1 on white, {c3:0.0}:1 on a card, {c2:0.0}:1 on the window", c1 >= 4.5 && c2 >= 4.5 && c3 >= 4.5);
        double d1 = Tema.Contraste(Tema.ErrorOscuro, 0x2B2B2B), d2 = Tema.Contraste(Tema.ErrorOscuro, 0x202020);
        Comprobar($"error red, dark window: {d1:0.0}:1 on a card, {d2:0.0}:1 on the window", d1 >= 4.5 && d2 >= 4.5);
    }

    // A process that exits while a child it started keeps its output open (as ffmpeg left behind by yt-dlp could):
    // the wait ends after the grace time, off the calling thread, and the child is stopped with it.
    private static void Rezagado()
    {
        var antes = ProcesoVigilado.Gracia;
        ProcesoVigilado.Gracia = TimeSpan.FromSeconds(2);
        try
        {
            var cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            System.Diagnostics.ProcessStartInfo Inicio(string orden)
            {
                // cmd reads its command line as it is, not with the quoting of ArgumentList.
                return new System.Diagnostics.ProcessStartInfo(cmd, "/d /c " + orden)
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
                };
            }
            // "start /b" runs ping inside the same console, with the same output handles, and cmd exits at once.
            using var p = new ProcesoVigilado(Inicio("start \"\" /b \"" + Path.Combine(Environment.SystemDirectory, "PING.EXE") + "\" -n 60 127.0.0.1"), _ => { });
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            p.Iniciar();
            var espera = Task.Run(p.EsperarAsync);
            // Never longer than half a minute, whatever goes wrong.
            if (!espera.Wait(TimeSpan.FromSeconds(30))) { p.Detener(); Fallo("the process did not end"); return; }
            var (codigo, detenido) = espera.Result;
            reloj.Stop();
            Comprobar($"the wait ended after the grace time ({reloj.Elapsed.TotalSeconds:0.0} s), stopping what was left",
                      detenido && reloj.Elapsed < TimeSpan.FromSeconds(15));
            Comprobar("exit code of the process itself", codigo == 0);

            // A process that ends normally is not stopped and nothing waits for the grace time.
            var lineas = new List<string>();
            using var q = new ProcesoVigilado(Inicio("echo one& echo two& exit 3"), l => { lock (lineas) lineas.Add(l.Trim()); });
            reloj.Restart();
            q.Iniciar();
            var espera2 = Task.Run(q.EsperarAsync);
            if (!espera2.Wait(TimeSpan.FromSeconds(30))) { q.Detener(); Fallo("a normal process did not end"); return; }
            var (codigo2, detenido2) = espera2.Result;
            Comprobar("a normal end: its exit code and every line, no stop, no wait",
                      codigo2 == 3 && !detenido2 && lineas.SequenceEqual(["one", "two"]) && reloj.Elapsed < TimeSpan.FromSeconds(2));
        }
        finally { ProcesoVigilado.Gracia = antes; }
    }

    private static void Versiones_()
    {
        Comprobar("1.7.0 > 1.6.0", Actualizacion.EsMasNueva("v1.7.0", "1.6.0"));
        Comprobar("1.6.0 = 1.6.0 is not newer", !Actualizacion.EsMasNueva("1.6.0", "1.6.0"));
        Comprobar("1.6 = 1.6.0 is not newer", !Actualizacion.EsMasNueva("1.6", "1.6.0"));
        Comprobar("1.6.0 = 1.6 is not newer", !Actualizacion.EsMasNueva("v1.6.0", "1.6"));
        Comprobar("1.5.9 < 1.6.0", !Actualizacion.EsMasNueva("1.5.9", "1.6.0"));
        Comprobar("1.10.0 > 1.9.0", Actualizacion.EsMasNueva("V1.10.0", "1.9.0"));
        Comprobar("garbage is never newer", !Actualizacion.EsMasNueva("latest", "1.6.0") && !Actualizacion.EsMasNueva("", "1.0") && !Actualizacion.EsMasNueva(null, "1.0"));
        Comprobar("unknown own version: nothing is newer", !Actualizacion.EsMasNueva("9.9.9", "?"));
        Comprobar("yt-dlp 2026.09.30 > 2026.08.19", Actualizacion.EsMasNueva("2026.09.30", "2026.08.19"));
        Comprobar("yt-dlp 2026.08.19.1 > 2026.08.19", Actualizacion.EsMasNueva("2026.08.19.1", "2026.08.19"));
        Comprobar("yt-dlp 2026.08.19 = 2026.8.19", !Actualizacion.EsMasNueva("2026.08.19", "2026.8.19"));
        Comprobar("yt-dlp 2025.12.31 < 2026.01.01", !Actualizacion.EsMasNueva("2025.12.31", "2026.01.01"));
        Comprobar("yt-dlp tag accepted", MotorYtDlp.EsEtiqueta("2026.09.30") && MotorYtDlp.EsEtiqueta("2026.09.30.1"));
        Comprobar("yt-dlp tag refused", !MotorYtDlp.EsEtiqueta("..\\2026") && !MotorYtDlp.EsEtiqueta("nightly") && !MotorYtDlp.EsEtiqueta("2026.09.30/x"));
        Comprobar("running version is major.minor.patch", Versiones.Actual.ToString() == Herramientas.VersionHaul && Versiones.Actual.Build >= 0);
        Comprobar("v1.7.0 tag → 1.7.0", Versiones.DeEtiqueta("v1.7.0") == new Version(1, 7, 0));
        Comprobar("included yt-dlp version is a yt-dlp tag", MotorYtDlp.EsEtiqueta(Herramientas.VersionYtDlpIncluido));
    }

    private static void Sumas_()
    {
        string h1 = new('a', 64), h2 = "0123456789ABCDEF0123456789abcdef0123456789abcdef0123456789ABCDEF";
        var sumas = $"{h1}  haul.msi\r\n{h2} *haul-setup.exe\n{new string('b', 63)}  short.exe\n{new string('z', 64)}  nothex.exe\n"
                    + $"{new string('c', 64)}\tyt-dlp.exe\n\n";
        Comprobar("two spaces", Actualizacion.HashDe(sumas, "haul.msi") == h1);
        Comprobar("binary marker and upper case → lower case", Actualizacion.HashDe(sumas, "haul-setup.exe") == h2.ToLowerInvariant());
        Comprobar("name is case-insensitive", Actualizacion.HashDe(sumas, "HAUL-SETUP.EXE") == h2.ToLowerInvariant());
        Comprobar("tab separator", Actualizacion.HashDe(sumas, "yt-dlp.exe") == new string('c', 64));
        Comprobar("short hash ignored", Actualizacion.HashDe(sumas, "short.exe") == null);
        Comprobar("non-hex hash ignored", Actualizacion.HashDe(sumas, "nothex.exe") == null);
        Comprobar("missing name", Actualizacion.HashDe(sumas, "other.exe") == null);
        Comprobar("partial name does not match", Actualizacion.HashDe(sumas, "setup.exe") == null);
        Comprobar("empty list", Actualizacion.HashDe("", "haul.msi") == null);
    }

    private static void LanzamientoHaul()
    {
        const string json = """
            {"tag_name":"v9.8.0","html_url":"https://github.com/Gabriel-Castiglia/haul-windows/releases/tag/v9.8.0",
             "assets":[{"name":"haul-setup.exe","browser_download_url":"https://example.invalid/haul-setup.exe"},
                       {"name":"haul.msi","browser_download_url":"https://example.invalid/haul.msi"},
                       {"name":"SHA256SUMS.txt","browser_download_url":"https://example.invalid/SHA256SUMS.txt"},
                       {"name":"release.sig","browser_download_url":"https://example.invalid/release.sig"},
                       {"name":"Haul-manual-en.pdf","browser_download_url":"https://example.invalid/m.pdf"}]}
            """;
        var l = Actualizacion.Leer(json);
        Comprobar("tag read", l.Etiqueta == "v9.8.0");
        Comprobar("page read", l.Pagina?.EndsWith("/v9.8.0") == true);
        Comprobar("5 files read", l.Archivos.Count == 5);
        Comprobar("newer than the running Haul", Versiones.EsNueva(l));
        var a = Versiones.Archivos(l);
        Comprobar("setup, checksums and signature chosen", a is { } x && x.instalador.EndsWith("/haul-setup.exe") && x.sumas.EndsWith("/SHA256SUMS.txt")
                  && x.firma.EndsWith("/release.sig"));
        var sinFirma = Actualizacion.Leer(json.Replace("release.sig", "release.txt"));
        Comprobar("without the author's signature nothing is installable", Versiones.Archivos(sinFirma) == null);
        // release.sig: real signatures by the author's two keys (the PC's, the Mac's) of the same bytes; any change is refused
        var firmado = System.Text.Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef0123456789abcdef0123456789ABCDEF  Vigia-Setup-9.9.9.exe\n"
                                                         + "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff *Vigia_Manual_EN.pdf\n");
        const string firmaPc = "rEEKYRSXLO7VKbID/El7ZkFBh1uKIPNS38KopNSZ6DEm7knfIUjxch9kmv8xfREc6bLKka+bePFZ1BKzO+15ww==";
        const string firmaMac = "K8+3h6FgPjn2ZyH3HsLWof4obfKfyW00p3YKE9WQFoEF+umfN6/OMxMQNrQbc3XqkKrZSz3QexCBIZEMrdrvKg==";
        var cambiado = (byte[])firmado.Clone(); cambiado[0] ^= 1;
        Comprobar("release signature: either key, exactly those bytes", ReleaseSignature.Valid(firmado, firmaPc + "\n") && ReleaseSignature.Valid(firmado, firmaMac)
                  && !ReleaseSignature.Valid(cambiado, firmaPc) && !ReleaseSignature.Valid(cambiado, firmaMac)
                  && !ReleaseSignature.Valid(firmado, "") && !ReleaseSignature.Valid(firmado, null) && !ReleaseSignature.Valid(firmado, "not base64!"));
        var sinSumas = Actualizacion.Leer("""{"tag_name":"v9.8.0","assets":[{"name":"haul-setup.exe","browser_download_url":"https://example.invalid/s.exe"}]}""");
        Comprobar("without SHA256SUMS.txt nothing is installable", Versiones.Archivos(sinSumas) == null && sinSumas.Pagina == null);
        var vieja = Actualizacion.Leer("""{"tag_name":"v1.0.0","assets":[]}""");
        Comprobar("an older release is not offered", !Versiones.EsNueva(vieja));
    }

    private static void CopiasYtDlp(string raiz)
    {
        string incluida = "2026.08.19";
        Comprobar("no folder: the included copy", MotorYtDlp.Elegir(raiz, incluida) == null);
        // A folder named after a version exists only once its copy passed every check (MotorYtDlp.InstalarAsync).
        void Copia(string nombre, bool exe)
        {
            var c = Path.Combine(raiz, nombre);
            Directory.CreateDirectory(c);
            if (exe) File.WriteAllText(Path.Combine(c, "yt-dlp.exe"), "test");
        }
        Copia("2026.07.01", true);             // older than the included one
        Copia("2026.09.30", true);             // the newest verified
        Copia("2026.11.01", false);            // no executable
        Copia("2026.12.01.download", true);    // a download that did not finish
        Copia("notes", true);                  // not a version
        var e = MotorYtDlp.Elegir(raiz, incluida);
        Comprobar("newest verified copy chosen", e is { } x && x.version == "2026.09.30" && x.ruta == Path.Combine(raiz, "2026.09.30", "yt-dlp.exe"));
        Comprobar("included newer than every copy: the included one", MotorYtDlp.Elegir(raiz, "2027.01.01") == null);
        MotorYtDlp.Limpiar(raiz, e?.ruta);
        var quedan = Directory.GetDirectories(raiz).Select(Path.GetFileName).ToList();
        Comprobar("cleaning keeps only the copy in use", quedan.Count == 1 && quedan[0] == "2026.09.30");
        MotorYtDlp.Limpiar(raiz, null);
        Comprobar("cleaning without a copy in use removes everything", Directory.GetDirectories(raiz).Length == 0);
    }

    private static void Verificado(string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        var f = Path.Combine(carpeta, "file.bin");
        File.WriteAllText(f, "abc");
        const string abc = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
        Comprobar("SHA-256 of \"abc\"", Actualizacion.Sha256(f) == abc);
        Comprobar("matching file kept", Actualizacion.Verificar(f, abc.ToUpperInvariant()) && File.Exists(f));
        Comprobar("mismatching file deleted", !Actualizacion.Verificar(f, new string('0', 64)) && !File.Exists(f));
        Comprobar("missing file is not verified", !Actualizacion.Verificar(f, abc));

        // The setup program of an update: checked and held open until it runs.
        var exe = Path.Combine(carpeta, "whoami.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "whoami.exe"), exe);
        var esperado = Actualizacion.Sha256(exe);
        using (var bloqueado = Actualizacion.AbrirVerificado(exe, esperado))
        {
            Comprobar("verified file held open", bloqueado != null && bloqueado.Position == 0);
            bool escribe = true, borra = true, renombra = true;
            try { using var w = new FileStream(exe, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete); } catch (IOException) { escribe = false; }
            try { File.Delete(exe); } catch (IOException) { borra = false; } catch (UnauthorizedAccessException) { borra = false; }
            try { File.Move(exe, exe + ".old"); } catch (IOException) { renombra = false; } catch (UnauthorizedAccessException) { renombra = false; }
            Comprobar("while held: it cannot be written, deleted or renamed", !escribe && !borra && !renombra && File.Exists(exe));
            var psi = new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
            using var p = System.Diagnostics.Process.Start(psi)!;
            p.StandardOutput.ReadToEnd();
            Comprobar("while held: it can still run", p.WaitForExit(30000) && p.ExitCode == 0);
        }
        Comprobar("released: it can be deleted", Borrar(exe));
        File.WriteAllText(f, "abc");
        Comprobar("a file that does not match is not held and is deleted", Actualizacion.AbrirVerificado(f, new string('0', 64)) == null && !File.Exists(f));
    }

    private static bool Borrar(string ruta)
    {
        try { File.Delete(ruta); return !File.Exists(ruta); }
        catch { return false; }
    }

    private static void ArchivoCola(string carpeta)
    {
        var archivo = Path.Combine(carpeta, "queue.json");
        var vacia = ColaGuardada.Cargar(archivo, out var danado);
        Comprobar("missing file: empty, not damaged", vacia.Vacio && !danado);

        var estado = new EstadoCola
        {
            EnPausa = true,
            Filas = [new FilaGuardada { Enlace = "https://www.example.com/watch?v=1", Destino = carpeta, Titulo = "Ejemplo «uno» 日本", Etapa = "Detenida", Progreso = 0.5 },
                     new FilaGuardada { Enlace = "notalink", Destino = carpeta }],
            Enlaces = ["https://www.example.com/watch?v=2", "file:///C:/x"],
        };
        ColaGuardada.Guardar(estado, archivo);
        Comprobar("saved", File.Exists(archivo));
        Comprobar("no temporary file left", !File.Exists(archivo + ".tmp"));
        var leida = ColaGuardada.Cargar(archivo, out danado);
        Comprobar("loaded, not damaged", !danado);
        Comprobar("paused flag kept", leida.EnPausa);
        Comprobar("only web links come back", leida.Filas.Count == 1 && leida.Enlaces.Count == 1 && leida.Enlaces[0].EndsWith("v=2"));
        Comprobar("text kept exactly", leida.Filas[0].Titulo == "Ejemplo «uno» 日本" && leida.Filas[0].Progreso == 0.5);

        // A save that died half-way leaves its temporary file; the queue in place is the previous one.
        File.WriteAllText(archivo + ".tmp", "{ half");
        leida = ColaGuardada.Cargar(archivo, out danado);
        Comprobar("stale temporary file ignored and removed", !danado && leida.Filas.Count == 1 && !File.Exists(archivo + ".tmp"));

        // Replacing an existing file.
        estado.Filas.Add(new FilaGuardada { Enlace = "https://www.example.com/watch?v=3", Destino = carpeta });
        ColaGuardada.Guardar(estado, archivo);
        Comprobar("existing file replaced", ColaGuardada.Cargar(archivo, out _).Filas.Count == 2);

        foreach (var (nombre, contenido) in new[] { ("truncated", "{\"version\":1,\"filas\":[{\"enlace\":\"https://www.ex"),
                                                    ("not JSON", "\0\0\0garbage"), ("wrong type", "{\"filas\": 5}"), ("JSON null", "null") })
        {
            File.WriteAllText(archivo, contenido);
            var d = ColaGuardada.Cargar(archivo, out danado);
            var aparte = Path.Combine(carpeta, "queue.damaged.json");
            Comprobar($"damaged file ({nombre}): empty queue, set aside, original gone",
                      danado && d.Vacio && File.Exists(aparte) && File.ReadAllText(aparte) == contenido && !File.Exists(archivo));
        }

        ColaGuardada.Guardar(estado, archivo);
        ColaGuardada.Guardar(new EstadoCola(), archivo);
        Comprobar("an empty queue removes the file", !File.Exists(archivo));
    }

    private static void FilasCola(string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        var archivo = Path.Combine(carpeta, "queue.json");
        var parcial = Path.Combine(carpeta, "Video A");
        var estado = new EstadoCola
        {
            Filas =
            [
                new() { Enlace = "https://www.example.com/watch?v=a", Destino = carpeta, Titulo = "A", Etapa = "Bajando",
                        BaseArchivo = parcial, Previos = [parcial + ".mp4"] },
                new() { Enlace = "https://www.example.com/watch?v=b", Destino = carpeta, Formato = "audio", Etapa = "EnCola",
                        Prefijo = "02", NombreLista = "Lista", Posicion = "2/9" },
                new() { Enlace = "https://www.example.com/watch?v=c", Destino = carpeta, Etapa = "Detenida", Progreso = 0.37 },
                new() { Enlace = "https://www.example.com/watch?v=d", Destino = carpeta, Etapa = "Pausada", Instrumental = true },
                new() { Enlace = "https://www.example.com/watch?v=e", Destino = carpeta, Etapa = "Fallo", Fallo = "ERROR: test" },
                new() { Enlace = "https://www.example.com/playlist?list=f", Destino = carpeta, Etapa = "Leyendo", EsLectura = true, ListaEntera = true },
                new() { Enlace = "https://www.example.com/watch?v=g", Destino = carpeta, Etapa = "Lista" },
                new() { Enlace = "https://www.example.com/watch?v=h", Destino = carpeta, Etapa = "Cancelada" },
                new() { Enlace = "https://www.example.com/watch?v=i", Destino = carpeta, Etapa = "Nonsense" },
            ],
            Enlaces = ["https://www.example.com/watch?v=j"],
        };

        // Rows put back without starting anything (no yt-dlp runs in the self-test).
        var cola = new Cola();
        cola.Restaurar(estado, arrancar: false);
        var e = cola.Elementos;
        Comprobar("rows restored in order, without finished or cancelled ones", e.Count == 7 && e[0].Enlace.EndsWith("v=a") && e[6].Enlace.EndsWith("v=i"));
        Comprobar("interrupted download comes back queued", e[0].Etapa == Etapa.EnCola);
        Comprobar("paused by the user stays paused, with its progress", e[2].Etapa == Etapa.Detenida && Math.Abs(e[2].Progreso - 0.37) < 1e-9);
        Comprobar("held instrumental stays held", e[3].Etapa == Etapa.Pausada && e[3].Instrumental);
        Comprobar("failed stays failed, with its message", e[4].Etapa == Etapa.Fallo && e[4].Fallo == "ERROR: test");
        Comprobar("playlist being read comes back to be read", e[5].Etapa == Etapa.EnCola && e[5].EsLectura && e[5].ListaEntera);
        Comprobar("playlist row keeps its place", e[1].Prefijo == "02" && e[1].Ubicacion == "Lista · 2/9" && e[1].Formato == Formato.Audio);
        Comprobar("unknown stage comes back queued", e[6].Etapa == Etapa.EnCola);

        // A download that finishes and one that is cancelled are not kept.
        e[1].Terminar();
        e[3].Etapa = Etapa.Cancelada;
        var guardado = cola.Estado();
        Comprobar("finished and cancelled rows are not kept", guardado.Filas.Count == 5
                  && !guardado.Filas.Any(f => f.Enlace.EndsWith("v=b") || f.Enlace.EndsWith("v=d")));
        Comprobar("waiting links kept", guardado.Enlaces.SequenceEqual(estado.Enlaces));
        Comprobar("partial files of the interrupted download kept", guardado.Filas[0].BaseArchivo == parcial
                  && guardado.Filas[0].Previos?.SequenceEqual([parcial + ".mp4"]) == true);

        // Through a file and into a second queue: the same rows again.
        ColaGuardada.Guardar(guardado, archivo);
        var otra = new Cola();
        otra.Restaurar(ColaGuardada.Cargar(archivo, out var danado), arrancar: false);
        var dos = otra.Estado();
        Comprobar("round trip through the file", !danado && ColaGuardada.Serializar(dos) == ColaGuardada.Serializar(guardado));

        // The queue a user leaves while a download runs, after pausing the whole queue.
        var pausada = new Cola();
        pausada.Restaurar(new EstadoCola { EnPausa = true, Filas = [estado.Filas[1]] }, arrancar: false);
        Comprobar("whole-queue pause kept", pausada.EnPausa && pausada.Estado().EnPausa && !pausada.HayTrabajo);
        Comprobar("queued work is work", otra.HayTrabajo);

        // Opening a damaged file through the queue itself.
        File.WriteAllText(archivo, "{");
        var rota = new Cola();
        Comprobar("damaged file opens as an empty queue", !rota.Abrir(archivo, arrancar: false) && rota.Elementos.Count == 0);
        rota.Cerrar();
        Comprobar("closing an empty queue writes nothing", !File.Exists(archivo));

        // Closing saves what is there, at once.
        var abierta = new Cola();
        abierta.Abrir(Path.Combine(carpeta, "nuevo.json"), arrancar: false);
        abierta.Restaurar(new EstadoCola { Filas = [estado.Filas[2]] }, arrancar: false);
        abierta.Cerrar();
        var cerrada = ColaGuardada.Cargar(Path.Combine(carpeta, "nuevo.json"), out _);
        Comprobar("closing saves the queue", cerrada.Filas.Count == 1 && cerrada.Filas[0].Etapa == "Detenida");
    }

    private static void NombresYArgumentos()
    {
        Comprobar("playlist folder: invalid characters replaced", Cola.NombreDeCarpeta("a/b:c?") == "a_b_c_");
        Comprobar("playlist folder: trailing dots and spaces dropped", Cola.NombreDeCarpeta("Mix 2026. . ") == "Mix 2026");
        Comprobar("playlist folder: reserved device names avoided",
                  Cola.NombreDeCarpeta("nul") != "nul" && Cola.NombreDeCarpeta("COM9") != "COM9" && Cola.NombreDeCarpeta("lpt5.mix") != "lpt5.mix"
                  && Cola.NombreDeCarpeta("") != "" && Cola.NombreDeCarpeta("Console") == "Console");
        var psi = Herramientas.Preparar(["--", "https://www.example.com/watch?v=-x"]);
        Comprobar("yt-dlp never reads a configuration file", psi.ArgumentList.Count == 6 && psi.ArgumentList[0] == "--ignore-config");
        Comprobar("yt-dlp loads only Haul's plugins", psi.ArgumentList[1] == "--no-plugin-dirs" && psi.ArgumentList[2] == "--plugin-dirs"
                  && psi.ArgumentList[3] == Herramientas.RutaComplementos && psi.ArgumentList[4] == "--");
    }

    private static void Red(string carpeta)
    {
        var haul = Task.Run(Versiones.UltimoAsync).GetAwaiter().GetResult();
        Comprobar($"latest Haul release ({haul.Etiqueta}) has its setup, checksums and signature", Versiones.Archivos(haul) != null);
        if (Versiones.Archivos(haul) is { } a)
        {
            using var http = Actualizacion.Cliente(TimeSpan.FromSeconds(30));
            var bytes = Task.Run(() => http.GetByteArrayAsync(a.sumas)).GetAwaiter().GetResult();
            var sumas = System.Text.Encoding.UTF8.GetString(bytes);
            Comprobar("its SHA256SUMS.txt names haul-setup.exe", Actualizacion.HashDe(sumas, Versiones.Instalador) != null);
            Comprobar("its SHA256SUMS.txt carries a valid signature of the author",
                      ReleaseSignature.Valid(bytes, Task.Run(() => http.GetStringAsync(a.firma)).GetAwaiter().GetResult()));
        }
        var yt = Task.Run(() => Actualizacion.UltimoAsync(MotorYtDlp.Repositorio)).GetAwaiter().GetResult();
        Comprobar($"latest yt-dlp release ({yt.Etiqueta}) has yt-dlp.exe and SHA2-256SUMS and a valid tag",
                  yt.Archivo(MotorYtDlp.Ejecutable) != null && yt.Archivo(MotorYtDlp.Sumas) != null && MotorYtDlp.EsEtiqueta(yt.Etiqueta));

        // A real update of yt-dlp into the temporary folder: download, checksum, "--version", mark, use.
        MotorYtDlp.Raiz = carpeta;
        var (nueva, version) = Task.Run(() => MotorYtDlp.ActualizarAsync()).GetAwaiter().GetResult();
        if (!Actualizacion.EsMasNueva(yt.Etiqueta, Herramientas.VersionYtDlpIncluido))
            Comprobar("yt-dlp included is the latest: nothing downloaded", !nueva && !Directory.Exists(carpeta));
        else
        {
            var exe = Path.Combine(carpeta, version, MotorYtDlp.Ejecutable);
            Comprobar($"yt-dlp {version} downloaded, checked and in use", nueva && version == yt.Etiqueta && File.Exists(exe)
                      && Herramientas.RutaYtDlp == exe && Herramientas.YtDlpActualizado);
            Comprobar("no unfinished download left", Directory.GetDirectories(carpeta).Length == 1);
            Comprobar("the new copy is chosen at the next start", MotorYtDlp.Elegir(carpeta, Herramientas.VersionYtDlpIncluido)?.ruta == exe);
            var otra = Task.Run(() => MotorYtDlp.ActualizarAsync()).GetAwaiter().GetResult();
            Comprobar("a second check finds it up to date", !otra.nueva && otra.version == version);
        }
    }

    // ---- Recording ----

    private static void Bloque(string nombre, Action prueba)
    {
        Linea("== " + nombre);
        try { prueba(); }
        catch (Exception ex) { Fallo($"{nombre}: exception: {ex.GetType().Name}: {ex.Message}"); }
    }

    private static void Comprobar(string que, bool bien)
    {
        if (bien) { pasadas++; Linea("PASS " + que); }
        else Fallo(que);
    }

    private static void Fallo(string que)
    {
        fallas++;
        Linea("FAIL " + que);
    }

    private static void Linea(string texto) => registro.Append(texto).Append('\n');
}
