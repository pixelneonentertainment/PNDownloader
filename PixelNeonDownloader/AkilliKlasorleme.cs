using System;
using System.IO;

namespace PixelNeonDownloader
{
    public static class AkilliKlasorleme
    {
        public static string KlasorBelirle(string dosyaAdi)
        {
            var uzanti = Path.GetExtension(dosyaAdi).ToLowerInvariant();

            return uzanti switch
            {
                ".mp4" or ".mkv" or ".avi" or ".mov" or
                ".wmv" or ".flv" or ".webm" or ".m4v" => "Videolar",

                ".mp3" or ".flac" or ".wav" or ".aac" or
                ".ogg" or ".m4a" or ".wma" or ".opus" => "Müzik",

                ".jpg" or ".jpeg" or ".png" or ".gif" or
                ".bmp" or ".webp" or ".svg" or ".tiff" => "Resimler",

                ".zip" or ".rar" or ".7z" or
                ".tar" or ".gz" or ".bz2" => "Arşivler",

                ".pdf" or ".doc" or ".docx" or ".xls" or
                ".xlsx" or ".ppt" or ".pptx" or ".txt" or
                ".odt" or ".rtf" => "Belgeler",

                ".exe" or ".msi" or ".apk" or
                ".dmg" or ".deb" or ".rpm" => "Yazılımlar",

                ".iso" or ".img" or ".bin" or ".nrg" => "Disk Imajlari",

                ".torrent" => "Torrentler",

                ".py" or ".js" or ".cs" or ".cpp" or
                ".java" or ".html" or ".css" or ".php" => "Kod",

                ".pak" or ".rpkg" or ".dat" => "Oyunlar",

                _ => "Diger"
            };
        }

        public static void DosyayiTasi(string kaynakYol, string hedefKlasor)
        {
            try
            {
                if (!File.Exists(kaynakYol)) return;

                Directory.CreateDirectory(hedefKlasor);

                var dosyaAdi = Path.GetFileName(kaynakYol);
                var hedefYol = Path.Combine(hedefKlasor, dosyaAdi);

                if (File.Exists(hedefYol))
                {
                    var isim = Path.GetFileNameWithoutExtension(dosyaAdi);
                    var uzanti = Path.GetExtension(dosyaAdi);
                    int sayac = 1;

                    while (File.Exists(hedefYol))
                    {
                        hedefYol = Path.Combine(hedefKlasor, $"{isim} ({sayac}){uzanti}");
                        sayac++;
                    }
                }

                File.Move(kaynakYol, hedefYol);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Taşıma hatası: {ex.Message}");
            }
        }
    }
}