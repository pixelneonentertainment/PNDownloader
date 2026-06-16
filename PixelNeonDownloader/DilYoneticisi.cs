using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PixelNeonDownloader
{
    public static class DilYoneticisi
    {
        private static string _mevcutDil = "TR";

        private static readonly string _ayarYolu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "dil.txt");

        private static readonly Dictionary<string, Dictionary<string, string>> _sozluk = new()
        {
            ["TR"] = new Dictionary<string, string>
            {
                // Ana Ekran ve Genel Çeviriler
                ["yeni_indirme"] = "YENİ İNDİRME",
                ["yeni_kisa"] = "YENİ",
                ["toplu_kisa"] = "TOPLU",
                ["ultra_kisa"] = "ULTRA",
                ["dur_kisa"] = "DUR",
                ["durdur"] = "DURDUR",
                ["devam"] = "DEVAM",
                ["iptal"] = "İPTAL",
                ["ayarlar"] = "Ayarlar",
                ["istatistikler"] = "İstatistikler",
                ["klasoru_ac"] = "Klasörü Aç",
                ["filtre"] = "FİLTRE:",
                ["tumu"] = "TÜMÜ",
                ["indiriliyor"] = "İNDİRİLİYOR",
                ["tamamlandi"] = "TAMAMLANDI",
                ["duraklatildi"] = "DURAKLATILDI",
                ["hata"] = "HATA",
                ["hazir"] = "Hazır",
                ["ara"] = "Ara...",
                ["dosya_adi"] = "DOSYA ADI",
                ["boyut"] = "BOYUT",
                ["ilerleme"] = "İLERLEME",
                ["hiz"] = "HIZ",
                ["kalan"] = "KALAN",
                ["durum"] = "DURUM",
                ["tur"] = "TÜR",
                ["devam_et"] = "Devam Et",
                ["duraklat"] = "Duraklat",
                ["url_kopyala"] = "URL'yi Kopyala",
                ["zamanla"] = "Zamanla",
                ["kaynak_izle"] = "Kaynak İzle",
                ["checksum"] = "Checksum Kontrol",
                ["kaldir"] = "Listeden Kaldır",
                ["indirme_tamamlandi"] = "İndirme Tamamlandı",
                ["indirme_hatasi"] = "İndirme Hatası",
                ["basarıyla_indirildi"] = "başarıyla indirildi!",
                ["indirilemedi"] = "indirilemedi.",
                ["lutfen_secin"] = "Lütfen bir indirme seçin.",
                ["pano_algilandi"] = "Kopyalanan link algılandı:",
                ["indir"] = "İNDİR",
                ["kapat"] = "KAPAT",
                ["kaydet"] = "KAYDET",
                ["guncel"] = "GÜNCEL",
                ["maksimum"] = "MAKSİMUM",
                ["aktif"] = "aktif",
                ["hazir_durum"] = "Hazır",
                ["indirme_aktif"] = "indirme aktif",
                ["baslatildi"] = "Başlatıldı:",
                ["duraklatildi_mesaj"] = "duraklatıldı.",
                ["devam_ediyor"] = "devam ediyor.",
                ["kaldirildi"] = "İndirme kaldırıldı.",
                ["url_kopyalandi"] = "URL kopyalandı.",
                ["klasor_bulunamadi"] = "Klasör bulunamadı.",
                ["panodan_eklendi"] = "Panodan eklendi:",
                ["liste_yenilendi"] = "Liste yenilendi.",
                ["sonuc"] = "sonuç",
                ["gecen"] = "GEÇEN",

                // Ayarlar Ekranı Yeni Sekme Başlıkları
                ["sistem_ayarlari"] = "SİSTEM AYARLARI",
                ["genel"] = "GENEL",
                ["indirme"] = "İNDİRME",
                ["gorunum"] = "GÖRÜNÜM",
                ["baglanti"] = "BAĞLANTI",
                ["gelismis_tab"] = "GELİŞMİŞ",

                // Ayarlar - Genel Paneli
                ["varsayilan_klasor"] = "VARSAYILAN İNDİRME KLASÖRÜ",
                ["sec"] = "SEÇ",
                ["baslangic_calistir"] = "SİSTEM BAŞLANGICINDA ÇALIŞTIR",
                ["baslangic_detay"] = "Bilgisayar açıldığında Pixel Neon otomatik olarak başlasın.",
                ["tepsi_kucult"] = "SİSTEM TEPSİSİNE KÜÇÜLT (TRAY)",
                ["tepsi_detay"] = "Kapatıldığında veya simge durumuna alındığında arka planda çalışmaya devam et.",
                ["ses_etkinlestir"] = "SES EFEKTLERİNİ ETKİNLEŞTİR",
                ["ses_detay"] = "İndirmeler tamamlandığında veya hata oluştuğunda sesli bildirim çal.",

                // Ayarlar - İndirme & Ağ Panelleri
                ["bant_limiti"] = "BANT GENİŞLİĞİ LİMİTİ",
                ["maks_hiz_baslik"] = "MAKSİMUM İNDİRME HIZI",
                ["disk_onbellek_baslik"] = "DİSK YAZMA ÖNBELLEĞİ (RAM CACHE)",
                ["onbellek_detay"] = "Önbellek boyutunu yükseltmek disk performansını artırır ancak RAM kullanımını hafifçe yükseltir.",
                ["akilli_kategori"] = "AKILLI KATEGORİ",
                ["dusuk_disk"] = "DÜŞÜK DİSK MODU",
                ["arsivleri_cikart"] = "ARŞİVLERİ ÇIKART",
                ["tema_secin"] = "PARLAK NEON TEMALARI SEÇİN",
                ["aktif_dil"] = "Aktif dil",

                // Ayarlar - Bağlantı & Gelişmiş Panelleri
                ["proxy_yapilandir"] = "PROXY SUNUCUSUNU YAPILANDIR",
                ["bağlanti_proxy"] = "BAĞLANTI ve PROXY YAPILANDIRMASI",
                ["yapay_zeka_baslik"] = "YAPAY ZEKA (AI) İNDİRME ASİSTANI",
                ["ai_asistan_ac"] = "AI DESTEKLİ İNDİRME ASİSTANINI AÇ",
                ["sistem_veri_yonetimi"] = "SİSTEM VE VERİ YÖNETİMİ",
                ["gecmisi_ac"] = "Geçmişi Aç",
                ["ultra_hiz"] = "Ultra Hız",
                ["bulut_depo"] = "Bulut Depo",
                ["ftp_tarayici"] = "FTP Tarayıcı",
                ["kisayollar"] = "Kısayollar",
                ["oto_yenile"] = "Oto Yenile",
                ["dil_secenekleri"] = "DİL SEÇENEKLERİ (LANGUAGE)",
                ["iptal_button"] = "İPTAL",
                ["kaydet_button"] = "KAYDET"
            },

            ["EN"] = new Dictionary<string, string>
            {
                // Ana Ekran ve Genel Çeviriler
                ["yeni_indirme"] = "NEW DOWNLOAD",
                ["yeni_kisa"] = "NEW",
                ["toplu_kisa"] = "BULK",
                ["ultra_kisa"] = "ULTRA",
                ["dur_kisa"] = "PAUSE",
                ["durdur"] = "PAUSE",
                ["devam"] = "RESUME",
                ["iptal"] = "CANCEL",
                ["ayarlar"] = "Settings",
                ["istatistikler"] = "Statistics",
                ["klasoru_ac"] = "Open Folder",
                ["filtre"] = "FILTER:",
                ["tumu"] = "ALL",
                ["indiriliyor"] = "DOWNLOADING",
                ["tamamlandi"] = "COMPLETED",
                ["duraklatildi"] = "PAUSED",
                ["hata"] = "ERROR",
                ["hazir"] = "Ready",
                ["ara"] = "Search...",
                ["dosya_adi"] = "FILE NAME",
                ["boyut"] = "SIZE",
                ["ilerleme"] = "PROGRESS",
                ["hiz"] = "SPEED",
                ["kalan"] = "REMAINING",
                ["durum"] = "STATUS",
                ["tur"] = "TYPE",
                ["devam_et"] = "Resume",
                ["duraklat"] = "Pause",
                ["url_kopyala"] = "Copy URL",
                ["zamanla"] = "Schedule",
                ["kaynak_izle"] = "Monitor Source",
                ["checksum"] = "Checksum Check",
                ["kaldir"] = "Remove from List",
                ["indirme_tamamlandi"] = "Download Complete",
                ["indirme_hatasi"] = "Download Error",
                ["basarıyla_indirildi"] = "downloaded successfully!",
                ["indirilemedi"] = "could not be downloaded.",
                ["lutfen_secin"] = "Please select a download.",
                ["pano_algilandi"] = "Clipboard link detected:",
                ["indir"] = "DOWNLOAD",
                ["kapat"] = "CLOSE",
                ["kaydet"] = "SAVE",
                ["guncel"] = "CURRENT",
                ["maksimum"] = "MAXIMUM",
                ["aktif"] = "active",
                ["hazir_durum"] = "Ready",
                ["indirme_aktif"] = "downloads active",
                ["baslatildi"] = "Started:",
                ["duraklatildi_mesaj"] = "paused.",
                ["devam_ediyor"] = "resuming.",
                ["kaldirildi"] = "Download removed.",
                ["url_kopyalandi"] = "URL copied.",
                ["klasor_bulunamadi"] = "Folder not found.",
                ["panodan_eklendi"] = "Added from clipboard:",
                ["liste_yenilendi"] = "List refreshed.",
                ["sonuc"] = "results",
                ["gecen"] = "ELAPSED",

                // Ayarlar Ekranı Yeni Sekme Başlıkları
                ["sistem_ayarlari"] = "SYSTEM SETTINGS",
                ["genel"] = "GENERAL",
                ["indirme"] = "DOWNLOADS",
                ["gorunum"] = "APPEARANCE",
                ["baglanti"] = "CONNECTION",
                ["gelismis_tab"] = "ADVANCED",

                // Ayarlar - Genel Paneli
                ["varsayilan_klasor"] = "DEFAULT DOWNLOAD DIRECTORY",
                ["sec"] = "SELECT",
                ["baslangic_calistir"] = "RUN AT SYSTEM STARTUP",
                ["baslangic_detay"] = "Launch Pixel Neon automatically when your computer starts.",
                ["tepsi_kucult"] = "MINIMIZE TO SYSTEM TRAY",
                ["tepsi_detay"] = "Continue running in the background when closed or minimized.",
                ["ses_etkinlestir"] = "ENABLE SOUND EFFECTS",
                ["ses_detay"] = "Play a sound notification when downloads complete or fail.",

                // Ayarlar - İndirme & Ağ Panelleri
                ["bant_limiti"] = "BANDWIDTH LIMIT",
                ["maks_hiz_baslik"] = "MAXIMUM DOWNLOAD SPEED",
                ["disk_onbellek_baslik"] = "DISK WRITE CACHE",
                ["onbellek_detay"] = "Increasing the cache improves disk performance but slightly increases RAM usage.",
                ["akilli_kategori"] = "SMART CATEGORY",
                ["dusuk_disk"] = "LOW DISK MODE",
                ["arsivleri_cikart"] = "EXTRACT ARCHIVES",
                ["tema_secin"] = "SELECT GLOWING NEON THEME",
                ["aktif_dil"] = "Active language",

                // Ayarlar - Bağlantı & Gelişmiş Panelleri
                ["proxy_yapilandir"] = "CONFIGURE PROXY SERVER",
                ["bağlanti_proxy"] = "CONNECTION & PROXY CONFIGURATION",
                ["yapay_zeka_baslik"] = "ARTIFICIAL INTELLIGENCE (AI) ASSISTANT",
                ["ai_asistan_ac"] = "OPEN AI DOWNLOAD ASSISTANT",
                ["sistem_veri_yonetimi"] = "SYSTEM & DATA MANAGEMENT",
                ["gecmisi_ac"] = "Open History",
                ["ultra_hiz"] = "Ultra Speed",
                ["bulut_depo"] = "Cloud Storage",
                ["ftp_tarayici"] = "FTP Browser",
                ["kisayollar"] = "Shortcuts",
                ["oto_yenile"] = "Auto Refresh",
                ["dil_secenekleri"] = "LANGUAGE OPTIONS",
                ["iptal_button"] = "CANCEL",
                ["kaydet_button"] = "SAVE"
            },

            ["DE"] = new Dictionary<string, string>
            {
                // Ana Ekran ve Genel Çeviriler
                ["yeni_indirme"] = "NEU HERUNTERLADEN",
                ["yeni_kisa"] = "NEU",
                ["toplu_kisa"] = "BATCH",
                ["ultra_kisa"] = "ULTRA",
                ["dur_kisa"] = "PAUSE",
                ["durdur"] = "PAUSIEREN",
                ["devam"] = "FORTSETZEN",
                ["iptal"] = "ABBRECHEN",
                ["ayarlar"] = "Einstellungen",
                ["istatistikler"] = "Statistiken",
                ["klasoru_ac"] = "Ordner öffnen",
                ["filtre"] = "FILTER:",
                ["tumu"] = "ALLE",
                ["indiriliyor"] = "WIRD GELADEN",
                ["tamamlandi"] = "ABGESCHLOSSEN",
                ["duraklatildi"] = "PAUSIERT",
                ["hata"] = "FEHLER",
                ["hazir"] = "Bereit",
                ["ara"] = "Suchen...",
                ["dosya_adi"] = "DATEINAME",
                ["boyut"] = "GRÖßE",
                ["ilerleme"] = "FORTSCHRITT",
                ["hiz"] = "GESCHWINDIGKEIT",
                ["kalan"] = "VERBLEIBEND",
                ["durum"] = "STATUS",
                ["tur"] = "TYP",
                ["devam_et"] = "Fortsetzen",
                ["duraklat"] = "Pausieren",
                ["url_kopyala"] = "URL kopieren",
                ["zamanla"] = "Planen",
                ["kaynak_izle"] = "Quelle überwachen",
                ["checksum"] = "Prüfsumme",
                ["kaldir"] = "Aus Liste entfernen",
                ["indirme_tamamlandi"] = "Download abgeschlossen",
                ["indirme_hatasi"] = "Download-Fehler",
                ["basarıyla_indirildi"] = "erfolgreich heruntergeladen!",
                ["indirilemedi"] = "konnte nicht heruntergeladen werden.",
                ["lutfen_secin"] = "Bitte einen Download auswählen.",
                ["pano_algilandi"] = "Zwischenablage-Link erkannt:",
                ["indir"] = "HERUNTERLADEN",
                ["kapat"] = "SCHLIEßEN",
                ["kaydet"] = "SPEICHERN",
                ["guncel"] = "AKTUELL",
                ["maksimum"] = "MAXIMUM",
                ["aktif"] = "aktiv",
                ["hazir_durum"] = "Bereit",
                ["indirme_aktif"] = "Downloads aktif",
                ["baslatildi"] = "Gestartet:",
                ["duraklatildi_mesaj"] = "pausiert.",
                ["devam_ediyor"] = "wird fortgesetzt.",
                ["kaldirildi"] = "Download entfernt.",
                ["url_kopyalandi"] = "URL kopiert.",
                ["klasor_bulunamadi"] = "Ordner nicht gefunden.",
                ["panodan_eklendi"] = "Aus Zwischenablage hinzugefügt:",
                ["liste_yenilendi"] = "Liste aktualisiert.",
                ["sonuc"] = "Ergebnisse",
                ["gecen"] = "ABGELAUFEN",

                // Ayarlar Ekranı Yeni Sekme Başlıkları
                ["sistem_ayarlari"] = "SYSTEMEINSTELLUNGEN",
                ["genel"] = "ALLGEMEIN",
                ["indirme"] = "DOWNLOADS",
                ["gorunum"] = "ANSICHT",
                ["baglanti"] = "VERBINDUNG",
                ["gelismis_tab"] = "ERWEITERT",

                // Ayarlar - Genel Paneli
                ["varsayilan_klasor"] = "STANDARD-DOWNLOAD-ORDNER",
                ["sec"] = "WÄHLEN",
                ["baslangic_calistir"] = "BEIM SYSTEMSTART AUSFÜHREN",
                ["baslangic_detay"] = "Pixel Neon beim Systemstart automatisch starten.",
                ["tepsi_kucult"] = "IN SYSTEMTRAY MINIMIEREN",
                ["tepsi_detay"] = "Beim Schließen im Hintergrund weiterlaufen.",
                ["ses_etkinlestir"] = "SOUNDEFFEKTE AKTIVIEREN",
                ["ses_detay"] = "Sound abspielen, wenn Downloads abgeschlossen sind.",

                // Ayarlar - İndirme & Ağ Panelleri
                ["bant_limiti"] = "BANDBREITENBEGRENZUNG",
                ["maks_hiz_baslik"] = "MAXIMALE DOWNLOAD-GESCHWINDIGKEIT",
                ["disk_onbellek_baslik"] = "SCHREIBPuffer (RAM CACHE)",
                ["onbellek_detay"] = "Erhöhen des Puffers verbessert die Festplattenleistung.",
                ["akilli_kategori"] = "INTELLIGENTE KATEGORIE",
                ["dusuk_disk"] = "GERINGER SPEICHER-MODUS",
                ["arsivleri_cikart"] = "ARCHIVE ENTPACKEN",
                ["tema_secin"] = "WÄHLEN SIE EIN GLOW-NEON-DESIGN",
                ["aktif_dil"] = "Aktive Sprache",

                // Ayarlar - Bağlantı & Gelişmiş Panelleri
                ["proxy_yapilandir"] = "PROXY-SERVER KONFIGURIEREN",
                ["bağlanti_proxy"] = "VERBINDUNGS- & PROXYKONFIGURATION",
                ["yapay_zeka_baslik"] = "KÜNSTLICHE INTELLIGENZ (AI) ASSISTENT",
                ["ai_asistan_ac"] = "KI-DOWNLOAD-ASSISTENTEN ÖFFNEN",
                ["sistem_veri_yonetimi"] = "SYSTEM- & DATENVERWALTUNG",
                ["gecmisi_ac"] = "Verlauf öffnen",
                ["ultra_hiz"] = "Ultra-Geschwindigkeit",
                ["bulut_depo"] = "Cloud-Speicher",
                ["ftp_tarayici"] = "FTP-Browser",
                ["kisayollar"] = "Tastenkombinationen",
                ["oto_yenile"] = "Auto-Aktualisierung",
                ["dil_secenekleri"] = "SPRACHOPTIONEN",
                ["iptal_button"] = "ABBRECHEN",
                ["kaydet_button"] = "SPEICHERN"
            }
        };

        public static void DilYukle()
        {
            try
            {
                if (File.Exists(_ayarYolu))
                {
                    var ad = File.ReadAllText(_ayarYolu).Trim().ToUpperInvariant();
                    if (ad == "GB") ad = "EN";
                    if (_sozluk.ContainsKey(ad))
                        _mevcutDil = ad;
                }
            }
            catch { _mevcutDil = "TR"; }
        }

        public static void DilDegistir(string dil)
        {
            var ad = dil.Trim().ToUpperInvariant();
            if (ad == "GB") ad = "EN";
            _mevcutDil = ad;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ayarYolu)!);
                File.WriteAllText(_ayarYolu, ad);
            }
            catch { }
        }

        public static string Al(string anahtar)
        {
            if (_sozluk.TryGetValue(_mevcutDil, out var dilSozluk))
                if (dilSozluk.TryGetValue(anahtar, out var deger))
                    return deger;
            return anahtar;
        }

        // ── SIFIR XAML: Güvenli ve Bellek Korumalı Hibrit Çeviri Motoru (WPF Ağaç Tarayıcı) ──
        public static void PencereyiCevir(DependencyObject anaOge)
        {
            // Bellek taşmalarını (StackOverflowException) önlemek için ziyaret takipçisi
            var ziyaretEdilenler = new HashSet<DependencyObject>();
            PencereyiCevirInternal(anaOge, ziyaretEdilenler);
        }

        private static void PencereyiCevirInternal(DependencyObject anaOge, HashSet<DependencyObject> ziyaretEdilenler)
        {
            if (anaOge == null) return;

            // Eğer bu nesne daha önce tarandıysa, sonsuz döngüyü kesmek için çık (StackOverflow Kalkanı)
            if (!ziyaretEdilenler.Add(anaOge)) return;

            // 1. Kontrolün Kendisini Çevir (TextBlock, Button, MenuItem vb.)
            if (anaOge is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
            {
                var anahtar = TurkceMetindenAnahtarBul(tb.Text);
                if (anahtar != null) tb.Text = Al(anahtar);
            }
            else if (anaOge is System.Windows.Controls.Button btn && btn.Content is string btnMetin && !string.IsNullOrEmpty(btnMetin))
            {
                var anahtar = TurkceMetindenAnahtarBul(btnMetin);
                if (anahtar != null) btn.Content = Al(anahtar);
            }
            else if (anaOge is System.Windows.Controls.MenuItem mi && mi.Header is string miMetin && !string.IsNullOrEmpty(miMetin))
            {
                var anahtar = TurkceMetindenAnahtarBul(miMetin);
                if (anahtar != null) mi.Header = Al(anahtar);
            }

            // 2. Mantıksal Ağacı (Logical Tree) tara - GİZLİ (Collapsed) tüm sekmeleri yakalar!
            try
            {
                foreach (var oge in LogicalTreeHelper.GetChildren(anaOge))
                {
                    if (oge is DependencyObject depObj)
                    {
                        PencereyiCevirInternal(depObj, ziyaretEdilenler);
                    }
                }
            }
            catch { }

            // 3. Görsel Ağacı (Visual Tree) tara - Çizilen alt şablonları yakalar!
            try
            {
                int cocukSayisi = VisualTreeHelper.GetChildrenCount(anaOge);
                for (int i = 0; i < cocukSayisi; i++)
                {
                    var cocuk = VisualTreeHelper.GetChild(anaOge, i);
                    PencereyiCevirInternal(cocuk, ziyaretEdilenler);
                }
            }
            catch { }
        }

        private static string? TurkceMetindenAnahtarBul(string metin)
        {
            var temizMetin = AlfanumerikTemizle(metin);
            if (string.IsNullOrEmpty(temizMetin)) return null;

            foreach (var oge in _sozluk["TR"])
            {
                var trDeger = AlfanumerikTemizle(oge.Value);
                if (trDeger == temizMetin)
                    return oge.Key;
            }

            return null;
        }

        private static string AlfanumerikTemizle(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in metin)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().ToUpperInvariant();
        }

        public static string MevcutDil => _mevcutDil;
        public static string[] DesteklenenDiller => new[] { "TR", "EN", "DE" };

        public static string DilAdi(string kod) => kod switch
        {
            "TR" => "🇹🇷 Türkçe",
            "EN" or "GB" => "🇬🇧 English",
            "DE" => "🇩🇪 Deutsch",
            _ => kod
        };
    }
}