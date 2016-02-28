using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro;
using TidalDownloaderUI.Enums;
using TidalSharp.Controllers;
using TidalSharp.Models;
using Configuration = TidalDownloaderUI.Utils.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MaterialDesignThemes.Wpf;
using TidalSharp.Models.Static;
// ReSharper disable UnusedVariable

namespace TidalDownloaderUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public Configuration Configuration { get; set; }
        public MainWindow()
        {
            InitializeComponent();
        }

        public void ChangeAppStyle(AvailablePrimaryColors primaryColor, bool isDark)
        {
            var paletteHelper = new PaletteHelper();
            paletteHelper.ReplacePrimaryColor(primaryColor.ToString());
            paletteHelper.SetLightDark(isDark);
        }

        private TidalController TidalController { get; set; }

        private void InitTidalLib()
        {
            var initTidal = new Tidal();
            TidalController = new TidalController();

            TidalController.ApiRequest<CountryModel>("/country/");
            TidalController.ApiRequest<LoginModel>($"/login/username?countryCode={TidalController.Tidal.CountryModel.CountryCode}", TidalController.RequestType.Post, $"username={Configuration.ConfigData.Login.TidalUserName}&password={Configuration.HashUtility.DecryptHash(Configuration.ConfigData.Login.TidalPassword)}");
            TidalController.ApiRequest<UsersSubscriptionsModel>($"/users/{TidalController.Tidal.LoginModel.UserId}/subscription?sessionId={TidalController.Tidal.LoginModel.SessionId}&countryCode={TidalController.Tidal.CountryModel.CountryCode}");
            TidalController.ApiRequest<FeaturedAlbumsModel>($"/featured/new/albums?limit=100&sessionId={TidalController.Tidal.LoginModel.SessionId}&countryCode={TidalController.Tidal.CountryModel.CountryCode}");

            var featureAlbumsDataTabel = Configuration.ToDataTable(TidalController.Tidal.FeaturedAlbumsModel.Items);
            DataGridTopAlbums.ItemsSource = featureAlbumsDataTabel.AsDataView();
        }

        private string GetLibDirectory()
        {
            var ffmpegDirs = Directory.GetDirectories(Configuration.DirectoryCurrent, "ffmpeg*");

            if (!ffmpegDirs.Any())
            {
                ButtonFfmpegInstalledNo.Visibility = Visibility.Visible;
                ButtonFfmpegInstalledYes.Visibility = Visibility.Hidden;
                TextBoxLibDir.IsReadOnly = false;
                return null;
            }

            ButtonFfmpegInstalledNo.Visibility = Visibility.Hidden;
            ButtonFfmpegInstalledYes.Visibility = Visibility.Visible;
            TextBoxLibDir.IsReadOnly = true;
            return ffmpegDirs.First();
        }

        private void DownloadSong(string ffmpegCmd, string ffmpegDir)
        {
            var fPath = Path.GetDirectoryName(ffmpegDir);
            if (fPath != null && !Directory.Exists(fPath))
                Directory.CreateDirectory(fPath);

            using (var albumDownloadProcess = new Process())
            {
                albumDownloadProcess.ErrorDataReceived += AlbumDownloadProcess_ErrorDataReceived;
                albumDownloadProcess.EnableRaisingEvents = true;

                albumDownloadProcess.StartInfo = new ProcessStartInfo
                {
                    Arguments = ffmpegCmd,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = Path.Combine(Configuration.ConfigData.Lib.LibDirectory, @"bin\ffmpeg.exe"),
                    CreateNoWindow = true
                };

                albumDownloadProcess.Start();

                albumDownloadProcess.BeginErrorReadLine();
                albumDownloadProcess.WaitForExit();
            }
        }

        private void AlbumDownloadProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"[DEBUG]: {e.Data}");
        }

        private void WindowTidalDownloader_Initialized(object sender, EventArgs e)
        {
            Configuration = new Configuration();

            if (!string.IsNullOrEmpty(Configuration.ConfigData.Login.TidalUserName) && !string.IsNullOrEmpty(Configuration.ConfigData.Login.TidalPassword))
            {
                TextBoxEmail.Text = Configuration.ConfigData.Login.TidalUserName;
                TextBoxPassword.Password = Configuration.HashUtility.DecryptHash(Configuration.ConfigData.Login.TidalPassword);
                InitTidalLib();
            }
            else
            {
                DataGridTopAlbums.Visibility = Visibility.Hidden;
                TabControlDownloader.SelectedIndex = 1;
            }
            
            ComboBoxThemeStyle.ItemsSource = Enum.GetValues(typeof(AvailablePrimaryColors));
            CheckBoxIsDark.IsChecked = Configuration.ConfigData.Theme.IsDark;

            var configThemePrimaryColor = Configuration.ConfigData.Theme.ThemePrimaryColor;
            var isDark = Configuration.ConfigData.Theme.IsDark;

            ChangeAppStyle(configThemePrimaryColor, isDark);
            ComboBoxThemeStyle.Text = configThemePrimaryColor.ToString();

            Configuration.ConfigData.Lib.LibDirectory = GetLibDirectory();
            TextBoxLibDir.Text = Configuration.ConfigData.Lib.LibDirectory;

            TextBoxDownloadsDirectory.IsReadOnly = true;
            TextBoxDownloadsDirectory.Text = Configuration.ConfigData.Downloads.DownloadDirectory;

            TextBoxConcurrentDownloads.Text = Configuration.ConfigData.Downloads.DownloadLimit.ToString();
            
            Configuration.SaveConfig();

            _isInitialized = true;
        }

        private void DataGridTopAlbums_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataGridTopAlbums.SelectedItem == null) return;
            StartDownload(DataGridTopAlbums.SelectedItem);
        }

        private async void StartDownload(object selectedItem)
        {
            var selectedRow = ((DataRowView)selectedItem).Row.ItemArray;
            var tempAlbum = new DataGridAlbumModel
            {
                Id = Convert.ToInt32(selectedRow[0]),
                Title = selectedRow[1].ToString(),
                NumberOfTracks = Convert.ToInt32(selectedRow[2]),
                Artist = selectedRow[3].ToString()
            };

            var downloadResult = await ShowDialog("Download Confirmation", $"Are you sure you want to download: {tempAlbum.Artist} - {tempAlbum.Title}?", MessageDialogStyle.AffirmativeAndNegative);

            if (downloadResult != MessageDialogResult.Affirmative) return;

            TidalController.ApiRequest<AlbumTracksModel>($"/albums/{tempAlbum.Id}/tracks?sessionId={TidalController.Tidal.LoginModel.SessionId}&countryCode={TidalController.Tidal.CountryModel.CountryCode}");

            Console.WriteLine($"Downloading Album: {tempAlbum.Title}({tempAlbum.Id}) by {tempAlbum.Artist} [{tempAlbum.NumberOfTracks} tracks]");

            var trackItems = new List<TracksModel>();

            foreach (var item in TidalController.Tidal.AlbumTracksModel.Items)
            {
                Console.WriteLine($"Getting information for: {item.Title}({item.Id})");

                if (!item.AllowStreaming)
                    Console.WriteLine($"Ignored track: {item.Title} ({item.Id}) as it is not allowed to stream.");
                else
                {
                    TidalController.ApiRequest<TracksModel>($"/tracks/{item.Id}/streamUrl?soundQuality={TidalController.Tidal.UsersSubscriptionsModel.HighestSoundQuality}&sessionId={TidalController.Tidal.LoginModel.SessionId}&countryCode={TidalController.Tidal.CountryModel.CountryCode}");
                    trackItems.Add(TidalController.Tidal.TracksModel);
                }
            }

            var albumCoverUrl = "";

            var pOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Configuration.ConfigData.Downloads.DownloadLimit
            };

            await Task.Run(() =>
            {
                Parallel.ForEach(trackItems, pOptions, trackItem =>
                {
                    var track = TidalController.Tidal.AlbumTracksModel.Items.First(t => t.Id == trackItem.TrackId);
                    var urlRaw = trackItem.Url.Split(':');
                    var formattedNumber = track.TrackNumber < 10 ? $"0{track.TrackNumber}" : track.TrackNumber.ToString();
                    var dDir = $"{Configuration.ConfigData.Downloads.DownloadDirectory}\\{track.Artist.Name}\\{track.Album.Title}\\{formattedNumber}. {track.Title}.mp3";

                    var metadata = ";FFMETADATA1\r\n" +
                                   $"title={track.Title}\r\n" +
                                   $"artist={track.Artist.Name}\r\n" +
                                   $"album={track.Album.Title}\r\n" +
                                   $"track={track.TrackNumber}/{trackItems.Count}";

                    var tempId = $"tmp_{trackItem.TrackId}.txt";

                    using (var writer = new StreamWriter(Path.Combine(Configuration.DirectoryCurrent, tempId)))
                        foreach (var line in metadata)
                            writer.Write(line);

                    var coverFile = $"{track.Album.Cover}-320x320.jpg";

                    if (string.IsNullOrEmpty(albumCoverUrl))
                    {
                        albumCoverUrl = $"https://resources.tidal.com/images/{track.Album.Cover.Replace('-', '/')}/320x320.jpg";
                        DownloadAlbumCover(coverFile, albumCoverUrl);
                    }

                    var urlFinal = $"-i \"{urlRaw[0].Replace(".com", ".com:80").Replace("rtmp.", "rtmp://rtmp.")}/mp4:{urlRaw[1]}\" -i {tempId} -i {coverFile} -map_metadata 1 -c copy -id3v2_version 3 -codec:a libmp3lame -ab 320k -f mp3 \"{dDir}\"";

                    DownloadSong(urlFinal, dDir);
                });
            });
        }

        private void DownloadAlbumCover(string coverFile, string url)
        {
            using (var client = new WebClient())
            {
                client.Proxy = null;
                client.DownloadFile(url, coverFile);
            }
        }

        private async Task<MessageDialogResult> ShowDialog(string title, string message, MessageDialogStyle style)
        {
            return await this.ShowMessageAsync(title, message, style);
        }

        private bool _isInitialized;

        private void AvailableThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitialized)
                ChangeAppStyle(EnumUtils.Parse<AvailablePrimaryColors>(ComboBoxThemeStyle.SelectedItem.ToString()), true);
        }

        private void ButtonSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Configuration.ConfigData.Login.TidalUserName = TextBoxEmail.Text;
            Configuration.ConfigData.Login.TidalPassword = string.IsNullOrEmpty(TextBoxPassword.Password) ? string.Empty : Configuration.HashUtility.CreateHash(TextBoxPassword.Password);

            Configuration.ConfigData.Theme.ThemePrimaryColor = EnumUtils.Parse<AvailablePrimaryColors>(ComboBoxThemeStyle.Text);

            if (CheckBoxIsDark.IsChecked != null)
                Configuration.ConfigData.Theme.IsDark = CheckBoxIsDark.IsChecked.Value;

            GetLibDirectory();
            Configuration.ConfigData.Lib.LibDirectory = TextBoxLibDir.Text;

            Configuration.ConfigData.Downloads.DownloadLimit = int.Parse(TextBoxConcurrentDownloads.Text);

            Configuration.SaveConfig();
        }

        private void CheckBoxIsDark_Checked(object sender, RoutedEventArgs e)
        {
            if (!CheckBoxIsDark.IsChecked.HasValue || string.IsNullOrEmpty(ComboBoxThemeStyle.Text)) return;
            
            Configuration.ConfigData.Theme.IsDark = CheckBoxIsDark.IsChecked.Value;
            ChangeAppStyle(EnumUtils.Parse<AvailablePrimaryColors>(ComboBoxThemeStyle.Text), Configuration.ConfigData.Theme.IsDark);
        }
    }
}
