using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PixelNeonDownloader
{
    public partial class ProxyPenceresi : Window
    {
        private bool _proxyAcik = false;
        private bool _kimlikAcik = false;
        private bool _sslAtla = false;

        public ProxyPenceresi()
        {
            InitializeComponent();
            MevcutAyarlariYukle();
        }

        private void MevcutAyarlariYukle()
        {
            var ayarlar = IndirmeServisi.Proxy;
            _proxyAcik = ayarlar.AktifMi;
            _kimlikAcik = ayarlar.KimlikDogrulamaMi;
            _sslAtla = ayarlar.SslAtlaMi;

            TxtAdres.Text = string.IsNullOrEmpty(ayarlar.Adres)
                ? "127.0.0.1" : ayarlar.Adres;
            TxtPort.Text = ayarlar.Port.ToString();
            TxtKullaniciAdi.Text = ayarlar.KullaniciAdi;
            TxtSifre.Password = ayarlar.Sifre;

            ToggleGuncelle(ToggleProxy, ToggleProxyDaire, _proxyAcik);
            ToggleGuncelle(ToggleKimlik, ToggleKimlikDaire, _kimlikAcik);
            ToggleGuncelle(ToggleSSL, ToggleSSLDaire, _sslAtla);

            PanelKimlik.Visibility = _kimlikAcik
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void ToggleProxy_Click(object sender, MouseButtonEventArgs e)
        {
            _proxyAcik = !_proxyAcik;
            ToggleGuncelle(ToggleProxy, ToggleProxyDaire, _proxyAcik);
        }

        private void ToggleKimlik_Click(object sender, MouseButtonEventArgs e)
        {
            _kimlikAcik = !_kimlikAcik;
            ToggleGuncelle(ToggleKimlik, ToggleKimlikDaire, _kimlikAcik);
            PanelKimlik.Visibility = _kimlikAcik
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleSSL_Click(object sender, MouseButtonEventArgs e)
        {
            _sslAtla = !_sslAtla;
            ToggleGuncelle(ToggleSSL, ToggleSSLDaire, _sslAtla);
        }

        private static void ToggleGuncelle(
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

        private async void ProxyTest_Click(object sender, RoutedEventArgs e)
        {
            TxtTestSonuc.Visibility = Visibility.Visible;
            TxtTestSonuc.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xFF, 0xD7, 0x00));
            TxtTestSonuc.Text = "🔄 Test ediliyor...";

            try
            {
                var ayarlar = AyarlariOku();
                var proxy = ayarlar.WebProxyOlustur();

                var handler = new HttpClientHandler();

                // Kullanıcı SSL'i atla dediyse bypass et, yoksa varsayılan doğrulamayı koru
                if (ayarlar.SslAtlaMi)
                {
                    handler.ServerCertificateCustomValidationCallback = (m, c, ch, e2) => true;
                }

                if (proxy != null)
                {
                    handler.UseProxy = true;
                    handler.Proxy = proxy;
                }

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var response = await client.GetAsync("https://httpbin.org/ip");

                if (response.IsSuccessStatusCode)
                {
                    TxtTestSonuc.Foreground = new SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x39, 0xFF, 0x14));
                    TxtTestSonuc.Text = "✅ Proxy çalışıyor!";
                }
                else
                {
                    TxtTestSonuc.Foreground = new SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
                    TxtTestSonuc.Text = $"❌ Bağlantı başarısız: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                TxtTestSonuc.Foreground = new SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
                TxtTestSonuc.Text = $"❌ Hata: {ex.Message}";
            }
        }

        private ProxyAyarlari AyarlariOku()
        {
            int.TryParse(TxtPort.Text, out var port);

            return new ProxyAyarlari
            {
                AktifMi = _proxyAcik,
                Adres = TxtAdres.Text.Trim(),
                Port = port > 0 ? port : 8080,
                KimlikDogrulamaMi = _kimlikAcik,
                KullaniciAdi = TxtKullaniciAdi.Text.Trim(),
                Sifre = TxtSifre.Password,
                SslAtlaMi = _sslAtla
            };
        }

        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            var ayarlar = AyarlariOku();
            IndirmeServisi.ProxyGuncelle(ayarlar);

            System.Windows.MessageBox.Show(
                $"Proxy ayarları kaydedildi!\n\n" +
                $"Durum: {(_proxyAcik ? "Aktif ✓" : "Kapalı")}\n" +
                $"Adres: {ayarlar.Adres}:{ayarlar.Port}",
                "Pixel Neon",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            Close();
        }
    }
}