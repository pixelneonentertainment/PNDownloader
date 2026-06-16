using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using TextTrimming = System.Windows.TextTrimming;

namespace PixelNeonDownloader
{
    public partial class RaporPenceresi : Window
    {
        private readonly ObservableCollection<DownloadItem> _indirmeler;

        public RaporPenceresi(ObservableCollection<DownloadItem> indirmeler)
        {
            InitializeComponent();
            _indirmeler = indirmeler;
            RaporOlustur();
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void RaporOlustur()
        {
            var tumIndirmeler = _indirmeler.ToList();
            var tamamlananlar = tumIndirmeler
                .Where(i => i.Durum == Durum.Tamamlandi).ToList();

            TxtToplamIndirme.Text = tumIndirmeler.Count.ToString();

            var toplamBoyut = tamamlananlar.Sum(i => i.DosyaBoyutu);
            TxtToplamBoyut.Text = BoyutFormatla(toplamBoyut);

            var basariOrani = tumIndirmeler.Count > 0
                ? (double)tamamlananlar.Count / tumIndirmeler.Count * 100
                : 0;
            TxtBasariOrani.Text = $"%{basariOrani:F0}";
            TxtBasariOrani.Foreground = new SolidColorBrush(
                basariOrani >= 80
                    ? Color.FromRgb(0x39, 0xFF, 0x14)
                    : basariOrani >= 50
                        ? Color.FromRgb(0xFF, 0xD7, 0x00)
                        : Color.FromRgb(0xFF, 0x22, 0x44));

            var hizlar = tamamlananlar
                .Where(i => i.OrtalamaHiz > 0)
                .Select(i => i.OrtalamaHiz)
                .ToList();

            if (hizlar.Count > 0)
            {
                TxtOrtalamaHiz.Text = HizFormatla(hizlar.Average());
                TxtMaksHiz.Text = HizFormatla(hizlar.Max());
            }

            var toplamSure = tamamlananlar
                .Where(i => i.IndirmeSuresi.TotalSeconds > 0)
                .Sum(i => i.IndirmeSuresi.TotalSeconds);
            TxtToplamSure.Text = SureFormatla(toplamSure);

            KategoriPaneli.Children.Clear();
            var kategoriler = tumIndirmeler
                .GroupBy(i => i.Kategori)
                .OrderByDescending(g => g.Count())
                .ToList();

            foreach (var kategori in kategoriler)
                KategoriSatirEkle(kategori.Key,
                    kategori.Count(), tumIndirmeler.Count);

            SonIndirmelerPaneli.Children.Clear();
            var sonlar = tumIndirmeler
                .OrderByDescending(i => i.Durum == Durum.Tamamlandi)
                .Take(15)
                .ToList();

            foreach (var item in sonlar)
                SonIndirmeSatirEkle(item);
        }

        private void KategoriSatirEkle(string kategori, int sayi, int toplam)
        {
            var yuzde = toplam > 0 ? (double)sayi / toplam : 0;

            var panel = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            var baslik = new System.Windows.Controls.Grid();
            baslik.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
            baslik.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition
                { Width = GridLength.Auto });

            var adText = new System.Windows.Controls.TextBlock
            {
                Text = kategori,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(0xE0, 0xF0, 0xFF)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11
            };

            var sayiText = new System.Windows.Controls.TextBlock
            {
                Text = $"{sayi} dosya (%{yuzde * 100:F0})",
                Foreground = new SolidColorBrush(
                    Color.FromRgb(0x7A, 0x9C, 0xC0)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10
            };

            System.Windows.Controls.Grid.SetColumn(adText, 0);
            System.Windows.Controls.Grid.SetColumn(sayiText, 1);
            baslik.Children.Add(adText);
            baslik.Children.Add(sayiText);

            var progArka = new System.Windows.Controls.Border
            {
                Height = 4,
                Background = new SolidColorBrush(
                    Color.FromRgb(0x0A, 0x0F, 0x1E)),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 4, 0, 0)
            };

            // Responsive esnek oran ızgarası
            var gridOran = new System.Windows.Controls.Grid();
            gridOran.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(yuzde, GridUnitType.Star) });
            gridOran.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1 - yuzde, GridUnitType.Star) });

            var progDolgu = new System.Windows.Controls.Border
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var gradient = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 0)
            };
            gradient.GradientStops.Add(new GradientStop(
                Color.FromRgb(0x00, 0xFF, 0xE5), 0));
            gradient.GradientStops.Add(new GradientStop(
                Color.FromRgb(0xBD, 0x00, 0xFF), 1));
            progDolgu.Background = gradient;

            System.Windows.Controls.Grid.SetColumn(progDolgu, 0);
            gridOran.Children.Add(progDolgu);
            progArka.Child = gridOran;

            panel.Children.Add(baslik);
            panel.Children.Add(progArka);
            KategoriPaneli.Children.Add(panel);
        }

        private void SonIndirmeSatirEkle(DownloadItem item)
        {
            var border = new System.Windows.Controls.Border
            {
                BorderBrush = new SolidColorBrush(
                    Color.FromArgb(0x15, 0x00, 0xFF, 0xE5)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition
                { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition
                { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(
                new System.Windows.Controls.ColumnDefinition
                { Width = new GridLength(80) });

            var ad = new System.Windows.Controls.TextBlock
            {
                Text = item.DosyaAdi,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(0xE0, 0xF0, 0xFF)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };

            var boyut = new System.Windows.Controls.TextBlock
            {
                Text = BoyutFormatla(item.DosyaBoyutu),
                Foreground = new SolidColorBrush(
                    Color.FromRgb(0x7A, 0x9C, 0xC0)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var hiz = new System.Windows.Controls.TextBlock
            {
                Text = item.OrtalamaHiz > 0
                    ? HizFormatla(item.OrtalamaHiz) : "-",
                Foreground = new SolidColorBrush(
                    Color.FromRgb(0x00, 0xFF, 0xE5)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var durumRenk = item.Durum switch
            {
                Durum.Tamamlandi => Color.FromRgb(0x39, 0xFF, 0x14),
                Durum.Hata => Color.FromRgb(0xFF, 0x22, 0x44),
                Durum.Indiriliyor => Color.FromRgb(0x00, 0xFF, 0xE5),
                _ => Color.FromRgb(0x7A, 0x9C, 0xC0)
            };

            var durum = new System.Windows.Controls.TextBlock
            {
                Text = item.DurumMetni,
                Foreground = new SolidColorBrush(durumRenk),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            System.Windows.Controls.Grid.SetColumn(ad, 0);
            System.Windows.Controls.Grid.SetColumn(boyut, 1);
            System.Windows.Controls.Grid.SetColumn(hiz, 2);
            System.Windows.Controls.Grid.SetColumn(durum, 3);

            grid.Children.Add(ad);
            grid.Children.Add(boyut);
            grid.Children.Add(hiz);
            grid.Children.Add(durum);
            border.Child = grid;
            SonIndirmelerPaneli.Children.Add(border);
        }

        private void BtnKopyala_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== PIXEL NEON DOWNLOADER — İNDİRME RAPORU ===");
            sb.AppendLine($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine();
            sb.AppendLine($"Toplam İndirme : {TxtToplamIndirme.Text}");
            sb.AppendLine($"Toplam Boyut   : {TxtToplamBoyut.Text}");
            sb.AppendLine($"Başarı Oranı   : {TxtBasariOrani.Text}");
            sb.AppendLine($"Ortalama Hız   : {TxtOrtalamaHiz.Text}");
            sb.AppendLine($"En Yüksek Hız  : {TxtMaksHiz.Text}");
            sb.AppendLine($"Toplam Süre    : {TxtToplamSure.Text}");
            sb.AppendLine();
            sb.AppendLine("=== DOSYALAR ===");
            foreach (var item in _indirmeler)
                sb.AppendLine(
                    $"{item.DurumMetni,-12} | {BoyutFormatla(item.DosyaBoyutu),-10} | {item.DosyaAdi}");

            System.Windows.Clipboard.SetText(sb.ToString());
            System.Windows.MessageBox.Show(
                "Rapor panoya kopyalandı! ✅",
                "Kopyalandı",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private static string BoyutFormatla(long bytes)
        {
            if (bytes <= 0) return "0 B";
            if (bytes >= 1_073_741_824)
                return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576)
                return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }

        private static string HizFormatla(double bytesPerSec)
        {
            if (bytesPerSec <= 0) return "0 B/s";
            if (bytesPerSec >= 1_073_741_824)
                return $"{bytesPerSec / 1_073_741_824:F1} GB/s";
            if (bytesPerSec >= 1_048_576)
                return $"{bytesPerSec / 1_048_576:F1} MB/s";
            if (bytesPerSec >= 1024)
                return $"{bytesPerSec / 1024:F1} KB/s";
            return $"{bytesPerSec:F0} B/s";
        }

        private static string SureFormatla(double saniye)
        {
            if (saniye <= 0) return "0 sn";
            var sure = TimeSpan.FromSeconds(saniye);
            if (sure.TotalHours >= 1)
                return $"{(int)sure.TotalHours}s {sure.Minutes}dk";
            if (sure.TotalMinutes >= 1)
                return $"{(int)sure.TotalMinutes}dk {sure.Seconds}sn";
            return $"{(int)sure.TotalSeconds}sn";
        }
    }
}