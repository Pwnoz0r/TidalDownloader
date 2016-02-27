using System.IO;
using TidalDownloaderUI.Enums;

namespace TidalDownloaderUI.Utils.Models
{
    public class ConfigurationModel
    {
        public DownloadSettings Downloads { get; set; } = new DownloadSettings
        {
            DownloadDirectory = Path.Combine(Configuration.DirectoryCurrent, "Downloads"),
            DownloadLimit = 5
        };

        public LoginSettings Login { get; set; } = new LoginSettings();

        public ThemeSettings Theme { get; set; } = new ThemeSettings
        {
            ThemePrimaryColor = AvailablePrimaryColors.DeepOrange,
            IsDark = true
        };

        public LibSettings Lib { get; set; } = new LibSettings();

        public class DownloadSettings
        {
            public string DownloadDirectory { get; set; }
            public int DownloadLimit { get; set; }
        }

        public class LoginSettings
        {
            public string TidalUserName { get; set; }
            public string TidalPassword { get; set; }
        }

        public class ThemeSettings
        {
            public AvailablePrimaryColors ThemePrimaryColor { get; set; }
            public bool IsDark { get; set; }
        }

        public class LibSettings
        {
            public string LibDirectory { get; set; }
        }
    }
}
