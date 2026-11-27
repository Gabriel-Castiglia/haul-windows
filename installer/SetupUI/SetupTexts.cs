//
//  SetupTexts.cs
//  Haul setup: the texts of the setup window, in English, Spanish and Japanese.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: November 27, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

namespace Haul.Setup;

/// <summary>
/// Every text of the setup window, one table per language with the same keys; "{0}", "{1}"… are filled in by
/// Loc.T. Haul.exe compiles this file too, so its self-test (--self-test) checks that the three tables have the same
/// keys and, for each key, the same placeholders. The Spanish texts address the user as "tú".
/// </summary>
public static class SetupTexts
{
    public static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>
    {
        { "Title", "Haul Setup" },
        { "Subtitle", "A front end for yt-dlp  ·  version {0}" },
        { "Checking", "Checking this computer…" },
        { "InstallIn", "Haul will be installed for all users in:" },
        { "Install", "Install" },
        { "UpgradeText", "Haul {0} is installed. It will be updated to version {1}; each user's settings are kept." },
        { "Update", "Update" },
        { "MaintenanceText", "Haul {0} is already installed on this computer." },
        { "MaintenanceNote", "Repair copies its files again; uninstall removes it from the computer." },
        { "Repair", "Repair" },
        { "Uninstall", "Uninstall" },
        { "UninstallText", "Haul will be removed from this computer." },
        { "UninstallNote", "Downloaded files and each user's settings (the %APPDATA%\\Haul folder) are kept, and FFmpeg and Deno stay installed." },
        { "NewerText", "A newer version of Haul ({0}) is already installed. This version cannot be installed over it." },
        { "ToolsLine", "Then it installs or upgrades FFmpeg and Deno with winget, which yt-dlp needs." },
        { "Uninstalling", "Uninstalling Haul…" },
        { "Repairing", "Repairing Haul…" },
        { "Updating", "Updating Haul…" },
        { "Installing", "Installing Haul…" },
        { "Cancel", "Cancel" },
        { "ToolsProgress", "Installing or upgrading FFmpeg and Deno with winget…" },
        { "ToolsWhy", "yt-dlp needs them to join picture and sound and to solve JavaScript challenges. This can take a few minutes." },
        { "Canceled", "Canceled. The computer was left as it was." },
        { "Failed", "The operation could not be completed." },
        { "ViewLog", "View log" },
        { "Uninstalled", "Haul was uninstalled." },
        { "Repaired", "Haul was repaired." },
        { "Installed", "Haul is installed. It is in the Start menu." },
        { "Restart", "Windows must be restarted to finish." },
        { "OpenHaul", "Open Haul" },
        { "LicenseName", "GNU General Public License, version 2" },
        { "FreeBefore", "Haul is free software, under the " },
        { "FreeAfter", "." },
        { "Close", "Close" },
        { "CancelQuestion", "Cancel the operation? What was done so far will be undone." },
        { "NoWinget", "winget was not found. Install \"App Installer\" from the Microsoft Store and run this setup again to install FFmpeg and Deno." },
        { "WingetFailed", "winget could not install {0}. Haul needs it to download: run this setup again later." },
    };

    public static readonly IReadOnlyDictionary<string, string> Es = new Dictionary<string, string>
    {
        { "Title", "Instalación de Haul" },
        { "Subtitle", "Una interfaz para yt-dlp  ·  versión {0}" },
        { "Checking", "Revisando este equipo…" },
        { "InstallIn", "Haul se instalará para todos los usuarios en:" },
        { "Install", "Instalar" },
        { "UpgradeText", "Haul {0} está instalado. Se actualizará a la versión {1}; los ajustes de cada usuario se conservan." },
        { "Update", "Actualizar" },
        { "MaintenanceText", "Haul {0} ya está instalado en este equipo." },
        { "MaintenanceNote", "Reparar vuelve a copiar sus archivos; desinstalar lo quita del equipo." },
        { "Repair", "Reparar" },
        { "Uninstall", "Desinstalar" },
        { "UninstallText", "Haul se quitará de este equipo." },
        { "UninstallNote", "Los archivos descargados y los ajustes de cada usuario (carpeta %APPDATA%\\Haul) se conservan, y FFmpeg y Deno siguen instalados." },
        { "NewerText", "Ya está instalada una versión más nueva de Haul ({0}). Esta versión no se puede instalar encima." },
        { "ToolsLine", "Después instala o actualiza con winget FFmpeg y Deno, que yt-dlp necesita." },
        { "Uninstalling", "Desinstalando Haul…" },
        { "Repairing", "Reparando Haul…" },
        { "Updating", "Actualizando Haul…" },
        { "Installing", "Instalando Haul…" },
        { "Cancel", "Cancelar" },
        { "ToolsProgress", "Instalando o actualizando FFmpeg y Deno con winget…" },
        { "ToolsWhy", "yt-dlp los necesita para unir imagen y sonido y para resolver desafíos de JavaScript. Puede tardar unos minutos." },
        { "Canceled", "Se canceló. El equipo quedó como estaba." },
        { "Failed", "La operación no se pudo completar." },
        { "ViewLog", "Ver registro" },
        { "Uninstalled", "Haul se desinstaló." },
        { "Repaired", "Haul quedó reparado." },
        { "Installed", "Haul quedó instalado. Está en el menú Inicio." },
        { "Restart", "Hay que reiniciar Windows para terminar." },
        { "OpenHaul", "Abrir Haul" },
        { "LicenseName", "Licencia Pública General GNU, versión 2" },
        { "FreeBefore", "Haul es software libre, bajo la " },
        { "FreeAfter", "." },
        { "Close", "Cerrar" },
        { "CancelQuestion", "¿Cancelar la operación? Se deshará lo hecho hasta ahora." },
        { "NoWinget", "No se encontró winget. Instala «Instalador de aplicación» desde Microsoft Store y vuelve a ejecutar este programa para instalar FFmpeg y Deno." },
        { "WingetFailed", "winget no pudo instalar {0}. Haul lo necesita para descargar: vuelve a ejecutar este programa más tarde." },
    };

    public static readonly IReadOnlyDictionary<string, string> Ja = new Dictionary<string, string>
    {
        { "Title", "Haul セットアップ" },
        { "Subtitle", "yt-dlp のフロントエンド  ·  バージョン {0}" },
        { "Checking", "このコンピューターを確認しています…" },
        { "InstallIn", "Haul をすべてのユーザー用に次の場所へインストールします:" },
        { "Install", "インストール" },
        { "UpgradeText", "Haul {0} がインストールされています。バージョン {1} に更新します。各ユーザーの設定はそのまま残ります。" },
        { "Update", "更新" },
        { "MaintenanceText", "Haul {0} はこのコンピューターに既にインストールされています。" },
        { "MaintenanceNote", "[修復] はファイルをもう一度コピーし、[アンインストール] はコンピューターから削除します。" },
        { "Repair", "修復" },
        { "Uninstall", "アンインストール" },
        { "UninstallText", "Haul をこのコンピューターから削除します。" },
        { "UninstallNote", "ダウンロードしたファイルと各ユーザーの設定 (%APPDATA%\\Haul フォルダー) は残ります。FFmpeg と Deno もインストールされたままです。" },
        { "NewerText", "より新しいバージョンの Haul ({0}) が既にインストールされています。このバージョンを上書きしてインストールすることはできません。" },
        { "ToolsLine", "続いて、yt-dlp に必要な FFmpeg と Deno を winget でインストールまたは更新します。" },
        { "Uninstalling", "Haul をアンインストールしています…" },
        { "Repairing", "Haul を修復しています…" },
        { "Updating", "Haul を更新しています…" },
        { "Installing", "Haul をインストールしています…" },
        { "Cancel", "キャンセル" },
        { "ToolsProgress", "winget で FFmpeg と Deno をインストールまたは更新しています…" },
        { "ToolsWhy", "yt-dlp は、映像と音声の結合や JavaScript チャレンジの解決にこれらを使います。数分かかることがあります。" },
        { "Canceled", "キャンセルしました。コンピューターは元の状態のままです。" },
        { "Failed", "操作を完了できませんでした。" },
        { "ViewLog", "ログを表示" },
        { "Uninstalled", "Haul をアンインストールしました。" },
        { "Repaired", "Haul を修復しました。" },
        { "Installed", "Haul をインストールしました。スタート メニューから起動できます。" },
        { "Restart", "完了するには Windows を再起動する必要があります。" },
        { "OpenHaul", "Haul を開く" },
        { "LicenseName", "GNU 一般公衆利用許諾書 バージョン 2" },
        { "FreeBefore", "Haul は " },
        { "FreeAfter", " に基づくフリー ソフトウェアです。" },
        { "Close", "閉じる" },
        { "CancelQuestion", "操作をキャンセルしますか？ ここまでの変更は元に戻されます。" },
        { "NoWinget", "winget が見つかりません。Microsoft Store から「アプリ インストーラー」をインストールし、このプログラムをもう一度実行して FFmpeg と Deno をインストールしてください。" },
        { "WingetFailed", "winget で {0} をインストールできませんでした。Haul のダウンロードに必要なため、後でこのプログラムをもう一度実行してください。" },
    };
}
