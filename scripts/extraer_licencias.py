#!/usr/bin/env python3
#
# extraer_licencias.py: extracts THIRD_PARTY_LICENSES.txt from the official yt-dlp.exe.
#
# Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
# Date: October 20, 2026
# SPDX-License-Identifier: GPL-2.0-only
#
# This program is free software: you can redistribute it and/or modify it under the terms of
# the GNU General Public License, version 2, as published by the Free Software Foundation.
# It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
#
# Usage: python scripts/extraer_licencias.py vendor/yt-dlp/yt-dlp.exe vendor/yt-dlp/licenses/yt-dlp-exe-THIRD_PARTY_LICENSES.txt
#
# yt-dlp.exe is a PyInstaller one-file executable: the Python runtime and the libraries it
# bundles are stored in an archive appended to the executable, together with the aggregated
# license texts of those libraries. This script reads that archive with the standard library
# only (no PyInstaller needed) and writes the license file unchanged.
#
# Archive layout (PyInstaller 4 to 6): the file ends with a "cookie" that holds the archive
# length and the position of its table of contents; each table entry gives the offset, sizes,
# compression flag and name of one stored file.

import struct
import sys
import zlib

MAGIA = b"MEI\014\013\012\013\016"
COOKIE = struct.Struct("!8sIIII64s")   # magic, archive length, TOC offset, TOC length, Python version, library name
ENTRADA = struct.Struct("!IIIIBc")     # entry length, data offset, stored length, full length, compressed, type
NOMBRE = "THIRD_PARTY_LICENSES.txt"


def extraer(exe, nombre):
    datos = open(exe, "rb").read()
    pos = datos.rfind(MAGIA)
    if pos < 0:
        raise SystemExit(f"{exe}: no PyInstaller archive found")
    _, largo, toc, largo_toc, _, _ = COOKIE.unpack_from(datos, pos)
    inicio = pos + COOKIE.size - largo
    i = inicio + toc
    fin = i + largo_toc
    while i < fin:
        largo_e, desde, guardado, completo, comprimido, _ = ENTRADA.unpack_from(datos, i)
        nombre_e = datos[i + ENTRADA.size:i + largo_e].rstrip(b"\0").decode("utf-8")
        if nombre_e == nombre:
            contenido = datos[inicio + desde:inicio + desde + guardado]
            contenido = zlib.decompress(contenido) if comprimido else contenido
            if len(contenido) != completo:
                raise SystemExit(f"{nombre}: expected {completo} bytes, got {len(contenido)}")
            return contenido
        i += largo_e
    raise SystemExit(f"{exe}: {nombre} is not in the archive")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit(__doc__ or "Usage: extraer_licencias.py <yt-dlp.exe> <output file>")
    contenido = extraer(sys.argv[1], NOMBRE)
    with open(sys.argv[2], "wb") as salida:
        salida.write(contenido)
    print(f"{sys.argv[2]}: {len(contenido)} bytes")
