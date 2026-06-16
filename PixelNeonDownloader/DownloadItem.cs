using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PixelNeonDownloader
{
    public enum Durum
    {
        Bekliyor,
        Indiriliyor,
        Duraklatildi,
        Tamamlandi,
        Hata,
        Cikartiliyor
    }

    public enum IndirmeTuru
    {
        HTTP,
        FTP,
        Torrent,
        Magnet
    }

    public class DownloadItem : INotifyPropertyChanged
    {
        private string _dosyaAdi = "";
        private string _url = "";
        private string _kayitYolu = "";
        private long _dosyaBoyutu;
        private long _indirilenbytes;
        private double _ilerleme;
        private double _hiz;
        private string _kalanSure = "--:--";
        private Durum _durum = Durum.Bekliyor;
        private IndirmeTuru _tur = IndirmeTuru.HTTP;
        private string _kategori = "Genel";
        private DateTime? _zamanlanmisBaslangic;
        private bool _zamanlanmis;

        public string DosyaAdi
        {
            get => _dosyaAdi;
            set
            {
                _dosyaAdi = value;
                Degisti();
                Degisti(nameof(DosyaIkonu));
                Degisti(nameof(DosyaIkonuRengi));
            }
        }
        // DownloadItem.cs içerisindeki uygun bir alana ekleyin:
        private string _referrer = "";
        public string Referrer
        {
            get => _referrer;
            set { _referrer = value; Degisti(); }
        }

        public double OrtalamaHiz { get; set; } = 0;
        private TimeSpan _indirmeSuresi = TimeSpan.Zero;

        public TimeSpan IndirmeSuresi
        {
            get => _indirmeSuresi;
            set { _indirmeSuresi = value; Degisti(); Degisti(nameof(IndirmeSuresiMetni)); }
        }

        public string IndirmeSuresiMetni
        {
            get
            {
                var sure = IndirmeSuresi;
                if (sure.TotalHours >= 1)
                    return $"{(int)sure.TotalHours}s {sure.Minutes:D2}d";
                if (sure.TotalMinutes >= 1)
                    return $"{(int)sure.TotalMinutes}dk {sure.Seconds}sn";
                return $"{(int)sure.TotalSeconds}sn";
            }
        }

        public string Url
        {
            get => _url;
            set { _url = value; Degisti(); }
        }

        public string KayitYolu
        {
            get => _kayitYolu;
            set { _kayitYolu = value; Degisti(); }
        }

        public long DosyaBoyutu
        {
            get => _dosyaBoyutu;
            set { _dosyaBoyutu = value; Degisti(); Degisti(nameof(BoyutMetni)); }
        }

        public long IndirilenBytes
        {
            get => _indirilenbytes;
            set { _indirilenbytes = value; Degisti(); }
        }

        public double Ilerleme
        {
            get => _ilerleme;
            set { _ilerleme = value; Degisti(); Degisti(nameof(IlerlemeYuzde)); }
        }

        public double IlerlemeYuzde => Ilerleme * 100;

        public double Hiz
        {
            get => _hiz;
            set { _hiz = value; Degisti(); Degisti(nameof(HizMetni)); }
        }

        public string KalanSure
        {
            get => _kalanSure;
            set { _kalanSure = value; Degisti(); }
        }

        public Durum Durum
        {
            get => _durum;
            set
            {
                _durum = value;
                Degisti();
                Degisti(nameof(DurumMetni));
                Degisti(nameof(DurumRengi));
            }
        }

        public IndirmeTuru Tur
        {
            get => _tur;
            set { _tur = value; Degisti(); Degisti(nameof(TurIkonu)); }
        }

        public string Kategori
        {
            get => _kategori;
            set { _kategori = value; Degisti(); }
        }

        public DateTime? ZamanlanmisBaslangic
        {
            get => _zamanlanmisBaslangic;
            set { _zamanlanmisBaslangic = value; Degisti(); }
        }

        public bool Zamanlanmis
        {
            get => _zamanlanmis;
            set { _zamanlanmis = value; Degisti(); }
        }

        public string BoyutMetni => ByteFormatla(DosyaBoyutu);
        public string HizMetni => $"↓ {ByteFormatla((long)Hiz)}/s";

        public string DurumMetni => Durum switch
        {
            Durum.Bekliyor => "BEKLEYOR",
            Durum.Indiriliyor => "İNDİRİLİYOR",
            Durum.Duraklatildi => "DURAKLATILDI",
            Durum.Tamamlandi => "TAMAMLANDI",
            Durum.Hata => "HATA",
            Durum.Cikartiliyor => "ÇIKARILIYOR",
            _ => "?"
        };

        public string DurumRengi => Durum switch
        {
            Durum.Indiriliyor => "#00FFE5",
            Durum.Tamamlandi => "#39FF14",
            Durum.Hata => "#FF2244",
            Durum.Duraklatildi => "#FFD700",
            Durum.Cikartiliyor => "#BD00FF",
            _ => "#7A9CC0"
        };

        public string TurIkonu => Tur switch
        {
            IndirmeTuru.Torrent => "⟁",
            IndirmeTuru.Magnet => "⚡",
            IndirmeTuru.FTP => "⬡",
            _ => "↓"
        };

        public string DosyaIkonu
        {
            get
            {
                var uzanti = System.IO.Path.GetExtension(DosyaAdi).ToLowerInvariant();

                return uzanti switch
                {
                    ".mp4" or ".mkv" or ".avi" or ".mov" or
                    ".wmv" or ".flv" or ".webm" => "🎬",
                    ".mp3" or ".flac" or ".wav" or ".aac" or
                    ".ogg" or ".m4a" or ".wma" => "🎵",
                    ".jpg" or ".jpeg" or ".png" or ".gif" or
                    ".bmp" or ".webp" or ".svg" => "🖼",
                    ".zip" or ".rar" or ".7z" or
                    ".tar" or ".gz" or ".bz2" => "📦",
                    ".torrent" => "⟁",
                    ".pdf" => "📕",
                    ".doc" or ".docx" => "📝",
                    ".xls" or ".xlsx" => "📊",
                    ".ppt" or ".pptx" => "📊",
                    ".txt" => "📄",
                    ".exe" or ".msi" or ".apk" => "⚙",
                    ".iso" or ".img" => "💿",
                    ".py" or ".js" or ".cs" or ".cpp" or
                    ".java" or ".html" or ".css" => "💻",
                    ".pak" or ".rpkg" or ".dat" => "🎮",
                    _ => "📄"
                };
            }
        }

        public string DosyaIkonuRengi
        {
            get
            {
                var uzanti = System.IO.Path.GetExtension(DosyaAdi).ToLowerInvariant();

                return uzanti switch
                {
                    ".mp4" or ".mkv" or ".avi" or
                    ".mov" or ".wmv" or ".flv" => "#FF006E",
                    ".mp3" or ".flac" or ".wav" or
                    ".aac" or ".ogg" or ".m4a" => "#BD00FF",
                    ".jpg" or ".jpeg" or ".png" or
                    ".gif" or ".bmp" or ".webp" => "#FFD700",
                    ".zip" or ".rar" or ".7z" or
                    ".tar" or ".gz" => "#FF8C00",
                    ".torrent" => "#00FFE5",
                    ".pdf" => "#FF2244",
                    ".doc" or ".docx" => "#00AAFF",
                    ".xls" or ".xlsx" => "#39FF14",
                    ".exe" or ".msi" or ".apk" => "#7A9CC0",
                    ".iso" or ".img" => "#AAAAAA",
                    _ => "#7A9CC0"
                };
            }
        }

        private static string ByteFormatla(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] ekler = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double boyut = bytes;
            while (boyut >= 1024 && i < ekler.Length - 1)
            {
                boyut /= 1024;
                i++;
            }
            return $"{boyut:F1} {ekler[i]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Degisti([CallerMemberName] string? ad = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ad));

        public int DenemeSayisi { get; set; } = 0;
        public int MaksDenemeSayisi { get; set; } = 3;
        public bool YenidenDenemeAktif { get; set; } = true;
        public DateTime? SonHataTarihi { get; set; }
        public string SonHataMesaji { get; set; } = "";
    }
}