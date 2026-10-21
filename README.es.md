# Haul para Windows

Gabriel Castiglia · 21 de octubre de 2026 · versión 1.2.1 · licencia GPL-2.0 · hecho y probado con yt-dlp 2026.08.19

[English](README.md) · Español

Repositorio: <https://github.com/Gabriel-Castiglia/haul-windows>

Versión para macOS: <https://github.com/Gabriel-Castiglia/haul>

Manual en PDF: [`docs/Haul-manual-es.pdf`](docs/Haul-manual-es.pdf)

Haul es una aplicación de Windows que descarga video y audio de YouTube y de los demás sitios
compatibles con [yt-dlp](https://github.com/yt-dlp/yt-dlp). La aplicación arma el comando, muestra el
avance y maneja una cola de descargas. El trabajo de fondo lo hacen yt-dlp, incluido en la aplicación
en una versión fija y verificada, y ffmpeg. Es la hermana para Windows de la aplicación de macOS del
mismo nombre: mismas funciones, mismos formatos, mismo comportamiento. La interfaz sigue el tema de
Windows (claro u oscuro) y su color de énfasis, y está en inglés y en castellano; arranca en inglés la
primera vez y recuerda la elección.

## Requisitos

- Windows 11, o Windows 10 versión 1809 o posterior, en una PC de 64 bits. Probado en Windows 11 Pro.
- [winget](https://learn.microsoft.com/windows/package-manager/winget/) (el «Instalador de
  aplicación» de Microsoft Store, incluido en Windows 11).

yt-dlp viene con la aplicación. El SDK de .NET 10, ffmpeg y deno los instala el instalador, así que no
hay que instalar nada a mano. No hace falta Visual Studio.

## Instalación

```
git clone https://github.com/Gabriel-Castiglia/haul-windows.git
cd haul-windows
powershell -ExecutionPolicy Bypass -File scripts\instalar.ps1
```

El instalador:

1. instala con winget el SDK de .NET 10, ffmpeg y deno o, si ya están, los actualiza;
2. compila la aplicación, después de cotejar el yt-dlp incluido con sus sumas oficiales;
3. la copia a `%LOCALAPPDATA%\Programs\Haul`, cerrándola antes si está abierta, y crea el acceso del
   menú Inicio y la entrada en Configuración > Aplicaciones > Aplicaciones instaladas.

La aplicación en sí no necesita permisos de administrador. Volver a correr el instalador es la
manera de actualizar todo, dependencias y aplicación.

Para desinstalar: Configuración > Aplicaciones > Aplicaciones instaladas > Haul > Desinstalar, o
`powershell -ExecutionPolicy Bypass -File scripts\desinstalar.ps1`. Los archivos descargados no se
tocan nunca; las preferencias se conservan salvo que se agregue `-Preferencias`.

## Uso

1. Pegar uno o varios enlaces en el recuadro superior. Se aceptan enlaces sueltos, uno por línea o
   dentro de un texto copiado de otra parte; la aplicación los extrae y descarta los duplicados.
   Cuando la aplicación se abre o pasa al frente con el recuadro vacío, toma los enlaces del
   portapapeles.
2. Elegir el formato, Video (MP4) o Solo audio (MP3).
3. Elegir la carpeta de destino. La primera vez es Descargas; después se conserva la última
   elegida, igual que el formato.
4. Pulsar Descargar o Ctrl+Enter. El recuadro se vacía al empezar la descarga, y un enlace que ya
   está en la cola no vuelve a entrar solo desde el portapapeles.

Los enlaces entran en una cola y se descargan de a uno. Mientras la cola avanza se pueden agregar
más enlaces. Cada fila muestra la miniatura, el título, el porcentaje, la velocidad y el tiempo
restante, y al terminar ofrece mostrar el archivo en el Explorador o abrirlo. Las descargas con error
o canceladas se pueden reintentar o quitar. Una descarga cancelada borra los archivos a medias que
había creado; los que ya estaban en la carpeta, como una descarga anterior del mismo video en otro
formato, no se tocan nunca. La salida completa de yt-dlp está en «Detalles técnicos».

Una descarga que falla se reintenta hasta cuatro veces, con dos segundos entre cada intento, y la fila
muestra qué intento está en curso («Reintento 2/4»). Los archivos a medias se conservan entre intentos,
así yt-dlp sigue desde donde quedó. Cuando una descarga termina, la aplicación borra además los
archivos intermedios que yt-dlp no pudo quitar, como el audio original de un MP3 mientras un cliente
de sincronización de archivos lo tenía abierto.

La casilla «Limpiar terminadas automáticamente», debajo del formato, está siempre a la vista, aunque
la cola esté vacía. Mientras está marcada, cada descarga que termina o se cancela
sale sola de la lista y el resumen la sigue contando; las que fallaron quedan, para poder reintentarlas.
Al marcarla también se limpia lo que ya había terminado. La aplicación recuerda la elección.

Los videos con «instrumental» en el título esperan en la cola, en pausa, salvo que esté marcada
«Descargar instrumentales». Una fila en pausa se puede bajar sola con su botón de descarga, y al
marcar la opción se reanudan todas.

El botón ⓘ de la ventana muestra la versión, qué significa el nombre, el autor, la licencia y el
enlace a este repositorio, en el idioma de la interfaz.

También se puede iniciar con enlaces desde una terminal o un acceso directo:

```
"%LOCALAPPDATA%\Programs\Haul\Haul.exe" --descargar URL1 URL2
```

## Listas de reproducción

Un enlace a una lista de YouTube (`youtube.com/playlist?list=…`) o a la página de un canal
(`youtube.com/@nombre`) se lee primero, sin descargar nada, y cada video pasa a ser una fila propia de
la cola, con su miniatura y su posición («Mix · 3/12»). Los archivos van a una subcarpeta con el nombre
de la lista y se numeran en el orden de la lista: `01 - Título.mp4`, `02 - Título.mp4`, etcétera. Cada
video se puede cancelar o reintentar por separado. Una lista de más de 200 videos pide confirmación
antes de entrar a la cola.

Un enlace a un video abierto desde una lista (`watch?v=…&list=…`) muestra en la ventana una tarjeta
que pregunta si se baja solo ese video o la lista entera. Los enlaces de otros sitios se descargan tal
cual, un video cada uno.

## Formatos

| Formato | Resultado | Opciones de yt-dlp |
|---|---|---|
| Video | MP4 con H.264 y AAC, máximo 1080p en YouTube | `-f bv*+ba/b -S vcodec:h264,res,acodec:aac --merge-output-format mp4` |
| Solo audio | MP3 a la máxima calidad | `-x --audio-format mp3 --audio-quality 0` |

Se prefiere H.264 porque cualquier reproductor de Windows lo abre; sin esa preferencia YouTube entrega
AV1 con audio Opus. Los archivos llevan el título del video como nombre.

## Estructura del repositorio

| Ruta | Contenido |
|---|---|
| `fuentes/Haul/` | Código de la aplicación (C#, WPF sobre .NET 10) |
| `fuentes/Haul/Core/` | Cola, listas, enlaces, preferencias y el proceso de yt-dlp |
| `icono/icono.html` | Ícono en SVG, compartido con la versión de macOS |
| `icono/icono-1024.png`, `icono/icono-256.png`, `icono/Haul.ico` | Ícono renderizado y el archivo de ícono de Windows |
| `scripts/instalar.ps1` | Dependencias, compilación e instalación |
| `scripts/compilar.ps1` | Compilación en `build\Haul` |
| `scripts/desinstalar.ps1` | Desinstalador (también se copia junto a la aplicación) |
| `scripts/actualizar-yt-dlp.ps1` | Reemplaza el yt-dlp incluido por otra versión verificada |
| `scripts/extraer_licencias.py` | Lee las licencias de terceros guardadas dentro de `yt-dlp.exe` |
| `scripts/manual.ps1` | Arma los manuales en PDF a partir de los README (pandoc y Edge, Chrome o Brave) |
| `vendor/yt-dlp/` | Copia incluida de yt-dlp 2026.08.19, con fuentes, sumas, firma y licencias |
| `docs/Haul-manual-en.pdf`, `docs/Haul-manual-es.pdf` | Manuales en PDF, en inglés y en castellano |
| `docs/manual.css` | Hoja de estilo de los manuales |
| `README.md`, `README.es.md` | Esta documentación, en inglés y en castellano |

La carpeta `build\` y las carpetas `bin\` y `obj\` del compilador se generan al compilar y no forman
parte del repositorio.

## Ventana y tema

La ventana no se puede achicar por debajo de 560 × 560, así que su contenido siempre se lee; por
debajo de esa altura el contenido se desplaza,
mientras el encabezado queda fijo arriba. El tamaño y la posición se recuerdan entre sesiones.
La aplicación usa el tema Fluent de Windows 11: sigue el modo claro y el oscuro, y el color de énfasis
de Configuración > Personalización > Colores, también si cambian con la aplicación abierta.

## Mantenimiento

Haul está hecho y probado con yt-dlp 2026.08.19, y esa copia exacta viene con la aplicación. Una
versión fija mantiene el comportamiento estable y no depende de que el repositorio de yt-dlp siga en
línea. La contracara es que YouTube cambia seguido, y tarde o temprano una versión fija deja de
funcionar con él. Cuando eso pase, una versión nueva de Haul pasa a un yt-dlp más nuevo:

```
powershell -ExecutionPolicy Bypass -File scripts\actualizar-yt-dlp.ps1 <versión>
powershell -ExecutionPolicy Bypass -File scripts\instalar.ps1
```

El primer script descarga la versión, verifica su firma con la clave guardada en
`vendor\yt-dlp\public.key` (con el gpg que trae Git para Windows) y sus sumas SHA-256, y recién
entonces reemplaza los archivos y renueva los textos de licencia. Necesita Python para leer las
licencias guardadas dentro de `yt-dlp.exe`. Volver a correr `scripts\instalar.ps1` también actualiza
el SDK de .NET, ffmpeg y deno.

Los errores más comunes de yt-dlp se muestran en palabras simples en el idioma de la interfaz (video
no disponible, video privado, sitio no soportado, lista inexistente, sin conexión); el resto aparece
tal como lo escribe yt-dlp.

Las preferencias (idioma, carpeta, formato, tamaño de la ventana, «Limpiar terminadas automáticamente» y
«Descargar instrumentales») se guardan en
`%APPDATA%\Haul\settings.json`. Borrar ese archivo las devuelve a su valor inicial.

## Notas técnicas

La aplicación ejecuta el `yt-dlp.exe` incluido como proceso hijo oculto. Una aplicación abierta desde
el menú Inicio hereda el `PATH` que tenía el Explorador al arrancar, que no incluye lo instalado
después con winget; por eso Haul rearma el `PATH` desde el registro y le pasa a yt-dlp la ubicación
de ffmpeg. Para no depender del texto que yt-dlp imprime para una terminal, le pide la salida en un
formato propio, separado por tabulaciones, igual que la versión de macOS:

| Línea | Contenido | Origen |
|---|---|---|
| `META` | id, canal, autor, título y miniatura, que se revisan antes de bajar | `-O pre_process:…` |
| `INFO` | título, autor, duración, miniatura y nombre del archivo | `-O before_dl:…` |
| `AVANCE` | porcentaje, velocidad y tiempo restante | `--progress-template` |
| `ARCHIVO` | ruta final del archivo | `-O after_move:…` |

Los instrumentales en pausa se frenan con `--match-filters`, así yt-dlp no baja nada hasta que se
permiten.

Las listas se leen con `--flat-playlist -J`, que enumera los videos sin descargarlos. Cancelar detiene
yt-dlp junto con ffmpeg (todo el árbol de procesos). Con `-O`, yt-dlp no imprime nada más, así que los
archivos de una descarga se reconocen por el nombre de su línea `INFO`: al empezar, la aplicación
anota qué archivos con ese nombre ya existen, y al cancelar borra solo los nuevos.

## Créditos

Haul es una interfaz; el trabajo pesado es de estos proyectos:

| Proyecto | Papel en Haul | Licencia |
|---|---|---|
| [yt-dlp](https://github.com/yt-dlp/yt-dlp), de los colaboradores de yt-dlp, derivado de youtube-dl | Motor de descarga, incluido en `vendor/yt-dlp` | [Unlicense](https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE) |
| Componentes dentro de `yt-dlp.exe`: Python, OpenSSL, curl, curl_cffi, certifi, mutagen, websockets, brotli y otros | Entorno y bibliotecas del ejecutable de Windows | Las suyas, listadas en `vendor/yt-dlp/licenses/yt-dlp-exe-THIRD_PARTY_LICENSES.txt` |
| [yt-dlp-ejs](https://github.com/yt-dlp/ejs), incluido en yt-dlp | Resuelve los desafíos JavaScript de YouTube | Unlicense |
| [meriyah](https://github.com/meriyah/meriyah), de KFlash y otros, dentro de yt-dlp-ejs | Analizador de JavaScript | [ISC](https://github.com/meriyah/meriyah/blob/main/LICENSE.md) |
| [astring](https://github.com/davidbonnet/astring), de David Bonnet, dentro de yt-dlp-ejs | Generador de código JavaScript | [MIT](https://github.com/davidbonnet/astring/blob/main/LICENSE) |
| [FFmpeg](https://ffmpeg.org), compilado por el proyecto yt-dlp, instalado con winget, no incluido | Une imagen y sonido, convierte a MP3 | [GPL-3.0+](https://ffmpeg.org/legal.html) en esta compilación |
| [Deno](https://deno.com), instalado con winget | Entorno JavaScript que yt-dlp usa para YouTube | [MIT](https://github.com/denoland/deno/blob/main/LICENSE.md) |
| [.NET](https://dotnet.microsoft.com) y WPF, instalados con winget | Entorno de ejecución e interfaz gráfica | [MIT](https://github.com/dotnet/wpf/blob/main/LICENSE.TXT) |
| [Segoe Fluent Icons](https://learn.microsoft.com/windows/apps/design/style/segoe-fluent-icons-font), parte de Windows | Íconos de la interfaz | Licencia de Windows |
| [pandoc](https://pandoc.org), solo para armar los manuales en PDF | Conversión de Markdown a HTML | [GPL-2.0+](https://github.com/jgm/pandoc/blob/main/COPYRIGHT) |

Los textos de licencia de los componentes incluidos están en `vendor/yt-dlp/licenses/` y se instalan
junto a la aplicación, en `licenses\`.

## Licencia

Copyright (C) 2026 Gabriel Castiglia.

Haul es software libre, publicado bajo la Licencia Pública General de GNU, versión 2
(GPL-2.0-only), la misma del núcleo Linux. Se puede usar, estudiar, modificar y redistribuir; toda
versión modificada que se distribuya tiene que publicarse con la misma licencia y con su código
fuente. El texto completo está en [`LICENSE`](LICENSE).

Haul incluye una copia sin modificar de yt-dlp en `vendor/yt-dlp`, que conserva sus propias licencias
(Unlicense, más las de los componentes dentro de `yt-dlp.exe`) y no está cubierta por la GPL. Haul
ejecuta yt-dlp y ffmpeg como programas separados y no se enlaza con ellos; ffmpeg no se incluye.
