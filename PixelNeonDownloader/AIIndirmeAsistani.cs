using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PixelNeonDownloader
{
    public class AIAnalizSonucu
    {
        public string DosyaTuru { get; set; } = "";
        public string OnerigenKlasor { get; set; } = "";
        public int OnerilienParcaSayisi { get; set; } = 8;
        public string Aciklama { get; set; } = "";
        public string RiskSeviyesi { get; set; } = "Düşük";
        public string[] Etiketler { get; set; } = Array.Empty<string>();
        public bool OtomatikCikart { get; set; } = false;
        public string Oneri { get; set; } = "";
    }

    public class AITemizlikOneri
    {
        public string DosyaAdi { get; set; } = "";
        public string DosyaYolu { get; set; } = "";
        public string Neden { get; set; } = "";
        public long Boyut { get; set; }
        public string OncelikSeviyesi { get; set; } = "Orta";
    }

    public static class AIIndirmeAsistani
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string API_URL = "https://api.anthropic.com/v1/messages";
        private const string MODEL = "claude-3-5-sonnet-20241022"; // Kararlı ve güncel model kimliği tanımlandı

        public static async Task<AIAnalizSonucu> URLAnalizEt(string url)
        {
            var prompt = $@"Sen bir indirme yöneticisi asistanısın. 
Verilen URL'yi analiz et ve JSON formatında yanıt ver.

URL: {url}

Şu bilgileri belirle:
1. DosyaTuru: Video, Müzik, Yazılım, Oyun, Belge, Arşiv, Torrent, Resim, Diğer
2. OnerigenKlasor: Uygun klasör adı (Türkçe, örn: Videolar, Yazılımlar)
3. OnerilienParcaSayisi: 1-16 arası optimal parça sayısı (küçük dosya=1, büyük=8-16)
4. Aciklama: Kısa açıklama (max 50 karakter)
5. RiskSeviyesi: Düşük, Orta, Yüksek
6. Etiketler: Max 3 etiket dizisi
7. OtomatikCikart: true/false (arşiv ise true)
8. Oneri: Kullanıcıya kısa öneri (max 80 karakter)

SADECE geçerli JSON döndür, başka hiçbir şey yazma:
{{
  ""DosyaTuru"": ""..."",
  ""OnerigenKlasor"": ""..."",
  ""OnerilienParcaSayisi"": 8,
  ""Aciklama"": ""..."",
  ""RiskSeviyesi"": ""..."",
  ""Etiketler"": [""...""],
  ""OtomatikCikart"": false,
  ""Oneri"": ""...""
}}";

            var json = await APIGonder(prompt);
            if (json == null) return VarsayilanAnalizDondur(url);

            try
            {
                var sonuc = JsonSerializer.Deserialize<AIAnalizSonucu>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                return sonuc ?? VarsayilanAnalizDondur(url);
            }
            catch
            {
                return VarsayilanAnalizDondur(url);
            }
        }

        public static async Task<AITemizlikOneri[]> TemizlikAnalizEt(
            System.Collections.Generic.List<DownloadItem> indirmeler)
        {
            if (indirmeler.Count == 0)
                return Array.Empty<AITemizlikOneri>();

            var dosyaBilgileri = new StringBuilder();
            foreach (var item in indirmeler)
            {
                if (item.Durum != Durum.Tamamlandi) continue;

                var dosyaYolu = System.IO.Path.Combine(item.KayitYolu, item.DosyaAdi);

                DateTime sonErisim = DateTime.Now;
                long boyut = item.DosyaBoyutu;

                try
                {
                    if (System.IO.File.Exists(dosyaYolu))
                    {
                        var bilgi = new System.IO.FileInfo(dosyaYolu);
                        sonErisim = bilgi.LastAccessTime;
                        boyut = bilgi.Length;
                    }
                }
                catch { }

                var gunFarki = (DateTime.Now - sonErisim).Days;
                dosyaBilgileri.AppendLine(
                    $"- {item.DosyaAdi} | {ByteFormatla(boyut)} | " +
                    $"Son erişim: {gunFarki} gün önce | " +
                    $"Kategori: {item.Kategori}");
            }

            if (dosyaBilgileri.Length == 0)
                return Array.Empty<AITemizlikOneri>();

            var prompt = $@"Sen bir disk temizlik asistanısın.
Aşağıdaki indirilen dosyaları analiz et ve silinmesi önerilebilecekleri belirle.

Dosyalar:
{dosyaBilgileri}

Kriterler:
- 30+ gün erişilmemiş büyük dosyalar
- Geçici veya kurulum dosyaları (setup, installer vb.)
- Yedek/tekrar indirilebilir dosyalar

SADECE geçerli JSON dizisi döndür:
[
  {{
    ""DosyaAdi"": ""..."",
    ""Neden"": ""..."",
    ""OncelikSeviyesi"": ""Yüksek/Orta/Düşük""
  }}
]

Eğer silinecek dosya yoksa boş dizi döndür: []";

            var json = await APIGonder(prompt);
            if (json == null) return Array.Empty<AITemizlikOneri>();

            try
            {
                if (json.Trim() == "[]")
                    return Array.Empty<AITemizlikOneri>();

                var sonuclar = JsonSerializer.Deserialize<AITemizlikOneri[]>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (sonuclar != null)
                {
                    foreach (var oneri in sonuclar)
                    {
                        var eslesen = indirmeler.Find(i => i.DosyaAdi == oneri.DosyaAdi);
                        if (eslesen != null)
                        {
                            oneri.DosyaYolu = System.IO.Path.Combine(eslesen.KayitYolu, eslesen.DosyaAdi);
                            oneri.Boyut = eslesen.DosyaBoyutu;
                        }
                    }
                }

                return sonuclar ?? Array.Empty<AITemizlikOneri>();
            }
            catch
            {
                return Array.Empty<AITemizlikOneri>();
            }
        }

        private static async Task<string?> APIGonder(string prompt)
        {
            try
            {
                var istek = new
                {
                    model = MODEL,
                    max_tokens = 1000,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var json = JsonSerializer.Serialize(istek);
                var icerik = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-api-key", APIAnahtarYoneticisi.AnahtarAl());
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var yanit = await _httpClient.PostAsync(API_URL, icerik);

                if (!yanit.IsSuccessStatusCode) return null;

                var yanitJson = await yanit.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(yanitJson);

                var metin = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                if (metin == null) return null;
                metin = metin.Trim();

                // Hata toleranslı JSON Ayıklama: Yapay zekanın ürettiği metin içindeki geçerli JSON sınırlarını dinamik olarak yakalar
                int baslangic = metin.IndexOfAny(new char[] { '{', '[' });
                int bitis = metin.LastIndexOfAny(new char[] { '}', ']' });

                if (baslangic != -1 && bitis > baslangic)
                {
                    metin = metin.Substring(baslangic, bitis - baslangic + 1);
                }

                return metin.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static AIAnalizSonucu VarsayilanAnalizDondur(string url)
        {
            var uzanti = System.IO.Path.GetExtension(url).ToLowerInvariant();
            return new AIAnalizSonucu
            {
                DosyaTuru = "Genel",
                OnerigenKlasor = "Diğer",
                OnerilienParcaSayisi = 8,
                Aciklama = "Analiz yapılamadı",
                RiskSeviyesi = "Düşük",
                Etiketler = new[] { "genel" },
                OtomatikCikart = uzanti is ".zip" or ".rar" or ".7z",
                Oneri = "Manuel ayarlar kullanılacak"
            };
        }

        private static string ByteFormatla(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] ekler = { "B", "KB", "MB", "GB" };
            int i = 0;
            double boyut = bytes;
            while (boyut >= 1024 && i < ekler.Length - 1) { boyut /= 1024; i++; }
            return $"{boyut:F1} {ekler[i]}";
        }
    }
}