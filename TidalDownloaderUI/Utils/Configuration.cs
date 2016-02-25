using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TidalDownloaderUI.Utils.Models;
using TidalSharp.Models.Static;

namespace TidalDownloaderUI.Utils
{
    public class Configuration
    {
        public static string DirectoryCurrent = Directory.GetCurrentDirectory();
        public static string FileConfig = Path.Combine(DirectoryCurrent, "config.json");

        public ConfigurationModel ConfigData { get; set; } = new ConfigurationModel();
        public ConfigurationBuilder ConfigBuilder { get; set; } = new ConfigurationBuilder();
        public MD5Utility HashUtility { get; set; } = new MD5Utility();

        public Configuration()
        {
            if (!File.Exists(FileConfig))
            {
                File.Create(FileConfig).Close();
                SaveConfig();
            }

            ConfigBuilder.AddJsonFile(FileConfig);
            ConfigBuilder.Build().Bind(ConfigData);
        }

        public void SaveConfig()
        {
            File.WriteAllBytes(FileConfig, Encoding.Default.GetBytes(JsonConvert.SerializeObject(ConfigData, Formatting.Indented)));
        }

        private static readonly List<string> AllowedFeaturedAlbumsProperties = new List<string>
        {
            "Id", "Title", "NumberOfTracks", "Artist"
        };

        public static DataTable ToDataTable<T>(IEnumerable<T> data)
        {
            var properties = TypeDescriptor.GetProperties(typeof(T));
            var table = new DataTable();

            foreach (PropertyDescriptor prop in properties)
            {
                if (typeof(T) == typeof(Item))
                {
                    if (!AllowedFeaturedAlbumsProperties.Contains(prop.Name))
                        continue;
                    switch (prop.Name)
                    {
                        case "NumberOfTracks":
                            table.Columns.Add(prop.Name, typeof(int));
                            break;
                        case "Id":
                            table.Columns.Add(prop.Name, typeof(int));
                            break;
                        default:
                            table.Columns.Add(prop.Name, typeof(string));
                            break;
                    }
                }
                else
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (var item in data)
            {
                var row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    if (AllowedFeaturedAlbumsProperties.Contains(prop.Name))
                    {
                        switch (prop.Name)
                        {
                            case "Artist":
                                row[prop.Name] = ((Item)(object)item).Artist.Name;
                                break;
                            default:
                                row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                                break;
                        }
                    }
                }
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
