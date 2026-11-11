# Changelog

All notable changes to Haul for Windows. Versions follow [semantic versioning](https://semver.org): major.minor.patch.

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
