using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PixelNeonDownloader
{
    public partial class IstatistikPenceresi : Window
    {
        private static readonly System.Windows.Media.FontFamily _monoFont =
            new System.Windows.Media.FontFamily("Consolas");

        public IstatistikPenceresi(ObservableCollection<DownloadItem> indirmeler)
        {
            InitializeComponent();
            IstatistikleriGuncelle(indirmeler);
        }

        private void IstatistikleriGuncelle(ObservableCollection<DownloadItem> indirmeler)
        {
            TxtToplamIndirme.Text = indirmeler.Count.ToString();
            TxtTamamlanan.Text = indirmeler
                .Count(d => d.Durum == Durum.Tamamlandi).ToString();
            TxtHatali.Text = indirmeler
                .Count(d => d.Durum == Durum.Hata).ToString();
            TxtAktif.Text = indirmeler
                .Count(d => d.Durum == Durum.Indiriliyor).ToString();

            var toplamBoyut = indirmeler.Sum(d => d.IndirilenBytes);
            TxtToplamBoyut.Text = ByteFormatla(toplamBoyut);

            TxtHTTP.Text = indirmeler
                .Count(d => d.Tur == IndirmeTuru.HTTP).ToString();
            TxtTorrent.Text = indirmeler
                .Count(d => d.Tur == IndirmeTuru.Torrent).ToString();
            TxtMagnet.Text = indirmeler
                .Count(d => d.Tur == IndirmeTuru.Magnet).ToString();
            TxtFTP.Text = indirmeler
                .Count(d => d.Tur == IndirmeTuru.FTP).ToString();

            // Kategori dağılımı
            KategoriPanel.Children.Clear();

            var kategoriler = indirmeler
                .GroupBy(d => d.Kategori)
                .OrderByDescending(g => g.Count());

            foreach (var grup in kategoriler)
            {
                var yuzde = indirmeler.Count > 0
                    ? (double)grup.Count() / indirmeler.Count * 100
                    : 0;

                var satir = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                satir.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(80) });
                satir.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                satir.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(40) });

                // Kategori adı
                var etiket = new TextBlock
                {
                    Text = grup.Key,
                    Foreground = new SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x7A, 0x9C, 0xC0)),
                    FontFamily = _monoFont,
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(etiket, 0);

                // Progress bar arka plan
                var progresArka = new Border
                {
                    Background = new SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x0A, 0x0F, 0x1E)),
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(8, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(progresArka, 1);

                // Progress bar ön
                var progresOn = new Border
                {
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var gradient = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(1, 0)
                };
                gradient.GradientStops.Add(new GradientStop(
                    System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5), 0));
                gradient.GradientStops.Add(new GradientStop(
                    System.Windows.Media.Color.FromRgb(0xBD, 0x00, 0xFF), 1));
                progresOn.Background = gradient;

                var capturedYuzde = yuzde;
                progresArka.Loaded += (s, e) =>
                {
                    progresOn.Width = progresArka.ActualWidth * capturedYuzde / 100;
                };

                progresArka.Child = progresOn;

                // Sayı
                var sayi = new TextBlock
                {
                    Text = grup.Count().ToString(),
                    Foreground = new SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x00, 0xFF, 0xE5)),
                    FontFamily = _monoFont,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(sayi, 2);

                satir.Children.Add(etiket);
                satir.Children.Add(progresArka);
                satir.Children.Add(sayi);
                KategoriPanel.Children.Add(satir);
            }
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private static string ByteFormatla(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] ekler = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double boyut = bytes;
            while (boyut >= 1024 && i < ekler.Length - 1) { boyut /= 1024; i++; }
            return $"{boyut:F1} {ekler[i]}";
        }
    }
}