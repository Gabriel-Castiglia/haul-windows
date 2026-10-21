# Haul for Windows

Gabriel Castiglia · October 21, 2026 · version 1.2.1 · GPL-2.0 license · built and tested with yt-dlp 2026.08.19

English · [Español](README.es.md)

Repository: <https://github.com/Gabriel-Castiglia/haul-windows>

macOS version: <https://github.com/Gabriel-Castiglia/haul>

PDF manual: [`docs/Haul-manual-en.pdf`](docs/Haul-manual-en.pdf)

Haul is a Windows app that downloads video and audio from YouTube and every other site supported by
[yt-dlp](https://github.com/yt-dlp/yt-dlp). The app builds the command, shows the progress and
manages a download queue. The actual work is done by yt-dlp, bundled with the app at a fixed,
verified version, and by ffmpeg. It is the Windows sibling of the macOS app of the same name: same
features, same formats, same behavior. The interface follows the Windows theme (light or dark) and
accent color, and is available in English and Spanish; it starts in English on first launch and
remembers the choice.

## Requirements

- Windows 11, or Windows 10 version 1809 or later, on a 64-bit PC. Tested on Windows 11 Pro.
- [winget](https://learn.microsoft.com/windows/package-manager/winget/) (the "App Installer" from
  the Microsoft Store, included in Windows 11).

yt-dlp ships with the app. The .NET 10 SDK, ffmpeg and deno are installed by the installer, so nothing
has to be installed by hand. Visual Studio is not needed.

## Installation

```
git clone https://github.com/Gabriel-Castiglia/haul-windows.git
cd haul-windows
powershell -ExecutionPolicy Bypass -File scripts\instalar.ps1
```

The installer:

1. installs the .NET 10 SDK, ffmpeg and deno with winget or, if they are already present, upgrades
   them;
2. builds the app, after checking the bundled yt-dlp against its official checksums;
3. copies it to `%LOCALAPPDATA%\Programs\Haul`, quitting it first if it is running, and creates the
   Start menu shortcut and the entry in Settings > Apps > Installed apps.

No administrator rights are needed for the app itself. Running the installer again is how
everything gets updated, dependencies and app alike.

To uninstall: Settings > Apps > Installed apps > Haul > Uninstall, or
`powershell -ExecutionPolicy Bypass -File scripts\desinstalar.ps1`. Downloaded files are never
touched; preferences are kept unless `-Preferencias` is added.

## Usage

1. Paste one or more links into the box at the top. Links can be pasted on their own, one per
   line, or inside text copied from somewhere else; the app extracts them and drops duplicates. When
   the app opens or comes to the front with the box empty, it picks up the links on the clipboard.
2. Choose the format: Video (MP4) or Audio only (MP3).
3. Choose the destination folder. The first time it is Downloads; after that the app keeps the
   last folder chosen, and the format too.
4. Press Download or Ctrl+Enter. The box empties when the download starts, and a link already in the
   queue is not picked up again from the clipboard.

Links go into a queue and are downloaded one at a time. More links can be added while the queue
runs. Each row shows the thumbnail, title, percentage, speed and time remaining, and once finished
it offers to show the file in File Explorer or open it. Failed or cancelled downloads can be retried
or removed. A cancelled download deletes the partial files it had created; files that were already
in the folder, such as an earlier download of the same video in another format, are never touched.
The full yt-dlp output is under "Technical details".

A download that fails is tried again up to four times, two seconds apart, and the row shows which
attempt is running ("Retry 2/4"). Partial files are kept between attempts, so yt-dlp resumes where
it stopped. When a download finishes, the app also deletes the intermediate files yt-dlp could not
remove by itself, such as the original audio of an MP3 while a file sync client held it
open.

The "Clear finished automatically" checkbox, below the format, is always in view, even with an
empty queue. While it is checked, every download that finishes or is cancelled leaves the
list by itself, and the summary keeps counting it; failed ones stay, so they can be retried. Checking
it also clears what had already finished. The app remembers the setting.

Videos with "instrumental" in the title wait in the queue, paused, unless "Download instrumentals" is
checked. A paused row can be downloaded on its own with its download button, and checking the option
resumes all of them.

The ⓘ button in the window shows the version, the meaning of the name, the author, the license and
the link to this repository, in the interface language.

The app can also be started with links from a terminal or a shortcut:

```
"%LOCALAPPDATA%\Programs\Haul\Haul.exe" --descargar URL1 URL2
```

## Playlists

A YouTube playlist link (`youtube.com/playlist?list=…`) or a channel page (`youtube.com/@name`) is
read first, without downloading anything, and every video becomes its own row in the queue, with its
thumbnail and its position ("Mix · 3/12"). The files go into a subfolder named after the playlist and
are numbered in playlist order: `01 - Title.mp4`, `02 - Title.mp4`, and so on. Each video can be
cancelled or retried on its own. A playlist with more than 200 videos asks for confirmation first.

A link to a video that was opened from a playlist (`watch?v=…&list=…`) shows a card in the window that
asks whether to download only that video or the whole playlist. Links to other sites download as
they are, one video each.

## Formats

| Format | Result | yt-dlp options |
|---|---|---|
| Video | MP4 with H.264 and AAC, up to 1080p on YouTube | `-f bv*+ba/b -S vcodec:h264,res,acodec:aac --merge-output-format mp4` |
| Audio only | MP3 at the highest quality | `-x --audio-format mp3 --audio-quality 0` |

H.264 is preferred because every Windows player handles it; without that preference YouTube serves
AV1 with Opus audio. Files are named after the video title.

## Repository layout

| Path | Contents |
|---|---|
| `fuentes/Haul/` | App source code (C#, WPF on .NET 10) |
| `fuentes/Haul/Core/` | Queue, playlists, links, settings and the yt-dlp process |
| `icono/icono.html` | Icon as SVG, shared with the macOS version |
| `icono/icono-1024.png`, `icono/icono-256.png`, `icono/Haul.ico` | Rendered icon and the Windows icon file |
| `scripts/instalar.ps1` | Dependencies, build and installation |
| `scripts/compilar.ps1` | Build into `build\Haul` |
| `scripts/desinstalar.ps1` | Uninstaller (also copied next to the app) |
| `scripts/actualizar-yt-dlp.ps1` | Replaces the bundled yt-dlp with another verified release |
| `scripts/extraer_licencias.py` | Reads the third-party licenses stored inside `yt-dlp.exe` |
| `scripts/manual.ps1` | Builds the PDF manuals from the READMEs (pandoc and Edge, Chrome or Brave) |
| `vendor/yt-dlp/` | Bundled copy of yt-dlp 2026.08.19, with sources, checksums, signature and licenses |
| `docs/Haul-manual-en.pdf`, `docs/Haul-manual-es.pdf` | PDF manuals, in English and Spanish |
| `docs/manual.css` | Stylesheet for the manuals |
| `README.md`, `README.es.md` | This documentation, in English and Spanish |

The `build\` folder and the compiler's `bin\` and `obj\` folders are generated and are not part of
the repository.

## Window and theme

The window cannot be made smaller than 560 × 560, so its contents always stay readable; below that
height the content scrolls, while the header stays fixed at the top. Size and position are remembered between sessions. The app uses the
Windows 11 Fluent theme: it follows light and dark mode, and the accent color in Settings >
Personalization > Colors, also when they change while it is open.

## Maintenance

Haul is built and tested with yt-dlp 2026.08.19, and that exact copy ships with the app. A pinned
version keeps the behavior stable and does not depend on the yt-dlp repository staying online. The
trade-off is that YouTube changes often, and sooner or later a fixed version stops working with it.
When that happens, a new release of Haul moves to a newer yt-dlp:

```
powershell -ExecutionPolicy Bypass -File scripts\actualizar-yt-dlp.ps1 <version>
powershell -ExecutionPolicy Bypass -File scripts\instalar.ps1
```

The first script downloads the release, verifies its signature with the key stored in
`vendor\yt-dlp\public.key` (using the gpg that ships with Git for Windows) and its SHA-256
checksums, and only then replaces the files and refreshes the license texts. It needs Python to read
the licenses stored inside `yt-dlp.exe`. Running `scripts\instalar.ps1` again also upgrades the
.NET SDK, ffmpeg and deno.

The most common yt-dlp errors are shown in plain words in the interface language (video
unavailable, private video, unsupported site, missing playlist, no connection); anything else
appears exactly as yt-dlp prints it.

Preferences (language, folder, format, window size, "Clear finished automatically" and "Download instrumentals")
are stored in `%APPDATA%\Haul\settings.json`.
Deleting that file resets them.

## Technical notes

The app runs the bundled `yt-dlp.exe` as a hidden child process. An app started from the Start menu
inherits the `PATH` that File Explorer had when it started, which misses tools installed afterwards
with winget, so Haul rebuilds `PATH` from the registry and passes ffmpeg's location to yt-dlp. To
avoid depending on the text yt-dlp prints for a terminal, it asks for output in a custom,
tab-separated format, the same as the macOS version:

| Line | Contents | Source |
|---|---|---|
| `META` | id, channel, uploader, title and thumbnail, checked before downloading | `-O pre_process:…` |
| `INFO` | title, uploader, duration, thumbnail and file name | `-O before_dl:…` |
| `AVANCE` | percentage, speed and time remaining | `--progress-template` |
| `ARCHIVO` | final file path | `-O after_move:…` |

Paused instrumentals are held back with `--match-filters`, so yt-dlp downloads nothing until they
are allowed.

Playlists are read with `--flat-playlist -J`, which lists the videos without downloading them.
Cancelling stops yt-dlp together with ffmpeg (the whole process tree). With `-O`, yt-dlp prints
nothing else, so the files of a download are recognized by the file name in its `INFO` line: when
the download starts, the app notes which files with that name already exist, and on cancelling it
deletes only the new ones.

## Credits

Haul is a front end; the hard work belongs to these projects:

| Project | Role in Haul | License |
|---|---|---|
| [yt-dlp](https://github.com/yt-dlp/yt-dlp), by the yt-dlp contributors, fork of youtube-dl | Download engine, bundled in `vendor/yt-dlp` | [Unlicense](https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE) |
| Components inside `yt-dlp.exe`: Python, OpenSSL, curl, curl_cffi, certifi, mutagen, websockets, brotli and others | Runtime and libraries of the Windows executable | Their own licenses, listed in `vendor/yt-dlp/licenses/yt-dlp-exe-THIRD_PARTY_LICENSES.txt` |
| [yt-dlp-ejs](https://github.com/yt-dlp/ejs), included in yt-dlp | YouTube JavaScript challenge solver | Unlicense |
| [meriyah](https://github.com/meriyah/meriyah), by KFlash and others, inside yt-dlp-ejs | JavaScript parser | [ISC](https://github.com/meriyah/meriyah/blob/main/LICENSE.md) |
| [astring](https://github.com/davidbonnet/astring), by David Bonnet, inside yt-dlp-ejs | JavaScript code generator | [MIT](https://github.com/davidbonnet/astring/blob/main/LICENSE) |
| [FFmpeg](https://ffmpeg.org), build by the yt-dlp project, installed with winget, not bundled | Merges picture and sound, converts to MP3 | [GPL-3.0+](https://ffmpeg.org/legal.html) for this build |
| [Deno](https://deno.com), installed with winget | JavaScript runtime used by yt-dlp for YouTube | [MIT](https://github.com/denoland/deno/blob/main/LICENSE.md) |
| [.NET](https://dotnet.microsoft.com) and WPF, installed with winget | Runtime and user interface framework | [MIT](https://github.com/dotnet/wpf/blob/main/LICENSE.TXT) |
| [Segoe Fluent Icons](https://learn.microsoft.com/windows/apps/design/style/segoe-fluent-icons-font), part of Windows | Icons of the interface | Windows license |
| [pandoc](https://pandoc.org), only to build the PDF manuals | Markdown to HTML conversion | [GPL-2.0+](https://github.com/jgm/pandoc/blob/main/COPYRIGHT) |

The license texts of the bundled components are in `vendor/yt-dlp/licenses/` and are installed
next to the app, under `licenses\`.

## License

Copyright (C) 2026 Gabriel Castiglia.

Haul is free software, released under the GNU General Public License, version 2
(GPL-2.0-only), the same license as the Linux kernel. It may be used, studied, modified and
redistributed; any modified version that is distributed must be released under the same license,
together with its source code. The full text is in [`LICENSE`](LICENSE).

Haul includes an unmodified copy of yt-dlp in `vendor/yt-dlp`, which keeps its own licenses
(Unlicense, plus the licenses of the components inside `yt-dlp.exe`) and is not covered by the GPL.
Haul runs yt-dlp and ffmpeg as separate programs and does not link to them; ffmpeg is not bundled.
