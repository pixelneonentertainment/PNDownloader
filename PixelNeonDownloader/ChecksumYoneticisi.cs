using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace PixelNeonDownloader
{
    public enum ChecksumTuru
    {
        MD5,
        SHA1,
        SHA256,
        SHA512
    }

    public class ChecksumSonucu
    {
        public string Algoritma { get; set; } = "";
        public string Deger { get; set; } = "";
        public bool Eslesme { get; set; } = false;
        public bool Dogrulandi { get; set; } = false;
        public string Hata { get; set; } = "";
    }

    public static class ChecksumYoneticisi
    {
        public static async Task<string> HesaplaAsync(
            string dosyaYolu,
            ChecksumTuru tur,
            IProgress<double>? ilerleme = null,
            CancellationToken iptalToken = default)
        {
            try
            {
                using HashAlgorithm algoritma = tur switch
                {
                    ChecksumTuru.MD5 => MD5.Create(),
                    ChecksumTuru.SHA1 => SHA1.Create(),
                    ChecksumTuru.SHA256 => SHA256.Create(),
                    ChecksumTuru.SHA512 => SHA512.Create(),
                    _ => SHA256.Create()
                };

                using var stream = new FileStream(
                    dosyaYolu, FileMode.Open, FileAccess.Read,
                    FileShare.Read, 81920, true);

                var tamBoyut = stream.Length;
                var buffer = new byte[81920];
                long toplamOkunan = 0;
                int okunan;
                double sonYuzde = -1; // Gereksiz arayüz tetiklemelerini önlemek için filtre eklendi

                while ((okunan = await stream.ReadAsync(buffer, iptalToken)) > 0)
                {
                    algoritma.TransformBlock(buffer, 0, okunan, null, 0);
                    toplamOkunan += okunan;

                    if (tamBoyut > 0)
                    {
                        // Performans İyileştirmesi: Her blokta değil, yalnızca yüzdesel tamsayı değiştiğinde arayüze bildirim gönderilir
                        double mevcutYuzde = Math.Floor((double)toplamOkunan / tamBoyut * 100);
                        if (mevcutYuzde > sonYuzde)
                        {
                            sonYuzde = mevcutYuzde;
                            ilerleme?.Report(mevcutYuzde);
                        }
                    }
                }

                algoritma.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                var hash = algoritma.Hash ?? Array.Empty<byte>();
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                return $"HATA: {ex.Message}";
            }
        }

        public static async Task<ChecksumSonucu> DogrulaAsync(
            string dosyaYolu,
            string beklenenHash,
            ChecksumTuru tur,
            IProgress<double>? ilerleme = null,
            CancellationToken iptalToken = default)
        {
            var sonuc = new ChecksumSonucu
            {
                Algoritma = tur.ToString()
            };

            try
            {
                sonuc.Deger = await HesaplaAsync(
                    dosyaYolu, tur, ilerleme, iptalToken);

                if (sonuc.Deger.StartsWith("HATA:"))
                {
                    sonuc.Hata = sonuc.Deger;
                    return sonuc;
                }

                sonuc.Dogrulandi = true;
                sonuc.Eslesme = string.Equals(
                    sonuc.Deger.Trim(),
                    beklenenHash.Trim().ToLowerInvariant(),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                sonuc.Hata = ex.Message;
            }

            return sonuc;
        }

        public static ChecksumTuru AlgoritmamiTespit(string hash)
        {
            return hash.Trim().Length switch
            {
                32 => ChecksumTuru.MD5,
                40 => ChecksumTuru.SHA1,
                64 => ChecksumTuru.SHA256,
                128 => ChecksumTuru.SHA512,
                _ => ChecksumTuru.SHA256
            };
        }
    }
}