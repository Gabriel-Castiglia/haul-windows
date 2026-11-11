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

# Draws the main window, light and dark, and the appearance window, in English and Spanish, to docs\images\.
# It uses the test option --captura of Haul: the settings are read but never written, nothing is downloaded,
# and the program exits by itself. Requires the .NET 10 SDK.

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$app = Join-Path $root 'build\screenshots'
dotnet build (Join-Path $root 'fuentes\Haul\Haul.csproj') -c Release -o $app
if ($LASTEXITCODE) { throw 'Haul build failed.' }
$images = Join-Path $root 'docs\images'
New-Item -ItemType Directory -Force $images | Out-Null

function Render([string]$png, [string[]]$arguments) {
    $p = Start-Process (Join-Path $app 'Haul.exe') -PassThru -ArgumentList (@('--captura', "`"$png`"") + $arguments)
    if (-not $p.WaitForExit(60000)) { $p.Kill(); throw "Timed out: $png" }
    if (-not (Test-Path $png)) { throw "No image: $png" }
    Write-Host "Image: docs\images\$(Split-Path $png -Leaf)"
}

foreach ($language in 'en', 'es') {
    Render (Join-Path $images "main-$language.png") @('--ventana', 'main', '--idioma', $language, '--tema', 'light', '--color', 'ember')
    Render (Join-Path $images "main-dark-$language.png") @('--ventana', 'main', '--idioma', $language, '--tema', 'dark', '--color', 'ember')
    Render (Join-Path $images "theme-$language.png") @('--ventana', 'tema', '--idioma', $language, '--tema', 'light', '--color', 'ember')
}
