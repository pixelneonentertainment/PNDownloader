using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Clipboard = System.Windows.Clipboard;

namespace PixelNeonDownloader
{
    public partial class ChecksumPenceresi : Window
    {
        private readonly DownloadItem _item;
        private string _hesaplananHash = "";
        private CancellationTokenSource? _iptalToken;

        public ChecksumPenceresi(DownloadItem item)
        {
            InitializeComponent();
            _item = item;

            TxtDosyaAdi.Text = item.DosyaAdi;

            var dosyaYolu = Path.Combine(item.KayitYolu, item.DosyaAdi);
            TxtDosyaYolu.Text = dosyaYolu;

            if (!File.Exists(dosyaYolu))
            {
                TxtDosyaYolu.Text = "⚠ Dosya bulunamadı!";
                TxtDosyaYolu.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
                BtnHesapla.IsEnabled = false;
            }
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            _iptalToken?.Cancel();
            _iptalToken?.Dispose(); // Temizlik işlemi eklendi
            Close();
        }

        private void TxtBeklenenHash_Degisti(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            var hash = TxtBeklenenHash.Text.Trim();
            if (string.IsNullOrEmpty(hash))
            {
                TxtAlgoritmaHint.Text = "";
                return;
            }

            var tespit = ChecksumYoneticisi.AlgoritmamiTespit(hash);
            TxtAlgoritmaHint.Text = $"💡 Hash uzunluğuna göre tespit: {tespit}";

            switch (tespit)
            {
                case ChecksumTuru.MD5: RdoMD5.IsChecked = true; break;
                case ChecksumTuru.SHA1: RdoSHA1.IsChecked = true; break;
                case ChecksumTuru.SHA256: RdoSHA256.IsChecked = true; break;
                case ChecksumTuru.SHA512: RdoSHA512.IsChecked = true; break;
            }
        }

        private ChecksumTuru SeciliAlgoritma()
        {
            if (RdoMD5.IsChecked == true) return ChecksumTuru.MD5;
            if (RdoSHA1.IsChecked == true) return ChecksumTuru.SHA1;
            if (RdoSHA512.IsChecked == true) return ChecksumTuru.SHA512;
            return ChecksumTuru.SHA256;
        }

        private async void BtnHesapla_Click(object sender, RoutedEventArgs e)
        {
            var dosyaYolu = Path.Combine(_item.KayitYolu, _item.DosyaAdi);

            if (!File.Exists(dosyaYolu))
            {
                TxtDurum.Text = "Dosya bulunamadı!";
                return;
            }

            BtnHesapla.IsEnabled = false;
            BtnHesapla.Content = "⏳  HESAPLANIYOR...";
            PrgHesaplama.Visibility = Visibility.Visible;
            PrgHesaplama.Value = 0;
            SonucPaneli.Visibility = Visibility.Collapsed;
            TxtDurum.Text = "Hash hesaplanıyor...";

            // Yeni istek başlamadan önce varsa eski token'ı temizle
            _iptalToken?.Dispose();
            _iptalToken = new CancellationTokenSource();

            try
            {
                var algoritma = SeciliAlgoritma();
                var beklenenHash = TxtBeklenenHash.Text.Trim();

                var ilerleme = new Progress<double>(yuzde =>
                {
                    PrgHesaplama.Value = yuzde;
                    TxtDurum.Text = $"Hesaplanıyor... {yuzde:F0}%";
                });

                ChecksumSonucu sonuc;

                if (string.IsNullOrEmpty(beklenenHash))
                {
                    _hesaplananHash = await ChecksumYoneticisi.HesaplaAsync(
                        dosyaYolu, algoritma, ilerleme, _iptalToken.Token);

                    sonuc = new ChecksumSonucu
                    {
                        Algoritma = algoritma.ToString(),
                        Deger = _hesaplananHash,
                        Dogrulandi = false
                    };
                }
                else
                {
                    sonuc = await ChecksumYoneticisi.DogrulaAsync(
                        dosyaYolu, beklenenHash, algoritma,
                        ilerleme, _iptalToken.Token);
                    _hesaplananHash = sonuc.Deger;
                }

                SonucGoster(sonuc, beklenenHash);
            }
            catch (OperationCanceledException)
            {
                TxtDurum.Text = "İptal edildi.";
            }
            catch (Exception ex)
            {
                TxtDurum.Text = $"Hata: {ex.Message}";
            }
            finally
            {
                BtnHesapla.IsEnabled = true;
                BtnHesapla.Content = "🔐  HESAPLA ve DOĞRULA";
                PrgHesaplama.Visibility = Visibility.Collapsed;
                _iptalToken?.Dispose(); // Her hesaplama bitiminde kaynakları temizle
                _iptalToken = null;
            }
        }

        private void SonucGoster(ChecksumSonucu sonuc, string beklenenHash)
        {
            SonucPaneli.Visibility = Visibility.Visible;
            TxtHesaplananHash.Text = sonuc.Deger;

            if (!string.IsNullOrEmpty(beklenenHash))
            {
                KarsilastirmaPaneli.Visibility = Visibility.Visible;
                TxtBeklenenHashGoster.Text = beklenenHash.ToLowerInvariant();

                if (sonuc.Eslesme)
                {
                    TxtSonucIkon.Text = "✅";
                    TxtSonucBaslik.Text = "DOĞRULANDI!";
                    TxtSonucBaslik.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x39, 0xFF, 0x14));
                    SonucPaneli.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x39, 0xFF, 0x14));
                    TxtDurum.Text = "✅ Hash eşleşti — dosya hasarsız!";
                    TxtDurum.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x39, 0xFF, 0x14));
                }
                else
                {
                    TxtSonucIkon.Text = "❌";
                    TxtSonucBaslik.Text = "EŞLEŞME YOK!";
                    TxtSonucBaslik.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
                    SonucPaneli.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
                    TxtDurum.Text = "❌ Hash eşleşmedi — dosya bozuk olabilir!";
                    TxtDurum.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
                }
            }
            else
            {
                KarsilastirmaPaneli.Visibility = Visibility.Collapsed;
                TxtSonucIkon.Text = "🔐";
                TxtSonucBaslik.Text = $"{sonuc.Algoritma} HASH";
                TxtSonucBaslik.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5));
                SonucPaneli.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5));
                TxtDurum.Text = "Hash hesaplandı.";
                TxtDurum.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7A, 0x9C, 0xC0));
            }
        }

        private void BtnKopyala_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_hesaplananHash))
            {
                Clipboard.SetText(_hesaplananHash);
                TxtDurum.Text = "Hash kopyalandı!";
            }
        }
    }
}