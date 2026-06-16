using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using TextTrimming = System.Windows.TextTrimming;
using Clipboard = System.Windows.Clipboard;

namespace PixelNeonDownloader
{
    public class FTPDosyaOgesi
    {
        public string Ad { get; set; } = "";
        public string TamYol { get; set; } = "";
        public bool KlasorMu { get; set; } = false;
        public long Boyut { get; set; } = 0;
        public DateTime? Tarih { get; set; }
        public string Ikon => KlasorMu ? "📁" : DosyaIkonu();
        public string IkonRengi => KlasorMu ? "#FFD700" : "#00FFE5";
        public string BoyutMetni => KlasorMu ? "<KLASÖR>" : BoyutFormatla(Boyut);
        public string TarihMetni => Tarih.HasValue ? Tarih.Value.ToString("dd.MM.yyyy HH:mm") : "";

        private string DosyaIkonu()
        {
            var uzanti = Path.GetExtension(Ad).ToLowerInvariant();
            return uzanti switch
            {
                ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
                ".mp4" or ".mkv" or ".avi" or ".mov" => "🎬",
                ".mp3" or ".flac" or ".wav" or ".aac" => "🎵",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "🖼",
                ".exe" or ".msi" or ".dmg" => "⚙",
                ".pdf" => "📄",
                ".txt" or ".md" => "📝",
                _ => "📄"
            };
        }

        private static string BoyutFormatla(long bytes)
        {
            if (bytes <= 0) return "0 B";
            if (bytes >= 1_073_741_824)
                return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576)
                return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }
    }

    public partial class FTPTarayiciPenceresi : Window
    {
        private string _mevcutYol = "/";
        private string _sunucuAdresi = "";
        private string _kullanici = "anonymous";
        private string _sifre = "";
        private int _port = 21;
        private bool _bagliMi = false;

        public List<DownloadItem> IndirmeListesi { get; private set; } = new();

        public FTPTarayiciPenceresi()
        {
            InitializeComponent();
            DosyaListesi.ItemTemplate = DosyaSatiriOlustur();
        }

        private DataTemplate DosyaSatiriOlustur()
        {
            var template = new DataTemplate(typeof(FTPDosyaOgesi));

            var gridFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Grid));
            gridFactory.SetValue(System.Windows.Controls.Grid.HeightProperty, 44.0);

            var col0 = new FrameworkElementFactory(typeof(System.Windows.Controls.ColumnDefinition));
            col0.SetValue(System.Windows.Controls.ColumnDefinition.WidthProperty, new GridLength(30));

            var col1 = new FrameworkElementFactory(typeof(System.Windows.Controls.ColumnDefinition));
            col1.SetValue(System.Windows.Controls.ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));

            var col2 = new FrameworkElementFactory(typeof(System.Windows.Controls.ColumnDefinition));
            col2.SetValue(System.Windows.Controls.ColumnDefinition.WidthProperty, new GridLength(100));

            var col3 = new FrameworkElementFactory(typeof(System.Windows.Controls.ColumnDefinition));
            col3.SetValue(System.Windows.Controls.ColumnDefinition.WidthProperty, new GridLength(140));

            gridFactory.AppendChild(col0);
            gridFactory.AppendChild(col1);
            gridFactory.AppendChild(col2);
            gridFactory.AppendChild(col3);

            var ikonFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
            ikonFactory.SetValue(System.Windows.Controls.Grid.ColumnProperty, 0);
            ikonFactory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding("Ikon"));
            ikonFactory.SetBinding(System.Windows.Controls.TextBlock.ForegroundProperty, new System.Windows.Data.Binding("IkonRengi")
            {
                Converter = new StringToColorConverter()
            });
            ikonFactory.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 18.0);
            ikonFactory.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            ikonFactory.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            var adFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
            adFactory.SetValue(System.Windows.Controls.Grid.ColumnProperty, 1);
            adFactory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding("Ad"));
            adFactory.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE0, 0xF0, 0xFF)));
            adFactory.SetValue(System.Windows.Controls.TextBlock.FontFamilyProperty, new System.Windows.Media.FontFamily("Consolas"));
            adFactory.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 12.0);
            adFactory.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            adFactory.SetValue(System.Windows.Controls.TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            adFactory.SetValue(System.Windows.Controls.TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));

            var boyutFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
            boyutFactory.SetValue(System.Windows.Controls.Grid.ColumnProperty, 2);
            boyutFactory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding("BoyutMetni"));
            boyutFactory.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7A, 0x9C, 0xC0)));
            boyutFactory.SetValue(System.Windows.Controls.TextBlock.FontFamilyProperty, new System.Windows.Media.FontFamily("Consolas"));
            boyutFactory.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 11.0);
            boyutFactory.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            boyutFactory.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            var tarihFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
            tarihFactory.SetValue(System.Windows.Controls.Grid.ColumnProperty, 3);
            tarihFactory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding("TarihMetni"));
            tarihFactory.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7A, 0x9C, 0xC0)));
            tarihFactory.SetValue(System.Windows.Controls.TextBlock.FontFamilyProperty, new System.Windows.Media.FontFamily("Consolas"));
            tarihFactory.SetValue(System.Windows.Controls.TextBlock.FontSizeProperty, 11.0);
            tarihFactory.SetValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            tarihFactory.SetValue(System.Windows.Controls.TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            gridFactory.AppendChild(ikonFactory);
            gridFactory.AppendChild(adFactory);
            gridFactory.AppendChild(boyutFactory);
            gridFactory.AppendChild(tarihFactory);

            template.VisualTree = gridFactory;
            return template;
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private async void BtnBaglan_Click(object sender, RoutedEventArgs e)
        {
            _sunucuAdresi = TxtSunucu.Text.Trim();
            _kullanici = TxtKullanici.Text.Trim();
            _sifre = TxtSifre.Password;

            if (int.TryParse(TxtPort.Text.Trim(), out var port))
                _port = port;

            if (string.IsNullOrEmpty(_sunucuAdresi))
            {
                TxtDurum.Text = "⚠ Sunucu adresi girin!";
                return;
            }

            if (!_sunucuAdresi.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                _sunucuAdresi = "ftp://" + _sunucuAdresi;

            _mevcutYol = "/";
            await KlasorYukle(_mevcutYol);
        }

        private async Task KlasorYukle(string yol)
        {
            YukleniyorOverlay.Visibility = Visibility.Visible;
            DosyaListesi.ItemsSource = null; // Eski bağlamayı temizler
            TxtDurum.Text = $"Yükleniyor: {yol}";

            try
            {
                var ogeler = await Task.Run(() =>
                    FTPListele(_sunucuAdresi, yol, _kullanici, _sifre));

                // Performans İyileştirmesi: Öğeleri döngüyle eklemek yerine direkt ItemsSource atandı.
                DosyaListesi.ItemsSource = ogeler;

                _mevcutYol = yol;
                TxtMevcutYol.Text = yol;
                _bagliMi = true;
                TxtBaglanti.Text = "● Bağlı";
                TxtBaglanti.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x39, 0xFF, 0x14));
                TxtDurum.Text = $"{ogeler.Count} öğe listelendi";
            }
            catch (Exception ex)
            {
                _bagliMi = false;
                TxtBaglanti.Text = "● Bağlı değil";
                TxtBaglanti.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
                TxtDurum.Text = $"❌ Hata: {ex.Message}";
            }
            finally
            {
                YukleniyorOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private List<FTPDosyaOgesi> FTPListele(
            string sunucu, string yol, string kullanici, string sifre)
        {
            var ogeler = new List<FTPDosyaOgesi>();
            var tamUrl = sunucu.TrimEnd('/') + yol;

#pragma warning disable SYSLIB0014
            var istek = (FtpWebRequest)WebRequest.Create(tamUrl);
#pragma warning restore SYSLIB0014
            istek.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            istek.Credentials = new NetworkCredential(kullanici, sifre);
            istek.UsePassive = true;
            istek.UseBinary = true;
            istek.KeepAlive = false;
            istek.Timeout = 10000;

            using var yanit = (FtpWebResponse)istek.GetResponse();
            using var akis = yanit.GetResponseStream();
            using var okuyucu = new StreamReader(akis);

            string? satir;
            while ((satir = okuyucu.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(satir)) continue;

                var oge = SatirCozumle(satir, sunucu, yol);
                if (oge != null) ogeler.Add(oge);
            }

            ogeler.Sort((a, b) =>
            {
                if (a.KlasorMu && !b.KlasorMu) return -1;
                if (!a.KlasorMu && b.KlasorMu) return 1;
                return string.Compare(a.Ad, b.Ad, StringComparison.OrdinalIgnoreCase);
            });

            return ogeler;
        }

        private FTPDosyaOgesi? SatirCozumle(
            string satir, string sunucu, string yol)
        {
            try
            {
                var parcalar = satir.Split(
                    new char[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parcalar.Length < 9) return null;

                var ad = string.Join(" ", parcalar, 8, parcalar.Length - 8);
                if (ad == "." || ad == "..") return null;

                var klasorMu = satir.StartsWith("d", StringComparison.OrdinalIgnoreCase);

                long boyut = 0;
                if (!klasorMu && parcalar.Length > 4)
                    long.TryParse(parcalar[4], out boyut);

                var tamYol = yol.TrimEnd('/') + "/" + ad;

                return new FTPDosyaOgesi
                {
                    Ad = ad,
                    KlasorMu = klasorMu,
                    Boyut = boyut,
                    TamYol = tamYol
                };
            }
            catch { return null; }
        }

        private async void DosyaListesi_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DosyaListesi.SelectedItem is FTPDosyaOgesi secili && secili.KlasorMu)
            {
                await KlasorYukle(secili.TamYol);
            }
        }

        private async void BtnUstKlasor_Click(object sender, RoutedEventArgs e)
        {
            if (!_bagliMi || _mevcutYol == "/") return;

            var ustYol = Path.GetDirectoryName(
                _mevcutYol.TrimEnd('/'))?.Replace('\\', '/') ?? "/";

            if (string.IsNullOrEmpty(ustYol)) ustYol = "/";
            await KlasorYukle(ustYol);
        }

        private async void BtnYenile_Click(object sender, RoutedEventArgs e)
        {
            if (!_bagliMi) return;
            await KlasorYukle(_mevcutYol);
        }

        private void BtnIndir_Click(object sender, RoutedEventArgs e)
        {
            if (DosyaListesi.SelectedItem is FTPDosyaOgesi secili && !secili.KlasorMu)
                IndirmeEkle(secili);
        }

        private void MenuIndir_Click(object sender, RoutedEventArgs e)
        {
            if (DosyaListesi.SelectedItem is FTPDosyaOgesi secili && !secili.KlasorMu)
                IndirmeEkle(secili);
        }

        private async void MenuKlasoreGir_Click(object sender, RoutedEventArgs e)
        {
            if (DosyaListesi.SelectedItem is FTPDosyaOgesi secili && secili.KlasorMu)
                await KlasorYukle(secili.TamYol);
        }

        private void MenuUrlKopyala_Click(object sender, RoutedEventArgs e)
        {
            if (DosyaListesi.SelectedItem is FTPDosyaOgesi secili)
            {
                var url = _sunucuAdresi.TrimEnd('/') + secili.TamYol;
                Clipboard.SetText(url);
                TxtDurum.Text = "URL kopyalandı!";
            }
        }

        private void IndirmeEkle(FTPDosyaOgesi oge)
        {
            var url = _sunucuAdresi.TrimEnd('/') + oge.TamYol;

            var item = new DownloadItem
            {
                Url = url,
                DosyaAdi = oge.Ad,
                KayitYolu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads", "PixelNeon"),
                Tur = IndirmeTuru.FTP,
                Kategori = AkilliKlasorleme.KlasorBelirle(oge.Ad),
                DosyaBoyutu = oge.Boyut,
                Durum = Durum.Bekliyor
            };

            IndirmeListesi.Add(item);
            TxtDurum.Text = $"✅ '{oge.Ad}' indirme listesine eklendi!";
        }

        private void DosyaListesi_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            BtnIndir.IsEnabled =
                DosyaListesi.SelectedItem is FTPDosyaOgesi secili && !secili.KlasorMu;
        }
    }

    public class StringToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            var hex = value?.ToString() ?? "#00FFE5";
            try
            {
                return new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                return new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Colors.White);
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
}