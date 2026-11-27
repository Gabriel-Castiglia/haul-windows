#!/usr/bin/env bash
# manual.sh: builds the PDF manuals from the READMEs.
#   README.md     -> docs/Haul-manual-en.pdf
#   README.es.md  -> docs/Haul-manual-es.pdf
#   README.ja.md  -> docs/Haul-manual-ja.pdf
#
# Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
# Date: October 19, 2026 (Japanese manual, cover and contents: November 27, 2026)
# SPDX-License-Identifier: GPL-2.0-only
#
# This program is free software: you can redistribute it and/or modify it under the terms of
# the GNU General Public License, version 2, as published by the Free Software Foundation.
# It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
#
# Also writes README.txt, README.es.txt and README.ja.txt, the plain-text copies that the installer puts next
# to Haul.exe for reading on Windows without a Markdown viewer; they are never edited by hand.
# Runs on macOS, or on Windows in Git Bash. Requires pandoc (brew install pandoc, winget install pandoc, or
# PANDOC=<path to pandoc>) and Google Chrome or Microsoft Edge, which prints the HTML to PDF (CHROME=<path> to
# choose another). Run it again after every change to a README or to the pictures in docs/images.

set -euo pipefail
RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
cd "$RAIZ"

REPO="https://github.com/Gabriel-Castiglia/haul-windows"
PANDOC="${PANDOC:-pandoc}"
if [[ -z "${CHROME:-}" ]]; then
    for c in "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" \
             "/c/Program Files/Google/Chrome/Application/chrome.exe" \
             "/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe"; do
        [[ -x "$c" ]] && { CHROME="$c"; break; }
    done
fi

command -v "$PANDOC" >/dev/null || { echo "pandoc is missing: brew install pandoc (or set PANDOC)" >&2; exit 1; }
[[ -n "${CHROME:-}" && -x "$CHROME" ]] || { echo "Google Chrome is missing (or set CHROME)" >&2; exit 1; }

# On Windows the programs get Windows paths (C:/...); on macOS paths stay as they are.
nativo() { if command -v cygpath >/dev/null; then cygpath -m "$1"; else printf '%s' "$1"; fi; }
url() { local p; p="$(nativo "$1")"; [[ "$p" == /* ]] || p="/$p"; printf 'file://%s' "$p"; }

# The line under the title of each README names the version (Muelle's went out three versions behind): it must be
# the program's, from fuentes/Haul/Haul.csproj.
VERSION=$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' fuentes/Haul/Haul.csproj | head -1)
YTDLP=$(tr -d '\r\n' < vendor/yt-dlp/VERSION)
for f in README.md:version README.es.md:versión README.ja.md:バージョン; do
    grep -q "· ${f#*:} $VERSION ·.*yt-dlp $YTDLP" "${f%%:*}" || { echo "${f%%:*} does not say \"${f#*:} $VERSION\" and yt-dlp $YTDLP under its title" >&2; exit 1; }
done

TEMPORAL="$(mktemp -d)"
trap 'rm -rf "$TEMPORAL"' EXIT

# Relative links in the README (LICENSE, docs/…) point to the repository in the PDF; otherwise
# they would become file:// paths inside the temporary folder of whoever builds it.
# The centered header of the README (icon, name, phrase, picture) is for GitHub; the cover has the icon and the name.
cat > "$TEMPORAL/enlaces.lua" <<LUA
function RawBlock(el)
  if el.format == "html" and el.text:match('align="center"') then return {} end
end
function Link(el)
  if not el.target:match("^%a[%w+.-]*:") and not el.target:match("^#") then
    el.target = "$REPO/blob/main/" .. el.target
  end
  return el
end
LUA

generar() {
    local fuente=$1 idioma=$2 titulo=$3 subtitulo=$4 indice=$5 version=$6 salida=$7
    # Cover: the icon, the name, what it is, the version and the repository; the contents follow on the same page.
    cat > "$TEMPORAL/portada-$idioma.html" <<HTML
<div class="portada">
  <img src="icono/icono-1024.png" alt="">
  <p class="nombre">Haul</p>
  <p class="subtitulo">$subtitulo</p>
  <p class="version">$version</p>
  <p class="repo"><a href="$REPO">${REPO#https://}</a></p>
</div>
HTML
    # Self-contained HTML: stylesheet, icon and pictures embedded, no references to local files.
    "$PANDOC" "$fuente" --from gfm --to html5 --standalone --embed-resources \
        --lua-filter "$(nativo "$TEMPORAL/enlaces.lua")" \
        --css docs/manual.css \
        --include-before-body "$(nativo "$TEMPORAL/portada-$idioma.html")" \
        --toc --toc-depth=2 --metadata toc-title="$indice" \
        --metadata pagetitle="$titulo" \
        --metadata lang="$idioma" \
        --output "$(nativo "$TEMPORAL/manual-$idioma.html")"
    # No header or footer of Chrome's own: it would print the local path of the HTML file in them (the page
    # numbers come from manual.css). A profile of its own, so a Chrome that is already open is left alone.
    "$CHROME" --headless --disable-gpu --no-pdf-header-footer --user-data-dir="$(nativo "$TEMPORAL/chrome")" \
        --print-to-pdf="$(nativo "$RAIZ/$salida")" "$(url "$TEMPORAL/manual-$idioma.html")" 2>/dev/null
    echo "Manual: $salida"
}

# Plain text: no images, titles underlined, links to the other README point to its .txt copy, and web
# links keep their address after the text.
cat > "$TEMPORAL/texto.lua" <<'LUA'
function Image(el) return {} end
-- Width on a fixed-width screen: Japanese characters (and other wide ones) take two columns, so the rule under a
-- Japanese title is as long as the title.
local function width(text)
  local w = 0
  for _, c in utf8.codes(text) do
    local wide = (c >= 0x1100 and c <= 0x115F) or (c >= 0x2E80 and c <= 0xA4CF) or (c >= 0xAC00 and c <= 0xD7A3)
              or (c >= 0xF900 and c <= 0xFAFF) or (c >= 0xFE30 and c <= 0xFE4F) or (c >= 0xFF00 and c <= 0xFF60)
              or (c >= 0xFFE0 and c <= 0xFFE6)
    w = w + (wide and 2 or 1)
  end
  return w
end
-- The centered GitHub header: only its title stays, underlined like the other titles.
function RawBlock(el)
  local title = el.text:match("<h1[^>]*>(.-)</h1>")
  if not title then return {} end
  return pandoc.Plain({pandoc.Str(title), pandoc.LineBreak(), pandoc.Str(string.rep("=", width(title)))})
end
function Header(el)
  local text = pandoc.utils.stringify(el.content)
  local rule = string.rep(el.level == 1 and "=" or "-", width(text))
  return pandoc.Plain({pandoc.Str(text), pandoc.LineBreak(), pandoc.Str(rule)})
end
function Link(el)
  local target = el.target:gsub("^README%.es%.md$", "README.es.txt"):gsub("^README%.ja%.md$", "README.ja.txt")
                          :gsub("^README%.md$", "README.txt")
  local text = pandoc.utils.stringify(el.content)
  if text == target then return pandoc.Str(target) end
  if not target:match("^https?:") then return pandoc.Str(text .. " (" .. target .. ")") end
  return pandoc.Str(text .. " <" .. target .. ">")
end
LUA

texto() {
    "$PANDOC" "$1" --from gfm --to plain --columns 100 --lua-filter "$(nativo "$TEMPORAL/texto.lua")" --output "$2"
    echo "Text: $2"
}

generar README.md    en "Haul · Manual" "Videos and music from the web to your folder · user manual" \
        "Contents" "Version $VERSION · Windows 10/11 x64 · yt-dlp $YTDLP" docs/Haul-manual-en.pdf
generar README.es.md es "Haul · Manual de usuario" "Videos y música de la web a tu carpeta · manual de usuario" \
        "Índice" "Versión $VERSION · Windows 10/11 x64 · yt-dlp $YTDLP" docs/Haul-manual-es.pdf
generar README.ja.md ja "Haul · ユーザーマニュアル" "Web の動画や音楽をフォルダーへ · ユーザーマニュアル" \
        "目次" "バージョン $VERSION · Windows 10/11 x64 · yt-dlp $YTDLP" docs/Haul-manual-ja.pdf
texto README.md    README.txt
texto README.es.md README.es.txt
texto README.ja.md README.ja.txt
