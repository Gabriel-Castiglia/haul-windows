# compilar.ps1: builds Haul for Windows into build\Haul.
#
# Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
# Date: October 20, 2026
# SPDX-License-Identifier: GPL-2.0-only
#
# This program is free software: you can redistribute it and/or modify it under the terms of
# the GNU General Public License, version 2, as published by the Free Software Foundation.
# It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
#
# Requires the .NET 10 SDK (installed by instalar.ps1). Visual Studio is not needed.
# Usage: powershell -ExecutionPolicy Bypass -File scripts\compilar.ps1

$ErrorActionPreference = 'Stop'
$Raiz = Split-Path -Parent $PSScriptRoot
Set-Location $Raiz

# The SDK may have been installed in this same session, before PATH knew about it.
$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source }
if (-not $dotnet) { throw 'The .NET 10 SDK is missing. Run scripts\instalar.ps1 first.' }

# 1. Bundled yt-dlp: verify it against the official checksums before building.
$linea = Select-String -Path 'vendor\yt-dlp\SHA2-256SUMS' -Pattern ' yt-dlp\.exe$' | Select-Object -First 1
if (-not $linea) { throw 'vendor\yt-dlp\SHA2-256SUMS has no entry for yt-dlp.exe.' }
$esperado = $linea.Line.Split(' ')[0].ToLowerInvariant()
$real = (Get-FileHash 'vendor\yt-dlp\yt-dlp.exe' -Algorithm SHA256).Hash.ToLowerInvariant()
if ($real -ne $esperado) {
    throw "vendor\yt-dlp\yt-dlp.exe does not match SHA2-256SUMS ($real); refusing to build."
}

# 2. Build: framework-dependent, so the app is small and uses the shared .NET Desktop Runtime.
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$salida = Join-Path $Raiz 'build\Haul'
if (Test-Path $salida) { Remove-Item $salida -Recurse -Force }
& $dotnet publish 'fuentes\Haul\Haul.csproj' -c Release -r win-x64 --self-contained false -o $salida -nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
Get-ChildItem $salida -Filter '*.pdb' | Remove-Item

$version = (Get-Item (Join-Path $salida 'Haul.exe')).VersionInfo.ProductVersion
$ytdlp = (Get-Content 'vendor\yt-dlp\VERSION').Trim()
Write-Host "Built: $salida (Haul $version, yt-dlp $ytdlp)"
