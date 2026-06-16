using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SharpCompress.Archives;
using SharpCompress.Common;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace PixelNeonDownloader
{
    public partial class SifreliArsivPenceresi : Window
    {
        private readonly string _arsivYolu;
        private bool _sifreGorunur = false;

        public SifreliArsivPenceresi(string arsivYolu)
        {
            InitializeComponent();
            _arsivYolu = arsivYolu;

            TxtDosyaAdi.Text = Path.GetFileName(arsivYolu);

            try
            {
                var bilgi = new FileInfo(arsivYolu);
                TxtDosyaBilgi.Text =
                    $"{BoyutFormatla(bilgi.Length)} • " +
                    $"{Path.GetExtension(arsivYolu).ToUpper().TrimStart('.')} arşivi";
            }
            catch { }

            TxtCikartmaYolu.Text = Path.GetDirectoryName(arsivYolu) ?? "";
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void KlasorSec_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Çıkartma klasörü seç",
                InitialDirectory = TxtCikartmaYolu.Text
            };
            if (dialog.ShowDialog() == true)
                TxtCikartmaYolu.Text = dialog.FolderName;
        }

        private void BtnSifreGoster_Click(object sender, RoutedEventArgs e)
        {
            _sifreGorunur = !_sifreGorunur;

            if (_sifreGorunur)
            {
                TxtSifreGorunur.Text = TxtSifre.Password;
                TxtSifreGorunur.Visibility = Visibility.Visible;
                TxtSifre.Visibility = Visibility.Collapsed;
                BtnSifreGoster.Content = "🙈";
            }
            else
            {
                TxtSifre.Password = TxtSifreGorunur.Text;
                TxtSifre.Visibility = Visibility.Visible;
                TxtSifreGorunur.Visibility = Visibility.Collapsed;
                BtnSifreGoster.Content = "👁";
            }
        }

        private async void BtnCikart_Click(object sender, RoutedEventArgs e)
        {
            var sifre = _sifreGorunur
                ? TxtSifreGorunur.Text
                : TxtSifre.Password;

            if (string.IsNullOrEmpty(sifre))
            {
                TxtDurum.Text = "⚠ Şifre boş olamaz!";
                return;
            }

            var hedefKlasor = TxtCikartmaYolu.Text.Trim();
            if (string.IsNullOrEmpty(hedefKlasor))
            {
                TxtDurum.Text = "⚠ Çıkartma klasörü seçin!";
                return;
            }

            BtnCikart.IsEnabled = false;
            PanelIlerleme.Visibility = Visibility.Visible;
            TxtIlerleme.Text = "Şifre kontrol ediliyor...";
            TxtDurum.Text = "Çıkartılıyor...";

            try
            {
                DateTime sonGuncelleme = DateTime.MinValue;

                await Task.Run(() =>
                {
                    Directory.CreateDirectory(hedefKlasor);

                    using var arsiv = ArchiveFactory.Open(
                        _arsivYolu,
                        new SharpCompress.Readers.ReaderOptions
                        {
                            Password = sifre
                        });

                    foreach (var giris in arsiv.Entries)
                    {
                        if (!giris.IsDirectory)
                        {
                            var simdi = DateTime.Now;
                            if ((simdi - sonGuncelleme).TotalMilliseconds > 100)
                            {
                                sonGuncelleme = simdi;
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    TxtIlerleme.Text = $"Çıkartılıyor: {giris.Key}";
                                }));
                            }

                            giris.WriteToDirectory(
                                hedefKlasor,
                                new ExtractionOptions
                                {
                                    ExtractFullPath = true,
                                    Overwrite = true
                                });
                        }
                    }
                });

                TxtDurum.Text = "✅ Başarıyla çıkartıldı!";
                TxtIlerleme.Text = "Tamamlandı!";
                ProgressCikartma.IsIndeterminate = false;
                ProgressCikartma.Value = 100;

                MessageBox.Show(
                    $"Arşiv başarıyla çıkartıldı!\n\n📁 {hedefKlasor}",
                    "Tamamlandı",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                System.Diagnostics.Process.Start("explorer.exe", hedefKlasor);
                Close();
            }
            catch (Exception ex)
            {
                PanelIlerleme.Visibility = Visibility.Collapsed;
                BtnCikart.IsEnabled = true;

                if (ex.Message.Contains("password") ||
                    ex.Message.Contains("Password") ||
                    ex.Message.Contains("şifre"))
                {
                    TxtDurum.Text = "❌ Yanlış şifre!";
                    MessageBox.Show(
                        "Şifre yanlış! Lütfen tekrar deneyin.",
                        "Hatalı Şifre",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    TxtDurum.Text = $"❌ Hata: {ex.Message}";
                    MessageBox.Show(
                        $"Çıkartma hatası:\n{ex.Message}",
                        "Hata",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private static string BoyutFormatla(long bytes)
        {
            if (bytes >= 1_073_741_824)
                return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576)
                return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }
    }
}