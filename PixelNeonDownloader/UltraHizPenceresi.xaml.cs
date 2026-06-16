using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

// Belirsizlik hatasını (CS0104) çözmek için WPF kütüphaneleri açıkça önceliklendirildi
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace PixelNeonDownloader
{
    public class UltraHizAyarlari
    {
        public bool UltraMod { get; set; } = false;
        public int ParcaSayisi { get; set; } = 16;
        public int BufferSeviye { get; set; } = 4;

        private static readonly string _yol = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "ultrahiz.json");

        public static UltraHizAyarlari Yukle()
        {
            try
            {
                if (File.Exists(_yol))
                    return JsonSerializer.Deserialize<UltraHizAyarlari>(
                        File.ReadAllText(_yol))
                        ?? new UltraHizAyarlari();
            }
            catch { }
            return new UltraHizAyarlari();
        }

        public void Kaydet()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_yol)!);
                File.WriteAllText(_yol,
                    JsonSerializer.Serialize(this,
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static int BufferBoyutuHesapla(int seviye) => seviye switch
        {
            1 => 65536,
            2 => 262144,
            3 => 524288,
            4 => 1048576,
            5 => 1572864,
            6 => 2097152,
            7 => 3145728,
            8 => 4194304,
            _ => 1048576
        };

        public static string BufferMetni(int seviye) => seviye switch
        {
            1 => "64 KB",
            2 => "256 KB",
            3 => "512 KB",
            4 => "1 MB",
            5 => "1.5 MB",
            6 => "2 MB",
            7 => "3 MB",
            8 => "4 MB",
            _ => "1 MB"
        };
    }

    public partial class UltraHizPenceresi : Window
    {
        private bool _ultraMod;
        private int _parcaSayisi;
        private int _bufferSeviye;

        public UltraHizPenceresi()
        {
            InitializeComponent();

            var ayarlar = UltraHizAyarlari.Yukle();
            _ultraMod = ayarlar.UltraMod;
            _parcaSayisi = ayarlar.ParcaSayisi;
            _bufferSeviye = ayarlar.BufferSeviye;

            SliderParca.Value = _parcaSayisi;
            SliderBuffer.Value = _bufferSeviye;

            ToggleUygula(_ultraMod);
            ParcaButonlariniVurgula(_parcaSayisi);
            BufferButonlariniVurgula(_bufferSeviye);
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void ToggleUltra_Click(object sender, MouseButtonEventArgs e)
        {
            _ultraMod = !_ultraMod;
            ToggleUygula(_ultraMod);

            if (_ultraMod)
            {
                SliderParca.Value = 32;
                SliderBuffer.Value = 8;
                TxtDurum.Text = "⚡ Ultra mod aktif — maksimum hız!";
            }
            else
            {
                SliderParca.Value = 16;
                SliderBuffer.Value = 4;
                TxtDurum.Text = "Normal mod";
            }
        }

        private void ToggleUygula(bool acik)
        {
            if (acik)
            {
                ToggleUltra.Background = new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14));
                ToggleUltraDaire.Fill = new SolidColorBrush(Colors.White);
                ToggleUltraDaire.Margin = new Thickness(25, 0, 0, 0);
            }
            else
            {
                ToggleUltra.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x39, 0xFF, 0x14));
                ToggleUltraDaire.Fill = new SolidColorBrush(Color.FromRgb(0x3A, 0x50, 0x70));
                ToggleUltraDaire.Margin = new Thickness(3, 0, 0, 0);
            }
        }

        // Dinamik Parça Buton Vurgulayıcı
        private void ParcaButonlariniVurgula(int aktifParca)
        {
            var parcaButonlar = new[] {
                (BtnParca1, 1), (BtnParca4, 4),
                (BtnParca8, 8), (BtnParca16, 16), (BtnParca32, 32)
            };

            foreach (var (btn, deger) in parcaButonlar)
            {
                if (btn == null) continue;
                bool aktif = (deger == aktifParca);
                btn.BorderBrush = new SolidColorBrush(aktif ? Color.FromRgb(0x39, 0xFF, 0x14) : Color.FromArgb(0x22, 0x3A, 0x50, 0x70));
                btn.BorderThickness = new Thickness(1);
                btn.Foreground = new SolidColorBrush(aktif ? Color.FromRgb(0x39, 0xFF, 0x14) : Color.FromRgb(0x7A, 0x9C, 0xC0));
            }
        }

        // Dinamik Buffer Buton Vurgulayıcı
        private void BufferButonlariniVurgula(int aktifSeviye)
        {
            var bufferButonlar = new[] {
                (BtnBuffer64, 1), (BtnBuffer256, 2), (BtnBuffer512, 3),
                (BtnBuffer1, 4), (BtnBuffer2, 6), (BtnBuffer4, 8)
            };

            foreach (var (btn, deger) in bufferButonlar)
            {
                if (btn == null) continue;
                bool aktif = (deger == aktifSeviye);
                btn.BorderBrush = new SolidColorBrush(aktif ? Color.FromRgb(0x00, 0xFF, 0xE5) : Color.FromArgb(0x22, 0x3A, 0x50, 0x70));
                btn.BorderThickness = new Thickness(1);
                btn.Foreground = new SolidColorBrush(aktif ? Color.FromRgb(0x00, 0xFF, 0xE5) : Color.FromRgb(0x7A, 0x9C, 0xC0));
            }
        }

        private void SliderParca_Degisti(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _parcaSayisi = (int)SliderParca.Value;
            if (TxtParcaSayisi != null)
                TxtParcaSayisi.Text = $"{_parcaSayisi} parça";
            ParcaButonlariniVurgula(_parcaSayisi);
        }

        private void SliderBuffer_Degisti(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _bufferSeviye = (int)SliderBuffer.Value;
            if (TxtBufferBoyutu != null)
                TxtBufferBoyutu.Text = UltraHizAyarlari.BufferMetni(_bufferSeviye);
            BufferButonlariniVurgula(_bufferSeviye);
        }

        private void ParcaKisayol_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && int.TryParse(btn.Tag?.ToString(), out var deger))
            {
                SliderParca.Value = deger;
            }
        }

        private void BufferKisayol_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && int.TryParse(btn.Tag?.ToString(), out var deger))
            {
                SliderBuffer.Value = deger;
            }
        }

        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
            var ayarlar = new UltraHizAyarlari
            {
                UltraMod = _ultraMod,
                ParcaSayisi = _parcaSayisi,
                BufferSeviye = _bufferSeviye
            };
            ayarlar.Kaydet();

            IndirmeServisi.ParcaSayisi = _parcaSayisi;
            IndirmeServisi.BufferBoyutu = UltraHizAyarlari.BufferBoyutuHesapla(_bufferSeviye);
            IndirmeServisi.UltraMod = _ultraMod;

            if (_ultraMod)
                IndirmeServisi.MaxHiz = 0;

            System.Windows.MessageBox.Show(
                $"⚡ Ultra Hız Ayarları Kaydedildi!\n\n" +
                $"Parça Sayısı : {_parcaSayisi}x\n" +
                $"Buffer Boyutu: {UltraHizAyarlari.BufferMetni(_bufferSeviye)}\n" +
                $"Ultra Mod    : {(_ultraMod ? "✅ Aktif" : "❌ Kapalı")}\n\n" +
                $"Yeni indirmeler bu ayarlarla başlayacak!",
                "Ultra Hız",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            Close();
        }
    }
}