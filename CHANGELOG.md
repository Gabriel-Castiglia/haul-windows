# Changelog

All notable changes to Haul for Windows. Versions follow [semantic versioning](https://semver.org): major.minor.patch.

English · [Español](CHANGELOG.es.md) · [日本語](CHANGELOG.ja.md)

## 1.8.0 - 2026-11-27
### Added

- Japanese interface: Haul and its setup window speak Japanese too (日本語 in their language menus), with the
  Japanese interface fonts of Windows (Yu Gothic UI, Meiryo UI). The setup window starts in Japanese on a Windows
  in Japanese, and Haul started without a setup follows Windows when it is in Spanish or Japanese.

### Changed

- The Spanish texts of Haul, of its setup window and of the documentation address the user as "tú" and use neutral
  Spanish: for example "Pega uno o varios enlaces", "Reanudar todo" (was "Seguir todo"), "Descargar listas enteras"
  (was "Bajar listas enteras") and "Quitar de la cola" (was "Sacar de la cola").
- "Check for updates" pressed while an update is already under way says so ("An update of Haul is already under
  way"); before, it did nothing.
- Copies of yt-dlp downloaded by Haul no longer get a `verified.txt` mark, which was never read: a copy is checked
  in a folder of its own, which takes the version's name only after every check has passed.

### Security

- yt-dlp always runs with `--ignore-config`. Before, it read a `yt-dlp.conf` from the download folder (usually
  Downloads, where any web page can leave a file), and options in it such as `--exec` would run commands. As a
  consequence, the user's own yt-dlp configuration files (`%APPDATA%\yt-dlp\config` and the like) are no longer
  applied either.
- Thumbnails are fetched only from `http` and `https` addresses; local and network paths are ignored.
- yt-dlp loads only Haul's own plugins (`--no-plugin-dirs`, then `--plugin-dirs` with Haul's `plugins` folder).
  Before, it also loaded any plugin in `%APPDATA%\yt-dlp\plugins` and its other default folders, where any program
  run by the user can leave Python code.
- The setup program downloaded by an update is checked again right before it runs and kept open until Haul has
  closed, so no other program can change, replace or delete it between that check and its start.

### Fixed

- The window could freeze when a download ended while a process started by yt-dlp still held its output open. The
  end of every yt-dlp is now awaited off the window's thread, and a process left behind is stopped after ten
  seconds, with everything yt-dlp started.
- A row whose last attempt failed turned red, and could be removed or retried, while yt-dlp was still running. It
  now turns red when yt-dlp exits.
- "Cancel all" with "Clear finished automatically" on removes the cancelled rows, as a single cancel already did.
- Playlist folders named like a Windows device with an extension, or `COM5`–`COM9` and `LPT4`–`LPT9` (`NUL.x`,
  `COM7`), get the "Playlist " prefix, as `CON` and `NUL` already did.
- Setup: the window can no longer be closed while it installs or upgrades FFmpeg and Deno. Before, closing it asked
  to cancel and then said "Canceled. The computer was left as it was", although Haul was already installed.

### Accessibility

- The colour tiles of the appearance window can be reached with Tab and chosen with Enter or Space, and screen
  readers read their names.
- Rows of the queue can be reordered from the keyboard: Tab reaches each row, and Alt+Up and Alt+Down move it one
  place; the row's menu (right-click, Shift+F10 or the menu key) has "Move up" and "Move down". Screen readers read
  each row's title and status.
- The red of a failed row is darker on a light window (contrast 5.2:1 or more) and brighter on a dark one, so it
  reads at 4.5:1 or more in both.

### Documentation

- The README, the PDF manual and this changelog are also in Japanese (`README.ja.md`, `docs/Haul-manual-ja.pdf`,
  `CHANGELOG.ja.md`), and the changelog in Spanish (`CHANGELOG.es.md`). The installer puts `README.ja.txt` next to
  `README.txt` and `README.es.txt`.
- New sections in the README and the manuals: the main window part by part, the options, troubleshooting with
  every message, and responsible use; pictures of the About window and of the uninstall page, and every picture
  drawn again with the current icon and version.
- The Japanese README and manual show Haul and its setup window in Japanese and quote their Japanese texts.

## 1.7.4 - 2026-11-27

### Changed

- **A new icon**: a download arrow going into a tray, drawn as a flat picture, cream with a black outline on the
  chosen colour, with the play sign of a video. It replaces the engraving of an eagle carrying a hare, and still
  follows the colour chosen in the settings, in all twelve colours. The drawing is in `assets/icon.svg`.

## 1.7.3 - 2026-11-26

### Changed

- Bundles **yt-dlp 2026.11.23** (it was 2026.08.19), unmodified, with its signature and checksums verified against
  the key of yt-dlp's releases (`AC0C BBE6 848D 6A87 3464 AF4E 57CF 6593 3B5A 7581`). The 2026.08.19 copy no longer
  downloaded from YouTube on Windows ("Requested format is not available"); checked with a real download.
- Building the manuals also stops when the yt-dlp version written under the title of a README is not the bundled one.

## 1.7.2 - 2026-11-25

### Changed

- **Updates signed by the author**: the `SHA256SUMS.txt` of a release must now carry the author's signature
  (`release.sig`, ECDSA P-256, checked against two keys built into Haul) before the setup is downloaded. Before,
  the setup was checked only against hashes that came from the same page, so a release put there by someone else
  would have installed itself. A release without a valid signature is offered as a page, to download by hand.
  Anyone can still download, install and share Haul as before; Haul 1.7.1 and older install this one as usual.
  The updates of yt-dlp are checked as before, against yt-dlp's own `SHA2-256SUMS`.

## 1.7.1 - 2026-11-20

### Fixed

- A saved queue that could not be read when Haul opened (the file held by another program) was then written over:
  the first save of the empty queue deleted it. Haul now leaves that file alone for the session, so the queue comes
  back the next time.

## 1.7.0 - 2026-11-20

### Added

- Updates that install themselves. Once a day, or with "Check for updates" in the ⓘ window, Haul asks GitHub
  for its latest release and, if it is newer, asks once: "Update now", "What's new" or "Later". "Update now"
  downloads `haul-setup.exe`, checks it against the release's `SHA256SUMS.txt` (a file that does not match is
  deleted and never run), asks what to do with a running download (update now or when the queue ends), saves
  the queue, closes Haul, installs in silence keeping the installed language, and opens Haul again.
- The setup program takes `/HAULWAITPID=<pid>` (waits for that Haul to close before replacing anything) and
  `/HAULRELAUNCH=1` (opens Haul again at the end, as the user, whether the installation worked or not). A
  layout (`/layout`) no longer touches an installed Haul or runs winget.
- yt-dlp updates from the app. Once a day (it can be turned off in the ⓘ window) and with "Update yt-dlp",
  Haul fetches yt-dlp's latest official release into `%LOCALAPPDATA%\Haul\yt-dlp\<version>`, checks it
  against `SHA2-256SUMS` and its own version number, and uses it for the next downloads; a running download
  keeps the copy it started with. The copy installed with Haul stays as it was.
- The queue survives closing Haul: queued, paused and failed rows and the links still waiting come back in the
  same order; a download that was running comes back queued and resumes from its partial files. It is saved in
  `%APPDATA%\Haul\queue.json` through a temporary file; a damaged file is set aside as `queue.damaged.json`.
- `Haul.exe --self-test <log>` checks the saved queue, the version logic of Haul and yt-dlp and the checksum
  lists with throw-away data, without a window and without touching the user's settings or queue; the exit
  code is the number of failures. `--red` adds checks against GitHub, including a real yt-dlp update into the
  temporary folder.

### Changed

- A more compact main window, so the queue shows several rows on small screens (1366 × 768 or 1280 × 720,
  with Windows at 125 % or 150 %): smaller header, margins and format cards, the destination and the Download
  button on one line, and the three options under a folding "Options" line that lists the ones that are on
  and remembers whether it was left open. The minimum size is now 560 × 420; in a window too low for two rows
  of the queue, the part above it scrolls with the queue instead of squeezing it.
- The settings file is also written through a temporary file.
- The pictures of the README use the default settings and an example queue, never the data of the computer
  they are drawn on; there is a new one of the question about a new version.

## 1.6.0 - 2026-11-11

### Added

- Light, dark or as Windows, in the new appearance window (the palette button in the header). "As Windows"
  follows Windows while Haul is open. A settings file from an earlier version keeps the dark window; a new
  installation follows Windows.
- Twelve colours. The one chosen colours the buttons, check boxes, selected items and the icon of the window
  and of its taskbar button. Before, the window took the accent colour of Windows.
- Notice of new versions: once a day Haul asks GitHub for the latest version number and, if it is newer,
  offers to open the download page. The ⓘ window turns it off and asks on request.
- Pictures of the window in the README, drawn by `scripts/screenshots.ps1`.

### Changed

- A new icon: the eagle carrying a hare from an engraving by the Monogrammist B (Italy, 17th century;
  Rijksmuseum, public domain), drawn by `scripts/make_icon.py` in the twelve colours, orange by default.
- The README and the manuals say x64 only and link to the latest release; the command-line example points to
  the installed `Haul.exe` in `C:\Program Files\Haul`.

## 1.5.0 - 2026-11-09

### Added

- Setup program, `haul-setup.exe`, in English and Spanish, and the same installation as an MSI package.
  Haul comes compiled, so the .NET SDK is no longer needed. The setup window installs or upgrades
  FFmpeg and Deno with winget.
- This changelog.

### Changed

- Haul installs for all users in `C:\Program Files\Haul`. Setup removes an installation made by the
  1.4 script, keeping its settings.

## 1.4.0 - 2026-10-26

### Added

- Pause for each row and for the whole queue; partial files are kept and the download resumes.
- Retry all failed downloads with one button.
- Drag rows to change the order of the queue.
- "Download whole playlists" setting, instead of a question every time.

### Changed

- Everything down to the Download button stays fixed; only the queue scrolls.
- The technical log and the tagline under the title were removed.

### Fixed

- Posts that link to an unlisted video instead of embedding it now download.

## 1.3.0 - 2026-10-24

### Added

- Haul's own yt-dlp plugin, for links the bundled yt-dlp does not handle.

### Changed

- The window is always dark, whatever the system appearance.
- The documentation and the messages describe Haul as a front end for yt-dlp, without naming sites.

## 1.2.1 - 2026-10-21

### Changed

- "Clear finished automatically" is a checkbox below the format, always visible; it used to sit in the
  queue header, which hides when the queue is empty.

### Fixed

- The link box no longer fills again with the same link when the window comes back to the front.

## 1.2.0 - 2026-10-21

### Added

- A failed download or playlist read is tried again up to four times, two seconds apart.
- Finished and cancelled rows can leave the list by themselves.
- Downloads with "instrumental" in the title wait paused unless "Download instrumentals" is checked.

### Changed

- The header stays fixed while the rest of the window scrolls.

### Fixed

- Intermediate files that yt-dlp could not delete, because another program held them open, are
  removed after the download.

## 1.1.0 - 2026-10-20

First release for Windows, with the same features as the macOS version 1.1.0: download queue, MP4 and
MP3 formats, playlists expanded into one row per item, cleanup of cancelled downloads, About window,
and English and Spanish interface. Bundles the official yt-dlp.exe 2026.08.19, verified against its
signed checksums; `scripts\instalar.ps1` installs the .NET SDK, FFmpeg and Deno with winget.
