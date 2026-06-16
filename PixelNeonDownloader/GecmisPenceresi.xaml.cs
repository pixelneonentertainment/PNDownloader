using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace PixelNeonDownloader
{
    public partial class GecmisPenceresi : Window
    {
        private readonly ObservableCollection<DownloadItem> _aktifIndirmeler;
        private string _aktifFiltre = "Tümü";
        public event Action<DownloadItem>? TekrarIndirmeIstendi;

        public GecmisPenceresi(
            ObservableCollection<DownloadItem> aktifIndirmeler)
        {
            InitializeComponent();
            _aktifIndirmeler = aktifIndirmeler;

            Loaded += (s, e) =>
            {
                ListeyiYukle();
                IstatistikGuncelle();
            };
        }

        private void Baslik_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e) => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void BtnFiltre_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn) return;
            _aktifFiltre = btn.Tag?.ToString() ?? "Tümü";

            foreach (var b in new[] {
                BtnFiltreTumu, BtnFiltreTamamlandi,
                BtnFiltreHata, BtnFiltreDuraklat })
            {
                var aktif = b.Tag?.ToString() == _aktifFiltre;
                b.BorderBrush = new SolidColorBrush(aktif
                    ? Color.FromRgb(0x00, 0xFF, 0xE5)
                    : Color.FromArgb(0x22, 0x3A, 0x50, 0x70));
                b.Foreground = new SolidColorBrush(aktif
                    ? Color.FromRgb(0x00, 0xFF, 0xE5)
                    : Color.FromRgb(0x7A, 0x9C, 0xC0));
            }

            ListeyiYukle();
        }

        // Performans İyileştirmesi: Arama işlemlerinde veri tabanı yükünün arayüzü kilitlemesi engellendi (Asenkron Okuma)
        private async void ListeyiYukle()
        {
            try
            {
                var arama = TxtArama?.Text?.Trim() ?? "";
                var filtre = _aktifFiltre;

                // Veritabanı sorgusu asenkron olarak arka planda çalıştırılır.
                var liste = await Task.Run(() => VeritabaniYoneticisi.GecmisGetir(arama, filtre));

                if (GecmisListesi != null)
                    GecmisListesi.ItemsSource = liste;
            }
            catch { }
        }

        private async void IstatistikGuncelle()
        {
            try
            {
                var (toplamBoyut, toplamSayi, basariliSayi, ortHiz) =
                    await Task.Run(() => VeritabaniYoneticisi.IstatistikGetir());

                if (TxtToplamSayi != null)
                    TxtToplamSayi.Text = toplamSayi.ToString();
                if (TxtBasariliSayi != null)
                    TxtBasariliSayi.Text = basariliSayi.ToString();
                if (TxtToplamBoyut != null)
                    TxtToplamBoyut.Text = BoyutFormatla(toplamBoyut);
                if (TxtOrtHiz != null)
                    TxtOrtHiz.Text = HizFormatla(ortHiz);
            }
            catch { }
        }

        private void TxtArama_Degisti(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
            => ListeyiYukle();

        private void BtnYenile_Click(object sender, RoutedEventArgs e)
        {
            ListeyiYukle();
            IstatistikGuncelle();
        }

        private void MenuKlasor_Click(object sender, RoutedEventArgs e)
        {
            if (GecmisListesi.SelectedItem is IndirmeKaydi kayit)
            {
                var yol = kayit.KayitYolu;
                if (!string.IsNullOrEmpty(yol) && System.IO.Directory.Exists(yol))
                {
                    // Yol boşluk içeriyorsa Explorer'ın doğru klasörü açması için çift tırnak tırnak eklendi.
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{yol}\"");
                }
            }
        }

        private void MenuUrl_Click(object sender, RoutedEventArgs e)
        {
            if (GecmisListesi.SelectedItem is IndirmeKaydi kayit)
                Clipboard.SetText(kayit.Url);
        }

        private void MenuTekrarIndir_Click(object sender, RoutedEventArgs e)
        {
            if (GecmisListesi.SelectedItem is IndirmeKaydi kayit)
            {
                var item = new DownloadItem
                {
                    Url = kayit.Url,
                    DosyaAdi = kayit.DosyaAdi,
                    KayitYolu = kayit.KayitYolu,
                    Kategori = kayit.Kategori,
                    Tur = Enum.TryParse<IndirmeTuru>(
                        kayit.Tur, out var tur)
                        ? tur : IndirmeTuru.HTTP,
                    Durum = Durum.Bekliyor
                };
                TekrarIndirmeIstendi?.Invoke(item);
                Close();
            }
        }

        private void MenuSil_Click(object sender, RoutedEventArgs e)
        {
            if (GecmisListesi.SelectedItem is IndirmeKaydi kayit)
            {
                VeritabaniYoneticisi.KayitSil(kayit.Id);
                ListeyiYukle();
                IstatistikGuncelle();
            }
        }

        private void BtnTemizle_Click(object sender, RoutedEventArgs e)
        {
            var sonuc = MessageBox.Show(
                "Tüm indirme geçmişi silinecek!\nEmin misiniz?",
                "Geçmişi Temizle",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (sonuc == MessageBoxResult.Yes)
            {
                VeritabaniYoneticisi.TumunuTemizle();
                ListeyiYukle();
                IstatistikGuncelle();
            }
        }

        private static string BoyutFormatla(long bytes)
        {
            if (bytes >= 1_073_741_824)
                return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576)
                return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }

        private static string HizFormatla(double bps)
        {
            if (bps >= 1_048_576) return $"{bps / 1_048_576:F1} MB/s";
            if (bps >= 1024) return $"{bps / 1024:F1} KB/s";
            return $"{bps:F0} B/s";
        }
    }
}