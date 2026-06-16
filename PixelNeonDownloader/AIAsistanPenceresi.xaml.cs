using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

// Belirsizliği gidermek için WPF kütüphaneleri açıkça tanımlandı
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Brushes = System.Windows.Media.Brushes;
using Cursors = System.Windows.Input.Cursors;

namespace PixelNeonDownloader
{
    public partial class AIAsistanPenceresi : Window
    {
        private readonly ObservableCollection<DownloadItem> _indirmeler;
        private AIAnalizSonucu? _sonAnalizSonucu;
        private string _sonAnalizURL = "";

        public AIAsistanPenceresi(ObservableCollection<DownloadItem> indirmeler)
        {
            InitializeComponent();
            _indirmeler = indirmeler;

            if (APIAnahtarYoneticisi.AnahtarMevcut())
                PanelAPIKey.Visibility = Visibility.Collapsed;
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void BtnAPIKeyKaydet_Click(object sender, RoutedEventArgs e)
        {
            var anahtar = TxtAPIKey.Password.Trim();
            if (string.IsNullOrEmpty(anahtar))
            {
                TxtDurum.Text = "API anahtarı boş olamaz!";
                return;
            }

            APIAnahtarYoneticisi.AnahtarKaydet(anahtar);
            PanelAPIKey.Visibility = Visibility.Collapsed;
            TxtDurum.Text = "✅ API anahtarı kaydedildi!";
        }

        private async void BtnAnalizEt_Click(object sender, RoutedEventArgs e)
        {
            var url = TxtAnalizURL.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                TxtDurum.Text = "Lütfen bir URL girin!";
                return;
            }

            if (!APIAnahtarYoneticisi.AnahtarMevcut())
            {
                PanelAPIKey.Visibility = Visibility.Visible;
                TxtDurum.Text = "Önce API anahtarını girin!";
                return;
            }

            TxtAnalizYukleniyor.Visibility = Visibility.Visible;
            AnalizSonucPaneli.Visibility = Visibility.Collapsed;
            TxtDurum.Text = "AI analiz ediyor...";

            try
            {
                var sonuc = await AIIndirmeAsistani.URLAnalizEt(url);
                _sonAnalizSonucu = sonuc;
                _sonAnalizURL = url;

                AnalizSonucuGoster(sonuc);
                TxtDurum.Text = "✅ Analiz tamamlandı!";
            }
            catch (Exception ex)
            {
                TxtDurum.Text = $"Hata: {ex.Message}";
            }
            finally
            {
                TxtAnalizYukleniyor.Visibility = Visibility.Collapsed;
            }
        }

        private void AnalizSonucuGoster(AIAnalizSonucu sonuc)
        {
            AnalizSonucPaneli.Visibility = Visibility.Visible;

            TxtSonucTur.Text = sonuc.DosyaTuru;
            TxtSonucKlasor.Text = sonuc.OnerigenKlasor;
            TxtSonucParca.Text = $"{sonuc.OnerilienParcaSayisi} parça";
            TxtSonucOneri.Text = $"💡 {sonuc.Oneri}";

            TxtSonucCikart.Text = sonuc.OtomatikCikart ? "✓ Evet" : "✗ Hayır";
            TxtSonucCikart.Foreground = new SolidColorBrush(
                sonuc.OtomatikCikart
                    ? Color.FromRgb(0x39, 0xFF, 0x14)
                    : Color.FromRgb(0x3A, 0x50, 0x70));

            TxtSonucRisk.Text = sonuc.RiskSeviyesi;
            TxtSonucRisk.Foreground = new SolidColorBrush(
                sonuc.RiskSeviyesi switch
                {
                    "Yüksek" => Color.FromRgb(0xFF, 0x22, 0x44),
                    "Orta" => Color.FromRgb(0xFF, 0xD7, 0x00),
                    _ => Color.FromRgb(0x39, 0xFF, 0x14)
                });
        }

        private void BtnAIileIndir_Click(object sender, RoutedEventArgs e)
        {
            if (_sonAnalizSonucu == null || string.IsNullOrEmpty(_sonAnalizURL))
                return;

            var anaKlasor = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "PixelNeon");

            string dosyaAdi;
            try
            {
                dosyaAdi = Path.GetFileName(new Uri(_sonAnalizURL).LocalPath);
                if (string.IsNullOrEmpty(dosyaAdi)) dosyaAdi = "indirilen_dosya";
            }
            catch { dosyaAdi = "indirilen_dosya"; }

            var item = new DownloadItem
            {
                Url = _sonAnalizURL,
                DosyaAdi = dosyaAdi,
                KayitYolu = anaKlasor,
                Kategori = _sonAnalizSonucu.OnerigenKlasor,
                Tur = _sonAnalizURL.StartsWith("magnet:") ? IndirmeTuru.Magnet
                    : _sonAnalizURL.StartsWith("ftp://") ? IndirmeTuru.FTP
                    : IndirmeTuru.HTTP,
                Durum = Durum.Bekliyor
            };

            _indirmeler.Add(item);
            TxtDurum.Text = $"✅ '{dosyaAdi}' indirme listesine eklendi!";
            TxtAnalizURL.Text = "";
            AnalizSonucPaneli.Visibility = Visibility.Collapsed;
        }

        private async void BtnTemizlikAnaliz_Click(object sender, RoutedEventArgs e)
        {
            if (!APIAnahtarYoneticisi.AnahtarMevcut())
            {
                PanelAPIKey.Visibility = Visibility.Visible;
                TxtDurum.Text = "Önce API anahtarını girin!";
                return;
            }

            var tamamlananlar = new List<DownloadItem>();
            foreach (var item in _indirmeler)
                if (item.Durum == Durum.Tamamlandi)
                    tamamlananlar.Add(item);

            if (tamamlananlar.Count == 0)
            {
                TxtDurum.Text = "Tamamlanmış indirme bulunamadı!";
                return;
            }

            BtnTemizlikAnaliz.IsEnabled = false;
            TxtTemizlikYukleniyor.Visibility = Visibility.Visible;
            TemizlikListesi.Children.Clear();
            TxtDurum.Text = "AI dosyaları analiz ediyor...";

            try
            {
                var oneriler = await AIIndirmeAsistani.TemizlikAnalizEt(tamamlananlar);

                TxtTemizlikYukleniyor.Visibility = Visibility.Collapsed;

                if (oneriler.Length == 0)
                {
                    var yok = new System.Windows.Controls.TextBlock
                    {
                        Text = "✅ Silinmesi önerilen dosya bulunamadı!",
                        Foreground = new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14)),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 11
                    };
                    TemizlikListesi.Children.Add(yok);
                    TxtDurum.Text = "✅ Diskinde her şey temiz!";
                    return;
                }

                foreach (var oneri in oneriler)
                    TemizlikOneriEkle(oneri);

                TxtDurum.Text = $"🧹 {oneriler.Length} dosya için temizlik önerisi!";
            }
            catch (Exception ex)
            {
                TxtDurum.Text = $"Hata: {ex.Message}";
                TxtTemizlikYukleniyor.Visibility = Visibility.Collapsed;
            }
            finally
            {
                BtnTemizlikAnaliz.IsEnabled = true;
            }
        }

        private void TemizlikOneriEkle(AITemizlikOneri oneri)
        {
            var border = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x06, 0x0C, 0x1A)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0x22, 0x44)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(70) });

            var sol = new System.Windows.Controls.StackPanel();

            var baslik = new System.Windows.Controls.TextBlock
            {
                Text = oneri.DosyaAdi,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xF0, 0xFF)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var aciklama = new System.Windows.Controls.TextBlock
            {
                Text = oneri.Neden,
                Foreground = new SolidColorBrush(Color.FromRgb(0x7A, 0x9C, 0xC0)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var oncelik = new System.Windows.Controls.TextBlock
            {
                Text = $"⚠ {oneri.OncelikSeviyesi} öncelik",
                Foreground = new SolidColorBrush(
                    oneri.OncelikSeviyesi == "Yüksek"
                        ? Color.FromRgb(0xFF, 0x22, 0x44)
                        : Color.FromRgb(0xFF, 0xD7, 0x00)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                Margin = new Thickness(0, 2, 0, 0)
            };

            sol.Children.Add(baslik);
            sol.Children.Add(aciklama);
            sol.Children.Add(oncelik);

            var silBtn = new System.Windows.Controls.Button
            {
                Content = "🗑 SİL",
                Height = 28,
                Margin = new Thickness(8, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0x22, 0x44)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x22, 0x44)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = oneri
            };
            silBtn.Click += BtnDosyaSil_Click;

            System.Windows.Controls.Grid.SetColumn(sol, 0);
            System.Windows.Controls.Grid.SetColumn(silBtn, 1);

            grid.Children.Add(sol);
            grid.Children.Add(silBtn);
            border.Child = grid;
            TemizlikListesi.Children.Add(border);
        }

        private void BtnDosyaSil_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn) return;
            if (btn.Tag is not AITemizlikOneri oneri) return;

            var sonuc = System.Windows.MessageBox.Show(
                $"'{oneri.DosyaAdi}' dosyasını silmek istiyor musunuz?\n\n{oneri.Neden}",
                "Dosya Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (sonuc != MessageBoxResult.Yes) return;

            try
            {
                if (!string.IsNullOrEmpty(oneri.DosyaYolu) && File.Exists(oneri.DosyaYolu))
                {
                    File.Delete(oneri.DosyaYolu);
                    TxtDurum.Text = $"✅ '{oneri.DosyaAdi}' silindi!";

                    for (int i = _indirmeler.Count - 1; i >= 0; i--)
                    {
                        var tamYol = Path.Combine(_indirmeler[i].KayitYolu, _indirmeler[i].DosyaAdi);
                        if (tamYol == oneri.DosyaYolu)
                        {
                            _indirmeler.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    TxtDurum.Text = "Dosya bulunamadı!";
                }

                var parent = btn.Parent as System.Windows.Controls.Grid;
                var border = parent?.Parent as System.Windows.Controls.Border;
                if (border != null)
                    TemizlikListesi.Children.Remove(border);
            }
            catch (Exception ex)
            {
                TxtDurum.Text = $"Silinemedi: {ex.Message}";
            }
        }
    }
}