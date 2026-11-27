Haul para Windows
=================

Gabriel Castiglia · 27 de noviembre de 2026 · versión 1.8.0 · GPL-2.0-only · hecho y probado con
yt-dlp 2026.11.23

English (README.txt) · Español · 日本語 (README.ja.txt)

Versión para macOS: https://github.com/Gabriel-Castiglia/haul (solo Apple silicon)

  Solo para Windows 10/11 en x64 (Intel o AMD de 64 bits). No hay versión para procesadores ARM.

Manual en PDF: docs/Haul-manual-es.pdf

Haul es una aplicación de Windows que hace de interfaz de yt-dlp <https://github.com/yt-dlp/yt-dlp>.
La aplicación arma el comando, muestra el avance y maneja una cola de descargas. El trabajo de fondo
lo hacen yt-dlp, incluido en la aplicación en una versión fija y verificada, y ffmpeg. Es la hermana
para Windows de la aplicación de macOS del mismo nombre: mismas funciones, mismos formatos, mismo
comportamiento. La ventana puede ser clara, oscura o seguir a Windows, en uno de doce colores, y la
interfaz está en inglés, español y japonés; se abre en el idioma elegido en la ventana de
instalación o, si no lo hay, en el de Windows cuando es español o japonés (si no, en inglés), y
recuerda la elección. Haul se mantiene actualizado solo, y mantiene actualizado a yt-dlp.

Requisitos
----------

- Windows 11, o Windows 10 versión 1809 o posterior, en una PC x64 (Intel o AMD de 64 bits). Probado
  en Windows 11 Pro.
- winget <https://learn.microsoft.com/windows/package-manager/winget/> (el «Instalador de
  aplicación» de Microsoft Store, incluido en Windows 11), que el programa de instalación usa para
  instalar FFmpeg y Deno.
- Conexión a internet, para descargar y para buscar actualizaciones.

yt-dlp viene con la aplicación, y Haul trae su propio entorno de .NET, así que no hay que instalar
nada a mano.

Instalación
-----------

Descarga haul-setup.exe de la
última versión publicada <https://github.com/Gabriel-Castiglia/haul-windows/releases/latest> y
ejecútalo. La ventana de instalación se abre en el idioma del Haul instalado, o en español o japonés
en un Windows en ese idioma, o si no en inglés; su menú de idioma, arriba a la derecha, cambia todas
las páginas al instante. Instala Haul para todos los usuarios en C:\Program Files\Haul, lo que pide
permisos de administrador (Windows los pide al empezar la instalación), y agrega Haul al menú
Inicio. Después, con el usuario que lo ejecutó, instala FFmpeg y Deno con winget, o los actualiza si
ya están; yt-dlp necesita FFmpeg para unir imagen y sonido y para convertir a MP3, y Deno para
resolver desafíos de JavaScript. El idioma elegido en la ventana de instalación es el que Haul usa
la primera vez que se abre.

  Archivo          Qué es
  ---------------- -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  haul-setup.exe   Programa de instalación, en inglés, español o japonés. La forma habitual de instalar.
  haul.msi         La misma instalación como paquete de Windows Installer, para instalar sin ventanas: msiexec /i haul.msi /qn. No instala FFmpeg ni Deno; se instalan con winget install yt-dlp.FFmpeg DenoLand.Deno.

Ejecutar un haul-setup.exe más nuevo actualiza el Haul instalado y conserva los ajustes de cada
usuario. Si Haul está abierto, el programa de instalación lo cierra antes de reemplazar sus
archivos. Una instalación hecha con los scripts de la versión 1.4 o anteriores (en
%LOCALAPPDATA%\Programs\Haul) se quita, conservando sus ajustes, para que quede un solo Haul. Una
versión más antigua no se puede instalar encima de una más nueva. Haul también se puede actualizar
solo: ver Actualizaciones (#actualizaciones).

Los archivos no están firmados con un certificado comercial, así que la primera vez Windows
SmartScreen puede mostrar «Windows protegió su PC». «Más información» → «Ejecutar de todas formas»
los abre. El archivo SHA256SUMS.txt de cada versión permite comprobar antes la descarga:
Get-FileHash haul-setup.exe.

Desinstalación
--------------

Abre Configuración → Aplicaciones → Aplicaciones instaladas, busca Haul y elige Desinstalar. Se abre
la misma ventana de instalación, con su menú de idioma. Cierra Haul si está abierto y quita el
programa y el acceso del menú Inicio. Los archivos descargados, los ajustes y la cola guardada de
cada usuario (%APPDATA%\Haul) se conservan, igual que las copias de yt-dlp que descargó Haul
(%LOCALAPPDATA%\Haul); borrar esas dos carpetas los quita. FFmpeg y Deno siguen instalados, porque
otros programas pueden usarlos; winget uninstall yt-dlp.FFmpeg DenoLand.Deno los quita.

Uso
---

1.  Pegar uno o varios enlaces en el recuadro superior, o pulsar «Pegar». Se aceptan enlaces
    sueltos, uno por línea o dentro de un texto copiado de otra parte; la aplicación los extrae y
    descarta los duplicados. Cuando la aplicación se abre o pasa al frente con el recuadro vacío,
    toma los enlaces del portapapeles.
2.  Elegir el formato, Video (MP4) o Solo audio (MP3).
3.  Elegir la carpeta de destino con «Cambiar…». La primera vez es Descargas; después se conserva la
    última elegida, igual que el formato.
4.  Pulsar Descargar o Ctrl+Enter. El recuadro se vacía al empezar la descarga, y un enlace que ya
    está en la cola no vuelve a entrar solo desde el portapapeles.

También se puede iniciar con enlaces desde una terminal o un acceso directo:

    "C:\Program Files\Haul\Haul.exe" --descargar URL1 URL2

La ventana principal
--------------------

  Parte                    Qué hace
  ------------------------ -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Menú de idioma (globo)   Cambia la interfaz entre inglés, español y japonés (日本語) al instante; la elección se recuerda.
  Botón de la paleta       Abre la ventana de apariencia: clara, oscura o como Windows, y el color (ver Ventana y tema (#ventana-y-tema)).
  Botón ⓘ                  Abre «Acerca de Haul»: versión, el nombre, autor, licencia, código fuente y los ajustes de actualización (ver Actualizaciones (#actualizaciones)).
  Recuadro de enlaces      Recibe los enlaces. La línea de abajo los cuenta («2 enlaces · Ctrl+Enter para descargar») o dice «No se encontró ningún enlace». «Borrar» vacía el recuadro; «Pegar» pega el portapapeles.
  Video / Solo audio       El formato: «MP4 · mejor calidad» o «MP3 · máxima calidad» (ver Formatos (#formatos)).
  Guardar en               La carpeta de destino. «Cambiar…» elige otra; el botón de al lado la abre en el Explorador.
  Descargar                Empieza la descarga. Dice «Descargar 3 enlaces» con varios enlaces, y «Agregar a la cola» mientras la cola avanza.
  Opciones                 Las tres opciones, plegadas de entrada (ver Opciones (#opciones)).
  Cola                     Las descargas, una por fila, con un resumen («2 de 5 listas · 1 con error · 1 en pausa») y los botones «Cancelar todo», «Pausar todo», «Reanudar todo» y «Reintentar todas», cada uno cuando corresponde.
  Línea de abajo           La versión de Haul y la de yt-dlp («incluido en la app» o «actualizado por Haul»), y el avance de una actualización.

Si no se encuentra yt-dlp, ffmpeg o Deno, un aviso encima de la cola dice cuál falta («Falta ffmpeg.
Vuelve a ejecutar haul-setup.exe, que instala FFmpeg y Deno con winget.»).

Desde el encabezado hasta las opciones todo queda fijo; solo la cola se desplaza, así que agregar
enlaces nunca mueve la ventana.

La cola
-------

Los enlaces entran en una cola y se descargan de uno en uno. Mientras la cola avanza se pueden
agregar más enlaces. Cada fila muestra la miniatura, el título, el porcentaje, la velocidad y el
tiempo restante, y lo que se está haciendo («Preparando…», «Uniendo imagen y sonido…», «Convirtiendo
a MP3…»). Una fila terminada muestra el canal y la duración, con botones para mostrar el archivo en
el Explorador («Mostrar en el Explorador») o abrirlo («Abrir»). Cada fila tiene los botones que le
corresponden, con su ayuda emergente: «Pausar», «Reanudar», «Cancelar esta descarga», «Probar de
nuevo», «Quitar de la cola». Una descarga cancelada borra los archivos a medias que había creado;
los que ya estaban en la carpeta, como una descarga anterior del mismo video en otro formato, no se
tocan nunca.

Cada fila se puede pausar con su botón de pausa y reanudar con el de reproducir; mientras tanto, los
archivos a medias quedan en el disco y la descarga retoma donde quedó. «Pausar todo», encima de la
cola, detiene la cola entera: la descarga en curso se detiene y espera primera en la fila hasta
«Reanudar todo». «Cancelar todo» cancela todo lo que no terminó. «Reintentar todas» aparece cuando
alguna descarga falló y vuelve a lanzar todas las fallidas. Las descargas empiezan en el orden de la
lista, que se puede cambiar arrastrando las filas con el mouse o con el teclado: Tab llega a cada
fila y a sus botones, y Alt+Flecha arriba o Alt+Flecha abajo mueve la fila un lugar. El menú de la
fila (clic derecho, Mayús+F10 o la tecla de menú) tiene los mismos movimientos, «Mover arriba» y
«Mover abajo».

Una descarga que falla se reintenta hasta cuatro veces, con dos segundos entre cada intento, y la
fila muestra qué intento está en curso («Reintento 2/4»). Los archivos a medias se conservan entre
intentos, así yt-dlp sigue desde donde quedó. Si fallan todos los intentos, la fila se pone roja con
el motivo (ver Solución de problemas (#solución-de-problemas)) cuando yt-dlp termina; hasta entonces
solo se puede cancelar. Cuando una descarga termina, la aplicación borra además los archivos
intermedios que yt-dlp no pudo quitar, como el audio original de un MP3 mientras un cliente de
sincronización de archivos lo tenía abierto.

La cola sobrevive al cierre de Haul: lo que está en cola, en pausa o con error, y los enlaces que
faltaba agregar, vuelven la próxima vez que se abre, en el mismo orden. Una descarga que estaba en
curso al cerrar vuelve a la cola y sigue desde sus archivos a medias; una lista que se estaba
leyendo se lee de nuevo. Las terminadas y las canceladas no se guardan. La cola se guarda en
%APPDATA%\Haul\queue.json, siempre a través de un archivo temporal que después reemplaza al
anterior, así que un corte nunca deja medio archivo; un archivo dañado se aparta como
queue.damaged.json y Haul se abre con la cola vacía.

Opciones
--------

«Descargar listas enteras», «Descargar instrumentales» y «Limpiar terminadas automáticamente» están
en «Opciones», debajo del botón Descargar. La línea viene plegada, para dejarle lugar a la cola, y
nombra las opciones activas («Opciones: listas enteras»); un clic la abre, y Haul recuerda si quedó
abierta. Cada opción tiene una ayuda emergente que la explica, y la aplicación las recuerda.

  Opción                                     Marcada                                                                                                                                                                                                   Desmarcada
  ------------------------------------------ --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- ---------------------------------------------------------------------------------------
  Descargar listas enteras (viene marcada)   Un video abierto desde una lista trae la lista entera.                                                                                                                                                    Una tarjeta pregunta cada vez (ver Listas de reproducción (#listas-de-reproducción)).
  Descargar instrumentales                   Los videos con «instrumental» en el título se descargan como los demás.                                                                                                                                   Esperan en la cola, en pausa («En pausa: es un instrumental»).
  Limpiar terminadas automáticamente         Cada descarga que termina o se cancela sale sola de la lista; el resumen la sigue contando. Las que fallaron quedan, para poder reintentarlas. Al marcarla también se limpia lo que ya había terminado.   Las filas terminadas quedan hasta que se quitan.

Un instrumental en pausa se puede descargar por separado con su botón de descarga («Descargar
igual»), y al marcar «Descargar instrumentales» se reanudan todos.

Listas de reproducción
----------------------

Un enlace a una lista o a la página de un canal se lee primero, sin descargar nada («Leyendo la
lista…»), y cada video pasa a ser una fila propia de la cola, con su miniatura y su posición («Mix ·
3/12»). Los archivos van a una subcarpeta con el nombre de la lista y se numeran en el orden de la
lista: 01 - Título.mp4, 02 - Título.mp4, etcétera. Cada video se puede cancelar o reintentar por
separado. Una lista de más de 200 videos pide confirmación antes de entrar a la cola («Una lista
grande»: «Agregar 250» o «Cancelar»).

Un enlace a un video abierto desde una lista trae la lista entera mientras esté marcada «Descargar
listas enteras», que viene marcada. Con la opción desmarcada, una tarjeta en la ventana pregunta
cada vez («Este video está dentro de una lista»): «La lista entera», «Solo este video» o «Cancelar».
Los demás enlaces se descargan tal cual, un video cada uno.

Algunas páginas de etiqueta que agrupan varias publicaciones, cada una con un video embebido, se
leen igual: una fila por publicación, de la más antigua a la más nueva, así una serie conserva el
orden de sus episodios.

Formatos
--------

  Formato      Resultado                                                        Opciones de yt-dlp
  ------------ ---------------------------------------------------------------- ---------------------------------------------------------------------
  Video        MP4 con H.264 y AAC, a la mayor resolución disponible en H.264   -f bv*+ba/b -S vcodec:h264,res,acodec:aac --merge-output-format mp4
  Solo audio   MP3 a la máxima calidad                                          -x --audio-format mp3 --audio-quality 0

Se prefiere H.264 porque cualquier reproductor de Windows lo abre. Los archivos llevan el título del
video como nombre. No hay un menú de calidad: Haul siempre pide lo mejor que el sitio ofrece en ese
formato.

Ventana y tema
--------------

La ventana es compacta, así que la cola tiene lugar para varias filas aun en una pantalla de 1366 ×
768 o 1280 × 720 con Windows al 125 % o al 150 %: la carpeta de destino y el botón Descargar
comparten una línea, y las opciones se pliegan. No se puede reducir por debajo de 560 × 420. Cuando
la ventana es tan baja que a la cola le quedaría lugar para menos de dos filas, la parte de arriba
se desplaza junto con la cola en vez de aplastarla, y vuelve a su lugar fijo cuando la ventana
crece. El tamaño y la posición se recuerdan entre sesiones.

La aplicación usa el tema Fluent de Windows 11, claro, oscuro o como Windows (lo que esté elegido
para las aplicaciones en Configuración > Personalización > Colores, y lo sigue con la aplicación
abierta). El botón de la paleta, en el encabezado, abre la ventana de apariencia, que también tiene
doce colores: Naranja (el de siempre), Dorado, Morado, Celeste, Rojo, Amarillo, Verde, Verde
azulado, Azul, Rosa, Negro y Blanco. El elegido pinta los botones, las casillas y lo seleccionado, y
el ícono de la ventana y de su botón en la barra de tareas, donde la flecha y la bandeja quedan
sobre un fondo de ese color. El ícono del menú Inicio sigue naranja, porque Haul se instala para
todos los usuarios y ese acceso es de todos. A los colores también se llega con Tab y se eligen con
Enter o la barra espaciadora, y los lectores de pantalla leen sus nombres. Un archivo de ajustes de
una versión anterior conserva la ventana oscura que tenía; una instalación nueva sigue a Windows.

Actualizaciones
---------------

El botón ⓘ abre «Acerca de Haul», que tiene los ajustes de actualización de Haul y de yt-dlp.

Haul. Una vez por día (mientras esté marcada «Buscar una versión nueva una vez al día», como viene)
y cada vez que se pulsa «Buscar actualizaciones», Haul le pregunta a GitHub por su última versión.
Si es más nueva, una ventana pequeña pregunta una sola vez: «Actualizar ahora», «Ver novedades»
(abre la página de la versión) o «Más tarde». Si se pulsa «Buscar actualizaciones» mientras ya hay
una búsqueda, una descarga o una espera de la cola en curso, Haul lo avisa («Ya hay una
actualización de Haul en curso»), y su avance se ve en la línea de abajo.

Con «Actualizar ahora» todo pasa solo: Haul comprueba primero que el SHA256SUMS.txt de la versión
lleve la firma del autor (release.sig, que se comprueba con dos claves que trae Haul; una versión
sin firma válida se rechaza y solo se puede descargar a mano desde su página), después descarga
haul-setup.exe y lo compara con ese SHA256SUMS.txt; un archivo que no coincide se borra y no se
ejecuta nunca. Justo antes de ejecutarlo lo vuelve a comprobar y lo mantiene abierto, de modo que
ningún otro programa pueda cambiarlo, reemplazarlo ni borrarlo hasta que Haul se haya cerrado. Si
hay una descarga en curso, Haul pregunta antes («Hay descargas en la cola»): «Actualizar ahora» (la
descarga se detiene y sigue desde sus archivos a medias cuando Haul vuelve), «Cuando termine la
cola» o «Cancelar». Después guarda la cola, se cierra y ejecuta el programa de instalación en
silencio, conservando el idioma elegido al instalar; Windows pide permiso, porque Haul se instala
para todos los usuarios. El programa de instalación espera a que Haul se cierre, instala y vuelve a
abrir Haul con su cola. Si la instalación no se hizo (por ejemplo, porque se negó el permiso), el
Haul que se abre lo dice. La consulta a GitHub no lleva más que el nombre del programa y su versión.

La actualización silenciosa es del propio programa de instalación:
haul-setup.exe /quiet /norestart /HAULWAITPID=<pid> /HAULRELAUNCH=1 espera a que termine ese proceso
y abre Haul al final. No toca FFmpeg ni Deno; ejecutar haul-setup.exe a mano los actualiza.

yt-dlp. Los sitios cambian seguido, así que Haul también puede traer un yt-dlp más nuevo que el que
vino al instalarlo. Una vez por día (mientras esté marcada «Actualizar yt-dlp solo», como viene) y
cada vez que se pulsa «Actualizar yt-dlp», Haul le pregunta a GitHub por la última versión oficial
de yt-dlp. Una más nueva se descarga en %LOCALAPPDATA%\Haul\yt-dlp\<versión>\, se compara con el
SHA2-256SUMS de la versión y se ejecuta una vez para confirmar su número de versión; solo entonces
Haul empieza a usarla, y la línea de abajo de la ventana dice «actualizado por Haul». Una descarga
que ya estaba en curso termina con la copia con la que empezó; las siguientes usan la nueva. La
copia que vino con Haul no se cambia nunca y se vuelve a usar si no hay una copia comprobada más
nueva; las copias descargadas más antiguas se borran al abrir Haul. La parte «Motor: yt-dlp» de la
ventana Acerca de Haul dice qué copia está en uso.

Solución de problemas
---------------------

Los errores más comunes de yt-dlp se muestran en palabras simples en el idioma de la interfaz; el
resto aparece tal como lo escribe yt-dlp.

  Mensaje o síntoma                                                                         Qué significa, qué hacer
  ----------------------------------------------------------------------------------------- --------------------------------------------------------------------------------------------------------------------------------------------
  «Ese formato no está disponible para este video.»                                         Casi siempre el sitio cambió y este yt-dlp ya no lo lee. Pulsa «Actualizar yt-dlp» en la ventana ⓘ y después «Probar de nuevo» en la fila.
  «El sitio pide verificar que no eres un robot: instala la versión más nueva de Haul.»     Actualiza yt-dlp, y Haul («Buscar actualizaciones»).
  «El video no está disponible (borrado, privado o bloqueado).» · «Es un video privado.»    El sitio no ofrece el video a quien no tiene acceso; Haul no lo puede descargar.
  «Pide confirmar la edad con una cuenta: no se puede descargar así.»                       Haul no inicia sesión en los sitios, así que estos videos no se pueden descargar.
  «Ese sitio no está soportado por yt-dlp.» · «Eso no parece un enlace válido.»             Revisa el enlace; yt-dlp no conoce ese sitio.
  «No se pudo abrir la página: revisa la conexión.»                                         No hay conexión, o el sitio no respondió. «Probar de nuevo» más tarde.
  «Ese canal no tiene videos para descargar.» · «La lista no existe o es privada.»          El canal o la lista no tienen nada público para descargar.
  «Falta ffmpeg. Vuelve a ejecutar haul-setup.exe, que instala FFmpeg y Deno con winget.»   Ejecuta de nuevo haul-setup.exe, o winget install yt-dlp.FFmpeg DenoLand.Deno. Sin ffmpeg no se unen imagen y sonido ni se genera el MP3.
  «No se pudo iniciar yt-dlp: …» · «yt-dlp terminó con el código …»                         Los archivos del programa están dañados o bloqueados (por ejemplo, por un antivirus). Ejecuta de nuevo haul-setup.exe.
  «La cola guardada estaba dañada y se apartó como queue.damaged.json»                      Haul se abrió con la cola vacía; el archivo anterior queda en %APPDATA%\Haul para revisarlo.
  «Haul … no se instaló (quizás no se dio el permiso de Windows).»                          La actualización no se instaló. Prueba de nuevo desde ⓘ › «Buscar actualizaciones» y acepta el aviso de Windows.
  «La firma de esta versión no es válida: no se descargó ni se instaló nada.»               La versión no lleva una firma válida del autor y Haul la rechaza. Descárgala a mano desde su página solo si confías en ella.
  «No se pudo actualizar yt-dlp: …»                                                         Se sigue usando el yt-dlp anterior; prueba de nuevo más tarde.
  «Ya hay una actualización de Haul en curso; su avance se ve al pie de la ventana.»        Se pulsó «Buscar actualizaciones» durante una actualización; espera a que termine.
  «Windows protegió su PC» al ejecutar el instalador                                        El instalador no está firmado con un certificado comercial: «Más información» → «Ejecutar de todas formas».

Las preferencias (idioma, carpeta, formato, tamaño y posición de la ventana, apariencia, color, las
opciones y si están plegadas, y los ajustes de actualización) se guardan en
%APPDATA%\Haul\settings.json, y la cola en %APPDATA%\Haul\queue.json. Borrar settings.json con Haul
cerrado devuelve las preferencias a su valor inicial.

Uso responsable
---------------

Haul descarga lo que un sitio ya deja ver a cualquiera, y solo a través de yt-dlp. Descargar puede
estar limitado por las leyes de derecho de autor y por las condiciones de uso de cada sitio. Usa
Haul solo para contenido que tengas derecho a guardar: el tuyo, el que tiene una licencia libre o es
de dominio público, o aquel cuyo titular lo permite, y respeta las leyes de tu país. Haul y yt-dlp
no descifran contenido protegido con DRM. Haul se distribuye sin ninguna garantía (ver
Licencia (#licencia)).

Mantenimiento
-------------

Haul está hecho y probado con yt-dlp 2026.11.23, y esa copia exacta viene con la aplicación. Una
versión fija mantiene el comportamiento estable y no depende de que el repositorio de yt-dlp siga en
línea. La desventaja es que los sitios cambian seguido, y tarde o temprano una versión fija deja de
funcionar con ellos; entre una versión de Haul y la siguiente, eso lo cubre la actualización de
yt-dlp desde la aplicación (ver Actualizaciones (#actualizaciones)). Una versión nueva de Haul pasa
la copia incluida a un yt-dlp más nuevo:

    powershell -ExecutionPolicy Bypass -File scripts\actualizar-yt-dlp.ps1 <versión>
    powershell -ExecutionPolicy Bypass -File scripts\build.ps1

El primer script descarga la versión, verifica su firma con la clave guardada en
vendor\yt-dlp\public.key (con el gpg que trae Git para Windows) y sus sumas SHA-256, y solo entonces
reemplaza los archivos y renueva los textos de licencia. Necesita Python para leer las licencias
guardadas dentro de yt-dlp.exe. Ejecutar el haul-setup.exe nuevo también actualiza FFmpeg y Deno.

Estructura del repositorio
--------------------------

  Ruta                                                                        Contenido
  --------------------------------------------------------------------------- -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  fuentes/Haul/                                                               Código de la aplicación (C#, WPF sobre .NET 10)
  fuentes/Haul/Core/                                                          Cola y su copia guardada, listas, enlaces, preferencias, actualizaciones, el proceso de yt-dlp y los textos de la interfaz en inglés, español y japonés (Textos.cs)
  assets/icon.svg                                                             El dibujo del ícono (en sus doce colores lo arma la herramienta de íconos del autor, la misma de la versión de macOS)
  icono/Haul.ico, icono/icono-256.png, icono/icono-1024.png, icono/accents/   El ícono, en su naranja de siempre y en cada color
  installer/Msi/                                                              El paquete MSI (WiX)
  installer/SetupUI/                                                          La ventana de instalación, en inglés, español y japonés (.NET Framework 4.7.2, WPF; sus textos en SetupTexts.cs), que además ejecuta winget
  installer/Setup/                                                            El programa de instalación que lleva la ventana y el MSI (paquete de WiX)
  scripts/build.ps1                                                           Comprueba el yt-dlp incluido y arma Haul, haul.msi y haul-setup.exe en dist\
  scripts/actualizar-yt-dlp.ps1                                               Reemplaza el yt-dlp incluido por otra versión verificada
  scripts/extraer_licencias.py                                                Lee las licencias de terceros guardadas dentro de yt-dlp.exe
  scripts/screenshots.ps1                                                     Dibuja las imágenes de las ventanas y de la ventana de instalación en docs/images, en los tres idiomas, con datos de ejemplo
  scripts/manual.sh                                                           Arma los tres manuales en PDF y README.txt, README.es.txt, README.ja.txt a partir de los README (en macOS, o en Git Bash en Windows, con pandoc y Google Chrome o Microsoft Edge)
  plugins/haul/                                                               Complemento de extractores propio de Haul para yt-dlp, para enlaces que la versión incluida no maneja
  vendor/yt-dlp/                                                              Copia incluida de yt-dlp 2026.11.23, con fuentes, sumas, firma y licencias
  docs/Haul-manual-en.pdf, docs/Haul-manual-es.pdf, docs/Haul-manual-ja.pdf   Manuales en PDF, en inglés, español y japonés
  docs/manual.css, docs/images/                                               Hoja de estilo e imágenes de los manuales
  README.md, README.es.md, README.ja.md                                       Esta documentación, en inglés, español y japonés
  README.txt, README.es.txt, README.ja.txt                                    Copias en texto plano que el instalador pone junto a Haul.exe
  CHANGELOG.md, CHANGELOG.es.md, CHANGELOG.ja.md                              Historial de versiones, en inglés, español y japonés

Las carpetas build\ y dist\ y las carpetas bin\ y obj\ del compilador se generan al compilar y no
forman parte del repositorio.

Para compilar desde el código, con el SDK de .NET 10 <https://dotnet.microsoft.com/download>
instalado:

    git clone https://github.com/Gabriel-Castiglia/haul-windows.git
    cd haul-windows
    powershell -ExecutionPolicy Bypass -File scripts\build.ps1

build.ps1 se niega a compilar si el yt-dlp.exe incluido no coincide con sus sumas oficiales. Publica
Haul como un Haul.exe autónomo, con yt-dlp.exe, plugins\ y licenses\ al lado, y arma dist\haul.msi y
dist\haul-setup.exe; el WiX Toolset se descarga solo como paquete NuGet. La versión se fija en un
solo lugar, fuentes\Haul\Haul.csproj.

Haul.exe --self-test <registro> comprueba los textos de la interfaz de Haul y de la ventana de
instalación (los tres idiomas deben tener las mismas claves y los mismos marcadores), la cola
guardada, el movimiento de filas, la lógica de versiones de Haul y de yt-dlp, las listas de sumas y
los argumentos con los que se inicia yt-dlp, con datos descartables, sin ventana y sin tocar los
ajustes ni la cola del usuario; el código de salida es la cantidad de fallas. --red agrega pruebas
contra GitHub, incluida una actualización real de yt-dlp en la carpeta temporal.

Notas técnicas
--------------

La aplicación ejecuta el yt-dlp.exe incluido como proceso hijo oculto, siempre con --ignore-config:
yt-dlp no lee ningún archivo de configuración, ni los del usuario (%APPDATA%\yt-dlp\config y
similares) ni un yt-dlp.conf que una página web pudiera haber dejado en la carpeta de descargas, así
que el resultado nunca depende de archivos ajenos a Haul. También se ejecuta con --no-plugin-dirs y
después --plugin-dirs con la carpeta plugins de Haul, así que los únicos complementos que carga
yt-dlp son los de Haul: ninguno de %APPDATA%\yt-dlp\plugins ni de las demás carpetas que yt-dlp
revisa de forma predeterminada, donde cualquier programa podría dejar código Python. Una aplicación
abierta desde el menú Inicio hereda el PATH que tenía el Explorador al iniciarse, que no incluye lo
instalado después con winget; por eso Haul rearma el PATH desde el registro y le pasa a yt-dlp la
ubicación de ffmpeg. Para no depender del texto que yt-dlp imprime para una terminal, le pide la
salida en un formato propio, separado por tabulaciones, igual que la versión de macOS:

  Línea     Contenido                                                                 Origen
  --------- ------------------------------------------------------------------------- ---------------------
  META      id, canal, autor, título y miniatura, que se revisan antes de descargar   -O pre_process:…
  INFO      título, autor, duración, miniatura y nombre del archivo                   -O before_dl:…
  AVANCE    porcentaje, velocidad y tiempo restante                                   --progress-template
  ARCHIVO   ruta final del archivo                                                    -O after_move:…

Los instrumentales en pausa se retienen con --match-filters, así yt-dlp no descarga nada hasta que
se permiten.

Algunos enlaces fallan dentro del propio yt-dlp, incluso en su última versión: una página de video
que ahora pide una cuenta aunque su reproductor embebido no la pide, y páginas cuyos datos se
mudaron de lugar. Haul trae un complemento de extractores propio en plugins/haul/, que se instala en
una carpeta plugins junto a yt-dlp.exe y se pasa con --plugin-dirs (la única carpeta de complementos
que usa yt-dlp). Los extractores de un complemento se prueban antes que los incluidos en yt-dlp, y
cada uno solo toma los enlaces que arregla. Cuando el reproductor embebido responde con un error 401
esporádico, el complemento reintenta hasta tres veces.

Las listas se leen con --flat-playlist -J, que enumera los videos sin descargarlos. Cada yt-dlp se
ejecuta en un objeto de trabajo de Windows junto con los procesos que inicia. Cancelar detiene
yt-dlp junto con ffmpeg (todo el árbol de procesos). El final de cada proceso se espera fuera del
hilo de la ventana: cuando yt-dlp termina, su salida tiene diez segundos para cerrarse, y si un
proceso que dejó atrás la sigue teniendo abierta, se detiene todo el objeto de trabajo, así la
ventana nunca se congela esperándolo. Con -O, yt-dlp no imprime nada más, así que los archivos de
una descarga se reconocen por el nombre de su línea INFO: al empezar, la aplicación anota qué
archivos con ese nombre ya existen, y al cancelar borra solo los nuevos.

Créditos
--------

Haul es una interfaz; el trabajo pesado es de estos proyectos:

  Proyecto                                                                                                               Papel en Haul                                     Licencia
  ---------------------------------------------------------------------------------------------------------------------- ------------------------------------------------- -----------------------------------------------------------------------------------
  yt-dlp <https://github.com/yt-dlp/yt-dlp>, de los colaboradores de yt-dlp, derivado de youtube-dl                      Motor de descarga, incluido en vendor/yt-dlp      Unlicense <https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE>
  Componentes dentro de yt-dlp.exe: Python, OpenSSL, curl, curl_cffi, certifi, mutagen, websockets, brotli y otros       Entorno y bibliotecas del ejecutable de Windows   Las suyas, listadas en vendor/yt-dlp/licenses/yt-dlp-exe-THIRD_PARTY_LICENSES.txt
  yt-dlp-ejs <https://github.com/yt-dlp/ejs>, incluido en yt-dlp                                                         Resuelve desafíos de JavaScript                   Unlicense
  meriyah <https://github.com/meriyah/meriyah>, de KFlash y otros, dentro de yt-dlp-ejs                                  Analizador de JavaScript                          ISC <https://github.com/meriyah/meriyah/blob/main/LICENSE.md>
  astring <https://github.com/davidbonnet/astring>, de David Bonnet, dentro de yt-dlp-ejs                                Generador de código JavaScript                    MIT <https://github.com/davidbonnet/astring/blob/main/LICENSE>
  FFmpeg <https://ffmpeg.org>, compilado por el proyecto yt-dlp, instalado con winget, no incluido                       Une imagen y sonido, convierte a MP3              GPL-3.0+ <https://ffmpeg.org/legal.html> en esta compilación
  Deno <https://deno.com>, instalado con winget                                                                          Entorno JavaScript que usa yt-dlp                 MIT <https://github.com/denoland/deno/blob/main/LICENSE.md>
  .NET <https://dotnet.microsoft.com> y WPF, dentro de Haul.exe                                                          Entorno de ejecución e interfaz gráfica           MIT <https://github.com/dotnet/wpf/blob/main/LICENSE.TXT>
  WiX Toolset <https://wixtoolset.org>, solo para armar los instaladores                                                 Paquete MSI y programa de instalación             MS-RL <https://github.com/wixtoolset/wix/blob/main/LICENSE.TXT>
  Segoe Fluent Icons <https://learn.microsoft.com/windows/apps/design/style/segoe-fluent-icons-font>, parte de Windows   Íconos de la interfaz                             Licencia de Windows
  pandoc <https://pandoc.org>, solo para armar los manuales en PDF                                                       Conversión de Markdown a HTML                     GPL-2.0+ <https://github.com/jgm/pandoc/blob/main/COPYRIGHT>

Los textos de licencia de los componentes incluidos están en vendor/yt-dlp/licenses/ y se instalan
junto a la aplicación, en licenses\.

Licencia
--------

Copyright (C) 2026 Gabriel Castiglia.

Haul es software libre, publicado bajo la Licencia Pública General de GNU, versión 2 (GPL-2.0-only),
la misma del núcleo Linux. Se puede usar, estudiar, modificar y redistribuir; toda versión
modificada que se distribuya tiene que publicarse con la misma licencia y con su código fuente. Se
distribuye sin ninguna garantía. El texto completo está en LICENSE.

Haul incluye una copia sin modificar de yt-dlp en vendor/yt-dlp, que conserva sus propias licencias
(Unlicense, más las de los componentes dentro de yt-dlp.exe) y no está cubierta por la GPL. Haul
ejecuta yt-dlp y ffmpeg como programas separados y no se enlaza con ellos; ffmpeg no se incluye.

El ícono, una flecha de descarga que entra en una bandeja, se dibujó para Haul (2026) y está bajo la
GPL-2.0 como el resto.

Historial de versiones: CHANGELOG.es.md.
