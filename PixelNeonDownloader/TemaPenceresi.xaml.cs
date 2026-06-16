using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace PixelNeonDownloader
{
    public partial class TemaPenceresi : Window
    {
        public TemaPenceresi()
        {
            InitializeComponent();
            TxtAktifTema.Text = TemaYoneticisi.MevcutTemaAdi;
            AktifKartiVurgula(TemaYoneticisi.MevcutTemaAdi);
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();

        private void TemaKart_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Controls.Border kart) return;
            var temaAdi = kart.Tag?.ToString() ?? "Cyan";

            TemaYoneticisi.TemaUygula(temaAdi,
                System.Windows.Application.Current.Resources);

            TxtAktifTema.Text = temaAdi;
            AktifKartiVurgula(temaAdi);

            if (System.Windows.Application.Current.MainWindow is MainWindow ana)
                ana.TemaGuncelle();
        }

        private void AktifKartiVurgula(string temaAdi)
        {
            var kartlar = new[]
            {
                (KartCyan,   "Cyan"),
                (KartPink,   "Pink"),
                (KartPurple, "Purple"),
                (KartGreen,  "Green"),
                (KartOrange, "Orange"),
                (KartRed,    "Red")
            };

            foreach (var (kart, ad) in kartlar)
            {
                if (ad == temaAdi)
                {
                    kart.BorderThickness = new Thickness(3);
                    kart.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = TemaYoneticisi.RenkCevir(
    TemaYoneticisi.Temalar[ad].AnaRenk),
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
    }
}