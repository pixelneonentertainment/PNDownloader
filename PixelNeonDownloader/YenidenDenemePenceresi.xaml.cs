using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PixelNeonDownloader
{
    public partial class YenidenDenemePenceresi : Window
    {
        private bool _aktif = true;
        private bool _artanBekleme = true;
        private int _denemeSayisi = 3;
        private int _beklemeAraligi = 5;

        public YenidenDenemePenceresi()
        {
            InitializeComponent();

            var ayarlar = YenidenDenemeAyarlari.Yukle();
            _aktif = ayarlar.Aktif;
            _artanBekleme = ayarlar.ArtanBekleme;
            _denemeSayisi = ayarlar.MaksDenemeSayisi;
            _beklemeAraligi = ayarlar.BeklemeAraligi;

            SliderDenemeSayisi.Value = _denemeSayisi;
            SliderBekleme.Value = _beklemeAraligi;

            ToggleAktifGuncelle();
            ToggleArtanGuncelle();
            ArtanOrnekGuncelle();
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void ToggleAktif_Click(object sender, MouseButtonEventArgs e)
        {
            _aktif = !_aktif;
            ToggleAktifGuncelle();
        }

        private void ToggleArtan_Click(object sender, MouseButtonEventArgs e)
        {
            _artanBekleme = !_artanBekleme;
            ToggleArtanGuncelle();
            ArtanOrnekGuncelle();
        }

        private void ToggleAktifGuncelle()
            => ToggliUygula(ToggleAktif, ToggleAktifDaire, _aktif);

        private void ToggleArtanGuncelle()
            => ToggliUygula(ToggleArtan, ToggleArtanDaire, _artanBekleme);

        private static void ToggliUygula(
            System.Windows.Controls.Border border,
            Ellipse daire, bool acik)
        {
            if (acik)
            {
                border.Background = new SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5));
                daire.Fill = new SolidColorBrush(Colors.White);
                daire.Margin = new Thickness(25, 0, 0, 0);
            }
            else
            {
                border.Background = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(0x33, 0x00, 0xFF, 0xE5));
                daire.Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x3A, 0x50, 0x70));
                daire.Margin = new Thickness(3, 0, 0, 0);
            }
        }

        private void SliderDenemeSayisi_Degisti(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            _denemeSayisi = (int)SliderDenemeSayisi.Value;
            if (TxtDenemeSayisiGosterge != null)
                TxtDenemeSayisiGosterge.Text = $"{_denemeSayisi} deneme";
        }

        private void SliderBekleme_Degisti(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            _beklemeAraligi = (int)SliderBekleme.Value;
            if (TxtBeklemeGosterge != null)
                TxtBeklemeGosterge.Text = $"{_beklemeAraligi} saniye";
            ArtanOrnekGuncelle();
        }

        private void DenemeKisayol_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn
                && int.TryParse(btn.Tag?.ToString(), out var deger))
                SliderDenemeSayisi.Value = deger;
        }

        private void ArtanOrnekGuncelle()
        {
            if (TxtArtanOrnek == null) return;
            if (_artanBekleme)
            {
                var s1 = _beklemeAraligi;
                var s2 = _beklemeAraligi * 2;
                var s3 = _beklemeAraligi * 4;
                TxtArtanOrnek.Text = $"Örnek: {s1}s → {s2}s → {s3}s";
            }
            else
            {
                TxtArtanOrnek.Text = $"Örnek: {_beklemeAraligi}s → " +
                    $"{_beklemeAraligi}s → {_beklemeAraligi}s";
            }
        }

        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            var ayarlar = new YenidenDenemeAyarlari
            {
                Aktif = _aktif,
                MaksDenemeSayisi = _denemeSayisi,
                BeklemeAraligi = _beklemeAraligi,
                ArtanBekleme = _artanBekleme
            };
            ayarlar.Kaydet();

            System.Windows.MessageBox.Show(
                $"Ayarlar kaydedildi!\n\n" +
                $"Durum: {(_aktif ? "Aktif ✓" : "Pasif")}\n" +
                $"Maks. Deneme: {_denemeSayisi}x\n" +
                $"Bekleme: {_beklemeAraligi}s\n" +
                $"Artan Bekleme: {(_artanBekleme ? "Açık ✓" : "Kapalı")}",
                "Yeniden Deneme Ayarları",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            Close();
        }
    }
}