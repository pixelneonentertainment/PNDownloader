using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PixelNeonDownloader
{
    public partial class IndirmePenceresi : Window
    {
        public DownloadItem? Sonuc { get; private set; }
        private bool _urlModu = true;
        private string _secileDosyaYolu = "";

        public IndirmePenceresi()
        {
            InitializeComponent();
            TxtKayitYolu.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "PixelNeon");
        }

        // ── Mod Değiştirme ───────────────────────────────
        private void BtnURLMod_Click(object sender, RoutedEventArgs e)
        {
            _urlModu = true;
            PanelURL.Visibility = Visibility.Visible;
            PanelDosya.Visibility = Visibility.Collapsed;

            // URL butonu aktif
            BtnURLMod.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5));
            BtnURLMod.BorderBrush = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5));

            // Dosya butonu pasif
            BtnDosyaMod.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x3A, 0x50, 0x70));
            BtnDosyaMod.BorderBrush = new SolidColorBrush(
                System.Windows.Media.Colors.Transparent);
        }

        private void BtnDosyaMod_Click(object sender, RoutedEventArgs e)
        {
            _urlModu = false;
            PanelURL.Visibility = Visibility.Collapsed;
            PanelDosya.Visibility = Visibility.Visible;

            // Dosya butonu aktif
            BtnDosyaMod.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5));
            BtnDosyaMod.BorderBrush = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5));

            // URL butonu pasif
            BtnURLMod.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x3A, 0x50, 0x70));
            BtnURLMod.BorderBrush = new SolidColorBrush(
                System.Windows.Media.Colors.Transparent);
        }

        // ── Dosya Seçme ──────────────────────────────────
        private void DosyaAlani_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Dosya Seç",
                Filter = "Tüm Desteklenen|*.torrent;*.zip;*.rar;*.7z;*.tar;*.gz;*.*" +
                         "|Torrent|*.torrent" +
                         "|Arşiv|*.zip;*.rar;*.7z;*.tar;*.gz" +
                         "|Tüm Dosyalar|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _secileDosyaYolu = dialog.FileName;
                TxtSecileDosya.Text = Path.GetFileName(_secileDosyaYolu);
                TxtSecileDosya.Foreground = new SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5));

                // Dosya adını otomatik doldur
                if (string.IsNullOrEmpty(TxtDosyaAdi.Text))
                    TxtDosyaAdi.Text = Path.GetFileName(_secileDosyaYolu);
            }
        }

        // ── URL Değişince ────────────────────────────────
        private void TxtUrl_Degisti(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TxtUrl == null || TipGostergesi == null) return;

            var url = TxtUrl.Text.Trim();

            if (string.IsNullOrEmpty(url))
            {
                TipGostergesi.Visibility = Visibility.Collapsed;
                return;
            }

            TipGostergesi.Visibility = Visibility.Visible;

            if (url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                TipIkonu.Text = "⚡";
                TipMetni.Text = "Magnet link tespit edildi";
                if (string.IsNullOrEmpty(TxtDosyaAdi.Text))
                    TxtDosyaAdi.Text = "magnet-indirme";
            }
            else if (url.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
            {
                TipIkonu.Text = "⟁";
                TipMetni.Text = "Torrent dosyası — Ekle'ye basınca detaylar açılacak";
                try
                {
                    var ad = Path.GetFileNameWithoutExtension(url);
                    if (!string.IsNullOrEmpty(ad) && string.IsNullOrEmpty(TxtDosyaAdi.Text))
                        TxtDosyaAdi.Text = ad;
                }
                catch { }
            }
            else if (url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                TipIkonu.Text = "⬡";
                TipMetni.Text = "FTP indirmesi tespit edildi";
                try
                {
                    var ad = Path.GetFileName(new Uri(url).LocalPath);
                    if (!string.IsNullOrEmpty(ad) && string.IsNullOrEmpty(TxtDosyaAdi.Text))
                        TxtDosyaAdi.Text = ad;
                }
                catch { }
            }
            else if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                TipIkonu.Text = "↓";
                TipMetni.Text = "HTTP indirmesi tespit edildi";
                try
                {
                    var ad = Path.GetFileName(new Uri(url).LocalPath);
                    if (!string.IsNullOrEmpty(ad) && string.IsNullOrEmpty(TxtDosyaAdi.Text))
                        TxtDosyaAdi.Text = ad;
                }
                catch { }
            }
            else
            {
                TipGostergesi.Visibility = Visibility.Collapsed;
            }
        }

        // ── Klasör Seç ───────────────────────────────────
        private void KlasorSec_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "İndirme klasörü seç",
                InitialDirectory = TxtKayitYolu.Text
            };
            if (dialog.ShowDialog() == true)
                TxtKayitYolu.Text = dialog.FolderName;
        }

        // ── Kapat ────────────────────────────────────────
        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            Sonuc = null;
            Close();
        }

        // ── Ekle ─────────────────────────────────────────
        private void Ekle_Click(object sender, RoutedEventArgs e)
        {
            var kategori = (CmbKategori.SelectedItem as
                System.Windows.Controls.ComboBoxItem)
                ?.Content?.ToString() ?? "Genel";

            if (_urlModu)
            {
                // URL modu
                if (string.IsNullOrWhiteSpace(TxtUrl.Text))
                {
                    TxtUrl.Focus();
                    return;
                }

                var url = TxtUrl.Text.Trim();

                // Torrent dosyasıysa özel pencere aç
                if (url.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(url))
                {
                    var torrentPencere = new TorrentPenceresi(url) { Owner = this };
                    torrentPencere.ShowDialog();

                    if (torrentPencere.Sonuc != null)
                    {
                        Sonuc = torrentPencere.Sonuc;
                        Close();
                    }
                    return;
                }

                IndirmeTuru tur;
                if (url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
                    tur = IndirmeTuru.Magnet;
                else if (url.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
                    tur = IndirmeTuru.Torrent;
                else if (url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                    tur = IndirmeTuru.FTP;
                else
                    tur = IndirmeTuru.HTTP;

                var dosyaAdi = TxtDosyaAdi.Text.Trim();
                if (string.IsNullOrEmpty(dosyaAdi))
                {
                    try { dosyaAdi = Path.GetFileName(new Uri(url).LocalPath); }
                    catch { dosyaAdi = "indirilen_dosya"; }
                }

                Sonuc = new DownloadItem
                {
                    Url = url,
                    DosyaAdi = dosyaAdi,
                    KayitYolu = TxtKayitYolu.Text.Trim(),
                    Tur = tur,
                    Kategori = kategori,
                    Durum = Durum.Bekliyor
                };
            }
            else
            {
                // Dosya modu
                if (string.IsNullOrEmpty(_secileDosyaYolu))
                {
                    System.Windows.MessageBox.Show(
                        "Lütfen bir dosya seçin!",
                        "Uyarı",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Torrent dosyasıysa özel pencere aç
                if (_secileDosyaYolu.EndsWith(".torrent",
                    StringComparison.OrdinalIgnoreCase))
                {
                    var torrentPencere = new TorrentPenceresi(_secileDosyaYolu)
                    {
                        Owner = this
                    };
                    torrentPencere.ShowDialog();

                    if (torrentPencere.Sonuc != null)
                    {
                        Sonuc = torrentPencere.Sonuc;
                        Close();
                    }
                    return;
                }

                // Normal dosya — kayıt klasörüne kopyala veya doğrudan çıkart
                var dosyaAdi = TxtDosyaAdi.Text.Trim();
                if (string.IsNullOrEmpty(dosyaAdi))
                    dosyaAdi = Path.GetFileName(_secileDosyaYolu);

                Sonuc = new DownloadItem
                {
                    Url = _secileDosyaYolu,
                    DosyaAdi = dosyaAdi,
                    KayitYolu = TxtKayitYolu.Text.Trim(),
                    Tur = IndirmeTuru.HTTP,
                    Kategori = kategori,
                    DosyaBoyutu = new FileInfo(_secileDosyaYolu).Length,
                    IndirilenBytes = new FileInfo(_secileDosyaYolu).Length,
                    Ilerleme = 1.0,
                    Durum = Durum.Tamamlandi
                };
            }

            Close();
        }
    }
}