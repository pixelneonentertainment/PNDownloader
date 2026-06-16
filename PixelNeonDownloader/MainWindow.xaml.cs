using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;          // Güncelleme kontrolü için eklendi
using System.Reflection;         // Güncelleme kontrolü için eklendi
using System.Text.Json;          // Güncelleme kontrolü için eklendi
using System.Threading;
using System.Threading.Tasks;    // Güncelleme kontrolü için eklendi
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using Forms = System.Windows.Forms;
using WpfApp = System.Windows.Application;
using WpfButton = System.Windows.Controls.Button;
using WpfClipboard = System.Windows.Clipboard;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace PixelNeonDownloader
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<DownloadItem> Indirmeler { get; set; } = new();

        private readonly IndirmeServisi _servis = new();
        private readonly Dictionary<DownloadItem, CancellationTokenSource> _iptalTokenlari = new();
        private TepsiYoneticisi? _tepsi;
        private IndirmeKuyrugu? _kuyruk;

        private string _aramaMetni = "";
        private string _aktifFiltre = "Tümü";
        private string _sonPano = "";

        private readonly Queue<double> _hizGecmisi = new();
        private double _maksHiz = 0;
        private const int GRAFIK_NOKTA_SAYISI = 60;

        // Mini mod
        private bool _miniMod = false;
        private double _normalYukseklik = 760;
        private double _normalGenislik = 1100;

        // Entegrasyon servis tanımları
        private YerelEntegrasyonSunucusu? _entegrasyonSunucusu;
        private PanoIzleyicisi? _panoIzleyicisi;

        public MainWindow()
        {
            InitializeComponent();
            IndirmeListesi.ItemsSource = Indirmeler;
            _tepsi = new TepsiYoneticisi(this);
            this.Loaded += (s, e) => DilYoneticisi.PencereyiCevir(this);

            // Yerel entegrasyon sunucusu başlatılıyor
            _entegrasyonSunucusu = new YerelEntegrasyonSunucusu(LinkYakalandiGiris);
            _entegrasyonSunucusu.Baslat();

            // Pano izleyicisi lambda uyuşmazlığı (CS1503) asenkron boş referer aktarımıyla çözüldü
            _panoIzleyicisi = new PanoIzleyicisi(url => LinkYakalandiGiris(url, ""));

            _kuyruk = new IndirmeKuyrugu(_servis, Indirmeler, _iptalTokenlari);
            _kuyruk.DurumDegisti += mesaj =>
                Dispatcher.Invoke(() => TxtDurum.Text = mesaj);
            _kuyruk.IndirmeTamamlandi += item => Dispatcher.Invoke(() =>
            {
                ListeKaydedici.Kaydet(Indirmeler);
                SesYoneticisi.TamamlandiSesi();
                _tepsi?.BildirimGoster(
                    "✅ İndirme Tamamlandı",
                    $"{item.DosyaAdi} başarıyla indirildi!",
                    Forms.ToolTipIcon.Info);
            });

            // Arka planda otomatik güncelleme kontrolü başlatılıyor (1.5 sn gecikmeli)
            Loaded += async (s, e) =>
            {
                await System.Threading.Tasks.Task.Delay(1500);
                await UpdateManager.CheckForUpdatesAsync(showNoUpdateMessage: false);
            };

            var hizTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            hizTimer.Tick += HizGuncelle;
            hizTimer.Start();

            var panoTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            panoTimer.Tick += PanoKontrol;
            panoTimer.Start();

            var kaydedilenler = ListeKaydedici.Yukle();
            foreach (var item in kaydedilenler)
                Indirmeler.Add(item);

            if (!string.IsNullOrEmpty(App.GelenkLink))
            {
                try
                {
                    var url = App.GelenkLink ?? "";
                    if (!string.IsNullOrEmpty(url) &&
                        (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                         url.StartsWith("ftp", StringComparison.OrdinalIgnoreCase) ||
                         url.StartsWith("magnet", StringComparison.OrdinalIgnoreCase)))
                    {
                        string dosyaAdi;
                        try
                        {
                            dosyaAdi = Path.GetFileName(new Uri(url).LocalPath);
                            if (string.IsNullOrEmpty(dosyaAdi))
                                dosyaAdi = "indirilen_dosya";
                        }
                        catch { dosyaAdi = "indirilen_dosya"; }

                        var item = new DownloadItem
                        {
                            Url = url,
                            DosyaAdi = dosyaAdi,
                            KayitYolu = Path.Combine(
                                Environment.GetFolderPath(
                                    Environment.SpecialFolder.UserProfile),
                                "Downloads", "PixelNeon"),
                            Tur = IndirmeTuru.HTTP,
                            Kategori = "Genel",
                            Durum = Durum.Bekliyor
                        };
                        Indirmeler.Add(item);
                        var cts = new CancellationTokenSource();
                        _iptalTokenlari[item] = cts;
                        _ = IndirVeBildir(item, cts.Token);
                        ListeKaydedici.Kaydet(Indirmeler);
                        TxtDurum.Text = $"Tarayıcıdan eklendi: {item.DosyaAdi}";
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(App.GelenkTorrent))
            {
                Loaded += async (s, e) =>
                {
                    await System.Threading.Tasks.Task.Delay(500);
                    var torrentPencere = new TorrentPenceresi(App.GelenkTorrent)
                    {
                        Owner = this
                    };
                    torrentPencere.ShowDialog();

                    if (torrentPencere.Sonuc != null)
                    {
                        var item = torrentPencere.Sonuc;
                        Indirmeler.Add(item);
                        SesYoneticisi.EklenmeSesi();
                        var cts = new CancellationTokenSource();
                        _iptalTokenlari[item] = cts;
                        _ = IndirVeBildir(item, cts.Token);
                        ListeKaydedici.Kaydet(Indirmeler);
                        TxtDurum.Text = $"Torrent eklendi: {item.DosyaAdi}";
                    }
                };
            }

            Loaded += async (s, e) =>
            {
                await System.Threading.Tasks.Task.Delay(800);
                var yaridaKalanlar = IndirmeLogYoneticisi.YaridaKalanlarYukle();

                if (yaridaKalanlar.Count > 0)
                {
                    var pencere = new KurtarmaPenceresi(yaridaKalanlar)
                    {
                        Owner = this
                    };
                    pencere.ShowDialog();

                    foreach (var log in pencere.SecilenLoglar)
                    {
                        var item = new DownloadItem
                        {
                            Url = log.Url,
                            DosyaAdi = log.DosyaAdi,
                            KayitYolu = log.KayitYolu,
                            DosyaBoyutu = log.DosyaBoyutu,
                            IndirilenBytes = IndirmeLogYoneticisi
                                .KurtarilabilirBytes(log),
                            Tur = Enum.TryParse<IndirmeTuru>(
                                log.Tur, out var tur)
                                ? tur : IndirmeTuru.HTTP,
                            Kategori = log.Kategori,
                            Durum = Durum.Bekliyor
                        };

                        if (item.DosyaBoyutu > 0)
                            item.Ilerleme = (double)item.IndirilenBytes
                                / item.DosyaBoyutu;

                        Indirmeler.Add(item);
                        var cts = new CancellationTokenSource();
                        _iptalTokenlari[item] = cts;
                        _ = IndirVeBildir(item, cts.Token);
                    }
                }
            };
        }

        // ── Mini Mod ─────────────────────────────────────
        private void MiniMod_Click(object sender, RoutedEventArgs e)
            => MiniModGecis();

        private void TitleBar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            => MiniModGecis();

        public void MiniModGecis()
        {
            _miniMod = !_miniMod;

            if (_miniMod)
            {
                _normalYukseklik = Height;
                _normalGenislik = Width;

                Height = 120;
                Width = 420;
                MinHeight = 120;
                MinWidth = 420;
                ResizeMode = ResizeMode.NoResize;

                PanelAracCubugu.Visibility = Visibility.Collapsed;
                PanelFiltre.Visibility = Visibility.Collapsed;
                PanelListe.Visibility = Visibility.Collapsed;
                PanoBanner.Visibility = Visibility.Collapsed;
                PanelGrafik.Visibility = Visibility.Collapsed;

                WindowState = WindowState.Normal;
                Topmost = true;
                TxtDurum.Text = "Mini mod — çift tıkla normal moda geç";
                TxtAltBaslik.Text = "çift tıkla → normal mod";
            }
            else
            {
                Height = _normalYukseklik;
                Width = _normalGenislik;
                MinHeight = 600;
                MinWidth = 900;
                ResizeMode = ResizeMode.CanResize;

                PanelAracCubugu.Visibility = Visibility.Visible;
                PanelFiltre.Visibility = Visibility.Visible;
                PanelListe.Visibility = Visibility.Visible;
                PanelGrafik.Visibility = Visibility.Visible;

                Topmost = false;
                TxtDurum.Text = "Hazır";
                TxtAltBaslik.Text = "by Pixel Neon Entertainment";
            }
        }

        // ── Klavye Kısayolları ───────────────────────────
        protected override void OnKeyDown(WpfKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.N:
                        BtnYeniIndirme_Click(this, new RoutedEventArgs());
                        e.Handled = true; break;
                    case Key.F:
                        TxtArama.Focus(); TxtArama.SelectAll();
                        e.Handled = true; break;
                    case Key.A:
                        IndirmeListesi.SelectAll();
                        e.Handled = true; break;
                    case Key.S:
                        if (IndirmeListesi.SelectedItem is DownloadItem s1)
                        {
                            if (s1.Durum == Durum.Indiriliyor)
                                BtnDuraklat_Click(this, new RoutedEventArgs());
                            else if (s1.Durum == Durum.Duraklatildi)
                                BtnDevam_Click(this, new RoutedEventArgs());
                        }
                        e.Handled = true; break;
                    case Key.O:
                        BtnKlasor_Click(this, new RoutedEventArgs());
                        e.Handled = true; break;
                    case Key.I:
                        BtnIstatistik_Click(this, new RoutedEventArgs());
                        e.Handled = true; break;
                    case Key.OemComma:
                        BtnAyarlar_Click(this, new RoutedEventArgs());
                        e.Handled = true; break;
                    case Key.OemQuestion:
                        new KisayollarPenceresi { Owner = this }.ShowDialog();
                        e.Handled = true; break;
                    case Key.M:
                        MiniModGecis();
                        e.Handled = true; break;
                }
            }

            switch (e.Key)
            {
                case Key.Delete:
                    if (IndirmeListesi.SelectedItem != null &&
                        !TxtArama.IsFocused)
                        MenuKaldir_Click(this, new RoutedEventArgs());
                    break;
                case Key.Escape:
                    if (_miniMod) MiniModGecis();
                    else if (!string.IsNullOrEmpty(TxtArama.Text))
                    {
                        BtnAramaTemizle_Click(this, new RoutedEventArgs());
                        TxtArama.Focus();
                    }
                    break;
                case Key.F5:
                    ListeyiGuncelle();
                    TxtDurum.Text = "Liste yenilendi.";
                    break;
            }
        }

        // ── Sürükle & Bırak ─────────────────────────────
        private void Ana_DragEnter(object sender, WpfDragEventArgs e)
        {
            if (GecerliSurukleMi(e))
            {
                SurukleBirakOverlay.Visibility = Visibility.Visible;
                e.Effects = WpfDragDropEffects.Copy;
            }
            else e.Effects = WpfDragDropEffects.None;
            e.Handled = true;
        }

        private void Ana_DragOver(object sender, WpfDragEventArgs e)
        {
            e.Effects = GecerliSurukleMi(e)
                ? WpfDragDropEffects.Copy
                : WpfDragDropEffects.None;
            e.Handled = true;
        }

        private void Ana_DragLeave(object sender, WpfDragEventArgs e)
            => SurukleBirakOverlay.Visibility = Visibility.Collapsed;

        private void Ana_Drop(object sender, WpfDragEventArgs e)
        {
            SurukleBirakOverlay.Visibility = Visibility.Collapsed;
            try
            {
                if (e.Data.GetDataPresent(WpfDataFormats.FileDrop))
                {
                    var dosyalar = (string[])e.Data.GetData(WpfDataFormats.FileDrop);
                    foreach (var dosyaYolu in dosyalar)
                        DosyaEkle(dosyaYolu);
                    return;
                }

                if (e.Data.GetDataPresent(WpfDataFormats.Text))
                {
                    var metin = e.Data.GetData(WpfDataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(metin))
                    {
                        foreach (var satir in metin.Split('\n'))
                        {
                            var url = satir.Trim();
                            if (LinkGecerliMi(url)) URLEkle(url);
                        }
                    }
                    return;
                }

                if (e.Data.GetDataPresent(WpfDataFormats.Html))
                {
                    var html = e.Data.GetData(WpfDataFormats.Html) as string;
                    if (!string.IsNullOrEmpty(html))
                    {
                        var urlBul = System.Text.RegularExpressions
                            .Regex.Match(html, @"href=""([^""]+)""");
                        if (urlBul.Success &&
                            LinkGecerliMi(urlBul.Groups[1].Value))
                            URLEkle(urlBul.Groups[1].Value);
                    }
                }
            }
            catch { }
        }

        private static bool GecerliSurukleMi(WpfDragEventArgs e)
            => e.Data.GetDataPresent(WpfDataFormats.FileDrop) ||
               e.Data.GetDataPresent(WpfDataFormats.Text) ||
               e.Data.GetDataPresent(WpfDataFormats.Html);

        private void DosyaEkle(string dosyaYolu)
        {
            if (dosyaYolu.EndsWith(".torrent",
                StringComparison.OrdinalIgnoreCase))
            {
                var pencere = new TorrentPenceresi(dosyaYolu) { Owner = this };
                pencere.ShowDialog();
                if (pencere.Sonuc != null)
                {
                    Indirmeler.Add(pencere.Sonuc);
                    var tcts = new CancellationTokenSource();
                    _iptalTokenlari[pencere.Sonuc] = tcts;
                    _ = IndirVeBildir(pencere.Sonuc, tcts.Token);
                    ListeKaydedici.Kaydet(Indirmeler);
                    TxtDurum.Text = $"Torrent eklendi: {pencere.Sonuc.DosyaAdi}";
                }
                return;
            }

            var dosyaAdi = Path.GetFileName(dosyaYolu);
            var item = new DownloadItem
            {
                Url = dosyaYolu,
                DosyaAdi = dosyaAdi,
                KayitYolu = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.UserProfile),
                    "Downloads", "PixelNeon"),
                Tur = IndirmeTuru.HTTP,
                Kategori = AkilliKlasorleme.KlasorBelirle(dosyaAdi),
                Durum = Durum.Bekliyor
            };

            try
            {
                item.DosyaBoyutu = new System.IO.FileInfo(dosyaYolu).Length;
                item.IndirilenBytes = item.DosyaBoyutu;
                item.Ilerleme = 1.0;
                item.Durum = Durum.Tamamlandi;
            }
            catch { }

            Indirmeler.Add(item);
            SesYoneticisi.EklenmeSesi();
            ListeKaydedici.Kaydet(Indirmeler);
            ListeyiGuncelle();
            TxtDurum.Text = $"Sürükle-bırak ile eklendi: {dosyaAdi}";
        }

        private async void URLEkle(string url)
        {
            // Eğer sürüklenen link bir .torrent linki ise arka planda indirip Torrent Seçim ekranını açar (CS1739 hatası giderildi)
            if (url.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase) && url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                TxtDurum.Text = "Torrent dosyası analiz ediliyor...";
                var tempYol = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".torrent");
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    var bytes = await client.GetByteArrayAsync(url);
                    await System.IO.File.WriteAllBytesAsync(tempYol, bytes);

                    var tp = new TorrentPenceresi(tempYol) { Owner = this };
                    tp.ShowDialog();
                    if (tp.Sonuc != null)
                    {
                        Indirmeler.Add(tp.Sonuc);
                        var tcts = new CancellationTokenSource();
                        _iptalTokenlari[tp.Sonuc] = tcts;
                        _ = IndirVeBildir(tp.Sonuc, tcts.Token);
                        ListeKaydedici.Kaydet(Indirmeler);
                        ListeyiGuncelle();
                        TxtDurum.Text = $"Torrent eklendi: {tp.Sonuc.DosyaAdi}";
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Torrent dosyası indirilemedi:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    try { if (File.Exists(tempYol)) File.Delete(tempYol); } catch { }
                }
                return;
            }

            // Normal indirme linkleri için standart akış devam eder
            string dosyaAdi;
            try
            {
                dosyaAdi = Path.GetFileName(new Uri(url).LocalPath);
                if (string.IsNullOrEmpty(dosyaAdi)) dosyaAdi = "indirilen_dosya";
            }
            catch { dosyaAdi = "indirilen_dosya"; }

            IndirmeTuru tur = url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase) ? IndirmeTuru.Magnet
                : url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ? IndirmeTuru.FTP
                : IndirmeTuru.HTTP;

            var item = new DownloadItem
            {
                Url = url,
                DosyaAdi = dosyaAdi,
                KayitYolu = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.UserProfile),
                    "Downloads", "PixelNeon"),
                Tur = tur,
                Kategori = "Genel",
                Durum = Durum.Bekliyor
            };

            Indirmeler.Add(item);
            SesYoneticisi.EklenmeSesi();
            var cts = new CancellationTokenSource();
            _iptalTokenlari[item] = cts;
            _ = IndirVeBildir(item, cts.Token);
            ListeKaydedici.Kaydet(Indirmeler);
            ListeyiGuncelle();
            TxtDurum.Text = $"Sürükle-bırak ile eklendi: {dosyaAdi}";
        }

        // ── Pano Takibi ──────────────────────────────────
        private void PanoKontrol(object? sender, EventArgs e)
        {
            try
            {
                string panoMetni = "";
                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (WpfClipboard.ContainsText())
                            panoMetni = WpfClipboard.GetText().Trim();
                    }
                    catch { }
                });

                if (string.IsNullOrEmpty(panoMetni)) return;
                if (panoMetni == _sonPano) return;
                _sonPano = panoMetni;
                if (!LinkGecerliMi(panoMetni)) return;
                if (Indirmeler.Any(d => d.Url == panoMetni)) return;
                PanoBildirimGoster(panoMetni);
            }
            catch { }
        }

        private static bool LinkGecerliMi(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin)) return false;
            if (metin.Length > 2000) return false;
            return metin.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   metin.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   metin.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                   metin.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase) ||
                   (metin.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase)
                    && System.IO.File.Exists(metin));
        }

        private void PanoBildirimGoster(string url)
        {
            PanoBanner.Visibility = Visibility.Visible;
            string dosyaAdi;
            try
            {
                dosyaAdi = url.StartsWith("magnet:") ? "Magnet Link"
                    : Path.GetFileName(new Uri(url).LocalPath);
            }
            catch { dosyaAdi = url.Length > 50 ? url[..50] + "..." : url; }

            TxtPanoBanner.Text = $"📋 Kopyalanan link algılandı: {dosyaAdi}";

            var gizleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            gizleTimer.Tick += (s, e) =>
            {
                PanoBanner.Visibility = Visibility.Collapsed;
                gizleTimer.Stop();
            };
            gizleTimer.Start();
        }

        private void BtnPanoEkle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WpfClipboard.ContainsText())
                {
                    var url = WpfClipboard.GetText().Trim();
                    if (string.IsNullOrWhiteSpace(url)) return;

                    bool gecerli = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                   url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                   url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                                   url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase);

                    if (gecerli)
                    {
                        string dosyaAdi = "indirilen_dosya";
                        try
                        {
                            dosyaAdi = Path.GetFileName(new Uri(url).LocalPath);
                            if (string.IsNullOrEmpty(dosyaAdi)) dosyaAdi = "indirilen_dosya";
                        }
                        catch { }

                        var item = new DownloadItem
                        {
                            Url = url,
                            DosyaAdi = dosyaAdi,
                            KayitYolu = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                "Downloads", "PixelNeon"),
                            Tur = url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase) ? IndirmeTuru.Magnet : IndirmeTuru.HTTP,
                            Kategori = AkilliKlasorleme.KlasorBelirle(dosyaAdi),
                            Durum = Durum.Bekliyor
                        };

                        Indirmeler.Add(item);
                        ToastBildirimYoneticisi.BildirimGoster("Yeni İndirme", $"'{dosyaAdi}' panodan listeye eklendi. ✅");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Panodaki metin geçerli bir indirme bağlantısı (URL) değil!",
                            "Pixel Neon", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Pano boş! Lütfen önce bir indirme bağlantısı kopyalayın.",
                        "Pixel Neon", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Panodan ekleme sırasında bir hata oluştu:\n{ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUltraHiz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pano uyuşmazlığı hatasını önlemek için doğrudan Ultra Hız Ayar penceresini açar
                var pencere = new UltraHizPenceresi { Owner = this };
                pencere.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ultra Hız Ayarları penceresi açılırken bir hata oluştu:\n{ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Yakalanan linki listeye ekleyen asenkron entegrasyon metodu (CS0103 Path hatası tam adıyla çözüldü)
        private async void LinkYakalandiGiris(string url, string referrer)
        {
            try
            {
                string dosyaAdi = "indirilen_dosya";
                try
                {
                    dosyaAdi = System.IO.Path.GetFileName(new Uri(url).LocalPath);
                    if (string.IsNullOrEmpty(dosyaAdi)) dosyaAdi = "indirilen_dosya";
                }
                catch { }

                // Eğer gelen link bir .torrent linki ise arka planda indirip Torrent Seçim ekranını açar
                if (url.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase) && url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var tempYol = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".torrent");
                    try
                    {
                        using var client = new System.Net.Http.HttpClient();
                        var bytes = await client.GetByteArrayAsync(url);
                        await System.IO.File.WriteAllBytesAsync(tempYol, bytes);

                        Dispatcher.Invoke(() =>
                        {
                            // 1. Katman Güvenlik: Torrent indirmesi için de onay istenir
                            var onay = System.Windows.MessageBox.Show(
                                $"Tarayıcıdan bir Torrent bağlantısı yakalandı!\n\n" +
                                $"Dosya: {dosyaAdi}\n\n" +
                                $"Bu torrent dosyasını incelemek istiyor musunuz?",
                                "Pixel Neon - Torrent Algılandı ⟁",
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Question);

                            if (onay != System.Windows.MessageBoxResult.Yes)
                            {
                                TxtDurum.Text = "Torrent yakalama iptal edildi.";
                                return;
                            }

                            var tp = new TorrentPenceresi(tempYol) { Owner = this };
                            tp.ShowDialog();
                            if (tp.Sonuc != null)
                            {
                                Indirmeler.Add(tp.Sonuc);
                                var tcts = new CancellationTokenSource();
                                _iptalTokenlari[tp.Sonuc] = tcts;
                                _ = IndirVeBildir(tp.Sonuc, tcts.Token);
                                ListeKaydedici.Kaydet(Indirmeler);
                                ListeyiGuncelle();
                                TxtDurum.Text = $"Torrent eklendi: {tp.Sonuc.DosyaAdi}";
                            }
                        });
                    }
                    catch { }
                    finally
                    {
                        try { if (File.Exists(tempYol)) File.Delete(tempYol); } catch { }
                    }
                    return;
                }

                // Standart indirme linkleri için veri modeli hazırlanır
                var item = new DownloadItem
                {
                    Url = url,
                    Referrer = referrer,
                    DosyaAdi = dosyaAdi,
                    KayitYolu = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads", "PixelNeon"),
                    Tur = url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase) ? IndirmeTuru.Magnet : IndirmeTuru.HTTP,
                    Kategori = AkilliKlasorleme.KlasorBelirle(dosyaAdi),
                    Durum = Durum.Bekliyor
                };

                // UI iş parçacığında kullanıcıya onay sorulur (GÜVENLİK KALKANI)
                Dispatcher.Invoke(() =>
                {
                    // Kontrolü tamamen kullanıcıya veren Evet/Hayır Onay Kutusu
                    var onaySonucu = System.Windows.MessageBox.Show(
                        $"İnternetten veya panodan bir indirme algılandı!\n\n" +
                        $"Dosya Adı : {dosyaAdi}\n" +
                        $"Dosya Türü: {item.Kategori}\n" +
                        $"Bağlantı  : {url}\n\n" +
                        $"Bu dosyayı listenize ekleyip indirmeyi başlatmak istiyor musunuz?",
                        "Pixel Neon - İndirme Onayı ⚡",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    // Kullanıcı 'Hayır' derse, reklamlar ve istem dışı tetiklemeler diske hiç yazılmadan reddedilir
                    if (onaySonucu != System.Windows.MessageBoxResult.Yes)
                    {
                        TxtDurum.Text = "Yakalama kullanıcı tarafından reddedildi.";
                        return;
                    }

                    // Sadece kullanıcı 'Evet' derse listeye eklenir ve indirme başlar:
                    Indirmeler.Add(item);
                    SesYoneticisi.EklenmeSesi();

                    if (_kuyruk != null && _kuyruk.KuyrukModu)
                    {
                        _kuyruk.Ekle(item);
                    }
                    else
                    {
                        var cts = new CancellationTokenSource();
                        _iptalTokenlari[item] = cts;
                        _ = IndirVeBildir(item, cts.Token);
                    }

                    ToastBildirimYoneticisi.BildirimGoster("İndirme Başladı", $"'{dosyaAdi}' başarıyla listeye eklendi ve başlatıldı. ✅");
                });
            }
            catch { }
        }


        // Pencere tamamen kapatılırken arka plan sunucularını ve dinleyicilerini durdurur
        protected override void OnClosed(EventArgs e)
        {
            _entegrasyonSunucusu?.Durdur();
            _panoIzleyicisi?.Durdur();
            base.OnClosed(e);
        }

        private void BtnPanoKapat_Click(object sender, RoutedEventArgs e)
            => PanoBanner.Visibility = Visibility.Collapsed;

        // ── Arama & Filtre ───────────────────────────────
        private void TxtArama_Degisti(object sender, TextChangedEventArgs e)
        {
            _aramaMetni = TxtArama.Text.Trim().ToLowerInvariant();
            BtnAramaTemizle.Visibility = string.IsNullOrEmpty(_aramaMetni)
                ? Visibility.Collapsed : Visibility.Visible;
            ListeyiGuncelle();
        }

        private void BtnAramaTemizle_Click(object sender, RoutedEventArgs e)
        {
            TxtArama.Text = "";
            _aramaMetni = "";
            BtnAramaTemizle.Visibility = Visibility.Collapsed;
            ListeyiGuncelle();
        }

        private void BtnFiltre_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfButton btn) return;
            _aktifFiltre = btn.Tag?.ToString() ?? "Tümü";

            var panel = btn.Parent as StackPanel;
            if (panel != null)
            {
                foreach (var child in panel.Children)
                {
                    if (child is WpfButton b)
                    {
                        b.Foreground = new SolidColorBrush(
                            Color.FromRgb(0x7A, 0x9C, 0xC0));
                        b.BorderBrush = new SolidColorBrush(
                            Color.FromArgb(0x22, 0x3A, 0x50, 0x70));
                    }
                }
            }

            btn.Foreground = new SolidColorBrush(
                Color.FromRgb(0x00, 0xFF, 0xE5));
            btn.BorderBrush = new SolidColorBrush(
                Color.FromRgb(0x00, 0xFF, 0xE5));

            ListeyiGuncelle();
        }

        private void ListeyiGuncelle()
        {
            var filtrelenmis = Indirmeler.AsEnumerable();

            if (_aktifFiltre != "Tümü")
            {
                if (Enum.TryParse<Durum>(_aktifFiltre, out var durum))
                    filtrelenmis = filtrelenmis.Where(d => d.Durum == durum);
            }

            if (!string.IsNullOrEmpty(_aramaMetni))
            {
                filtrelenmis = filtrelenmis.Where(d =>
                    d.DosyaAdi.ToLowerInvariant().Contains(_aramaMetni) ||
                    d.Url.ToLowerInvariant().Contains(_aramaMetni) ||
                    d.Kategori.ToLowerInvariant().Contains(_aramaMetni));
            }

            var sonuc = filtrelenmis.ToList();

            if (!string.IsNullOrEmpty(_aramaMetni) || _aktifFiltre != "Tümü")
            {
                IndirmeListesi.ItemsSource = sonuc;
                TxtSonucSayisi.Text = $"{sonuc.Count} sonuç";
                TxtSonucSayisi.Visibility = Visibility.Visible;
            }
            else
            {
                IndirmeListesi.ItemsSource = Indirmeler;
                TxtSonucSayisi.Visibility = Visibility.Collapsed;
            }
        }

        // ── Hız Göstergesi + Grafik ──────────────────────
        private void HizGuncelle(object? sender, EventArgs e)
        {
            double toplamIndirme = 0;
            int aktifSayi = 0;

            foreach (var item in Indirmeler)
            {
                if (item.Durum == Durum.Indiriliyor)
                {
                    toplamIndirme += item.Hiz;
                    aktifSayi++;
                }
            }

            TxtIndirmeHiz.Text = HizFormatla(toplamIndirme);
            TxtYuklemeHiz.Text = "0 B/s";
            TxtAktifSayi.Text = $"{aktifSayi} aktif";

            if (!_miniMod)
            {
                TxtDurum.Text = aktifSayi > 0
                    ? $"{aktifSayi} indirme aktif — ↓ {HizFormatla(toplamIndirme)}"
                    : "Hazır";
            }

            _hizGecmisi.Enqueue(toplamIndirme);
            if (_hizGecmisi.Count > GRAFIK_NOKTA_SAYISI)
                _hizGecmisi.Dequeue();

            if (toplamIndirme > _maksHiz) _maksHiz = toplamIndirme;

            TxtGrafikHiz.Text = HizFormatla(toplamIndirme);
            TxtGrafikMaks.Text = HizFormatla(_maksHiz);

            if (!_miniMod) HizGrafiginiCiz();

            foreach (var item in Indirmeler.ToList())
            {
                if (item.Zamanlanmis &&
                    item.ZamanlanmisBaslangic.HasValue &&
                    item.Durum == Durum.Bekliyor &&
                    DateTime.Now >= item.ZamanlanmisBaslangic.Value)
                {
                    item.Zamanlanmis = false;
                    var cts = new CancellationTokenSource();
                    _iptalTokenlari[item] = cts;
                    _ = IndirVeBildir(item, cts.Token);
                    TxtDurum.Text =
                        $"⏱ Zamanlanmış indirme başladı: {item.DosyaAdi}";
                    SesYoneticisi.EklenmeSesi();
                }
            }
        }

        private void HizGrafiginiCiz()
        {
            HizGrafik.Children.Clear();
            var noktalar = _hizGecmisi.ToArray();
            if (noktalar.Length < 2) return;

            var genislik = HizGrafik.ActualWidth;
            var yukseklik = HizGrafik.ActualHeight;
            if (genislik <= 0 || yukseklik <= 0) return;

            var maks = _maksHiz > 0 ? _maksHiz : 1;
            var adim = genislik / (GRAFIK_NOKTA_SAYISI - 1);
            var anaRenk = TemaYoneticisi.AnaRenkColor;

            var polygon = new System.Windows.Shapes.Polygon();
            var noktaListesi = new PointCollection();
            noktaListesi.Add(new System.Windows.Point(0, yukseklik));

            for (int i = 0; i < noktalar.Length; i++)
            {
                var x = (GRAFIK_NOKTA_SAYISI - noktalar.Length + i) * adim;
                var y = yukseklik - (noktalar[i] / maks * yukseklik * 0.85);
                noktaListesi.Add(new System.Windows.Point(x, y));
            }

            noktaListesi.Add(new System.Windows.Point(
                (GRAFIK_NOKTA_SAYISI - 1) * adim, yukseklik));
            polygon.Points = noktaListesi;

            var dolguGradient = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(0, 1)
            };
            dolguGradient.GradientStops.Add(new GradientStop(
                Color.FromArgb(0x55, anaRenk.R, anaRenk.G, anaRenk.B), 0));
            dolguGradient.GradientStops.Add(new GradientStop(
                Color.FromArgb(0x00, anaRenk.R, anaRenk.G, anaRenk.B), 1));
            polygon.Fill = dolguGradient;
            polygon.StrokeThickness = 0;
            HizGrafik.Children.Add(polygon);

            for (int i = 1; i < noktalar.Length; i++)
            {
                var x1 = (GRAFIK_NOKTA_SAYISI - noktalar.Length + i - 1) * adim;
                var y1 = yukseklik - (noktalar[i - 1] / maks * yukseklik * 0.85);
                var x2 = (GRAFIK_NOKTA_SAYISI - noktalar.Length + i) * adim;
                var y2 = yukseklik - (noktalar[i] / maks * yukseklik * 0.85);

                var cizgi = new System.Windows.Shapes.Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    StrokeThickness = 1.5,
                    Stroke = new SolidColorBrush(anaRenk)
                };
                HizGrafik.Children.Add(cizgi);
            }

            if (noktalar.Length > 0)
            {
                var sonX = (GRAFIK_NOKTA_SAYISI - 1) * adim;
                var sonY = yukseklik - (noktalar[^1] / maks * yukseklik * 0.85);
                var nokta = new System.Windows.Shapes.Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(anaRenk)
                };
                System.Windows.Controls.Canvas.SetLeft(nokta, sonX - 3);
                System.Windows.Controls.Canvas.SetTop(nokta, sonY - 3);
                HizGrafik.Children.Add(nokta);
            }
        }

        public void TemaGuncelle()
        {
            var tema = TemaYoneticisi.MevcutTema;
            var anaRenk = TemaYoneticisi.RenkCevir(tema.AnaRenk);
            var ikinciRenk = TemaYoneticisi.RenkCevir(tema.IkinciRenk);
            var vurguRenk = TemaYoneticisi.RenkCevir(tema.VurguRenk);

            TxtIndirmeHiz.Foreground = new SolidColorBrush(anaRenk);
            TxtYuklemeHiz.Foreground = new SolidColorBrush(ikinciRenk);
            TxtAktifSayi.Foreground = new SolidColorBrush(vurguRenk);
            TxtGrafikHiz.Foreground = new SolidColorBrush(anaRenk);
            TxtGrafikMaks.Foreground = new SolidColorBrush(ikinciRenk);
            _maksHiz = 0;
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

        // ── İndir ve Bildir ──────────────────────────────
        private async System.Threading.Tasks.Task IndirVeBildir(
            DownloadItem item, CancellationToken iptalToken)
        {
            var ayarlar = YenidenDenemeAyarlari.Yukle();

            while (true)
            {
                await _servis.IndirAsync(item, iptalToken);

                if (item.Durum == Durum.Tamamlandi)
                {
                    ListeKaydedici.Kaydet(Indirmeler);
                    SesYoneticisi.TamamlandiSesi();
                    _ = BulutEntegrasyonPenceresi.OtomatikYukle(item);
                    VeritabaniYoneticisi.KayitEkle(item);
                    break;
                }
                else if (item.Durum == Durum.Hata)
                {
                    if (iptalToken.IsCancellationRequested)
                    {
                        SesYoneticisi.HataSesi();
                        _tepsi?.BildirimGoster(
                            "❌ İndirme Hatası",
                            $"{item.DosyaAdi} indirilemedi.",
                            Forms.ToolTipIcon.Error);
                        break;
                    }

                    if (!ayarlar.Aktif ||
                        item.DenemeSayisi >= ayarlar.MaksDenemeSayisi)
                    {
                        SesYoneticisi.HataSesi();
                        _tepsi?.BildirimGoster(
                            "❌ İndirme Hatası",
                            $"{item.DosyaAdi} {item.DenemeSayisi} " +
                            $"denemede başarısız.",
                            Forms.ToolTipIcon.Error);
                        break;
                    }

                    item.DenemeSayisi++;
                    item.SonHataTarihi = DateTime.Now;

                    var bekleme = ayarlar.ArtanBekleme
                        ? ayarlar.BeklemeAraligi *
                          (int)Math.Pow(2, item.DenemeSayisi - 1)
                        : ayarlar.BeklemeAraligi;

                    item.Durum = Durum.Bekliyor;

                    for (int i = bekleme; i > 0; i--)
                    {
                        if (iptalToken.IsCancellationRequested) break;
                        Dispatcher.Invoke(() =>
                        {
                            TxtDurum.Text =
                                $"🔄 {item.DosyaAdi} — " +
                                $"{item.DenemeSayisi}/{ayarlar.MaksDenemeSayisi}. " +
                                $"deneme {i}sn sonra...";
                        });
                        await System.Threading.Tasks.Task.Delay(
                            1000, iptalToken).ContinueWith(_ => { });
                    }

                    if (iptalToken.IsCancellationRequested) break;

                    var yeniCts = new CancellationTokenSource();
                    _iptalTokenlari[item] = yeniCts;
                    iptalToken = yeniCts.Token;
                }
                else break;
            }
        }

        public void TepsidenYeniIndirme()
            => BtnYeniIndirme_Click(this, new RoutedEventArgs());

        // ── Pencere Kontrolleri ──────────────────────────
        private void TitleBar_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MiniModGecis();
                return;
            }
            if (e.ClickCount == 1) DragMove();
        }

        private void BtnGecmis_Click(object sender, RoutedEventArgs e)
        {
            var pencere = new GecmisPenceresi(Indirmeler)
            {
                Owner = this
            };
            pencere.TekrarIndirmeIstendi += item =>
            {
                Indirmeler.Add(item);
                var cts = new CancellationTokenSource();
                _iptalTokenlari[item] = cts;
                _ = IndirVeBildir(item, cts.Token);
                ListeKaydedici.Kaydet(Indirmeler);
                ListeyiGuncelle();
                TxtDurum.Text = $"Tekrar indiriliyor: {item.DosyaAdi}";
            };
            pencere.ShowDialog();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            _tepsi?.BildirimGoster(
                "Pixel Neon Downloader",
                "Uygulama arka planda çalışmaya devam ediyor.",
                Forms.ToolTipIcon.Info);
            Hide();
        }

        private void BtnBulut_Click(object sender, RoutedEventArgs e)
            => new BulutEntegrasyonPenceresi(Indirmeler) { Owner = this }.ShowDialog();

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (_miniMod) return;
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            ListeKaydedici.Kaydet(Indirmeler);
            _tepsi?.Dispose();
            WpfApp.Current.Shutdown();
        }

        protected override void OnClosing(
            System.ComponentModel.CancelEventArgs e)
        {
            // Kaydetme ve temizlik işlemleri
            ListeKaydedici.Kaydet(Indirmeler);
            _tepsi?.Dispose();
            base.OnClosing(e);
        }

        // ── Araç Çubuğu ─────────────────────────────────
        private void BtnYeniIndirme_Click(object sender, RoutedEventArgs e)
        {
            var pencere = new IndirmePenceresi { Owner = this };
            pencere.ShowDialog();

            if (pencere.Sonuc != null)
            {
                var item = pencere.Sonuc;
                Indirmeler.Add(item);
                SesYoneticisi.EklenmeSesi();
                TxtDurum.Text = $"Başlatıldı: {item.DosyaAdi}";

                if (_kuyruk != null && _kuyruk.KuyrukModu)
                    _kuyruk.Ekle(item);
                else
                {
                    var cts = new CancellationTokenSource();
                    _iptalTokenlari[item] = cts;
                    _ = IndirVeBildir(item, cts.Token);
                }

                ListeKaydedici.Kaydet(Indirmeler);
                ListeyiGuncelle();
            }
        }

        private void BtnTopluIndirme_Click(object sender, RoutedEventArgs e)
        {
            var pencere = new TopluIndirmePenceresi { Owner = this };
            pencere.ShowDialog();

            if (pencere.Sonuclar.Count > 0)
            {
                foreach (var item in pencere.Sonuclar)
                {
                    Indirmeler.Add(item);
                    SesYoneticisi.EklenmeSesi();

                    if (_kuyruk != null && _kuyruk.KuyrukModu)
                        _kuyruk.Ekle(item);
                    else
                    {
                        var cts = new CancellationTokenSource();
                        _iptalTokenlari[item] = cts;
                        _ = IndirVeBildir(item, cts.Token);
                    }
                }

                ListeKaydedici.Kaydet(Indirmeler);
                ListeyiGuncelle();
                TxtDurum.Text = $"✅ {pencere.Sonuclar.Count} indirme listeye eklendi!";
            }
        }

        private void BtnDuraklat_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                if (_iptalTokenlari.TryGetValue(secili, out var cts))
                    cts.Cancel();
                TxtDurum.Text = $"{secili.DosyaAdi} duraklatıldı.";
                ListeKaydedici.Kaydet(Indirmeler);
            }
            else TxtDurum.Text = "Lütfen bir indirme seçin.";
        }

        private void BtnDevam_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili
                && secili.Durum == Durum.Duraklatildi)
            {
                var cts = new CancellationTokenSource();
                _iptalTokenlari[secili] = cts;
                _ = IndirVeBildir(secili, cts.Token);
                TxtDurum.Text = $"{secili.DosyaAdi} devam ediyor.";
            }
            else TxtDurum.Text = "Lütfen duraklatılmış bir indirme seçin.";
        }

        private void BtnIptal_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                if (_iptalTokenlari.TryGetValue(secili, out var cts))
                {
                    cts.Cancel();
                    _iptalTokenlari.Remove(secili);
                }
                Indirmeler.Remove(secili);
                ListeKaydedici.Kaydet(Indirmeler);
                ListeyiGuncelle();
                TxtDurum.Text = "İndirme kaldırıldı.";
            }
            else TxtDurum.Text = "Lütfen bir indirme seçin.";
        }

        private void BtnKlasor_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                var yol = secili.KayitYolu;
                if (!string.IsNullOrEmpty(yol) &&
                    System.IO.Directory.Exists(yol))
                    System.Diagnostics.Process.Start("explorer.exe", yol);
                else TxtDurum.Text = "Klasör bulunamadı.";
            }
            else TxtDurum.Text = "Lütfen bir indirme seçin.";
        }

        private void BtnIstatistik_Click(object sender, RoutedEventArgs e)
            => new IstatistikPenceresi(Indirmeler) { Owner = this }.ShowDialog();

        private void BtnRapor_Click(object sender, RoutedEventArgs e)
            => new RaporPenceresi(Indirmeler) { Owner = this }.ShowDialog();

        private void BtnKuyruk_Click(object sender, RoutedEventArgs e)
        {
            if (_kuyruk == null) return;
            new KuyrukPenceresi(_kuyruk) { Owner = this }.ShowDialog();
        }

        private void BtnAyarlar_Click(object sender, RoutedEventArgs e)
            => new AyarlarPenceresi(
                System.IO.Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.UserProfile),
                    "Downloads", "PixelNeon"))
            { Owner = this }.ShowDialog();

        // ── Sağ Tık Menüsü ──────────────────────────────
        private void MenuDevam_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili
                && secili.Durum == Durum.Duraklatildi)
            {
                var cts = new CancellationTokenSource();
                _iptalTokenlari[secili] = cts;
                _ = IndirVeBildir(secili, cts.Token);
                TxtDurum.Text = $"{secili.DosyaAdi} devam ediyor.";
            }
        }

        private void MenuDuraklat_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                if (_iptalTokenlari.TryGetValue(secili, out var cts))
                    cts.Cancel();
                ListeKaydedici.Kaydet(Indirmeler);
                TxtDurum.Text = $"{secili.DosyaAdi} duraklatıldı.";
            }
        }

        private void MenuKlasor_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                var yol = secili.KayitYolu;
                if (!string.IsNullOrEmpty(yol) &&
                    System.IO.Directory.Exists(yol))
                    System.Diagnostics.Process.Start("explorer.exe", yol);
                else TxtDurum.Text = "Klasör bulunamadı.";
            }
        }

        private void MenuUrlKopyala_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                WpfClipboard.SetText(secili.Url);
                TxtDurum.Text = "URL kopyalandı.";
            }
        }

        private void MenuZamanla_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                var pencere = new ZamanlaPenceresi(secili) { Owner = this };
                pencere.ShowDialog();

                if (secili.Zamanlanmis && secili.ZamanlanmisBaslangic.HasValue)
                    TxtDurum.Text =
                        $"⏱ {secili.DosyaAdi} — " +
                        $"{secili.ZamanlanmisBaslangic.Value:HH:mm}'de başlayacak";
            }
        }

        private void MenuKaynakIzle_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                if (secili.Tur == IndirmeTuru.Torrent ||
                    secili.Tur == IndirmeTuru.Magnet)
                {
                    TxtDurum.Text =
                        "Torrent/Magnet için kaynak izleme desteklenmiyor.";
                    return;
                }
                new KaynakIzlePenceresi(secili) { Owner = this }.Show();
            }
            else TxtDurum.Text = "Lütfen bir indirme seçin.";
        }

        private void MenuChecksum_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                if (secili.Durum != Durum.Tamamlandi)
                {
                    TxtDurum.Text = "Checksum için indirme tamamlanmış olmalı!";
                    return;
                }
                new ChecksumPenceresi(secili) { Owner = this }.ShowDialog();
            }
            else TxtDurum.Text = "Lütfen bir indirme seçin.";
        }

        private void MenuSifreliArsiv_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                if (secili.Durum != Durum.Tamamlandi)
                {
                    TxtDurum.Text = "Önce indirme tamamlanmalı!";
                    return;
                }

                var dosyaYolu = System.IO.Path.Combine(
                    secili.KayitYolu, secili.DosyaAdi);
                var uzanti = System.IO.Path.GetExtension(secili.DosyaAdi)
                    .ToLowerInvariant();

                if (uzanti is not (".zip" or ".rar" or ".7z" or ".tar" or ".gz"))
                {
                    TxtDurum.Text = "Bu dosya bir arşiv değil!";
                    return;
                }

                if (!System.IO.File.Exists(dosyaYolu))
                {
                    TxtDurum.Text = "Dosya bulunamadı!";
                    return;
                }

                new SifreliArsivPenceresi(dosyaYolu) { Owner = this }.ShowDialog();
            }
            else TxtDurum.Text = "Lütfen bir indirme seçin.";
        }

        private void MenuKaldir_Click(object sender, RoutedEventArgs e)
        {
            if (IndirmeListesi.SelectedItem is DownloadItem secili)
            {
                if (_iptalTokenlari.TryGetValue(secili, out var cts))
                {
                    cts.Cancel();
                    _iptalTokenlari.Remove(secili);
                }
                Indirmeler.Remove(secili);
                ListeKaydedici.Kaydet(Indirmeler);
                ListeyiGuncelle();
                TxtDurum.Text = "İndirme kaldırıldı.";
            }
        }
    }

    // ==========================================
    // ENTEGRE EDİLEN OTO GÜNCELLEME SINIFLARI
    // ==========================================

    public class UpdateInfo
    {
        public string Version { get; set; }
        public string DownloadUrl { get; set; }
        public string Changelog { get; set; }
    }

    public class UpdateManager
    {
        // GitHub'daki raw JSON dosyanızın gerçek adresi
        private const string UpdateJsonUrl = "https://raw.githubusercontent.com/pixelneonentertainment/PNDownloader/main/update.json";

        // Mevcut uygulamanın versiyonunu otomatik alır (AssemblyInfo.cs dosyasındaki sürüm)
        private static readonly string CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        /// <summary>
        /// Güncellemeleri denetler.
        /// </summary>
        /// <param name="showNoUpdateMessage">Güncelleme yoksa kullanıcıya bilgi verilsin mi?</param>
        public static async Task CheckForUpdatesAsync(bool showNoUpdateMessage = false)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    string json = await client.GetStringAsync(UpdateJsonUrl);
                    UpdateInfo updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json);

                    if (updateInfo != null)
                    {
                        Version current = new Version(CurrentVersion);
                        Version remote = new Version(updateInfo.Version);

                        if (remote > current)
                        {
                            // Belirsizlikleri gidermek için System.Windows kütüphanesini tam adıyla çağırıyoruz:
                            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                                $"Yeni bir güncelleme mevcut! (v{updateInfo.Version})\nMevcut Sürümünüz: v{CurrentVersion}\n\nYenilikler:\n{updateInfo.Changelog}\n\nŞimdi indirip kurmak istiyor musunuz?",
                                "Güncelleme Mevcut",
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Information
                            );

                            if (result == System.Windows.MessageBoxResult.Yes)
                            {
                                await DownloadAndInstallUpdateAsync(updateInfo.DownloadUrl);
                            }
                        }
                        else if (showNoUpdateMessage)
                        {
                            System.Windows.MessageBox.Show("Uygulamanız zaten güncel sürümde.", "Güncelleme Kontrolü", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (showNoUpdateMessage)
                {
                    System.Windows.MessageBox.Show($"Güncelleme denetlenirken bir hata oluştu:\n{ex.Message}", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private static async Task DownloadAndInstallUpdateAsync(string downloadUrl)
        {
            try
            {
                string tempZipPath = Path.Combine(Path.GetTempPath(), "PixelNeonUpdate.zip");

                using (HttpClient client = new HttpClient())
                {
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(tempZipPath, fileBytes);
                }

                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string updaterPath = Path.Combine(currentDir, "updater.exe");

                if (!File.Exists(updaterPath))
                {
                    System.Windows.MessageBox.Show("Hata: 'updater.exe' dosyası ana dizinde bulunamadı! Güncelleme işlemi başlatılamıyor.", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Parametreler sırasıyla: [ZipYolu] [HedefKlasör] [KapatılacakProgramınPID] [UygulamanınExeAdı]
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"\"{tempZipPath}\" \"{currentDir}\" \"{Process.GetCurrentProcess().Id}\" \"PixelNeonDownloader.exe\"",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                System.Windows.Application.Current.Shutdown(); // Çakışmayı önlemek için tam namespace ile kapatılıyor
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Dosya indirme veya yükleyiciyi başlatma sırasında hata oluştu:\n{ex.Message}", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
