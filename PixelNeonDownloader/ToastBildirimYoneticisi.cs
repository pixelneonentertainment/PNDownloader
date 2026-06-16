using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PixelNeonDownloader
{
    public static class ToastBildirimYoneticisi
    {
        private static NotifyIcon? _notifyIcon;

        private static NotifyIcon AIcon()
        {
            if (_notifyIcon == null)
            {
                _notifyIcon = new NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    Visible = true,
                    Text = "Pixel Neon Downloader"
                };
            }
            return _notifyIcon;
        }

        public static void IndirmeTamamlandi(string dosyaAdi, string kayitYolu)
        {
            try
            {
                var icon = AIcon();
                icon.BalloonTipIcon = ToolTipIcon.Info;
                icon.BalloonTipTitle = "✅ İndirme Tamamlandı";
                icon.BalloonTipText = $"{dosyaAdi}\n📁 {kayitYolu}";
                icon.ShowBalloonTip(5000);
            }
            catch { }
        }

        public static void IndirmeHatasi(string dosyaAdi, string hata = "")
        {
            try
            {
                var icon = AIcon();
                icon.BalloonTipIcon = ToolTipIcon.Error;
                icon.BalloonTipTitle = "❌ İndirme Hatası";
                icon.BalloonTipText = string.IsNullOrEmpty(hata)
                    ? $"{dosyaAdi} indirilemedi."
                    : $"{dosyaAdi}\n{hata}";
                icon.ShowBalloonTip(5000);
            }
            catch { }
        }

        public static void YaridaKalanBulundu(int sayi)
        {
            try
            {
                var icon = AIcon();
                icon.BalloonTipIcon = ToolTipIcon.Warning;
                icon.BalloonTipTitle = "⚠ Yarıda Kalan İndirmeler";
                icon.BalloonTipText = $"{sayi} indirme kaldığı yerden devam edebilir.";
                icon.ShowBalloonTip(5000);
            }
            catch { }
        }

        public static void ZamanlanmisBasladi(string dosyaAdi)
        {
            try
            {
                var icon = AIcon();
                icon.BalloonTipIcon = ToolTipIcon.Info;
                icon.BalloonTipTitle = "⏱ Zamanlanmış İndirme Başladı";
                icon.BalloonTipText = dosyaAdi;
                icon.ShowBalloonTip(3000);
            }
            catch { }
        }

        public static void BildirimGoster(string baslik, string mesaj)
        {
            try
            {
                var icon = AIcon();
                icon.BalloonTipIcon = ToolTipIcon.Info;
                icon.BalloonTipTitle = baslik;
                icon.BalloonTipText = mesaj;
                icon.ShowBalloonTip(3000);
            }
            catch { }
        }

        public static void Dispose()
        {
            try
            {
                _notifyIcon?.Dispose();
                _notifyIcon = null;
            }
            catch { }
        }
    }
}