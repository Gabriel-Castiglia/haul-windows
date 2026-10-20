# desinstalar.ps1: removes Haul for the current user.
#
# Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
# Date: October 20, 2026
# SPDX-License-Identifier: GPL-2.0-only
#
# This program is free software: you can redistribute it and/or modify it under the terms of
# the GNU General Public License, version 2, as published by the Free Software Foundation.
# It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
#
# Usage: Settings > Apps > Installed apps > Haul > Uninstall, or
#        powershell -ExecutionPolicy Bypass -File scripts\desinstalar.ps1 [-Preferencias]
#
# Removes the app, its Start menu shortcut and its entry in Settings. Downloaded files are never
# touched. Preferences (%APPDATA%\Haul) are kept unless -Preferencias is given. ffmpeg, deno and
# the .NET SDK stay installed: other programs may use them (winget uninstall removes them).

param([switch]$Preferencias)

$ErrorActionPreference = 'Stop'
$Destino = Join-Path $env:LOCALAPPDATA 'Programs\Haul'
$Acceso = Join-Path ([Environment]::GetFolderPath('Programs')) 'Haul.lnk'
$Registro = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Haul'

# This script may be running from inside the folder it deletes.
Set-Location $env:TEMP

Get-Process Haul -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "$Destino*" } | ForEach-Object {
    $_.CloseMainWindow() | Out-Null
    if (-not $_.WaitForExit(5000)) { $_.Kill() }
}

foreach ($ruta in $Destino, $Acceso) {
    if (Test-Path $ruta) { Remove-Item $ruta -Recurse -Force }
}
if (Test-Path $Registro) { Remove-Item $Registro -Recurse -Force }

$ajustes = Join-Path $env:APPDATA 'Haul'
if ($Preferencias -and (Test-Path $ajustes)) {
    Remove-Item $ajustes -Recurse -Force
    Write-Host 'Haul and its preferences were removed.'
} else {
    Write-Host 'Haul was removed.'
    if (Test-Path $ajustes) { Write-Host "Preferences kept in $ajustes (run with -Preferencias to remove them)." }
}
