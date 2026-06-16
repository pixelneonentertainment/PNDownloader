using System;
using System.IO;
using System.Net;
using System.Text.Json;

namespace PixelNeonDownloader
{
    public class ProxyAyarlari
    {
        public bool AktifMi { get; set; } = false;
        public string Adres { get; set; } = "";
        public int Port { get; set; } = 8080;
        public bool KimlikDogrulamaMi { get; set; } = false;
        public string KullaniciAdi { get; set; } = "";
        public string Sifre { get; set; } = "";
        public bool SslAtlaMi { get; set; } = false;

        private static readonly string _kayitYolu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "proxy.json");

        public static ProxyAyarlari Yukle()
        {
            try
            {
                if (!File.Exists(_kayitYolu))
                    return new ProxyAyarlari();

                var json = File.ReadAllText(_kayitYolu);
                return JsonSerializer.Deserialize<ProxyAyarlari>(json)
                       ?? new ProxyAyarlari();
            }
            catch { return new ProxyAyarlari(); }
        }

        public void Kaydet()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_kayitYolu)!);
                var json = JsonSerializer.Serialize(this,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_kayitYolu, json);
            }
            catch { }
        }

        public WebProxy? WebProxyOlustur()
        {
            if (!AktifMi || string.IsNullOrEmpty(Adres))
                return null;

            try
            {
                var proxy = new WebProxy($"{Adres}:{Port}", false);

                if (KimlikDogrulamaMi &&
                    !string.IsNullOrEmpty(KullaniciAdi))
                {
                    proxy.Credentials = new NetworkCredential(
                        KullaniciAdi, Sifre);
                }

                return proxy;
            }
            catch { return null; }
        }
    }
}