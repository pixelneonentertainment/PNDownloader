using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace PixelNeonDownloader
{
    public class IndirmeKaydi
    {
        public int Id { get; set; }
        public string DosyaAdi { get; set; } = "";
        public string Url { get; set; } = "";
        public string KayitYolu { get; set; } = "";
        public long DosyaBoyutu { get; set; }
        public string Kategori { get; set; } = "";
        public string Tur { get; set; } = "";
        public string Durum { get; set; } = "";
        public DateTime BaslamaTarihi { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }
        public double OrtalamaHiz { get; set; }
        public double IndirmeSuresi { get; set; }
        public int DenemeSayisi { get; set; }
        public string BoyutMetni =>
            DosyaBoyutu >= 1_073_741_824
                ? $"{DosyaBoyutu / 1_073_741_824.0:F1} GB"
                : DosyaBoyutu >= 1_048_576
                    ? $"{DosyaBoyutu / 1_048_576.0:F1} MB"
                    : DosyaBoyutu >= 1024
                        ? $"{DosyaBoyutu / 1024.0:F1} KB"
                        : $"{DosyaBoyutu} B";
        public string TarihMetni =>
            TamamlanmaTarihi.HasValue
                ? TamamlanmaTarihi.Value.ToString("dd.MM.yyyy HH:mm")
                : BaslamaTarihi.ToString("dd.MM.yyyy HH:mm");
        public string DurumRengi => Durum switch
        {
            "Tamamlandi" => "#39FF14",
            "Hata" => "#FF2244",
            "Duraklatildi" => "#FFD700",
            _ => "#7A9CC0"
        };
    }

    public static class VeritabaniYoneticisi
    {
        private static readonly string _dbYolu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "indirmeler.db");

        private static string BaglantiCumlesiBal =>
            $"Data Source={_dbYolu}";

        public static void Baslat()
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(_dbYolu)!);

                using var baglanti = new SqliteConnection(BaglantiCumlesiBal);
                baglanti.Open();

                var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS IndirmeGecmisi (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DosyaAdi TEXT NOT NULL,
                        Url TEXT NOT NULL,
                        KayitYolu TEXT,
                        DosyaBoyutu INTEGER DEFAULT 0,
                        Kategori TEXT DEFAULT 'Genel',
                        Tur TEXT DEFAULT 'HTTP',
                        Durum TEXT DEFAULT 'Bekliyor',
                        BaslamaTarihi TEXT NOT NULL,
                        TamamlanmaTarihi TEXT,
                        OrtalamaHiz REAL DEFAULT 0,
                        IndirmeSuresi REAL DEFAULT 0,
                        DenemeSayisi INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS Istatistikler (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Tarih TEXT NOT NULL,
                        ToplamBoyut INTEGER DEFAULT 0,
                        ToplamSure REAL DEFAULT 0,
                        ToplamSayi INTEGER DEFAULT 0
                    );";
                komut.ExecuteNonQuery();
            }
            catch { }
        }

        public static void KayitEkle(DownloadItem item)
        {
            try
            {
                using var baglanti = new SqliteConnection(BaglantiCumlesiBal);
                baglanti.Open();

                var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO IndirmeGecmisi
                    (DosyaAdi, Url, KayitYolu, DosyaBoyutu,
                     Kategori, Tur, Durum, BaslamaTarihi,
                     TamamlanmaTarihi, OrtalamaHiz,
                     IndirmeSuresi, DenemeSayisi)
                    VALUES
                    ($dosyaAdi, $url, $kayitYolu, $dosyaBoyutu,
                     $kategori, $tur, $durum, $baslamaTarihi,
                     $tamamlanmaTarihi, $ortalamaHiz,
                     $indirmeSuresi, $denemeSayisi)";

                komut.Parameters.AddWithValue("$dosyaAdi", item.DosyaAdi);
                komut.Parameters.AddWithValue("$url", item.Url);
                komut.Parameters.AddWithValue("$kayitYolu", item.KayitYolu);
                komut.Parameters.AddWithValue("$dosyaBoyutu", item.DosyaBoyutu);
                komut.Parameters.AddWithValue("$kategori", item.Kategori);
                komut.Parameters.AddWithValue("$tur", item.Tur.ToString());
                komut.Parameters.AddWithValue("$durum", item.Durum.ToString());
                komut.Parameters.AddWithValue("$baslamaTarihi",
                    DateTime.Now.ToString("O"));
                komut.Parameters.AddWithValue("$tamamlanmaTarihi",
                    item.Durum == Durum.Tamamlandi
                        ? DateTime.Now.ToString("O")
                        : DBNull.Value);
                komut.Parameters.AddWithValue("$ortalamaHiz", item.OrtalamaHiz);
                komut.Parameters.AddWithValue("$indirmeSuresi",
                    item.IndirmeSuresi.TotalSeconds);
                komut.Parameters.AddWithValue("$denemeSayisi", item.DenemeSayisi);

                komut.ExecuteNonQuery();
            }
            catch { }
        }

        public static void KayitGuncelle(DownloadItem item)
        {
            try
            {
                using var baglanti = new SqliteConnection(BaglantiCumlesiBal);
                baglanti.Open();

                var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    UPDATE IndirmeGecmisi
                    SET Durum = $durum,
                        DosyaBoyutu = $dosyaBoyutu,
                        TamamlanmaTarihi = $tamamlanmaTarihi,
                        OrtalamaHiz = $ortalamaHiz,
                        IndirmeSuresi = $indirmeSuresi,
                        DenemeSayisi = $denemeSayisi
                    WHERE Url = $url AND DosyaAdi = $dosyaAdi
                    ORDER BY Id DESC
                    LIMIT 1";

                komut.Parameters.AddWithValue("$durum", item.Durum.ToString());
                komut.Parameters.AddWithValue("$dosyaBoyutu", item.DosyaBoyutu);
                komut.Parameters.AddWithValue("$tamamlanmaTarihi",
                    item.Durum == Durum.Tamamlandi
                        ? DateTime.Now.ToString("O")
                        : DBNull.Value);
                komut.Parameters.AddWithValue("$ortalamaHiz", item.OrtalamaHiz);
                komut.Parameters.AddWithValue("$indirmeSuresi",
                    item.IndirmeSuresi.TotalSeconds);
                komut.Parameters.AddWithValue("$denemeSayisi", item.DenemeSayisi);
                komut.Parameters.AddWithValue("$url", item.Url);
                komut.Parameters.AddWithValue("$dosyaAdi", item.DosyaAdi);

                komut.ExecuteNonQuery();
            }
            catch { }
        }

        public static List<IndirmeKaydi> GecmisGetir(
            string aramaMetni = "",
            string durumFiltre = "Tümü",
            int limit = 100)
        {
            var liste = new List<IndirmeKaydi>();

            try
            {
                using var baglanti = new SqliteConnection(BaglantiCumlesiBal);
                baglanti.Open();

                var sql = @"
                    SELECT Id, DosyaAdi, Url, KayitYolu,
                           DosyaBoyutu, Kategori, Tur, Durum,
                           BaslamaTarihi, TamamlanmaTarihi,
                           OrtalamaHiz, IndirmeSuresi, DenemeSayisi
                    FROM IndirmeGecmisi
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(aramaMetni))
                    sql += " AND (DosyaAdi LIKE $arama OR Url LIKE $arama)";

                if (durumFiltre != "Tümü")
                    sql += " AND Durum = $durum";

                sql += " ORDER BY Id DESC LIMIT $limit";

                var komut = baglanti.CreateCommand();
                komut.CommandText = sql;

                if (!string.IsNullOrEmpty(aramaMetni))
                    komut.Parameters.AddWithValue("$arama",
                        $"%{aramaMetni}%");

                if (durumFiltre != "Tümü")
                    komut.Parameters.AddWithValue("$durum", durumFiltre);

                komut.Parameters.AddWithValue("$limit", limit);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    var kayit = new IndirmeKaydi
                    {
                        Id = okuyucu.GetInt32(0),
                        DosyaAdi = okuyucu.GetString(1),
                        Url = okuyucu.GetString(2),
                        KayitYolu = okuyucu.IsDBNull(3)
                            ? "" : okuyucu.GetString(3),
                        DosyaBoyutu = okuyucu.GetInt64(4),
                        Kategori = okuyucu.IsDBNull(5)
                            ? "" : okuyucu.GetString(5),
                        Tur = okuyucu.IsDBNull(6)
                            ? "" : okuyucu.GetString(6),
                        Durum = okuyucu.GetString(7),
                        OrtalamaHiz = okuyucu.IsDBNull(10)
                            ? 0 : okuyucu.GetDouble(10),
                        IndirmeSuresi = okuyucu.IsDBNull(11)
                            ? 0 : okuyucu.GetDouble(11),
                        DenemeSayisi = okuyucu.IsDBNull(12)
                            ? 0 : okuyucu.GetInt32(12)
                    };

                    try
                    {
                        kayit.BaslamaTarihi = DateTime.Parse(
                            okuyucu.GetString(8));
                    }
                    catch { kayit.BaslamaTarihi = DateTime.Now; }

                    if (!okuyucu.IsDBNull(9))
                    {
                        try
                        {
                            kayit.TamamlanmaTarihi = DateTime.Parse(
                                okuyucu.GetString(9));
                        }
                        catch { }
                    }

                    liste.Add(kayit);
                }
            }
            catch { }

            return liste;
        }

        public static (long ToplamBoyut, int ToplamSayi,
            int BasariliSayi, double OrtalamaHiz) IstatistikGetir()
        {
            try
            {
                using var baglanti = new SqliteConnection(BaglantiCumlesiBal);
                baglanti.Open();

                var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT
                        COALESCE(SUM(DosyaBoyutu), 0),
                        COUNT(*),
                        SUM(CASE WHEN Durum = 'Tamamlandi' THEN 1 ELSE 0 END),
                        COALESCE(AVG(CASE WHEN OrtalamaHiz > 0
                            THEN OrtalamaHiz END), 0)
                    FROM IndirmeGecmisi";

                using var okuyucu = komut.ExecuteReader();
                if (okuyucu.Read())
                {
                    return (
                        okuyucu.GetInt64(0),
                        okuyucu.GetInt32(1),
                        okuyucu.GetInt32(2),
                        okuyucu.GetDouble(3)
                    );
                }
            }
            catch { }

            return (0, 0, 0, 0);
        }

        public static void KayitSil(int id)
        {
            try
            {
                using var baglanti = new SqliteConnection(BaglantiCumlesiBal);
                baglanti.Open();
                var komut = baglanti.CreateCommand();
                komut.CommandText =
                    "DELETE FROM IndirmeGecmisi WHERE Id = $id";
                komut.Parameters.AddWithValue("$id", id);
                komut.ExecuteNonQuery();
            }
            catch { }
        }

        public static void TumunuTemizle()
        {
            try
            {
                using var baglanti = new SqliteConnection(BaglantiCumlesiBal);
                baglanti.Open();
                var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM IndirmeGecmisi";
                komut.ExecuteNonQuery();
            }
            catch { }
        }
    }
}