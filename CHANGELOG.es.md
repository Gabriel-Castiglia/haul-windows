# Historial de versiones

Todos los cambios importantes de Haul para Windows. Las versiones siguen el
[versionado semántico](https://semver.org/lang/es/): mayor.menor.parche.

[English](CHANGELOG.md) · Español · [日本語](CHANGELOG.ja.md)

## 1.8.0 - 2026-11-27
### Añadido

- Interfaz en japonés: Haul y su ventana de instalación también hablan japonés (日本語 en sus menús de idioma),
  con las fuentes japonesas de la interfaz de Windows (Yu Gothic UI, Meiryo UI). La ventana de instalación se abre
  en japonés en un Windows en japonés, y Haul abierto sin instalador sigue a Windows cuando está en español o en
  japonés.

### Cambiado

- Los textos en español de Haul, de su ventana de instalación y de la documentación tratan al usuario de «tú» y
  usan un español neutro: por ejemplo «Pega uno o varios enlaces», «Reanudar todo» (antes «Seguir todo»),
  «Descargar listas enteras» (antes «Bajar listas enteras») y «Quitar de la cola» (antes «Sacar de la cola»).
- «Buscar actualizaciones» pulsado mientras ya hay una actualización en curso lo avisa («Ya hay una actualización
  de Haul en curso»); antes no hacía nada.
- Las copias de yt-dlp que descarga Haul ya no llevan la marca `verified.txt`, que nunca se leía: cada copia se
  comprueba en una carpeta propia, que recibe el nombre de la versión solo después de pasar todas las
  comprobaciones.

### Seguridad

- yt-dlp se ejecuta siempre con `--ignore-config`. Antes leía un `yt-dlp.conf` de la carpeta de descargas
  (casi siempre Descargas, donde cualquier página web puede dejar un archivo), y opciones como `--exec` habrían
  ejecutado comandos. Como consecuencia, tampoco se aplican ya los archivos de configuración de yt-dlp del propio
  usuario (`%APPDATA%\yt-dlp\config` y similares).
- Las miniaturas se descargan solo de direcciones `http` y `https`; las rutas locales y de red se ignoran.
- yt-dlp carga solo los complementos propios de Haul (`--no-plugin-dirs` y después `--plugin-dirs` con la carpeta
  `plugins` de Haul). Antes también cargaba cualquier complemento de `%APPDATA%\yt-dlp\plugins` y de sus otras
  carpetas predeterminadas, donde cualquier programa que ejecute el usuario puede dejar código Python.
- El programa de instalación que descarga una actualización se vuelve a comprobar justo antes de ejecutarse y se
  mantiene abierto hasta que Haul se cierra, así ningún otro programa puede cambiarlo, reemplazarlo ni borrarlo
  entre esa comprobación y su inicio.

### Corregido

- La ventana podía congelarse cuando una descarga terminaba mientras un proceso iniciado por yt-dlp seguía
  teniendo abierta su salida. Ahora el final de cada yt-dlp se espera fuera del hilo de la ventana, y un proceso que
  quedó atrás se detiene a los diez segundos, junto con todo lo que inició yt-dlp.
- Una fila cuyo último intento fallaba se ponía roja, y se podía quitar o reintentar, mientras yt-dlp seguía en
  marcha. Ahora se pone roja cuando yt-dlp termina.
- «Cancelar todo» con «Limpiar terminadas automáticamente» marcada quita las filas canceladas, como ya hacía una
  cancelación individual.
- Las carpetas de lista con nombre de dispositivo de Windows con extensión, o `COM5`–`COM9` y `LPT4`–`LPT9`
  (`NUL.x`, `COM7`), llevan el prefijo «Lista », como ya pasaba con `CON` y `NUL`.
- Instalación: la ventana ya no se puede cerrar mientras instala o actualiza FFmpeg y Deno. Antes, cerrarla
  preguntaba si cancelar y después decía «Se canceló. El equipo quedó como estaba.», aunque Haul ya estaba
  instalado.

### Accesibilidad

- A los colores de la ventana de apariencia se llega con Tab y se eligen con Enter o la barra espaciadora, y los
  lectores de pantalla leen sus nombres.
- Las filas de la cola se pueden reordenar con el teclado: Tab llega a cada fila, y Alt+Flecha arriba y Alt+Flecha
  abajo la mueven un lugar; el menú de la fila (clic derecho, Mayús+F10 o la tecla de menú) tiene «Mover arriba» y
  «Mover abajo». Los lectores de pantalla leen el título y el estado de cada fila.
- El rojo de una fila con error es más oscuro en una ventana clara (contraste de 5,2:1 o más) y más vivo en una
  oscura, así se lee con 4,5:1 o más en las dos.

### Documentación

- El README, el manual en PDF y este historial también están en japonés (`README.ja.md`,
  `docs/Haul-manual-ja.pdf`, `CHANGELOG.ja.md`), y el historial en español (`CHANGELOG.es.md`). El instalador
  pone `README.ja.txt` junto a `README.txt` y `README.es.txt`.
- Secciones nuevas en el README y los manuales: la ventana principal parte por parte, las opciones, la solución
  de problemas con cada mensaje y el uso responsable; imágenes de la ventana Acerca de Haul y de la página de
  desinstalación, y todas las imágenes dibujadas de nuevo con el ícono y la versión actuales.
- El README y el manual en japonés muestran Haul y su ventana de instalación en japonés y citan sus textos
  japoneses.

## 1.7.4 - 2026-11-27

### Cambiado

- **Un ícono nuevo**: una flecha de descarga que entra en una bandeja, dibujada en plano, crema con contorno negro
  sobre el color elegido, con el signo de reproducir de un video. Reemplaza al grabado del águila que lleva una
  liebre, y sigue el color elegido en los ajustes, en los doce colores. El dibujo está en `assets/icon.svg`.

## 1.7.3 - 2026-11-26

### Cambiado

- Trae **yt-dlp 2026.11.23** (era 2026.08.19), sin modificar, con su firma y sus sumas comprobadas con la clave de
  las versiones de yt-dlp (`AC0C BBE6 848D 6A87 3464 AF4E 57CF 6593 3B5A 7581`). La copia 2026.08.19 ya no descargaba
  de YouTube en Windows («Requested format is not available»); comprobado con una descarga real.
- Armar los manuales también se detiene cuando la versión de yt-dlp escrita bajo el título de un README no es la
  incluida.

## 1.7.2 - 2026-11-25

### Cambiado

- **Actualizaciones firmadas por el autor**: el `SHA256SUMS.txt` de una versión ahora tiene que llevar la firma del
  autor (`release.sig`, ECDSA P-256, comprobada con dos claves que trae Haul) antes de descargar el instalador. Antes,
  el instalador se comprobaba solo con sumas que venían de la misma página, así que una versión puesta ahí por
  otra persona se habría instalado sola. Una versión sin firma válida se ofrece como página, para descargarla a mano.
  Cualquiera puede seguir descargando, instalando y compartiendo Haul como antes; Haul 1.7.1 y anteriores instalan
  esta como siempre. Las actualizaciones de yt-dlp se comprueban como antes, con el `SHA2-256SUMS` del propio
  yt-dlp.

## 1.7.1 - 2026-11-20

### Corregido

- Una cola guardada que no se pudo leer al abrir Haul (el archivo estaba tomado por otro programa) después se
  sobrescribía: el primer guardado de la cola vacía la borraba. Ahora Haul deja ese archivo en paz durante la
  sesión, así que la cola vuelve la próxima vez.

## 1.7.0 - 2026-11-20

### Agregado

- Actualizaciones que se instalan solas. Una vez por día, o con «Buscar actualizaciones» en la ventana ⓘ, Haul le
  pregunta a GitHub por su última versión y, si es más nueva, pregunta una sola vez: «Actualizar ahora», «Ver
  novedades» o «Más tarde». «Actualizar ahora» descarga `haul-setup.exe`, lo compara con el `SHA256SUMS.txt` de la
  versión (un archivo que no coincide se borra y no se ejecuta nunca), pregunta qué hacer con una descarga en
  curso (actualizar ahora o cuando termine la cola), guarda la cola, cierra Haul, instala en silencio
  conservando el idioma instalado y vuelve a abrir Haul.
- El programa de instalación acepta `/HAULWAITPID=<pid>` (espera a que ese Haul se cierre antes de reemplazar
  nada) y `/HAULRELAUNCH=1` (vuelve a abrir Haul al final, como el usuario, haya funcionado o no la instalación).
  Un `/layout` ya no toca un Haul instalado ni ejecuta winget.
- Actualizaciones de yt-dlp desde la aplicación. Una vez por día (se puede apagar en la ventana ⓘ) y con
  «Actualizar yt-dlp», Haul descarga la última versión oficial de yt-dlp a `%LOCALAPPDATA%\Haul\yt-dlp\<versión>`, la
  compara con `SHA2-256SUMS` y con su propio número de versión, y la usa para las descargas siguientes; una
  descarga en curso sigue con la copia con la que empezó. La copia instalada con Haul queda como estaba.
- La cola sobrevive al cierre de Haul: las filas en cola, en pausa y con error y los enlaces que faltaba agregar
  vuelven en el mismo orden; una descarga que estaba en curso vuelve a la cola y sigue desde sus archivos a
  medias. Se guarda en `%APPDATA%\Haul\queue.json` a través de un archivo temporal; un archivo dañado se aparta
  como `queue.damaged.json`.
- `Haul.exe --self-test <registro>` comprueba la cola guardada, la lógica de versiones de Haul y de yt-dlp y las
  listas de sumas con datos descartables, sin ventana y sin tocar los ajustes ni la cola del usuario; el código
  de salida es la cantidad de fallas. `--red` agrega pruebas contra GitHub, incluida una actualización real de
  yt-dlp en la carpeta temporal.

### Cambiado

- Una ventana principal más compacta, para que la cola muestre varias filas en pantallas pequeñas (1366 × 768 o
  1280 × 720, con Windows al 125 % o al 150 %): encabezado, márgenes y tarjetas de formato más chicos, el destino
  y el botón Descargar en una línea, y las tres opciones bajo una línea plegable «Opciones» que nombra las
  activas y recuerda si quedó abierta. El tamaño mínimo ahora es 560 × 420; en una ventana demasiado baja para
  dos filas de la cola, la parte de arriba se desplaza con la cola en vez de aplastarla.
- El archivo de ajustes también se escribe a través de un archivo temporal.
- Las imágenes del README usan los ajustes iniciales y una cola de ejemplo, nunca los datos de la computadora en
  la que se dibujan; hay una nueva de la pregunta por una versión nueva.

## 1.6.0 - 2026-11-11

### Agregado

- Claro, oscuro o como Windows, en la nueva ventana de apariencia (el botón de la paleta en el encabezado).
  «Como Windows» sigue a Windows mientras Haul está abierto. Un archivo de ajustes de una versión anterior
  conserva la ventana oscura; una instalación nueva sigue a Windows.
- Doce colores. El elegido pinta los botones, las casillas, lo seleccionado y el ícono de la ventana y de su
  botón en la barra de tareas. Antes, la ventana tomaba el color de énfasis de Windows.
- Aviso de versiones nuevas: una vez por día Haul le pregunta a GitHub por el último número de versión y, si es
  más nueva, ofrece abrir la página de descarga. La ventana ⓘ lo apaga y pregunta cuando se le pide.
- Imágenes de la ventana en el README, dibujadas por `scripts/screenshots.ps1`.

### Cambiado

- Un ícono nuevo: el águila que lleva una liebre de un grabado del Monogramista B (Italia, siglo XVII;
  Rijksmuseum, dominio público), dibujado por `scripts/make_icon.py` en los doce colores, naranja de entrada.
- El README y los manuales dicen solo x64 y enlazan a la última versión publicada; el ejemplo de línea de
  comandos apunta al `Haul.exe` instalado en `C:\Program Files\Haul`.

## 1.5.0 - 2026-11-09

### Agregado

- Programa de instalación, `haul-setup.exe`, en inglés y en español, y la misma instalación como paquete MSI.
  Haul viene compilado, así que ya no hace falta el SDK de .NET. La ventana de instalación instala o actualiza
  FFmpeg y Deno con winget.
- Este historial de versiones.

### Cambiado

- Haul se instala para todos los usuarios en `C:\Program Files\Haul`. El instalador quita una instalación hecha
  con el script de la 1.4, conservando sus ajustes.

## 1.4.0 - 2026-10-26

### Agregado

- Pausa para cada fila y para la cola entera; los archivos a medias se conservan y la descarga retoma.
- Reintentar todas las descargas fallidas con un solo botón.
- Arrastrar filas para cambiar el orden de la cola.
- Ajuste «Bajar listas enteras», en vez de una pregunta cada vez.

### Cambiado

- Todo hasta el botón Descargar queda fijo; solo la cola se desplaza.
- Se quitaron el registro técnico y la frase bajo el título.

### Corregido

- Ahora se descargan las publicaciones que enlazan a un video no listado en vez de embeberlo.

## 1.3.0 - 2026-10-24

### Agregado

- Complemento propio de Haul para yt-dlp, para enlaces que el yt-dlp incluido no maneja.

### Cambiado

- La ventana es siempre oscura, sea cual sea la apariencia del sistema.
- La documentación y los mensajes describen Haul como una interfaz de yt-dlp, sin nombrar sitios.

## 1.2.1 - 2026-10-21

### Cambiado

- «Limpiar terminadas automáticamente» es una casilla debajo del formato, siempre visible; antes estaba en el
  encabezado de la cola, que se oculta cuando la cola está vacía.

### Corregido

- El recuadro de enlaces ya no se vuelve a llenar con el mismo enlace cuando la ventana pasa al frente.

## 1.2.0 - 2026-10-21

### Agregado

- Una descarga o lectura de lista que falla se reintenta hasta cuatro veces, con dos segundos entre cada intento.
- Las filas terminadas y canceladas pueden salir solas de la lista.
- Las descargas con «instrumental» en el título esperan en pausa salvo que esté marcada «Descargar
  instrumentales».

### Cambiado

- El encabezado queda fijo mientras el resto de la ventana se desplaza.

### Corregido

- Los archivos intermedios que yt-dlp no pudo borrar, porque otro programa los tenía abiertos, se quitan después
  de la descarga.

## 1.1.0 - 2026-10-20

Primera versión para Windows, con las mismas funciones que la versión 1.1.0 de macOS: cola de descargas, formatos
MP4 y MP3, listas desplegadas en una fila por elemento, limpieza de las descargas canceladas, ventana Acerca de e
interfaz en inglés y en español. Trae el yt-dlp.exe oficial 2026.08.19, verificado con sus sumas firmadas;
`scripts\instalar.ps1` instala el SDK de .NET, FFmpeg y Deno con winget.
