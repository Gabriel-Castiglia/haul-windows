# yt-dlp 2026.08.19, bundled copy

English · [Español](README.es.md)

This folder holds an unmodified copy of [yt-dlp](https://github.com/yt-dlp/yt-dlp) version
2026.08.19. Haul for Windows is built and tested with this exact version and installs it next to the
app, so its behavior does not change when yt-dlp publishes a new release, and the project keeps
working even if the upstream repository becomes unavailable.

| File | Contents |
|---|---|
| `yt-dlp.exe` | Official Windows executable (PyInstaller, includes Python); this is what Haul runs |
| `yt-dlp.tar.gz` | Official source tarball of the same release |
| `SHA2-256SUMS`, `SHA2-256SUMS.sig` | Official checksums for the release and their signature |
| `public.key` | yt-dlp's public signing key |
| `VERSION` | Bundled version, read by the build |
| `licenses/` | License texts, the list of yt-dlp authors and the third-party licenses of `yt-dlp.exe` |

## Provenance

The files were downloaded on October 20, 2026 from the
[2026.08.19 release](https://github.com/yt-dlp/yt-dlp/releases/tag/2026.08.19). The SHA-256 hashes
of `yt-dlp.exe` and `yt-dlp.tar.gz` match `SHA2-256SUMS`, and the signature on that file was verified
with the yt-dlp signing key `AC0C BBE6 848D 6A87 3464 AF4E 57CF 6593 3B5A 7581` (Simon Sawicki).
`scripts/compilar.ps1` checks the hash of `yt-dlp.exe` again on every build. `SHA2-256SUMS` is
byte-for-byte the same file the macOS version bundles.

`licenses/yt-dlp-exe-THIRD_PARTY_LICENSES.txt` is the license file stored inside `yt-dlp.exe`,
extracted unchanged with `scripts/extraer_licencias.py` (checked against PyInstaller's own archive
reader: same SHA-256).

To verify by hand (gpg ships with Git for Windows):

```
gpg --import public.key
gpg --verify SHA2-256SUMS.sig SHA2-256SUMS
certutil -hashfile yt-dlp.exe SHA256
```

## Credits and licenses

yt-dlp is developed by the yt-dlp contributors (see `licenses/yt-dlp-AUTHORS`) and is a fork of
youtube-dl. It is released into the public domain under the
[Unlicense](https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE) (`licenses/yt-dlp-LICENSE`).

The Windows executable bundles Python and the libraries yt-dlp uses (OpenSSL, curl, curl_cffi,
certifi, mutagen, websockets, brotli and others), each under its own license; all of them are listed,
with their full texts, in `licenses/yt-dlp-exe-THIRD_PARTY_LICENSES.txt`. yt-dlp also includes
[yt-dlp-ejs](https://github.com/yt-dlp/ejs) (Unlicense), which bundles
[meriyah](https://github.com/meriyah/meriyah) (ISC, `licenses/meriyah-LICENSE.md`) and
[astring](https://github.com/davidbonnet/astring) (MIT, `licenses/astring-LICENSE`).

These files keep their own licenses. Haul's GPL-2.0-only license does not apply to them.

## Updating

`scripts/actualizar-yt-dlp.ps1 <version>` downloads another release, verifies its signature and
checksums, replaces the files in this folder and refreshes every license text.
