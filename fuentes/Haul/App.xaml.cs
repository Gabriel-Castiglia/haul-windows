//
//  App.xaml.cs
//  Haul for Windows: a graphical front end for yt-dlp.
//
//  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
//  Date: October 20, 2026
//  SPDX-License-Identifier: GPL-2.0-only
//
//  This program is free software: you can redistribute it and/or modify it under the terms of
//  the GNU General Public License, version 2, as published by the Free Software Foundation.
//  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
//

using System.Windows;
using Haul.Core;

namespace Haul;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Ajustes.Cargar();
        Loc.Iniciar();
        Acento.Aplicar();
        var ventana = new MainWindow(e.Args);
        MainWindow = ventana;
        ventana.Show();
    }
}

