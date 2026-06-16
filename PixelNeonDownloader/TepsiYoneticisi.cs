using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WpfWindow = System.Windows.Window;
using WpfApp = System.Windows.Application;
using System.Linq;

namespace PixelNeonDownloader
{
    public class TepsiYoneticisi : IDisposable
    {
        private readonly NotifyIcon _tepsiIkonu;
        private readonly WpfWindow _anaEkran;
        private System.Collections.ObjectModel.ObservableCollection<DownloadItem>? _indirmeler;
        private Icon? _baseIcon;
        private Icon? _lastGeneratedIcon;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public TepsiYoneticisi(WpfWindow anaEkran)
        {
            _anaEkran = anaEkran;
            _tepsiIkonu = new NotifyIcon();

            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                    _baseIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            }
            catch { }

            if (_baseIcon == null)
            {
                try
                {
                    var localIcon = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "pixel_neon_icon.ico");
                    if (System.IO.File.Exists(localIcon))
                        _baseIcon = new System.Drawing.Icon(localIcon);
                }
                catch { }
            }

            _baseIcon ??= SystemIcons.Application;
            _tepsiIkonu.Icon = (Icon)_baseIcon.Clone();
            _tepsiIkonu.Text = "Pixel Neon Downloader";
            _tepsiIkonu.Visible = true;

            try
            {
                if (_anaEkran is MainWindow mw)
                {
                    SubscribeToIndirmeler(mw.Indirmeler);
                    UpdateBadge();
                }
            }
            catch { }

            var menu = new ContextMenuStrip();

            var goster = new ToolStripMenuItem("📋 Göster");
            goster.Click += (s, e) => Goster();

            var yeniIndirme = new ToolStripMenuItem("⊕ Yeni İndirme");
            yeniIndirme.Click += (s, e) =>
            {
                Goster();
                if (_anaEkran is MainWindow mw)
                    mw.TepsidenYeniIndirme();
            };

            var ayarlar = new ToolStripMenuItem("⚙ Ayarlar");
            ayarlar.Click += (s, e) =>
            {
                try
                {
                    var defaultFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads", "PixelNeon");
                    var pencere = new AyarlarPenceresi(defaultFolder)
                    {
                        Owner = _anaEkran
                    };
                    pencere.ShowDialog();
                }
                catch { }
            };

            var separator = new ToolStripSeparator();
            var cikis = new ToolStripMenuItem("✕ Çıkış");
            cikis.Click += (s, e) => WpfApp.Current.Shutdown();

            menu.Items.Add(goster);
            menu.Items.Add(yeniIndirme);
            menu.Items.Add(ayarlar);
            menu.Items.Add(separator);
            menu.Items.Add(cikis);

            _tepsiIkonu.ContextMenuStrip = menu;
            _tepsiIkonu.DoubleClick += (s, e) => Goster();
        }

        private void SubscribeToIndirmeler(System.Collections.ObjectModel.ObservableCollection<DownloadItem> col)
        {
            if (col == null) return;
            _indirmeler = col;
            _indirmeler.CollectionChanged += Indirmeler_CollectionChanged;
            foreach (var it in _indirmeler)
            {
                if (it is INotifyPropertyChanged inpc)
                    inpc.PropertyChanged += Indirme_PropertyChanged;
            }
        }

        private void Indirmeler_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var o in e.OldItems)
                    if (o is INotifyPropertyChanged inpc)
                        inpc.PropertyChanged -= Indirme_PropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (var n in e.NewItems)
                    if (n is INotifyPropertyChanged inpc)
                        inpc.PropertyChanged += Indirme_PropertyChanged;
            }

            UpdateBadge();
        }

        private void Indirme_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DownloadItem.Durum) || e.PropertyName == nameof(DownloadItem.IndirilenBytes))
                UpdateBadge();
        }

        private void UpdateBadge()
        {
            try
            {
                int aktif = 0;
                if (_indirmeler != null)
                    aktif = _indirmeler.Count(i => i.Durum == Durum.Indiriliyor);

                _tepsiIkonu.Text = aktif > 0 ? $"Pixel Neon — {aktif} aktif" : "Pixel Neon Downloader";
                GenerateBadgeIcon(aktif);
            }
            catch { }
        }

        private void GenerateBadgeIcon(int count)
        {
            try
            {
                if (_baseIcon == null) return;

                using var bmp = _baseIcon.ToBitmap();
                using var g = Graphics.FromImage(bmp);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                if (count > 0)
                {
                    int diameter = Math.Max(20, bmp.Width / 4);
                    var rect = new Rectangle(bmp.Width - diameter - 2, 2, diameter, diameter);
                    using var brush = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0x22, 0x44));
                    g.FillEllipse(brush, rect);

                    using var font = new Font("Segoe UI", diameter / 2.5f, FontStyle.Bold, GraphicsUnit.Pixel);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    using var fore = new SolidBrush(Color.White);
                    var text = count > 99 ? "99+" : count.ToString();
                    g.DrawString(text, font, fore, new RectangleF(rect.X, rect.Y, rect.Width, rect.Height), sf);
                }

                var hIcon = bmp.GetHicon();
                var ico = Icon.FromHandle(hIcon);

                var eskiIkon = _lastGeneratedIcon;

                _lastGeneratedIcon = (Icon)ico.Clone();
                _tepsiIkonu.Icon = _lastGeneratedIcon;

                eskiIkon?.Dispose();
                ico.Dispose();
                DestroyIcon(hIcon);
            }
            catch { }
        }

        public void Goster()
        {
            _anaEkran.Show();
            _anaEkran.WindowState = System.Windows.WindowState.Normal;
            _anaEkran.Activate();
        }

        public void Gizle()
        {
            _anaEkran.Hide();
        }

        public void BildirimGoster(string baslik, string mesaj, ToolTipIcon ikon = ToolTipIcon.Info)
        {
            _tepsiIkonu.ShowBalloonTip(3000, baslik, mesaj, ikon);
        }

        public void Dispose()
        {
            if (_indirmeler != null)
            {
                _indirmeler.CollectionChanged -= Indirmeler_CollectionChanged;
                foreach (var it in _indirmeler)
                {
                    if (it is INotifyPropertyChanged inpc)
                        inpc.PropertyChanged -= Indirme_PropertyChanged;
                }
            }

            _tepsiIkonu.Visible = false;
            _tepsiIkonu.Dispose();
            _lastGeneratedIcon?.Dispose();
            _baseIcon?.Dispose();
        }
    }
}