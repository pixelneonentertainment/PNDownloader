using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PixelNeonDownloader
{
    public class IndirmeKayit
    {
        public string DosyaAdi { get; set; } = "";
        public string Url { get; set; } = "";
        public string KayitYolu { get; set; } = "";
        public long DosyaBoyutu { get; set; }
        public long IndirilenBytes { get; set; }
        public double Ilerleme { get; set; }
        public string Durum { get; set; } = "";
        public string Tur { get; set; } = "";
        public string Kategori { get; set; } = "";
        public string KalanSure { get; set; } = "";
    }

    public static class ListeKaydedici
    {
        private static readonly string _kayitDosyasi = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader",
            "indirmeler.json");

        public static void Kaydet(IEnumerable<DownloadItem> indirmeler)
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(_kayitDosyasi)!);

                var kayitlar = new List<IndirmeKayit>();

                foreach (var item in indirmeler)
                {
                    kayitlar.Add(new IndirmeKayit
                    {
                        DosyaAdi = item.DosyaAdi,
                        Url = item.Url,
                        KayitYolu = item.KayitYolu,
                        DosyaBoyutu = item.DosyaBoyutu,
                        IndirilenBytes = item.IndirilenBytes,
                        Ilerleme = item.Ilerleme,
                        Durum = item.Durum.ToString(),
                        Tur = item.Tur.ToString(),
                        Kategori = item.Kategori,
                        KalanSure = item.KalanSure
                    });
                }

                var json = JsonSerializer.Serialize(kayitlar,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(_kayitDosyasi, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kaydetme hatası: {ex.Message}");
            }
        }

        public static List<DownloadItem> Yukle()
        {
            var liste = new List<DownloadItem>();

            try
            {
                if (!File.Exists(_kayitDosyasi))
                    return liste;

                var json = File.ReadAllText(_kayitDosyasi);
                var kayitlar = JsonSerializer.Deserialize<List<IndirmeKayit>>(json);

                if (kayitlar == null) return liste;

                foreach (var kayit in kayitlar)
                {
                    var item = new DownloadItem
                    {
                        DosyaAdi = kayit.DosyaAdi,
                        Url = kayit.Url,
                        KayitYolu = kayit.KayitYolu,
                        DosyaBoyutu = kayit.DosyaBoyutu,
                        IndirilenBytes = kayit.IndirilenBytes,
                        Ilerleme = kayit.Ilerleme,
                        Kategori = kayit.Kategori,
                        KalanSure = kayit.KalanSure
                    };

                    // Durum
                    if (Enum.TryParse<Durum>(kayit.Durum, out var durum))
                    {
                        // İndiriliyor durumundakiler duraklatılmış sayılsın
                        item.Durum = durum == Durum.Indiriliyor
                            ? Durum.Duraklatildi
                            : durum;
                    }

                    // Tür
                    if (Enum.TryParse<IndirmeTuru>(kayit.Tur, out var tur))
                        item.Tur = tur;

                    liste.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Yükleme hatası: {ex.Message}");
            }

            return liste;
        }
    }
}