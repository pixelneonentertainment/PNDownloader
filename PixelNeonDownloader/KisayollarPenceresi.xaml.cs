using System.Windows;
using System.Windows.Input;

namespace PixelNeonDownloader
{
    public partial class KisayollarPenceresi : Window
    {
        public KisayollarPenceresi()
        {
            InitializeComponent();
        }

        private void Baslik_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void Kapat_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}