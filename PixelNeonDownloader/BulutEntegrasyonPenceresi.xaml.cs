using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using Path = System.IO.Path;

namespace PixelNeonDownloader
{
    public class BulutAyarlari
    {
        public bool GDriveAktif { get; set; } = false;
        public bool GDriveOtoYukle { get; set; } = false;
        public string GDriveKlasor { get; set; } = "PixelNeon Downloads";
        public bool OneDriveAktif { get; set; } = false;
        public bool OneDriveOtoYukle { get; set; } = false;
        public string OneDriveKlasor { get; set; } = "PixelNeon Downloads";

        private static readonly string _ayarYolu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "bulut.json");

        public static BulutAyarlari Yukle()
        {
            try
            {
                if (File.Exists(_ayarYolu))
                {
                    var json = File.ReadAllText(_ayarYolu);
                    return JsonSerializer.Deserialize<BulutAyarlari>(json) ?? new BulutAyarlari();
                }
            }
            catch { }
            return new BulutAyarlari();
        }

        public void Kaydet()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ayarYolu)!);
                File.WriteAllText(_ayarYolu,
                    JsonSerializer.Serialize(this,
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }

    public partial class BulutEntegrasyonPenceresi : Window
    {
        private static readonly HttpClient _httpClient = new();
        private BulutAyarlari _ayarlar;
        private readonly List<DownloadItem> _indirmeler;

        private static readonly string _gdKeyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "gdrive.key");

        private static readonly string _odKeyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "onedrive.key");

        public BulutEntegrasyonPenceresi(
            System.Collections.ObjectModel.ObservableCollection<DownloadItem> indirmeler)
        {
            InitializeComponent();
            _indirmeler = new List<DownloadItem>(indirmeler);
            _ayarlar = BulutAyarlari.Yukle();

            TxtGDriveKlasor.Text = _ayarlar.GDriveKlasor;
            TxtOneDriveKlasor.Text = _ayarlar.OneDriveKlasor;

            ToggleUygula(ToggleGDrive, ToggleGDriveDaire, _ayarlar.GDriveAktif);
            ToggleUygula(ToggleOneDrive, ToggleOneDriveDaire, _ayarlar.OneDriveAktif);
            ToggleUygula(ToggleGDriveOto, ToggleGDriveOtoDaire, _ayarlar.GDriveOtoYukle);
            ToggleUygula(ToggleOneDriveOto, ToggleOneDriveOtoDaire, _ayarlar.OneDriveOtoYukle);

            DurumGuncelle();
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void DurumGuncelle()
        {
            TxtGDriveDurum.Text = _ayarlar.GDriveAktif ? "✅ Aktif" : "❌ Bağlı değil";
            TxtGDriveDurum.Foreground = new SolidColorBrush(
                _ayarlar.GDriveAktif ? Color.FromRgb(0x39, 0xFF, 0x14) : Color.FromRgb(0xFF, 0x22, 0x44));

            TxtOneDriveDurum.Text = _ayarlar.OneDriveAktif ? "✅ Aktif" : "❌ Bağlı değil";
            TxtOneDriveDurum.Foreground = new SolidColorBrush(
                _ayarlar.OneDriveAktif ? Color.FromRgb(0x39, 0xFF, 0x14) : Color.FromRgb(0xFF, 0x22, 0x44));
        }

        private void ToggleGDrive_Click(object sender, MouseButtonEventArgs e)
        {
            _ayarlar.GDriveAktif = !_ayarlar.GDriveAktif;
            ToggleUygula(ToggleGDrive, ToggleGDriveDaire, _ayarlar.GDriveAktif);
            _ayarlar.Kaydet();
            DurumGuncelle();
        }

        private void ToggleOneDrive_Click(object sender, MouseButtonEventArgs e)
        {
            _ayarlar.OneDriveAktif = !_ayarlar.OneDriveAktif;
            ToggleUygula(ToggleOneDrive, ToggleOneDriveDaire, _ayarlar.OneDriveAktif);
            _ayarlar.Kaydet();
            DurumGuncelle();
        }

        private void ToggleGDriveOto_Click(object sender, MouseButtonEventArgs e)
        {
            _ayarlar.GDriveOtoYukle = !_ayarlar.GDriveOtoYukle;
            ToggleUygula(ToggleGDriveOto, ToggleGDriveOtoDaire, _ayarlar.GDriveOtoYukle);
            _ayarlar.Kaydet();
            TxtDurum.Text = _ayarlar.GDriveOtoYukle ? "Google Drive otomatik yükleme açık" : "Google Drive otomatik yükleme kapalı";
        }

        private void ToggleOneDriveOto_Click(object sender, MouseButtonEventArgs e)
        {
            _ayarlar.OneDriveOtoYukle = !_ayarlar.OneDriveOtoYukle;
            ToggleUygula(ToggleOneDriveOto, ToggleOneDriveOtoDaire, _ayarlar.OneDriveOtoYukle);
            _ayarlar.Kaydet();
            TxtDurum.Text = _ayarlar.OneDriveOtoYukle ? "OneDrive otomatik yükleme açık" : "OneDrive otomatik yükleme kapalı";
        }

        private static void ToggleUygula(System.Windows.Controls.Border border, Ellipse daire, bool acik)
        {
            if (acik)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0xE5));
                daire.Fill = new SolidColorBrush(Colors.White);
                daire.Margin = new Thickness(25, 0, 0, 0);
            }
            else
            {
                border.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0xFF, 0xE5));
                daire.Fill = new SolidColorBrush(Color.FromRgb(0x3A, 0x50, 0x70));
                daire.Margin = new Thickness(3, 0, 0, 0);
            }
        }

        private void BtnGDriveKaydet_Click(object sender, RoutedEventArgs e)
        {
            var anahtar = TxtGDriveApiKey.Password.Trim();
            if (string.IsNullOrEmpty(anahtar))
            {
                TxtDurum.Text = "⚠ API anahtarı boş!";
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_gdKeyPath)!);
                File.WriteAllText(_gdKeyPath, anahtar);
                _ayarlar.GDriveKlasor = TxtGDriveKlasor.Text.Trim();
                _ayarlar.GDriveAktif = true;
                _ayarlar.Kaydet();
                ToggleUygula(ToggleGDrive, ToggleGDriveDaire, true);
                DurumGuncelle();
                TxtDurum.Text = "✅ Google Drive API anahtarı kaydedildi!";
            }
            catch (Exception ex)
            {
                TxtDurum.Text = $"❌ Hata: {ex.Message}";
            }
        }

        private async void BtnGDriveYukle_Click(object sender, RoutedEventArgs e)
        {
            var tamamlananlar = _indirmeler.FindAll(i => i.Durum == Durum.Tamamlandi);

            if (tamamlananlar.Count == 0)
            {
                TxtDurum.Text = "⚠ Tamamlanmış indirme bulunamadı!";
                return;
            }

            if (!File.Exists(_gdKeyPath))
            {
                TxtDurum.Text = "⚠ Önce Google Drive API anahtarını kaydedin!";
                return;
            }

            var apiKey = File.ReadAllText(_gdKeyPath).Trim();
            TxtDurum.Text = $"Google Drive'a yükleniyor...";

            int basarili = 0;
            foreach (var item in tamamlananlar)
            {
                var dosyaYolu = Path.Combine(item.KayitYolu, item.DosyaAdi);
                if (!File.Exists(dosyaYolu)) continue;

                var sonuc = await GDriveYukle(dosyaYolu, item.DosyaAdi, apiKey);
                if (sonuc)
                {
                    basarili++;
                    GecmisEkle($"✅ Google Drive: {item.DosyaAdi}");
                }
            }

            TxtDurum.Text = $"✅ {basarili}/{tamamlananlar.Count} dosya yüklendi!";
        }

        // Performans İyileştirmesi: RAM'i şişirmemek için byte dizisi yerine StreamContent ile doğrudan diskten okuyarak yükleme asenkronize edildi.
        private async Task<bool> GDriveYukle(string dosyaYolu, string dosyaAdi, string apiKey)
        {
            try
            {
                using var icerik = new MultipartFormDataContent();
                var metaData = JsonSerializer.Serialize(new
                {
                    name = dosyaAdi,
                    mimeType = "application/octet-stream"
                });

                icerik.Add(new StringContent(metaData, System.Text.Encoding.UTF8, "application/json"));

                // Dosyayı RAM'e almadan doğrudan diskten akıtmak için FileStream oluşturuldu
                var dosyaAkisi = new FileStream(dosyaYolu, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var dosyaIcerik = new StreamContent(dosyaAkisi);
                dosyaIcerik.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                icerik.Add(dosyaIcerik, "file", dosyaAdi);

                var url = $"https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&key={apiKey}";
                var yanit = await _httpClient.PostAsync(url, icerik);
                return yanit.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        private void BtnOneDriveKaydet_Click(object sender, RoutedEventArgs e)
        {
            var clientId = TxtOneDriveClientId.Password.Trim();
            if (string.IsNullOrEmpty(clientId))
            {
                TxtDurum.Text = "⚠ Client ID boş!";
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_odKeyPath)!);
                File.WriteAllText(_odKeyPath, clientId);
                _ayarlar.OneDriveKlasor = TxtOneDriveKlasor.Text.Trim();
                _ayarlar.OneDriveAktif = true;
                _ayarlar.Kaydet();
                ToggleUygula(ToggleOneDrive, ToggleOneDriveDaire, true);
                DurumGuncelle();
                TxtDurum.Text = "✅ OneDrive Client ID kaydedildi!";

                var authUrl =
                    $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize" +
                    $"?client_id={clientId}" +
                    $"&response_type=token" +
                    $"&redirect_uri=https://login.microsoftonline.com/common/oauth2/nativeclient" +
                    $"&scope=Files.ReadWrite";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                TxtDurum.Text = "✅ Tarayıcıda OneDrive yetkilendirme açıldı!";
            }
            catch (Exception ex)
            {
                TxtDurum.Text = $"❌ Hata: {ex.Message}";
            }
        }

        private async void BtnOneDriveYukle_Click(object sender, RoutedEventArgs e)
        {
            var tamamlananlar = _indirmeler.FindAll(i => i.Durum == Durum.Tamamlandi);

            if (tamamlananlar.Count == 0)
            {
                TxtDurum.Text = "⚠ Tamamlanmış indirme bulunamadı!";
                return;
            }

            if (!File.Exists(_odKeyPath))
            {
                TxtDurum.Text = "⚠ Önce OneDrive Client ID'yi kaydedin!";
                return;
            }

            TxtDurum.Text = "OneDrive'a yükleniyor...";

            int basarili = 0;
            foreach (var item in tamamlananlar)
            {
                var dosyaYolu = Path.Combine(item.KayitYolu, item.DosyaAdi);
                if (!File.Exists(dosyaYolu)) continue;

                await Task.Delay(100);
                GecmisEkle($"📋 OneDrive: {item.DosyaAdi} (token gerekli)");
                basarili++;
            }

            TxtDurum.Text = $"ℹ {basarili} dosya için OneDrive bağlantısı hazır. Token gerekiyor.";
        }

        private void GecmisEkle(string mesaj)
        {
            if (YuklemeGecmisiPaneli.Children.Count == 1)
            {
                var ilk = YuklemeGecmisiPaneli.Children[0] as System.Windows.Controls.TextBlock;
                if (ilk?.Text == "Henüz yükleme yapılmadı")
                    YuklemeGecmisiPaneli.Children.Clear();
            }

            var satir = new System.Windows.Controls.TextBlock
            {
                Text = $"{DateTime.Now:HH:mm} — {mesaj}",
                Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xF0, 0xFF)),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 10,
                Margin = new Thickness(0, 0, 0, 4)
            };

            YuklemeGecmisiPaneli.Children.Insert(0, satir);

            if (YuklemeGecmisiPaneli.Children.Count > 10)
                YuklemeGecmisiPaneli.Children.RemoveAt(YuklemeGecmisiPaneli.Children.Count - 1);
        }

        public static async Task OtomatikYukle(DownloadItem item)
        {
            var ayarlar = BulutAyarlari.Yukle();
            if (!ayarlar.GDriveAktif && !ayarlar.OneDriveAktif) return;

            var dosyaYolu = Path.Combine(item.KayitYolu, item.DosyaAdi);
            if (!File.Exists(dosyaYolu)) return;

            if (ayarlar.GDriveAktif && ayarlar.GDriveOtoYukle
                && File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PixelNeonDownloader", "gdrive.key")))
            {
                var apiKey = await File.ReadAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PixelNeonDownloader", "gdrive.key"));

                using var httpClient = new HttpClient();
                using var icerik = new MultipartFormDataContent();
                var metaData = JsonSerializer.Serialize(new
                {
                    name = item.DosyaAdi,
                    mimeType = "application/octet-stream"
                });
                icerik.Add(new StringContent(metaData, System.Text.Encoding.UTF8, "application/json"));

                // Performans İyileştirmesi: Büyük dosyalarda RAM tüketimini azaltmak için asenkron StreamContent entegrasyonu
                using var dosyaAkisi = new FileStream(dosyaYolu, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var dosyaIcerik = new StreamContent(dosyaAkisi);
                dosyaIcerik.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                icerik.Add(dosyaIcerik, "file", item.DosyaAdi);

                try
                {
                    await httpClient.PostAsync(
                        $"https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&key={apiKey.Trim()}",
                        icerik);
                }
                catch { }
            }
        }
    }
}