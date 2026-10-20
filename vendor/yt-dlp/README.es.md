# yt-dlp 2026.08.19, copia incluida

[English](README.md) · Español

Esta carpeta guarda una copia sin modificar de [yt-dlp](https://github.com/yt-dlp/yt-dlp), versión
2026.08.19. Haul para Windows está hecho y probado con esta versión exacta y la instala junto a la
aplicación, así su comportamiento no cambia cuando yt-dlp publica una versión nueva, y el proyecto
sigue funcionando aunque el repositorio original deje de estar disponible.

| Archivo | Contenido |
|---|---|
| `yt-dlp.exe` | Ejecutable oficial para Windows (PyInstaller, incluye Python); es lo que corre Haul |
| `yt-dlp.tar.gz` | Código fuente oficial de la misma versión |
| `SHA2-256SUMS`, `SHA2-256SUMS.sig` | Sumas oficiales de la versión y su firma |
| `public.key` | Clave pública de firma de yt-dlp |
| `VERSION` | Versión incluida, la lee la compilación |
| `licenses/` | Textos de licencia, la lista de autores de yt-dlp y las licencias de terceros de `yt-dlp.exe` |

## Procedencia

Los archivos se bajaron el 20 de octubre de 2026 de la
[versión 2026.08.19](https://github.com/yt-dlp/yt-dlp/releases/tag/2026.08.19). Las sumas SHA-256 de
`yt-dlp.exe` y `yt-dlp.tar.gz` coinciden con `SHA2-256SUMS`, y la firma de ese archivo se verificó con
la clave de firma de yt-dlp `AC0C BBE6 848D 6A87 3464 AF4E 57CF 6593 3B5A 7581` (Simon Sawicki).
`scripts/compilar.ps1` vuelve a cotejar la suma de `yt-dlp.exe` en cada compilación. `SHA2-256SUMS` es,
byte a byte, el mismo archivo que incluye la versión de macOS.

`licenses/yt-dlp-exe-THIRD_PARTY_LICENSES.txt` es el archivo de licencias guardado dentro de
`yt-dlp.exe`, extraído sin cambios con `scripts/extraer_licencias.py` (cotejado con el lector de
archivos del propio PyInstaller: misma SHA-256).

Para verificar a mano (gpg viene con Git para Windows):

```
gpg --import public.key
gpg --verify SHA2-256SUMS.sig SHA2-256SUMS
certutil -hashfile yt-dlp.exe SHA256
```

## Créditos y licencias

yt-dlp lo desarrollan los colaboradores de yt-dlp (ver `licenses/yt-dlp-AUTHORS`) y es un derivado de
youtube-dl. Está liberado al dominio público con la
[Unlicense](https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE) (`licenses/yt-dlp-LICENSE`).

El ejecutable de Windows incluye Python y las bibliotecas que usa yt-dlp (OpenSSL, curl, curl_cffi,
certifi, mutagen, websockets, brotli y otras), cada una con su licencia; todas están listadas, con sus
textos completos, en `licenses/yt-dlp-exe-THIRD_PARTY_LICENSES.txt`. yt-dlp incluye además
[yt-dlp-ejs](https://github.com/yt-dlp/ejs) (Unlicense), que trae
[meriyah](https://github.com/meriyah/meriyah) (ISC, `licenses/meriyah-LICENSE.md`) y
[astring](https://github.com/davidbonnet/astring) (MIT, `licenses/astring-LICENSE`).

Estos archivos conservan sus propias licencias. La licencia GPL-2.0-only de Haul no se aplica a ellos.

## Actualización

`scripts/actualizar-yt-dlp.ps1 <versión>` descarga otra versión, verifica su firma y sus sumas,
reemplaza los archivos de esta carpeta y renueva todos los textos de licencia.
