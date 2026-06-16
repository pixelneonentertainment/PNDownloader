using System;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace PixelNeonDownloader
{
    public class PanoIzleyicisi
    {
        private readonly DispatcherTimer _timer;
        private string _sonMetin = "";
        private readonly Action<string> _linkBulunduCallback;
        private bool _aktif = true;

        private static readonly string[] _izlenenUzantilar = {
            ".rar", ".zip", ".7z", ".tar", ".gz",
            ".mp4", ".mkv", ".avi", ".mp3", ".wav",
            ".exe", ".msi", ".apk", ".iso", ".img",
            ".pdf", ".torrent"
        };

        public bool Aktif
        {
            get => _aktif;
            set => _aktif = value;
        }

        public PanoIzleyicisi(Action<string> linkBulunduCallback)
        {
            _linkBulunduCallback = linkBulunduCallback;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_aktif) return;

            try
            {
                // Belirsizlik hatasını (CS0104) gidermek için WPF Clipboard sınıfı açıkça belirtildi
                if (System.Windows.Clipboard.ContainsText())
                {
                    var metin = System.Windows.Clipboard.GetText().Trim();
                    if (string.IsNullOrEmpty(metin) || metin == _sonMetin) return;

                    _sonMetin = metin;

                    if (LinkMiVeIzleniyorMu(metin))
                    {
                        _linkBulunduCallback?.Invoke(metin);
                    }
                }
            }
            catch { }
        }

        private static bool LinkMiVeIzleniyorMu(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin)) return false;

            bool isUrl = metin.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         metin.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                         metin.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);

            bool isMagnet = metin.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase);

            if (isMagnet) return true;

            if (isUrl)
            {
                try
                {
                    var uri = new Uri(metin);
                    var dosyaAdi = Path.GetFileName(uri.LocalPath).ToLowerInvariant();
                    var uzanti = Path.GetExtension(dosyaAdi);

                    if (_izlenenUzantilar.Contains(uzanti))
                        return true;
                }
                catch { }
            }

            return false;
        }

        public void Durdur()
        {
            _timer.Stop();
        }
    }
}