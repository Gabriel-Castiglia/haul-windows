# instalar.ps1: installs or updates the dependencies, builds Haul and installs it for the current user.
#
# Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
# Date: October 20, 2026
# SPDX-License-Identifier: GPL-2.0-only
#
# This program is free software: you can redistribute it and/or modify it under the terms of
# the GNU General Public License, version 2, as published by the Free Software Foundation.
# It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
#
# Usage: powershell -ExecutionPolicy Bypass -File scripts\instalar.ps1
#
# yt-dlp itself is not installed: the app bundles the pinned copy in vendor\yt-dlp.
# Dependencies it manages (through winget):
#   Microsoft.DotNet.SDK.10   builds the app (and brings the .NET Desktop Runtime it runs on)
#   yt-dlp.FFmpeg             merges picture and sound, and converts to MP3
#   DenoLand.Deno             JavaScript runtime that yt-dlp needs for full YouTube support
# If they are already installed, they are upgraded to the latest version.
#
# The app goes to %LOCALAPPDATA%\Programs\Haul, with a Start menu shortcut and an entry in
# Settings > Apps, so no administrator rights are needed for the app itself.

$ErrorActionPreference = 'Stop'
$Raiz = Split-Path -Parent $PSScriptRoot
$Destino = Join-Path $env:LOCALAPPDATA 'Programs\Haul'
$Acceso = Join-Path ([Environment]::GetFolderPath('Programs')) 'Haul.lnk'
$Registro = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Haul'

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw 'winget is missing. Install "App Installer" from the Microsoft Store.'
}

foreach ($paquete in 'Microsoft.DotNet.SDK.10', 'yt-dlp.FFmpeg', 'DenoLand.Deno') {
    $lista = winget list --id $paquete --exact --accept-source-agreements --disable-interactivity 2>$null | Out-String
    if ($lista -match [regex]::Escape($paquete)) {
        Write-Host "Upgrading $paquete..."
        winget upgrade --id $paquete --exact --silent --accept-package-agreements --accept-source-agreements --disable-interactivity | Out-Null
        # winget returns an error code when there is nothing to upgrade; that is not a failure.
    } else {
        Write-Host "Installing $paquete..."
        winget install --id $paquete --exact --silent --accept-package-agreements --accept-source-agreements --disable-interactivity | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "winget could not install $paquete (exit code $LASTEXITCODE)." }
    }
}

& (Join-Path $PSScriptRoot 'compilar.ps1')

# Quit the app if it is running before replacing it.
Get-Process Haul -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "$Destino*" } | ForEach-Object {
    $_.CloseMainWindow() | Out-Null
    if (-not $_.WaitForExit(5000)) { $_.Kill() }
}

if (Test-Path $Destino) { Remove-Item $Destino -Recurse -Force }
New-Item -ItemType Directory -Path $Destino -Force | Out-Null
Copy-Item (Join-Path $Raiz 'build\Haul\*') $Destino -Recurse
Copy-Item (Join-Path $PSScriptRoot 'desinstalar.ps1') $Destino

# Start menu shortcut.
$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut($Acceso)
$lnk.TargetPath = Join-Path $Destino 'Haul.exe'
$lnk.WorkingDirectory = $Destino
$lnk.Description = 'Video and music from YouTube and almost any site'
$lnk.Save()

# Entry in Settings > Apps > Installed apps, with its uninstaller.
$exe = Join-Path $Destino 'Haul.exe'
$version = (Get-Item $exe).VersionInfo.ProductVersion
$tamano = [int]((Get-ChildItem $Destino -Recurse | Measure-Object Length -Sum).Sum / 1KB)
New-Item -Path $Registro -Force | Out-Null
$valores = @{
    DisplayName = 'Haul'; DisplayVersion = $version; Publisher = 'Gabriel Castiglia'
    DisplayIcon = $exe; InstallLocation = $Destino
    URLInfoAbout = 'https://github.com/Gabriel-Castiglia/haul-windows'
    UninstallString = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$Destino\desinstalar.ps1`""
}
foreach ($k in $valores.Keys) { Set-ItemProperty -Path $Registro -Name $k -Value $valores[$k] }
Set-ItemProperty -Path $Registro -Name EstimatedSize -Value $tamano -Type DWord
Set-ItemProperty -Path $Registro -Name NoModify -Value 1 -Type DWord
Set-ItemProperty -Path $Registro -Name NoRepair -Value 1 -Type DWord

# Versions of what was installed. ffmpeg and deno are looked up the way the app does it,
# because this console may still have the PATH it had before winget ran.
$ruta = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User') +
        ';' + (Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Links')
$env:Path = $ruta
Write-Host ''
Write-Host "Installed at $Destino (Haul $version)"
Write-Host "  yt-dlp $(& (Join-Path $Destino 'yt-dlp.exe') --version) (bundled)"
$ff = Get-Command ffmpeg -ErrorAction SilentlyContinue
$dn = Get-Command deno -ErrorAction SilentlyContinue
Write-Host "  $(if ($ff) { (& $ff.Source -version | Select-Object -First 1) -replace ' Copyright.*', '' } else { 'ffmpeg: NOT FOUND' })"
Write-Host "  $(if ($dn) { (& $dn.Source --version | Select-Object -First 1) } else { 'deno: NOT FOUND' })"
Write-Host "  Start menu: $Acceso"
