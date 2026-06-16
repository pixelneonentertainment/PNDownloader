using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;


namespace PixelNeonDownloader
{
    public class LogGorunumModel
    {
        public string Id { get; set; } = "";
        public string DosyaAdi { get; set; } = "";
        public string KurtarilabilirMetin { get; set; } = "";
        public string ToplamMetin { get; set; } = "";
        public string TarihMetni { get; set; } = "";
        public double IlerlemeYuzde { get; set; }
        public IndirmeLog Log { get; set; } = null!;
        public bool Secili { get; set; } = true;
    }

    public partial class KurtarmaPenceresi : Window
    {
        private readonly ObservableCollection<LogGorunumModel> _loglar = new();
        public List<IndirmeLog> SecilenLoglar { get; private set; } = new();

        public KurtarmaPenceresi(List<IndirmeLog> yaridaKalanlar)
        {
            InitializeComponent();
            LogListesi.ItemsSource = _loglar;

            foreach (var log in yaridaKalanlar)
            {
                var kurtarilabilir = IndirmeLogYoneticisi.KurtarilabilirBytes(log);
                var yuzde = log.DosyaBoyutu > 0
                    ? (double)kurtarilabilir / log.DosyaBoyutu * 100
                    : 0;

                _loglar.Add(new LogGorunumModel
                {
                    Id = log.Id,
                    DosyaAdi = log.DosyaAdi,
                    KurtarilabilirMetin = ByteFormatla(kurtarilabilir),
                    ToplamMetin = log.DosyaBoyutu > 0
                        ? ByteFormatla(log.DosyaBoyutu) : "? MB",
                    TarihMetni = log.SonGuncelleme.ToString("dd.MM HH:mm"),
                    IlerlemeYuzde = yuzde,
                    Log = log,
                    Secili = true
                });
            }
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            SecilenLoglar.Clear();
            Close();
        }

        private void BtnDevamEt_Click(object sender, RoutedEventArgs e)
        {
            SecilenLoglar.Clear();

            foreach (var model in _loglar)
                if (model.Secili)
                    SecilenLoglar.Add(model.Log);

            Close();
        }

        private void BtnLogSil_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn
                && btn.Tag is string id)
            {
                var silinecek = _loglar
                    .ToArray()
                    .FirstOrDefault(m => m.Id == id);

                if (silinecek != null)
                {
                    IndirmeLogYoneticisi.LogTamamla(silinecek.Log);
                    _loglar.Remove(silinecek);

                    if (_loglar.Count == 0) Close();
                }
            }
        }

        private void BtnTumunuSil_Click(object sender, RoutedEventArgs e)
        {
            IndirmeLogYoneticisi.TumLoglarTemizle();
            Close();
        }

        private static string ByteFormatla(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] ekler = { "B", "KB", "MB", "GB" };
            int i = 0;
            double boyut = bytes;
            while (boyut >= 1024 && i < ekler.Length - 1)
            { boyut /= 1024; i++; }
            return $"{boyut:F1} {ekler[i]}";
        }
    }
}