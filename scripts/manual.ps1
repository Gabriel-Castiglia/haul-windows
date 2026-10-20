# manual.ps1: builds the PDF manuals from the READMEs.
#   README.md     -> docs\Haul-manual-en.pdf
#   README.es.md  -> docs\Haul-manual-es.pdf
#
# Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
# Date: October 20, 2026
# SPDX-License-Identifier: GPL-2.0-only
#
# This program is free software: you can redistribute it and/or modify it under the terms of
# the GNU General Public License, version 2, as published by the Free Software Foundation.
# It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
#
# Requires pandoc (winget install JohnMacFarlane.Pandoc) and a Chromium browser (Edge, Chrome or
# Brave), which prints the HTML to PDF. Run it again after every change to either README.

$ErrorActionPreference = 'Stop'
$Raiz = Split-Path -Parent $PSScriptRoot
Set-Location $Raiz
$Repo = 'https://github.com/Gabriel-Castiglia/haul-windows'

$pandoc = (Get-Command pandoc -ErrorAction SilentlyContinue).Source
if (-not $pandoc) {
    $pandoc = Get-ChildItem (Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages') -Recurse -Filter pandoc.exe -ErrorAction SilentlyContinue |
              Select-Object -First 1 -ExpandProperty FullName
}
if (-not $pandoc) { throw 'pandoc is missing: winget install JohnMacFarlane.Pandoc' }
$edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
          "$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe",
          "$env:ProgramFiles\BraveSoftware\Brave-Browser\Application\brave.exe",
          "$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\Application\brave.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { throw 'A Chromium browser (Edge, Chrome or Brave) is needed to print the PDF.' }

$temporal = Join-Path $env:TEMP ("haul-manual-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $temporal | Out-Null
try {
    # Cover block with icon, name and repository link.
    $portada = @"
<div class="portada">
  <img src="icono/icono-1024.png" alt="">
  <div>
    <p class="nombre">Haul</p>
    <p class="repo"><a href="$Repo">$($Repo -replace '^https://', '')</a></p>
  </div>
</div>
"@
    [IO.File]::WriteAllText((Join-Path $temporal 'portada.html'), $portada)

    # Relative links in the README (LICENSE, docs/...) point to the repository in the PDF.
    $lua = @"
function Link(el)
  if not el.target:match("^%a[%w+.-]*:") and not el.target:match("^#") then
    el.target = "$Repo/blob/main/" .. el.target
  end
  return el
end
"@
    [IO.File]::WriteAllText((Join-Path $temporal 'enlaces.lua'), $lua)

    foreach ($m in @(@('README.md', 'en', 'Haul for Windows - Manual', 'docs\Haul-manual-en.pdf'),
                     @('README.es.md', 'es', 'Haul para Windows - Manual de usuario', 'docs\Haul-manual-es.pdf'))) {
        $html = Join-Path $temporal "manual-$($m[1]).html"
        # Self-contained HTML: stylesheet and icon embedded, no references to local files.
        & $pandoc $m[0] --from gfm --to html5 --standalone --embed-resources `
            --lua-filter (Join-Path $temporal 'enlaces.lua') --css docs/manual.css `
            --include-before-body (Join-Path $temporal 'portada.html') `
            --metadata "pagetitle=$($m[2])" --metadata "lang=$($m[1])" --output $html
        if ($LASTEXITCODE -ne 0) { throw "pandoc failed on $($m[0])." }
        $pdf = Join-Path $Raiz $m[3]
        if (Test-Path $pdf) { Remove-Item $pdf }
        # No header or footer: the browser would print the local path of the HTML file in them.
        $p = Start-Process $edge -ArgumentList @('--headless', '--disable-gpu', '--no-pdf-header-footer',
                                                 "--user-data-dir=$temporal\edge", "--print-to-pdf=$pdf",
                                                 ([Uri]$html).AbsoluteUri) -PassThru -Wait -WindowStyle Hidden
        if (-not (Test-Path $pdf)) { throw "The browser did not produce $($m[3])." }
        Write-Host "Manual: $($m[3])"
    }
} finally {
    Remove-Item $temporal -Recurse -Force -ErrorAction SilentlyContinue
}
