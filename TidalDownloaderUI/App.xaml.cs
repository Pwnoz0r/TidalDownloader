using System;
using System.Windows;
using MahApps.Metro;

namespace TidalDownloaderUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var theme = ThemeManager.DetectAppStyle(Current);
            ThemeManager.ChangeAppStyle(Current, ThemeManager.GetAccent("Orange"), ThemeManager.GetAppTheme("BaseDark"));
            base.OnStartup(e);
        }
    }
}
