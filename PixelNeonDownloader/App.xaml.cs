using System;
using System.Windows;

namespace PixelNeonDownloader
{
    public partial class App : System.Windows.Application
    {
        public static string GelenLink { get; set; } = "";
        public static string GelenTorrent { get; set; } = "";

        // MainWindow.xaml.cs dosyanızın hata vermemesi için geriye dönük uyumluluk köprüleri (alias):
        public static string GelenkLink => GelenLink;
        public static string GelenkTorrent => GelenTorrent;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                var arg = e.Args[0];

                if (arg.StartsWith("pixelneon://", StringComparison.OrdinalIgnoreCase))
                    GelenLink = arg.Replace("pixelneon://", "");

                else if (arg.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
                    GelenTorrent = arg;

                else if (arg.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                         arg.StartsWith("ftp", StringComparison.OrdinalIgnoreCase) ||
                         arg.StartsWith("magnet", StringComparison.OrdinalIgnoreCase))
                    GelenLink = arg;
            }

            try { DilYoneticisi.DilYukle(); } catch { }
            try { TemaYoneticisi.TemaYukle(); } catch { }
            try { VeritabaniYoneticisi.Baslat(); } catch { }

            var ana = new MainWindow();
            ana.Show();

            base.OnStartup(e);

            try
            {
                var ultraAyarlar = UltraHizAyarlari.Yukle();
                IndirmeServisi.ParcaSayisi = ultraAyarlar.ParcaSayisi;
                IndirmeServisi.BufferBoyutu = UltraHizAyarlari.BufferBoyutuHesapla(ultraAyarlar.BufferSeviye);
                IndirmeServisi.UltraMod = ultraAyarlar.UltraMod;
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ToastBildirimYoneticisi.Dispose();
            base.OnExit(e);
        }
    }
}