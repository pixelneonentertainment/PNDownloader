using System.Windows;

namespace PixelNeonDownloader
{
    public partial class KisayolSatir : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty KisayolProperty =
            DependencyProperty.Register("Kisayol", typeof(string),
                typeof(KisayolSatir),
                new PropertyMetadata("", OnKisayolChanged));

        public static readonly DependencyProperty AciklamaProperty =
            DependencyProperty.Register("Aciklama", typeof(string),
                typeof(KisayolSatir),
                new PropertyMetadata("", OnAciklamaChanged));

        public string Kisayol
        {
            get => (string)GetValue(KisayolProperty);
            set => SetValue(KisayolProperty, value);
        }

        public string Aciklama
        {
            get => (string)GetValue(AciklamaProperty);
            set => SetValue(AciklamaProperty, value);
        }

        public KisayolSatir()
        {
            InitializeComponent();
        }

        private static void OnKisayolChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is KisayolSatir control)
                control.TxtKisayol.Text = e.NewValue?.ToString() ?? "";
        }

        private static void OnAciklamaChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is KisayolSatir control)
                control.TxtAciklama.Text = e.NewValue?.ToString() ?? "";
        }
    }
}