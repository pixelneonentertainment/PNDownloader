using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PixelNeonDownloader
{
    public partial class KaynakIzlePenceresi : Window
    {
        private readonly DownloadItem _item;
        private readonly List<double> _pingGecmisi = new();
        private DispatcherTimer? _otomatikYenileTimer;
        private int _basariliSayisi = 0;
        private int _toplamDeneme = 0;

        public KaynakIzlePenceresi(DownloadItem item)
        {
            InitializeComponent();
            _item = item;

            TxtDosyaAdi.Text = item.DosyaAdi;
            TxtUrl.Text = item.Url;

            // URL'den IP ve port
            try
            {
                var uri = new Uri(item.Url);
                TxtPort.Text = uri.Port > 0 ? uri.Port.ToString() : "80";
                TxtProtokol.Text = uri.Scheme.ToUpper();
            }
            catch { }

            // Otomatik yenile (30 sn)
            _otomatikYenileTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _otomatikYenileTimer.Tick += async (s, e) => await AnalizEt();
            _otomatikYenileTimer.Start();

            Loaded += async (s, e) => await AnalizEt();
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
        {
            _otomatikYenileTimer?.Stop();
            Close();
        }

        private async void BtnYenile_Click(object sender, RoutedEventArgs e)
        {
            BtnYenile.Content = "🔄  ANALİZ EDİLİYOR...";
            BtnYenile.IsEnabled = false;
            await AnalizEt();
            BtnYenile.Content = "🔄  YENİLE";
            BtnYenile.IsEnabled = true;
        }

        private async System.Threading.Tasks.Task AnalizEt()
        {
            try
            {
                var uri = new Uri(_item.Url);
                var host = uri.Host;

                // IP çözümle
                try
                {
                    var addresses = await Dns.GetHostAddressesAsync(host);
                    if (addresses.Length > 0)
                        TxtIP.Text = addresses[0].ToString();
                }
                catch { TxtIP.Text = "Çözümlenemedi"; }

                // Ping ölç
                var pingDegeri = await PingOlc(host);
                _toplamDeneme++;

                if (pingDegeri >= 0)
                {
                    _basariliSayisi++;
                    _pingGecmisi.Add(pingDegeri);
                    if (_pingGecmisi.Count > 20) _pingGecmisi.RemoveAt(0);

                    TxtPing.Text = pingDegeri.ToString("F0");
                    TxtPing.Foreground = new SolidColorBrush(PingRengiBelirle(pingDegeri));
                    PingGrafiginiCiz();
                }
                else
                {
                    TxtPing.Text = "∞";
                    TxtPing.Foreground = new SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
                }

                // Kararlılık
                var kararlilik = _toplamDeneme > 0
                    ? (double)_basariliSayisi / _toplamDeneme * 100
                    : 0;
                TxtKararlilik.Text = kararlilik.ToString("F0");
                TxtKararlilik.Foreground = new SolidColorBrush(
                    kararlilik >= 90
                        ? System.Windows.Media.Color.FromRgb(0x39, 0xFF, 0x14)
                        : kararlilik >= 70
                            ? System.Windows.Media.Color.FromRgb(0xFF, 0xD7, 0x00)
                            : System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));

                // HTTP yanıt süresi ve sunucu bilgisi
                await HttpBilgiAl(uri);

                TxtSonGuncelleme.Text =
                    $"Son güncelleme: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                TxtSonGuncelleme.Text = $"Hata: {ex.Message}";
            }
        }

        private static async System.Threading.Tasks.Task<double> PingOlc(string host)
        {
            try
            {
                using var ping = new Ping();
                var sw = Stopwatch.StartNew();
                var sonuc = await ping.SendPingAsync(host, 5000);
                sw.Stop();

                return sonuc.Status == IPStatus.Success
                    ? sonuc.RoundtripTime
                    : -1;
            }
            catch { return -1; }
        }

        private async System.Threading.Tasks.Task HttpBilgiAl(Uri uri)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        (m, c, ch, e) => true
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var sw = Stopwatch.StartNew();
                var request = new HttpRequestMessage(HttpMethod.Head, uri);
                var response = await client.SendAsync(request);
                sw.Stop();

                TxtYanitSuresi.Text = sw.ElapsedMilliseconds.ToString();
                TxtYanitSuresi.Foreground = new SolidColorBrush(
                    PingRengiBelirle(sw.ElapsedMilliseconds));

                // Sunucu
                TxtSunucu.Text = response.Headers.Server?.ToString() ?? "Bilinmiyor";

                // İçerik türü
                TxtIcerikTuru.Text =
                    response.Content.Headers.ContentType?.MediaType ?? "Bilinmiyor";

                // Çoklu parça desteği
                var cokluParca = response.Headers.AcceptRanges.Contains("bytes");
                TxtCokluParca.Text = cokluParca ? "✓ Destekleniyor" : "✗ Desteklenmiyor";
                TxtCokluParca.Foreground = new SolidColorBrush(
                    cokluParca
                        ? System.Windows.Media.Color.FromRgb(0x39, 0xFF, 0x14)
                        : System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));

                // Bağlantı durumu
                var durum = (int)response.StatusCode;
                TxtBaglantiDurumu.Text = $"{durum} {response.StatusCode}";
                TxtBaglantiDurumu.Foreground = new SolidColorBrush(
                    durum >= 200 && durum < 300
                        ? System.Windows.Media.Color.FromRgb(0x39, 0xFF, 0x14)
                        : System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
            }
            catch (Exception ex)
            {
                TxtSunucu.Text = "Bağlantı hatası";
                TxtBaglantiDurumu.Text = ex.Message.Length > 30
                    ? ex.Message[..30] + "..." : ex.Message;
                TxtBaglantiDurumu.Foreground = new SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44));
            }
        }

        private void PingGrafiginiCiz()
        {
            PingGrafik.Children.Clear();

            if (_pingGecmisi.Count < 2) return;

            var genislik = PingGrafik.ActualWidth;
            var yukseklik = PingGrafik.ActualHeight;

            if (genislik <= 0 || yukseklik <= 0) return;

            double maxPing = 0;
            foreach (var p in _pingGecmisi)
                if (p > maxPing) maxPing = p;

            if (maxPing <= 0) maxPing = 100;

            // Gradient çizgi
            for (int i = 1; i < _pingGecmisi.Count; i++)
            {
                var x1 = (i - 1) * genislik / (_pingGecmisi.Count - 1);
                var y1 = yukseklik - (_pingGecmisi[i - 1] / maxPing * yukseklik * 0.9);
                var x2 = i * genislik / (_pingGecmisi.Count - 1);
                var y2 = yukseklik - (_pingGecmisi[i] / maxPing * yukseklik * 0.9);

                var cizgi = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(
                        PingRengiBelirle(_pingGecmisi[i]))
                };
                PingGrafik.Children.Add(cizgi);

                // Nokta
                var nokta = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = new SolidColorBrush(
                        PingRengiBelirle(_pingGecmisi[i]))
                };
                System.Windows.Controls.Canvas.SetLeft(nokta, x2 - 2.5);
                System.Windows.Controls.Canvas.SetTop(nokta, y2 - 2.5);
                PingGrafik.Children.Add(nokta);
            }
        }

        private static System.Windows.Media.Color PingRengiBelirle(double ms)
        {
            if (ms < 50)
                return System.Windows.Media.Color.FromRgb(0x39, 0xFF, 0x14); // Yeşil
            if (ms < 100)
                return System.Windows.Media.Color.FromRgb(0xFF, 0xD7, 0x00); // Sarı
            if (ms < 200)
                return System.Windows.Media.Color.FromRgb(0xFF, 0x8C, 0x00); // Turuncu
            return System.Windows.Media.Color.FromRgb(0xFF, 0x22, 0x44);     // Kırmızı
        }
    }
}