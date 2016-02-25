// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
// ReSharper disable RedundantAssignment

namespace TidalDownloaderUI.Utils
{
    public class MD5Utility
    {
        public string CreateHash(string plainText)
        {
            byte[] completeKey;
            using (var stream = new MemoryStream())
            {
                using (var alg = new RijndaelManaged())
                {
                    alg.KeySize = 256;
                    alg.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(GetUniqueInfo("Win32_Processor", "ProcessorID"), GenerateSalt(), 1000);
                    alg.Key = key.GetBytes(alg.KeySize / 8);
                    alg.IV = key.GetBytes(alg.BlockSize / 8);
                    alg.Mode = CipherMode.CBC;

                    using (var cryptoStream = new CryptoStream(stream, alg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(Encoding.ASCII.GetBytes(plainText), 0, plainText.Length);
                        cryptoStream.Close();
                    }
                }
                completeKey = stream.ToArray();
                plainText = null;
            }
            return BitConverter.ToString(completeKey);
        }

        public string DecryptHash(string hash)
        {
            var completeKey = FromHex(hash);
            using (var stream = new MemoryStream())
            {
                using (var alg = new RijndaelManaged())
                {
                    alg.KeySize = 256;
                    alg.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(GetUniqueInfo("Win32_Processor", "ProcessorID"), GenerateSalt(),
                        1000);
                    alg.Key = key.GetBytes(alg.KeySize/8);
                    alg.IV = key.GetBytes(alg.BlockSize/8);
                    alg.Mode = CipherMode.CBC;

                    using (var cryptoStream = new CryptoStream(stream, alg.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(completeKey, 0, completeKey.Length);
                        cryptoStream.Close();
                    }
                }
                completeKey = stream.ToArray();
            }
            return Encoding.UTF8.GetString(completeKey);
        }

        private static byte[] FromHex(string hex)
        {
            hex = hex.Replace("-", "");
            var raw = new byte[hex.Length / 2];
            for (var i = 0; i < raw.Length; i++)
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return raw;
        }


        private byte[] GenerateSalt()
        {
            return Encoding.ASCII.GetBytes($"{GetMachineName()}TiDALStreaMing{GetUniqueInfo("Win32_DiskDrive", "SerialNumber")}");
        }

        private string GetUniqueInfo(string mc, string mo)
        {
            string[] result = {""};
            var managementClass = new ManagementClass(mc);
            var managementObjectClass = managementClass.GetInstances();

            foreach (var managementObject in managementObjectClass.Cast<ManagementObject>().Where(managementObject => result[0] == ""))
                result[0] = managementObject[mo].ToString();

            return result[0];
        }

        private string GetMachineName()
        {
            return Environment.MachineName;
        }
    }
}
