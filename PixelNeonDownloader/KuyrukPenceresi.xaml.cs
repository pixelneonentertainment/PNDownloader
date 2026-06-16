using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using TextTrimming = System.Windows.TextTrimming;
using Thickness = System.Windows.Thickness;
using Visibility = System.Windows.Visibility;

namespace PixelNeonDownloader
{
    public partial class KuyrukPenceresi : Window
    {
        private readonly IndirmeKuyrugu _kuyruk;
        private readonly DispatcherTimer _yenilemeTimer;

        public KuyrukPenceresi(IndirmeKuyrugu kuyruk)
        {
            InitializeComponent();
            _kuyruk = kuyruk;

            _yenilemeTimer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(1)
            };
            _yenilemeTimer.Tick += (s, e) => Yenile();
            _yenilemeTimer.Start();

            ToggleGuncelle();
            Yenile();
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            _yenilemeTimer.Stop();
            Close();
        }

        private void ToggleKuyruk_Click(object sender, MouseButtonEventArgs e)
        {
            _kuyruk.KuyrukModu = !_kuyruk.KuyrukModu;
            ToggleGuncelle();
        }

        private void ToggleGuncelle()
        {
            if (_kuyruk.KuyrukModu)
            {
                ToggleKuyruk.Background = new SolidColorBrush(
                    Color.FromRgb(0x00, 0xFF, 0xE5));
                ToggleKuyrukDaire.Fill = new SolidColorBrush(Colors.White);
                ToggleKuyrukDaire.Margin = new Thickness(25, 0, 0, 0);
            }
            else
            {
                ToggleKuyruk.Background = new SolidColorBrush(
                    Color.FromArgb(0x33, 0x00, 0xFF, 0xE5));
                ToggleKuyrukDaire.Fill = new SolidColorBrush(
                    Color.FromRgb(0x3A, 0x50, 0x70));
                ToggleKuyrukDaire.Margin = new Thickness(3, 0, 0, 0);
            }
        }

        private void Yenile()
        {
            var mevcut = _kuyruk.MevcutIndirme;
            if (mevcut != null)
            {
                PanelMevcut.Visibility = Visibility.Visible;
                TxtMevcutIndirme.Text = mevcut.DosyaAdi;
                ProgressMevcut.Value = mevcut.IlerlemeYuzde;
            }
            else
            {
                PanelMevcut.Visibility = Visibility.Collapsed;
            }

            var liste = _kuyruk.KuyrukListesi();
            TxtKuyrukSayisi.Text = $"{liste.Count} bekliyor";
            KuyrukListesi.Children.Clear();

            for (int i = 0; i < liste.Count; i++)
                KuyrukListesi.Children.Add(
                    KuyrukSatirOlustur(i + 1, liste[i]));

            if (liste.Count == 0 && mevcut == null)
                TxtDurum.Text = "Kuyruk boş";
        }

        private System.Windows.Controls.Border KuyrukSatirOlustur(
            int sira, DownloadItem item)
        {
            var border = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(0x0D, 0x14, 0x28)),
                BorderBrush = new SolidColorBrush(
                    Color.FromArgb(0x15, 0x00, 0xFF, 0xE5)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition
                { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition
                { Width = new GridLength(60) });

            var siraNo = new System.Windows.Controls.TextBlock
            {
                Text = $"#{sira}",
                Foreground = new SolidColorBrush(
                    Color.FromRgb(0x3A, 0x50, 0x70)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };

            var dosyaAdi = new System.Windows.Controls.TextBlock
            {
                Text = item.DosyaAdi,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(0xE0, 0xF0, 0xFF)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };

            var tur = new System.Windows.Controls.TextBlock
            {
                Text = item.Tur.ToString(),
                Foreground = new SolidColorBrush(
                    Color.FromRgb(0x7A, 0x9C, 0xC0)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            System.Windows.Controls.Grid.SetColumn(siraNo, 0);
            System.Windows.Controls.Grid.SetColumn(dosyaAdi, 1);
            System.Windows.Controls.Grid.SetColumn(tur, 2);

            grid.Children.Add(siraNo);
            grid.Children.Add(dosyaAdi);
            grid.Children.Add(tur);

            border.Child = grid;
            return border;
        }

        private void BtnTemizle_Click(object sender, RoutedEventArgs e)
        {
            _kuyruk.Temizle();
            Yenile();
        }
    }
}