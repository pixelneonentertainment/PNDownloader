using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using Button = System.Windows.Controls.Button;

namespace PixelNeonDownloader
{
    public partial class AyarlarPenceresi : Window
    {
        private bool _baslangicAcik = false;
        private bool _tepsiAcik = false;
        private bool _sesAcik = true;
        private bool _hizSiniriAcik = false;
        private bool _akilliKlasorAcik = true;
        private bool _dusukDiskModu = false;
        private bool _otoCikartAcik = false;
        private long _maxHizKB = 1024;
        private int _onbellekSeviye = 5;

        public string SecilenKlasor => TxtKlasor.Text;

        private static int OnbellekBoyutuHesapla(int seviye) => seviye switch
        {
            1 => 4096,
            2 => 8192,
            3 => 16384,
            4 => 32768,
            5 => 81920,
            6 => 131072,
            7 => 262144,
            8 => 524288,
            9 => 786432,
            10 => 1048576,
            _ => 81920
        };

        private static string OnbellekMetni(int seviye) => seviye switch
        {
            1 => "4 KB",
            2 => "8 KB",
            3 => "16 KB",
            4 => "32 KB",
            5 => "80 KB (Varsayılan)",
            6 => "128 KB",
            7 => "256 KB",
            8 => "512 KB",
            9 => "768 KB",
            10 => "1 MB",
            _ => "80 KB"
        };

        public AyarlarPenceresi(string mevcutKlasor)
        {
            InitializeComponent();
            TxtKlasor.Text = mevcutKlasor;
            _baslangicAcik = BaslangicYoneticisi.BaslangictaMi();
            _sesAcik = SesYoneticisi.SesAcikMi();
            _akilliKlasorAcik = IndirmeServisi.AkilliKlasorlemeAcik;
            _dusukDiskModu = IndirmeServisi.DusukDiskModu;
            this.Loaded += (s, e) => DilYoneticisi.PencereyiCevir(this);

            var mevcutBoyut = IndirmeServisi.DiskOnbellekBoyutu;
            for (int i = 1; i <= 10; i++)
            {
                if (OnbellekBoyutuHesapla(i) >= mevcutBoyut)
                {
                    _onbellekSeviye = i;
                    break;
                }
            }
            SliderOnbellek.Value = _onbellekSeviye;
            TxtOnbellekGosterge.Text = OnbellekMetni(_onbellekSeviye);

            if (IndirmeServisi.MaxHiz > 0)
            {
                _hizSiniriAcik = true;
                _maxHizKB = IndirmeServisi.MaxHiz / 1024;
                SliderHiz.Value = _maxHizKB;
                PanelHizSlider.Visibility = Visibility.Visible;
                TxtHizGosterge.Text = HizFormatla(_maxHizKB);
            }

            ToggleGuncelle();
            ToggleTepsiGuncelle();
            ToggleSesGuncelle();
            ToggleHizSinirGuncelle();
            ToggleAkilliKlasorGuncelle();
            ToggleDusukDiskGuncelle();
            ToggleOtoCikartGuncelle();

            TxtMevcutDil.Text = $"Aktif dil: {DilYoneticisi.DilAdi(DilYoneticisi.MevcutDil)}";
            foreach (var btn in new[] { BtnTR, BtnEN, BtnDE })
            {
                var aktif = btn.Tag?.ToString() == DilYoneticisi.MevcutDil;
                btn.Foreground = new SolidColorBrush(aktif ? Color.FromRgb(0x00, 0xFF, 0xE5) : Color.FromRgb(0x7A, 0x9C, 0xC0));
                btn.BorderBrush = new SolidColorBrush(aktif ? Color.FromRgb(0x00, 0xFF, 0xE5) : Color.FromArgb(0x22, 0x3A, 0x50, 0x70));
            }

            TxtAktifTema.Text = TemaYoneticisi.MevcutTemaAdi;
            AktifKartiVurgula(TemaYoneticisi.MevcutTemaAdi);
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void BtnSekme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var sekme = btn.Tag?.ToString() ?? "Genel";

            PanelGenel.Visibility = Visibility.Collapsed;
            PanelIndirme.Visibility = Visibility.Collapsed;
            PanelGorunum.Visibility = Visibility.Collapsed;
            PanelAg.Visibility = Visibility.Collapsed;
            PanelGelismis.Visibility = Visibility.Collapsed;

            foreach (var b in new[] {
                BtnSekGenel, BtnSekIndirme,
                BtnSekGorunum, BtnSekAg, BtnSekGelismis })
            {
                b.Foreground = new SolidColorBrush(Color.FromRgb(0x7A, 0x9C, 0xC0));
                b.BorderBrush = new SolidColorBrush(Colors.Transparent);
                b.FontWeight = FontWeights.Normal;
            }

            btn.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0xE5));
            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0xE5));
            btn.FontWeight = FontWeights.Bold;

            switch (sekme)
            {
                case "Genel": PanelGenel.Visibility = Visibility.Visible; break;
                case "Indirme": PanelIndirme.Visibility = Visibility.Visible; break;
                case "Gorunum": PanelGorunum.Visibility = Visibility.Visible; break;
                case "Ag": PanelAg.Visibility = Visibility.Visible; break;
                case "Gelismis": PanelGelismis.Visibility = Visibility.Visible; break;
            }
        }

        private void BtnGecmisAc_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow ana)
            {
                var pencere = new GecmisPenceresi(ana.Indirmeler) { Owner = this };
                pencere.TekrarIndirmeIstendi += item =>
                {
                    ana.Indirmeler.Add(item);
                };
                pencere.ShowDialog();
            }
        }

        private void BtnUltraHizAc_Click(object sender, RoutedEventArgs e)
            => new UltraHizPenceresi { Owner = this }.ShowDialog();

        private void BtnBulutAc_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow ana)
                new BulutEntegrasyonPenceresi(ana.Indirmeler) { Owner = this }.ShowDialog();
        }

        private void BtnFTPAc_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow ana)
            {
                var pencere = new FTPTarayiciPenceresi { Owner = this };
                pencere.ShowDialog();
                foreach (var item in pencere.IndirmeListesi)
                    ana.Indirmeler.Add(item);
            }
        }

        private void KlasorSec_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "İndirme klasörü seç",
                InitialDirectory = TxtKlasor.Text
            };
            if (dialog.ShowDialog() == true)
                TxtKlasor.Text = dialog.FolderName;
        }

        private void Toggle_Click(object sender, MouseButtonEventArgs e)
        { _baslangicAcik = !_baslangicAcik; ToggleGuncelle(); }

        private void ToggleTepsi_Click(object sender, MouseButtonEventArgs e)
        { _tepsiAcik = !_tepsiAcik; ToggleTepsiGuncelle(); }

        private void ToggleSes_Click(object sender, MouseButtonEventArgs e)
        { _sesAcik = !_sesAcik; ToggleSesGuncelle(); }

        private void ToggleHizSinir_Click(object sender, MouseButtonEventArgs e)
        {
            _hizSiniriAcik = !_hizSiniriAcik;
            PanelHizSlider.Visibility = _hizSiniriAcik ? Visibility.Visible : Visibility.Collapsed;
            TxtHizGosterge.Text = _hizSiniriAcik ? HizFormatla(_maxHizKB) : "Sınırsız";
            ToggleHizSinirGuncelle();
        }

        private void ToggleAkilliKlasor_Click(object sender, MouseButtonEventArgs e)
        { _akilliKlasorAcik = !_akilliKlasorAcik; ToggleAkilliKlasorGuncelle(); }

        private void ToggleDusukDisk_Click(object sender, MouseButtonEventArgs e)
        {
            _dusukDiskModu = !_dusukDiskModu;
            ToggleDusukDiskGuncelle();
            if (_dusukDiskModu && SliderOnbellek.Value > 3)
                SliderOnbellek.Value = 2;
        }

        private void ToggleOtoCikart_Click(object sender, MouseButtonEventArgs e)
        { _otoCikartAcik = !_otoCikartAcik; ToggleOtoCikartGuncelle(); }

        private void ToggleGuncelle()
            => ToggleUygula(ToggleBorder, ToggleDaire, _baslangicAcik);
        private void ToggleTepsiGuncelle()
            => ToggleUygula(ToggleTepsi, ToggleTepsiDaire, _tepsiAcik);
        private void ToggleSesGuncelle()
            => ToggleUygula(ToggleSes, ToggleSesDaire, _sesAcik);
        private void ToggleHizSinirGuncelle()
            => ToggleUygula(ToggleHizSinir, ToggleHizSinirDaire, _hizSiniriAcik);
        private void ToggleAkilliKlasorGuncelle()
            => ToggleUygula(ToggleAkilliKlasor, ToggleAkilliKlasorDaire, _akilliKlasorAcik);
        private void ToggleDusukDiskGuncelle()
            => ToggleUygula(ToggleDusukDisk, ToggleDusukDiskDaire, _dusukDiskModu);
        private void ToggleOtoCikartGuncelle()
            => ToggleUygula(ToggleOtoCikart, ToggleOtoCikartDaire, _otoCikartAcik);

        private static void ToggleUygula(Border border, Ellipse daire, bool acik)
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

        private void SliderHiz_Degisti(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _maxHizKB = (long)SliderHiz.Value;
            if (TxtHizDeger != null) TxtHizDeger.Text = HizFormatla(_maxHizKB);
            if (TxtHizGosterge != null) TxtHizGosterge.Text = HizFormatla(_maxHizKB);
        }

        private void HizKisayol_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && long.TryParse(btn.Tag?.ToString(), out var deger))
                SliderHiz.Value = deger;
        }

        private void SliderOnbellek_Degisti(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _onbellekSeviye = (int)SliderOnbellek.Value;
            if (TxtOnbellekGosterge != null)
                TxtOnbellekGosterge.Text = OnbellekMetni(_onbellekSeviye);
        }

        private void BtnDil_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var dil = btn.Tag?.ToString() ?? "TR";

            DilYoneticisi.DilDegistir(dil);
            TxtMevcutDil.Text = $"Aktif dil: {DilYoneticisi.DilAdi(dil)}";

            foreach (var b in new[] { BtnTR, BtnEN, BtnDE })
            {
                var aktif = b.Tag?.ToString() == dil;
                b.Foreground = new SolidColorBrush(aktif ? Color.FromRgb(0x00, 0xFF, 0xE5) : Color.FromRgb(0x7A, 0x9C, 0xC0));
                b.BorderBrush = new SolidColorBrush(aktif ? Color.FromRgb(0x00, 0xFF, 0xE5) : Color.FromArgb(0x22, 0x3A, 0x50, 0x70));
            }

            var sonuc = System.Windows.MessageBox.Show(
                $"{DilYoneticisi.DilAdi(dil)} seçildi!\n\nYeniden başlatılsın mı?",
                "Dil Değişikliği",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (sonuc == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    if (System.Windows.Application.Current.MainWindow is MainWindow ana)
                        ListeKaydedici.Kaydet(ana.Indirmeler);
                }
                catch { }

                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    System.Diagnostics.Process.Start(exePath);
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private void TemaKart_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border kart) return;
            var temaAdi = kart.Tag?.ToString() ?? "Cyan";

            TemaYoneticisi.TemaUygula(temaAdi, System.Windows.Application.Current.Resources);
            TxtAktifTema.Text = temaAdi;
            AktifKartiVurgula(temaAdi);

            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                try
                {
                    var mi = w.GetType().GetMethod("TemaGuncelle");
                    if (mi != null) mi.Invoke(w, null);
                    w.InvalidateVisual();
                }
                catch { }
            }
        }

        private void AktifKartiVurgula(string temaAdi)
        {
            var kartlar = new[]
            {
                (KartCyan, "Cyan"), (KartPink, "Pink"),
                (KartPurple, "Purple"), (KartGreen, "Green"),
                (KartOrange, "Orange"), (KartRed, "Red")
            };

            foreach (var (kart, ad) in kartlar)
            {
                if (ad == temaAdi)
                {
                    kart.BorderThickness = new Thickness(3);
                    kart.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = TemaYoneticisi.RenkCevir(TemaYoneticisi.Temalar[ad].AnaRenk),
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };
                }
                else
                {
                    kart.BorderThickness = new Thickness(2);
                    kart.Effect = null;
                }
            }
        }

        private void BtnProxyAc_Click(object sender, RoutedEventArgs e)
        {
            var pencere = new ProxyPenceresi { Owner = this };
            pencere.ShowDialog();
        }

        private void BtnKisayollarAc_Click(object sender, RoutedEventArgs e)
        {
            var pencere = new KisayollarPenceresi { Owner = this };
            pencere.ShowDialog();
        }

        private void BtnYenidenDenemeAc_Click(object sender, RoutedEventArgs e)
        {
            var pencere = new YenidenDenemePenceresi { Owner = this };
            pencere.ShowDialog();
        }

        private void BtnAIAsistanAc_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow ana)
            {
                var pencere = new AIAsistanPenceresi(ana.Indirmeler) { Owner = this };
                pencere.ShowDialog();
            }
        }

        // Yenilenen AyarlarPenceresi.xaml üzerindeki butona tıklandığında çalışacak asenkron olay tetikleyicisi
        private async void BtnGuncelleDenetle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // showNoUpdateMessage: true yaparak, güncelleme yoksa "Güncel" mesaj kutusunu tetikliyoruz.
                await UpdateManager.CheckForUpdatesAsync(showNoUpdateMessage: true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Güncelleme denetleme hatası: {ex.Message}", "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            if (_baslangicAcik) BaslangicYoneticisi.BaslangicaEkle();
            else BaslangicYoneticisi.BaslangictanKaldir();

            SesYoneticisi.SesAyariniKaydet(_sesAcik);
            IndirmeServisi.MaxHiz = _hizSiniriAcik ? _maxHizKB * 1024 : 0;
            IndirmeServisi.AkilliKlasorlemeAcik = _akilliKlasorAcik;
            IndirmeServisi.DusukDiskModu = _dusukDiskModu;
            IndirmeServisi.DiskOnbellekBoyutu = OnbellekBoyutuHesapla(_onbellekSeviye);

            System.Windows.MessageBox.Show(
                "Ayarlar kaydedildi! ✅",
                "Pixel Neon",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            Close();
        }

        private static string HizFormatla(long kb)
        {
            if (kb >= 1048576) return $"{kb / 1048576.0:F1} GB/s";
            if (kb >= 1024) return $"{kb / 1024.0:F1} MB/s";
            return $"{kb} KB/s";
        }
    }
}