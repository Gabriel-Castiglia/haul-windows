# actualizar-yt-dlp.ps1: replaces the bundled yt-dlp in vendor\yt-dlp with another official release.
#
# Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
# Date: October 20, 2026
# SPDX-License-Identifier: GPL-2.0-only
#
# This program is free software: you can redistribute it and/or modify it under the terms of
# the GNU General Public License, version 2, as published by the Free Software Foundation.
# It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
#
# Usage: powershell -ExecutionPolicy Bypass -File scripts\actualizar-yt-dlp.ps1 <version>
#        e.g. scripts\actualizar-yt-dlp.ps1 2026.09.30
#
# Downloads the release's Windows executable, source tarball, checksums and signature; checks
# the signature with the key already stored in vendor\yt-dlp\public.key (using the gpg that
# ships with Git for Windows) and the SHA-256 hashes; only then replaces the files and refreshes
# the license texts, including the third-party licenses stored inside yt-dlp.exe (needs Python).
# Afterwards: update the version in both READMEs and vendor\yt-dlp\README*.md, test, and run
# scripts\instalar.ps1 again.

param([Parameter(Mandatory = $true)][string]$Version)

$ErrorActionPreference = 'Stop'
$Raiz = Split-Path -Parent $PSScriptRoot
$Destino = Join-Path $Raiz 'vendor\yt-dlp'
$Url = "https://github.com/yt-dlp/yt-dlp/releases/download/$Version"

$gpg = Join-Path $env:ProgramFiles 'Git\usr\bin\gpg.exe'
if (-not (Test-Path $gpg)) { $gpg = (Get-Command gpg -ErrorAction SilentlyContinue).Source }
if (-not $gpg) { throw 'gpg is missing (it ships with Git for Windows).' }
$python = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $python) { throw 'Python is missing; it is needed to read the licenses inside yt-dlp.exe.' }

$temporal = Join-Path $env:TEMP ("haul-yt-dlp-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $temporal | Out-Null
try {
    Set-Location $temporal
    $ProgressPreference = 'SilentlyContinue'
    foreach ($archivo in 'yt-dlp.exe', 'yt-dlp.tar.gz', 'SHA2-256SUMS', 'SHA2-256SUMS.sig') {
        Invoke-WebRequest -Uri "$Url/$archivo" -OutFile $archivo -UseBasicParsing
    }

    # Signature: the key is the one already trusted in the repository, not a fresh download.
    $env:GNUPGHOME = Join-Path $temporal 'gnupg'
    New-Item -ItemType Directory -Path $env:GNUPGHOME | Out-Null
    & $gpg --quiet --import (Join-Path $Destino 'public.key') 2>$null
    & $gpg --verify SHA2-256SUMS.sig SHA2-256SUMS
    if ($LASTEXITCODE -ne 0) { throw 'The signature of SHA2-256SUMS is not valid.' }

    foreach ($archivo in 'yt-dlp.exe', 'yt-dlp.tar.gz') {
        $linea = Select-String -Path SHA2-256SUMS -Pattern (' ' + [regex]::Escape($archivo) + '$') | Select-Object -First 1
        $esperado = $linea.Line.Split(' ')[0].ToLowerInvariant()
        $real = (Get-FileHash $archivo -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($real -ne $esperado) { throw "$archivo does not match SHA2-256SUMS." }
        Write-Host "$archivo`: OK"
    }

    # License files: from the source tarball, and from inside the executable.
    tar -xzf yt-dlp.tar.gz yt-dlp/LICENSE yt-dlp/AUTHORS
    & $python -I -B (Join-Path $PSScriptRoot 'extraer_licencias.py') yt-dlp.exe THIRD_PARTY_LICENSES.txt
    if ($LASTEXITCODE -ne 0) { throw 'Could not read the licenses inside yt-dlp.exe.' }

    foreach ($archivo in 'yt-dlp.exe', 'yt-dlp.tar.gz', 'SHA2-256SUMS', 'SHA2-256SUMS.sig') {
        Copy-Item $archivo $Destino -Force
    }
    Copy-Item 'yt-dlp\LICENSE' (Join-Path $Destino 'licenses\yt-dlp-LICENSE') -Force
    Copy-Item 'yt-dlp\AUTHORS' (Join-Path $Destino 'licenses\yt-dlp-AUTHORS') -Force
    Copy-Item 'THIRD_PARTY_LICENSES.txt' (Join-Path $Destino 'licenses\yt-dlp-exe-THIRD_PARTY_LICENSES.txt') -Force
    [IO.File]::WriteAllText((Join-Path $Destino 'VERSION'), "$Version`n")
} finally {
    Set-Location $Raiz
    Remove-Item $temporal -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "vendor\yt-dlp now holds yt-dlp $Version."
Write-Host 'Next: review vendor\yt-dlp\licenses, update the version in the READMEs, then scripts\instalar.ps1'
