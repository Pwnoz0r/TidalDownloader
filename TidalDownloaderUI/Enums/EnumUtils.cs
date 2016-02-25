using System;

namespace TidalDownloaderUI.Enums
{
    public class EnumUtils
    {
        public static T Parse<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}
