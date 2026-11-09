# build.ps1: builds Haul and its installers into dist\.
#   haul-setup.exe  setup program in English and Spanish that contains the MSI
#   haul.msi        MSI package (silent install: msiexec /i haul.msi /qn)
#
# Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
# Date: November 9, 2026
# SPDX-License-Identifier: GPL-2.0-only
#
# This program is free software: you can redistribute it and/or modify it under the terms of
# the GNU General Public License, version 2, as published by the Free Software Foundation.
# It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
#
# Usage: powershell -ExecutionPolicy Bypass -File scripts\build.ps1
# Requires the .NET 10 SDK; the WiX Toolset is restored automatically as a NuGet package.
# The version is read from fuentes\Haul\Haul.csproj, the only place where it is set.

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'fuentes\Haul\Haul.csproj'
$version = ([xml](Get-Content $project -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw 'Haul.csproj has no <Version>.' }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

# 1) The bundled yt-dlp must match the official checksums.
$line = Select-String -Path (Join-Path $root 'vendor\yt-dlp\SHA2-256SUMS') -Pattern ' yt-dlp\.exe$' | Select-Object -First 1
if (-not $line) { throw 'vendor\yt-dlp\SHA2-256SUMS has no entry for yt-dlp.exe.' }
$expected = $line.Line.Split(' ')[0].ToLowerInvariant()
$actual = (Get-FileHash (Join-Path $root 'vendor\yt-dlp\yt-dlp.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actual -ne $expected) { throw "vendor\yt-dlp\yt-dlp.exe does not match SHA2-256SUMS ($actual); refusing to build." }

$dist = Join-Path $root 'dist'
$publish = Join-Path $root 'build\publish\'
foreach ($dir in $dist, (Join-Path $root 'build')) {
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
    New-Item -ItemType Directory -Force $dir | Out-Null
}
Write-Host "Haul $version, yt-dlp $((Get-Content (Join-Path $root 'vendor\yt-dlp\VERSION')).Trim())"

# 2) Self-contained Haul.exe, with yt-dlp.exe, plugins\ and licenses\ next to it.
dotnet publish $project -c Release -o $publish
if ($LASTEXITCODE) { throw 'App publish failed.' }

# 3) MSI.
dotnet build (Join-Path $root 'installer\Msi\Haul.Msi.wixproj') -c Release -o $dist -p:PublishDir=$publish -p:Version=$version
if ($LASTEXITCODE) { throw 'MSI build failed.' }

# 4) The setup window (English and Spanish), then the setup .exe that carries it and the MSI.
$setupUi = Join-Path $root 'build\setupui\'
dotnet build (Join-Path $root 'installer\SetupUI\HaulSetup.csproj') -c Release -o $setupUi
if ($LASTEXITCODE) { throw 'Setup window build failed.' }
dotnet build (Join-Path $root 'installer\Setup\Haul.Setup.wixproj') -c Release -o $dist -p:MsiPath="$dist\haul.msi" -p:SetupUiDir=$setupUi -p:Version=$version
if ($LASTEXITCODE) { throw 'Setup .exe build failed.' }

Get-ChildItem $dist -Include *.exe, *.msi -Recurse | Select-Object Name, Length
