# screenshots.ps1
# Draws the windows of Haul into images for the README and the manuals.
#
# Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
# Date: November 11, 2026
# SPDX-License-Identifier: GPL-2.0-only
#
# This program is free software: you can redistribute it and/or modify it under the terms of
# the GNU General Public License, version 2, as published by the Free Software Foundation.
# It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

# Draws the main window, light and dark, the appearance window, the About window and the question about a new
# version, in English, Spanish and Japanese, to docs\images\, and the install and uninstall pages of the setup window.
# It uses the test option --captura of Haul: the default settings and an example queue are used, the clipboard is not read, nothing is written or downloaded, and the program exits by itself. Requires the .NET 10 SDK.

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$app = Join-Path $root 'build\screenshots'
dotnet build (Join-Path $root 'fuentes\Haul\Haul.csproj') -c Release -o $app
if ($LASTEXITCODE) { throw 'Haul build failed.' }
$images = Join-Path $root 'docs\images'
New-Item -ItemType Directory -Force $images | Out-Null

# Runs one program that draws a picture and exits. HAUL_SCREENSHOT_LAUNCHER may name a script that runs it
# elsewhere, for example on a desktop of its own so that no window shows up; it gets -Exe, -ArgLine and -TimeoutSec.
function Run-Drawing([string]$exe, [string[]]$arguments, [string]$png) {
    if ($env:HAUL_SCREENSHOT_LAUNCHER) {
        & $env:HAUL_SCREENSHOT_LAUNCHER -Exe $exe -ArgLine:($arguments -join ' ') -TimeoutSec 60 | Out-Null
    } else {
        $p = Start-Process $exe -PassThru -ArgumentList $arguments
        if (-not $p.WaitForExit(60000)) { $p.Kill(); throw "Timed out: $png" }
    }
    if (-not (Test-Path $png)) { throw "No image: $png" }
    Write-Host "Image: docs\images\$(Split-Path $png -Leaf)"
}

function Render([string]$png, [string[]]$arguments) {
    Run-Drawing (Join-Path $app 'Haul.exe') (@('--captura', "`"$png`"") + $arguments) $png
}

foreach ($language in 'en', 'es', 'ja') {
    Render (Join-Path $images "main-$language.png") @('--ventana', 'main', '--idioma', $language, '--tema', 'light', '--color', 'ember')
    Render (Join-Path $images "main-dark-$language.png") @('--ventana', 'main', '--idioma', $language, '--tema', 'dark', '--color', 'ember')
    Render (Join-Path $images "theme-$language.png") @('--ventana', 'tema', '--idioma', $language, '--tema', 'light', '--color', 'ember')
    Render (Join-Path $images "update-$language.png") @('--ventana', 'aviso', '--idioma', $language, '--tema', 'light', '--color', 'ember')
    Render (Join-Path $images "about-$language.png") @('--ventana', 'acerca', '--idioma', $language, '--tema', 'light', '--color', 'ember')
}

# The setup window has a test option of its own, --screenshot, which draws one page without the setup engine.
$setup = Join-Path $root 'build\setupshots'
dotnet build (Join-Path $root 'installer\SetupUI\HaulSetup.csproj') -c Release -o $setup
if ($LASTEXITCODE) { throw 'Setup window build failed.' }
foreach ($language in 'en', 'es', 'ja') {
    foreach ($page in 'install', 'uninstall') {
        $png = Join-Path $images "setup-$page-$language.png"
        Run-Drawing (Join-Path $setup 'HaulSetup.exe') @('--screenshot', "`"$png`"", '--page', $page, '--language', $language) $png
    }
}
