using System;
using System.IO;

namespace PixelNeonDownloader
{
    public class YenidenDenemeAyarlari
    {
        public bool Aktif { get; set; } = true;
        public int MaksDenemeSayisi { get; set; } = 3;
        public int BeklemeAraligi { get; set; } = 5; // saniye
        public bool ArtanBekleme { get; set; } = true; // 5s, 10s, 20s...

        private static readonly string _ayarYolu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "yeniden_deneme.json");

        public static YenidenDenemeAyarlari Yukle()
        {
            try
            {
                if (File.Exists(_ayarYolu))
                {
                    var json = File.ReadAllText(_ayarYolu);
                    var ayarlar = System.Text.Json.JsonSerializer
                        .Deserialize<YenidenDenemeAyarlari>(json);
                    return ayarlar ?? new YenidenDenemeAyarlari();
                }
            }
            catch { }
            return new YenidenDenemeAyarlari();
        }

        public void Kaydet()
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(_ayarYolu)!);
                var json = System.Text.Json.JsonSerializer.Serialize(this,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                File.WriteAllText(_ayarYolu, json);
            }
            catch { }
        }
    }
}