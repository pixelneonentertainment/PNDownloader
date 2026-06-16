using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MonoTorrent;

namespace PixelNeonDownloader
{
    public class TorrentDosya : INotifyPropertyChanged
    {
        private bool _secili = true;

        public string Ad { get; set; } = "";
        public long Boyut { get; set; }
        public string BoyutMetni => ByteFormatla(Boyut);

        public bool Secili
        {
            get => _secili;
            set { _secili = value; Degisti(); }
        }

        private static string ByteFormatla(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] ekler = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double boyut = bytes;
            while (boyut >= 1024 && i < ekler.Length - 1) { boyut /= 1024; i++; }
            return $"{boyut:F1} {ekler[i]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Degisti([CallerMemberName] string? ad = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ad));
    }

    public partial class TorrentPenceresi : Window
    {
        public DownloadItem? Sonuc { get; private set; }

        private readonly ObservableCollection<TorrentDosya> _dosyalar = new();
        private Torrent? _torrent;
        private string _torrentYolu = "";

        public TorrentPenceresi(string torrentYolu)
        {
            InitializeComponent();
            DosyaListesi.ItemsSource = _dosyalar;
            _torrentYolu = torrentYolu;

            TxtKayitYolu.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "PixelNeon");

            TorrentYukle(torrentYolu);
        }

        private async void TorrentYukle(string yol)
        {
            try
            {
                _torrent = await Torrent.LoadAsync(yol);

                TxtTorrentAdi.Text = _torrent.Name;
                TxtToplamBoyut.Text = ByteFormatla(_torrent.Size);

                _dosyalar.Clear();

                foreach (var dosya in _torrent.Files)
                {
                    var yeniDosya = new TorrentDosya
                    {
                        Ad = dosya.Path,
                        Boyut = dosya.Length,
                        Secili = true
                    };

                    yeniDosya.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(TorrentDosya.Secili))
                        {
                            SeciliBoyutGuncelle();
                        }
                    };

                    _dosyalar.Add(yeniDosya);
                }

                SeciliBoyutGuncelle();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Torrent yüklenemedi:\n{ex.Message}",
                    "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Close();
            }
        }

        private void SeciliBoyutGuncelle()
        {
            var toplam = _dosyalar.Where(d => d.Secili).Sum(d => d.Boyut);
            TxtSeciliBoyut.Text = $"Seçili: {ByteFormatla(toplam)}";
        }

        private void TumunuSec_Click(object sender, RoutedEventArgs e)
        {
            foreach (var d in _dosyalar) d.Secili = true;
            SeciliBoyutGuncelle();
        }

        private void HicbiriniSec_Click(object sender, RoutedEventArgs e)
        {
            foreach (var d in _dosyalar) d.Secili = false;
            SeciliBoyutGuncelle();
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

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            Sonuc = null;
            Close();
        }

        private void Ekle_Click(object sender, RoutedEventArgs e)
        {
            if (_torrent == null) return;

            var kategori = (CmbKategori.SelectedItem as
                System.Windows.Controls.ComboBoxItem)
                ?.Content?.ToString() ?? "Genel";

            Sonuc = new DownloadItem
            {
                DosyaAdi = _torrent.Name,
                Url = _torrentYolu,
                KayitYolu = TxtKayitYolu.Text,
                DosyaBoyutu = _torrent.Size,
                Tur = IndirmeTuru.Torrent,
                Kategori = kategori,
                Durum = Durum.Bekliyor
            };

            Close();
        }

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