using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PixelNeonDownloader
{
    public class IndirmeLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Url { get; set; } = "";
        public string Referrer { get; set; } = "";
        public string DosyaAdi { get; set; } = "";
        public string KayitYolu { get; set; } = "";
        public long DosyaBoyutu { get; set; }
        public long IndirilenBytes { get; set; }
        public string Tur { get; set; } = "HTTP";
        public string Kategori { get; set; } = "Genel";
        public DateTime BaslangicZamani { get; set; } = DateTime.Now;
        public DateTime SonGuncelleme { get; set; } = DateTime.Now;
        public string Durum { get; set; } = "Devam Ediyor";
        public List<ParcaLog> Parcalar { get; set; } = new();
    }

    public class ParcaLog
    {
        public int Index { get; set; }
        public long Baslangic { get; set; }
        public long Bitis { get; set; }
        public long IndirilenBytes { get; set; }
        public bool Tamamlandi { get; set; }
    }

    public static class IndirmeLogYoneticisi
    {
        private static readonly string _logKlasor = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "logs");

        private static readonly JsonSerializerOptions _jsonSecenekler =
            new() { WriteIndented = true };

        private static readonly object _lockObject = new();

        static IndirmeLogYoneticisi()
        {
            Directory.CreateDirectory(_logKlasor);
        }

        public static IndirmeLog LogOlustur(DownloadItem item)
        {
            var log = new IndirmeLog
            {
                Id = Guid.NewGuid().ToString(),
                Url = item.Url,
                Referrer = item.Referrer, // Referrer alanı burada nesne kapsamı içinde doğrudan atanıyor
                DosyaAdi = item.DosyaAdi,
                KayitYolu = item.KayitYolu,
                DosyaBoyutu = item.DosyaBoyutu,
                IndirilenBytes = 0,
                Tur = item.Tur.ToString(),
                Kategori = item.Kategori,
                BaslangicZamani = DateTime.Now,
                SonGuncelleme = DateTime.Now,
                Durum = "Devam Ediyor"
            };

            LogKaydet(log);
            return log;
        }

        public static void LogGuncelle(IndirmeLog log, long indirilenBytes)
        {
            lock (_lockObject)
            {
                log.IndirilenBytes = indirilenBytes;
                log.SonGuncelleme = DateTime.Now;
            }
            LogKaydet(log);
        }

        public static void ParcaGuncelle(IndirmeLog log,
            int index, long indirilenBytes, bool tamamlandi = false)
        {
            lock (_lockObject)
            {
                var parca = log.Parcalar.Find(p => p.Index == index);
                if (parca != null)
                {
                    parca.IndirilenBytes = indirilenBytes;
                    parca.Tamamlandi = tamamlandi;
                }
                log.SonGuncelleme = DateTime.Now;
            }
            LogKaydet(log);
        }

        public static void ParcaEkle(IndirmeLog log,
            int index, long baslangic, long bitis)
        {
            lock (_lockObject)
            {
                log.Parcalar.Add(new ParcaLog
                {
                    Index = index,
                    Baslangic = baslangic,
                    Bitis = bitis,
                    IndirilenBytes = 0,
                    Tamamlandi = false
                });
            }
        }

        public static void LogTamamla(IndirmeLog log)
        {
            lock (_lockObject)
            {
                log.Durum = "Tamamlandi";
                log.SonGuncelleme = DateTime.Now;
            }
            LogKaydet(log);

            try
            {
                var dosyaYolu = LogDosyasiYolu(log.Id);
                if (File.Exists(dosyaYolu))
                    File.Delete(dosyaYolu);
            }
            catch { }
        }

        public static void LogHata(IndirmeLog log, string hata = "")
        {
            lock (_lockObject)
            {
                log.Durum = "Hata";
                log.SonGuncelleme = DateTime.Now;
            }
            LogKaydet(log);
        }

        private static void LogKaydet(IndirmeLog log)
        {
            lock (_lockObject)
            {
                try
                {
                    var json = JsonSerializer.Serialize(log, _jsonSecenekler);
                    File.WriteAllText(LogDosyasiYolu(log.Id), json);
                }
                catch { }
            }
        }

        public static List<IndirmeLog> YaridaKalanlarYukle()
        {
            var loglar = new List<IndirmeLog>();

            try
            {
                var dosyalar = Directory.GetFiles(_logKlasor, "*.json");

                foreach (var dosya in dosyalar)
                {
                    try
                    {
                        var json = File.ReadAllText(dosya);
                        var log = JsonSerializer.Deserialize<IndirmeLog>(json);

                        if (log != null && log.Durum == "Devam Ediyor")
                        {
                            if ((DateTime.Now - log.SonGuncelleme).TotalHours > 24)
                            {
                                File.Delete(dosya);
                                continue;
                            }

                            loglar.Add(log);
                        }
                    }
                    catch
                    {
                        try { File.Delete(dosya); } catch { }
                    }
                }
            }
            catch { }

            return loglar;
        }

        public static void TumLoglarTemizle()
        {
            lock (_lockObject)
            {
                try
                {
                    var dosyalar = Directory.GetFiles(_logKlasor, "*.json");
                    foreach (var dosya in dosyalar)
                        File.Delete(dosya);
                }
                catch { }
            }
        }

        private static string LogDosyasiYolu(string id)
            => Path.Combine(_logKlasor, $"{id}.json");

        public static long KurtarilabilirBytes(IndirmeLog log)
        {
            lock (_lockObject)
            {
                if (log.Parcalar.Count == 0)
                    return log.IndirilenBytes;

                long toplam = 0;
                foreach (var parca in log.Parcalar)
                    toplam += parca.IndirilenBytes;
                return toplam;
            }
        }
    }
}