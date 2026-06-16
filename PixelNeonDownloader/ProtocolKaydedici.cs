using Microsoft.Win32;
using System;

namespace PixelNeonDownloader
{
    public static class ProtocolKaydedici
    {
        private const string PROTOKOL = "pixelneon";

        public static void Kaydet()
        {
            try
            {
                // .NET modern sürümleri için en güvenilir süreç yolu alma yöntemi
                var exeYolu = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exeYolu))
                {
                    Console.WriteLine("Uygulama yolu tespit edilemedi.");
                    return;
                }

                using var key = Registry.CurrentUser.CreateSubKey(
                    $@"Software\Classes\{PROTOKOL}");

                key.SetValue("", "URL:Pixel Neon Downloader Protocol");
                key.SetValue("URL Protocol", "");

                using var iconKey = key.CreateSubKey("DefaultIcon");
                iconKey.SetValue("", $"{exeYolu},0");

                using var commandKey = key.CreateSubKey(@"shell\open\command");
                commandKey.SetValue("", $"\"{exeYolu}\" \"%1\"");

                Console.WriteLine("Protokol kaydedildi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Protokol kayıt hatası: {ex.Message}");
            }
        }

        public static void Kaldir()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(
                    $@"Software\Classes\{PROTOKOL}", false);
            }
            catch { }
        }

        public static string? UrlCozumle(string protokolLink)
        {
            try
            {
                var uri = new Uri(protokolLink);
                var query = uri.Query.TrimStart('?');

                foreach (var param in query.Split('&'))
                {
                    var parcalar = param.Split('=', 2);
                    if (parcalar.Length == 2 &&
                        parcalar[0].Equals("url", StringComparison.OrdinalIgnoreCase))
                    {
                        return Uri.UnescapeDataString(parcalar[1]);
                    }
                }
            }
            catch { }

            return null;
        }
    }
}