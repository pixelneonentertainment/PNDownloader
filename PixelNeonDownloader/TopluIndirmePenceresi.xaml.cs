using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace PixelNeonDownloader
{
    public partial class TopluIndirmePenceresi : Window
    {
        public List<DownloadItem> Sonuclar { get; private set; } = new();

        public TopluIndirmePenceresi()
        {
            InitializeComponent();
            TxtKayitYolu.Text = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "Downloads", "PixelNeon");
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            Sonuclar.Clear();
            Close();
        }

        private void KlasorSec_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Kayıt klasörü seç",
                InitialDirectory = TxtKayitYolu.Text
            };
            if (dialog.ShowDialog() == true)
                TxtKayitYolu.Text = dialog.FolderName;
        }

        private void BtnPanoYapistir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    var metin = System.Windows.Clipboard.GetText();
                    if (!string.IsNullOrEmpty(metin))
                    {
                        TxtLinkler.Text = string.IsNullOrEmpty(TxtLinkler.Text)
                            ? metin
                            : TxtLinkler.Text + "\n" + metin;
                        TxtLinkler.ScrollToEnd();
                        SayilariGuncelle();
                    }
                }
            }
            catch { }
        }

        private void TxtLinkler_LostFocus(object sender, RoutedEventArgs e)
        {
            SayilariGuncelle();
        }

        private void SayilariGuncelle()
        {
            if (TxtLinkler == null) return;

            var satirlar = TxtLinkler.Text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            int toplam = 0;
            int gecerli = 0;
            int gecersiz = 0;

            foreach (var satir in satirlar)
            {
                var url = satir.Trim();
                if (string.IsNullOrEmpty(url)) continue;

                toplam++;
                if (LinkGecerliMi(url))
                    gecerli++;
                else
                    gecersiz++;
            }

            TxtToplamSayisi.Text = toplam.ToString();
            TxtGecerliSayisi.Text = gecerli.ToString();
            TxtGecersizSayisi.Text = gecersiz.ToString();

            BtnTopluEkle.IsEnabled = gecerli > 0;
            TxtDurum.Text = gecerli > 0
                ? $"{gecerli} geçerli link bulundu"
                : "Geçerli link bulunamadı";
        }

        private static bool LinkGecerliMi(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (url.Length > 2000) return false;

            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase) ||
                   (url.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(url));
        }

        private IndirmeTuru TurBelirle(string url)
        {
            if (url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
                return IndirmeTuru.Magnet;
            if (url.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
                return IndirmeTuru.Torrent;
            if (url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                return IndirmeTuru.FTP;
            return IndirmeTuru.HTTP;
        }

        private void BtnTopluEkle_Click(object sender, RoutedEventArgs e)
        {
            Sonuclar.Clear();

            var satirlar = TxtLinkler.Text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var kayitYolu = TxtKayitYolu.Text.Trim();

            foreach (var satir in satirlar)
            {
                var url = satir.Trim();
                if (!LinkGecerliMi(url)) continue;

                string dosyaAdi;
                try
                {
                    dosyaAdi = Path.GetFileName(new Uri(url).LocalPath);
                    if (string.IsNullOrEmpty(dosyaAdi))
                        dosyaAdi = $"indirme_{Sonuclar.Count + 1}";
                }
                catch
                {
                    dosyaAdi = $"indirme_{Sonuclar.Count + 1}";
                }

                var tur = TurBelirle(url);
                var kategori = AkilliKlasorleme.KlasorBelirle(dosyaAdi);

                Sonuclar.Add(new DownloadItem
                {
                    Url = url,
                    DosyaAdi = dosyaAdi,
                    KayitYolu = kayitYolu,
                    Tur = tur,
                    Kategori = kategori,
                    Durum = Durum.Bekliyor
                });
            }

            TxtDurum.Text = $"✅ {Sonuclar.Count} indirme eklendi!";
            Close();
        }
    }
}