using System;
using System.IO;
using System.Media;

namespace PixelNeonDownloader
{
    public static class SesYoneticisi
    {
        private static bool _sesAcik = true;
        private static SoundPlayer? _tamamlandiPlayer;
        private static SoundPlayer? _hataPlayer;

        // Tamamlanma sesi
        public static void TamamlandiSesi()
        {
            if (!_sesAcik) return;
            try
            {
                var sesDosyasi = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Sounds", "tamamlandi.wav");

                if (File.Exists(sesDosyasi))
                {
                    if (_tamamlandiPlayer == null)
                    {
                        _tamamlandiPlayer = new SoundPlayer(sesDosyasi);
                        _tamamlandiPlayer.Load();
                    }
                    _tamamlandiPlayer.Play();
                }
                else
                {
                    SystemSounds.Asterisk.Play();
                }
            }
            catch { }
        }

        // Hata sesi
        public static void HataSesi()
        {
            if (!_sesAcik) return;
            try
            {
                var sesDosyasi = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Sounds", "hata.wav");

                if (File.Exists(sesDosyasi))
                {
                    if (_hataPlayer == null)
                    {
                        _hataPlayer = new SoundPlayer(sesDosyasi);
                        _hataPlayer.Load();
                    }
                    _hataPlayer.Play();
                }
                else
                {
                    SystemSounds.Hand.Play();
                }
            }
            catch { }
        }

        // Ekleme sesi
        public static void EklenmeSesi()
        {
            if (!_sesAcik) return;
            try
            {
                SystemSounds.Beep.Play();
            }
            catch { }
        }

        // Ses ayarını kaydet
        public static void SesAyariniKaydet(bool acik)
        {
            try
            {
                _sesAcik = acik;

                var ayarDosyasi = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "PixelNeonDownloader", "ayarlar.json");

                Directory.CreateDirectory(
                    Path.GetDirectoryName(ayarDosyasi)!);

                File.WriteAllText(ayarDosyasi, acik ? "true" : "false");
            }
            catch { }
        }

        // Ses açık mı?
        public static bool SesAcikMi()
        {
            try
            {
                var ayarDosyasi = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "PixelNeonDownloader", "ayarlar.json");

                if (!File.Exists(ayarDosyasi)) return true;

                var deger = File.ReadAllText(ayarDosyasi).Trim();
                _sesAcik = deger != "false";
                return _sesAcik;
            }
            catch { return true; }
        }
    }
}