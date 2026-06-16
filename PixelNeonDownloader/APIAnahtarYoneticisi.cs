using System;
using System.IO;

namespace PixelNeonDownloader
{
    public static class APIAnahtarYoneticisi
    {
        private static readonly string _kayitYolu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "api.key");

        public static string AnahtarAl()
        {
            try
            {
                if (File.Exists(_kayitYolu))
                    return File.ReadAllText(_kayitYolu).Trim();
            }
            catch { }
            return "";
        }

        public static void AnahtarKaydet(string anahtar)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_kayitYolu)!);
                File.WriteAllText(_kayitYolu, anahtar.Trim());
            }
            catch { }
        }

        public static bool AnahtarMevcut()
            => !string.IsNullOrEmpty(AnahtarAl());
    }
}