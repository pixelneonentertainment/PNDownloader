using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PixelNeonDownloader
{
    public partial class ZamanlaPenceresi : Window
    {
        private int _saat = 8;
        private int _dakika = 0;
        private readonly DownloadItem _item;
        private DispatcherTimer? _kalanSureTimer;

        public ZamanlaPenceresi(DownloadItem item)
        {
            InitializeComponent();
            _item = item;
            TxtDosyaAdi.Text = item.DosyaAdi;
            TarihSecici.SelectedDate = DateTime.Today;

            // Şu anki saatten 1 saat sonrası
            _saat = DateTime.Now.Hour + 1;
            if (_saat >= 24) _saat = 0;
            _dakika = 0;

            GuncelleSaatGosterge();
            KalanSureGuncelle();

            // Kalan süre timer ayarı
            _kalanSureTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _kalanSureTimer.Tick += (s, e) => KalanSureGuncelle();
            _kalanSureTimer.Start();

            // Pencere kapatıldığında timer'ın durdurulması için olay tanımı
            this.Closed += ZamanlaPenceresi_Closed;
        }

        private void ZamanlaPenceresi_Closed(object? sender, EventArgs e)
        {
            if (_kalanSureTimer != null)
            {
                _kalanSureTimer.Stop();
                _kalanSureTimer = null;
            }
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ── Saat Kontrolleri ────────────────────────────
        private void SaatArttir_Click(object sender, RoutedEventArgs e)
        {
            _saat = (_saat + 1) % 24;
            GuncelleSaatGosterge();
            KalanSureGuncelle();
        }

        private void SaatAzalt_Click(object sender, RoutedEventArgs e)
        {
            _saat = (_saat - 1 + 24) % 24;
            GuncelleSaatGosterge();
            KalanSureGuncelle();
        }

        private void DakikaArttir_Click(object sender, RoutedEventArgs e)
        {
            _dakika = (_dakika + 5) % 60;
            GuncelleSaatGosterge();
            KalanSureGuncelle();
        }

        private void DakikaAzalt_Click(object sender, RoutedEventArgs e)
        {
            _dakika = (_dakika - 5 + 60) % 60;
            GuncelleSaatGosterge();
            KalanSureGuncelle();
        }

        private void GuncelleSaatGosterge()
        {
            TxtSaat.Text = _saat.ToString("D2");
            TxtDakika.Text = _dakika.ToString("D2");
        }

        private void KalanSureGuncelle()
        {
            var hedefZaman = HedefZamanHesapla();
            if (hedefZaman == null)
            {
                TxtKalanSure.Text = "Geçersiz tarih!";
                return;
            }

            var kalan = hedefZaman.Value - DateTime.Now;
            if (kalan.TotalSeconds <= 0)
            {
                TxtKalanSure.Text = "⚠ Bu zaman geçmiş!";
                return;
            }

            if (kalan.TotalHours >= 1)
                TxtKalanSure.Text = $"⏱ {(int)kalan.TotalHours} saat {kalan.Minutes} dakika sonra başlar";
            else
                TxtKalanSure.Text = $"⏱ {kalan.Minutes} dakika sonra başlar";
        }

        private DateTime? HedefZamanHesapla()
        {
            if (TarihSecici.SelectedDate == null) return null;
            var tarih = TarihSecici.SelectedDate.Value;
            return new DateTime(tarih.Year, tarih.Month, tarih.Day, _saat, _dakika, 0);
        }

        // Tarih seçimi değiştiğinde kalan süreyi hemen günceller
        private void TarihSecici_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            KalanSureGuncelle();
        }

        private void Zamanla_Click(object sender, RoutedEventArgs e)
        {
            var hedef = HedefZamanHesapla();
            if (hedef == null)
            {
                // MessageBox yerine System.Windows.MessageBox kullanıldı
                System.Windows.MessageBox.Show("Lütfen geçerli bir tarih seçin!",
                    "Uyarı", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (hedef.Value <= DateTime.Now)
            {
                // MessageBox yerine System.Windows.MessageBox kullanıldı
                System.Windows.MessageBox.Show("Seçilen zaman geçmiş! Gelecek bir zaman seçin.",
                    "Uyarı", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            _item.ZamanlanmisBaslangic = hedef.Value;
            _item.Zamanlanmis = true;
            _item.Durum = Durum.Bekliyor;
            _item.KalanSure = $"⏱ {hedef.Value:HH:mm}'de başlar";

            Close();
        
    }
    }
}