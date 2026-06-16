using Microsoft.Win32;
using System;

namespace PixelNeonDownloader
{
    public static class BaslangicYoneticisi
    {
        private const string UYGULAMA_ADI = "PixelNeonDownloader";
        private const string REGISTRY_YOLU = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static void BaslangicaEkle()
        {
            try
            {
                var exeYolu = GetExeYolu();
                if (string.IsNullOrEmpty(exeYolu)) return;

                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_YOLU, true);
                key?.SetValue(UYGULAMA_ADI, $"\"{exeYolu}\"");
            }
            catch { }
        }

        public static void BaslangictanKaldir()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_YOLU, true);
                key?.DeleteValue(UYGULAMA_ADI, false);
            }
            catch { }
        }

        public static bool BaslangictaMi()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_YOLU, false);
                return key?.GetValue(UYGULAMA_ADI) != null;
            }
            catch { return false; }
        }

        public static void TorrentDosyasiniIliskilendir()
        {
            try
            {
                var exeYolu = GetExeYolu();
                if (string.IsNullOrEmpty(exeYolu)) return;

                using var torrentKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.torrent");
                torrentKey.SetValue("", "PixelNeon.Torrent");

                using var progIdKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\PixelNeon.Torrent");
                progIdKey.SetValue("", "Torrent Dosyası");

                using var iconKey = progIdKey.CreateSubKey("DefaultIcon");
                iconKey.SetValue("", $"\"{exeYolu}\",0");

                using var commandKey = progIdKey.CreateSubKey(@"shell\open\command");
                commandKey.SetValue("", $"\"{exeYolu}\" \"%1\"");

                // Platform uyumluluğu için sabit parametrelerle çağrı yapıldı
                SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }

        public static void TorrentIliskilendirmesiniKaldir()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.torrent", false);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\PixelNeon.Torrent", false);
                SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }

        public static bool TorrentIliskilendirilmisMi()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.torrent", false);
                return key?.GetValue("")?.ToString() == "PixelNeon.Torrent";
            }
            catch { return false; }
        }

        private static string? GetExeYolu()
        {
            // .NET modern sürümleri için en güvenli süreç yolu alma yöntemi
            return Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        }

        // P/Invoke marshalling güvenliği için opsiyonel parametreler kaldırılarak kesin imzayla tanımlandı
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern void SHChangeNotify(
            int wEventId,
            uint uFlags,
            IntPtr dwItem1,
            IntPtr dwItem2);
    }
}