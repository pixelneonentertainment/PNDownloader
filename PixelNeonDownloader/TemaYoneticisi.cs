using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace PixelNeonDownloader
{
    public class Tema
    {
        public string Ad { get; set; } = "";
        public string AnaRenk { get; set; } = "#00FFE5";
        public string IkinciRenk { get; set; } = "#BD00FF";
        public string VurguRenk { get; set; } = "#39FF14";
        // Ek renkler: temaya daha fazla vurgu / ton katmak için
        public string EkRenk1 { get; set; } = "#00A7A0";
        public string EkRenk2 { get; set; } = "#0077CC";
        public string EkRenk3 { get; set; } = "#FFAA00";
        public string EkRenk4 { get; set; } = "#CC0077";
        public string HataRenk { get; set; } = "#FF2244";
        public string ArkaplanRenk { get; set; } = "#050810";
        public string PanelRenk { get; set; } = "#0A0F1E";
        public string IkonMetin { get; set; } = "◈";
    }

    public static class TemaYoneticisi
    {
        private static readonly string _ayarYolu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelNeonDownloader", "tema.txt");

        private static string _mevcutTemaAdi = "Cyan";

        public static readonly Dictionary<string, Tema> Temalar = new()
        {
            ["Cyan"] = new Tema
            {
                Ad = "Cyan",
                AnaRenk = "#00FFE5",
                IkinciRenk = "#BD00FF",
                VurguRenk = "#39FF14",
                EkRenk1 = "#00B5A8",
                EkRenk2 = "#0077CC",
                EkRenk3 = "#00FFC2",
                EkRenk4 = "#22D1FF",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#050810",
                PanelRenk = "#0A0F1E",
                IkonMetin = "◈"
            },
            ["Pink"] = new Tema
            {
                Ad = "Pink",
                AnaRenk = "#FF006E",
                IkinciRenk = "#FF00CC",
                VurguRenk = "#FFD700",
                EkRenk1 = "#FF77AA",
                EkRenk2 = "#FF4488",
                EkRenk3 = "#FFC0D0",
                EkRenk4 = "#FF88CC",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#0A0510",
                PanelRenk = "#150A1E",
                IkonMetin = "◆"
            },
            ["Purple"] = new Tema
            {
                Ad = "Purple",
                AnaRenk = "#BD00FF",
                IkinciRenk = "#7700FF",
                VurguRenk = "#00FFE5",
                EkRenk1 = "#AA66FF",
                EkRenk2 = "#8844FF",
                EkRenk3 = "#CC99FF",
                EkRenk4 = "#6600CC",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#060510",
                PanelRenk = "#0D0A1E",
                IkonMetin = "◉"
            },
            ["Green"] = new Tema
            {
                Ad = "Green",
                AnaRenk = "#39FF14",
                IkinciRenk = "#00FF88",
                VurguRenk = "#FFD700",
                EkRenk1 = "#66FF66",
                EkRenk2 = "#00CC66",
                EkRenk3 = "#99FFCC",
                EkRenk4 = "#55FF88",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#050A05",
                PanelRenk = "#0A1A0A",
                IkonMetin = "◎"
            },
            ["Orange"] = new Tema
            {
                Ad = "Orange",
                AnaRenk = "#FF8C00",
                IkinciRenk = "#FF4500",
                VurguRenk = "#FFD700",
                EkRenk1 = "#FFB266",
                EkRenk2 = "#FF6A00",
                EkRenk3 = "#FFD9B3",
                EkRenk4 = "#FF9933",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#0A0500",
                PanelRenk = "#1A0A00",
                IkonMetin = "◇"
            },
            ["Red"] = new Tema
            {
                Ad = "Red",
                AnaRenk = "#FF2244",
                IkinciRenk = "#FF0066",
                VurguRenk = "#FFD700",
                EkRenk1 = "#FF5566",
                EkRenk2 = "#FF3344",
                EkRenk3 = "#FF99AA",
                EkRenk4 = "#CC0022",
                HataRenk = "#FF8C00",
                ArkaplanRenk = "#0A0005",
                PanelRenk = "#1A000A",
                IkonMetin = "◈"
            },
            ["Blue"] = new Tema
            {
                Ad = "Blue",
                AnaRenk = "#00A7FF",
                IkinciRenk = "#0066FF",
                VurguRenk = "#00FFE5",
                EkRenk1 = "#3399FF",
                EkRenk2 = "#66CCFF",
                EkRenk3 = "#0044CC",
                EkRenk4 = "#88DDFF",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#051028",
                PanelRenk = "#071A2A",
                IkonMetin = "◈"
            },
            ["Teal"] = new Tema
            {
                Ad = "Teal",
                AnaRenk = "#00CCB3",
                IkinciRenk = "#00A899",
                VurguRenk = "#FFD700",
                EkRenk1 = "#33DDCC",
                EkRenk2 = "#009988",
                EkRenk3 = "#66EECC",
                EkRenk4 = "#008877",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#041015",
                PanelRenk = "#081A1A",
                IkonMetin = "◆"
            },
            ["Yellow"] = new Tema
            {
                Ad = "Yellow",
                AnaRenk = "#FFD700",
                IkinciRenk = "#FFCC00",
                VurguRenk = "#FF0066",
                EkRenk1 = "#FFE066",
                EkRenk2 = "#FFDD44",
                EkRenk3 = "#FFEE99",
                EkRenk4 = "#CCAA00",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#141004",
                PanelRenk = "#1A1308",
                IkonMetin = "◎"
            },
            ["Coral"] = new Tema
            {
                Ad = "Coral",
                AnaRenk = "#FF6B6B",
                IkinciRenk = "#FF4C4C",
                VurguRenk = "#FFD700",
                EkRenk1 = "#FF8A80",
                EkRenk2 = "#FF5252",
                EkRenk3 = "#FFCFCF",
                EkRenk4 = "#FF3B3B",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#100507",
                PanelRenk = "#1A0B0B",
                IkonMetin = "◉"
            },
            ["Midnight"] = new Tema
            {
                Ad = "Midnight",
                AnaRenk = "#4455FF",
                IkinciRenk = "#2233AA",
                VurguRenk = "#00FFE5",
                EkRenk1 = "#6677FF",
                EkRenk2 = "#112244",
                EkRenk3 = "#8899FF",
                EkRenk4 = "#334477",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#020214",
                PanelRenk = "#0A0D1A",
                IkonMetin = "◇"
            },
            ["Lime"] = new Tema
            {
                Ad = "Lime",
                AnaRenk = "#A6FF00",
                IkinciRenk = "#7FFF00",
                VurguRenk = "#00FFE5",
                EkRenk1 = "#CCFF66",
                EkRenk2 = "#99FF33",
                EkRenk3 = "#EEFFAA",
                EkRenk4 = "#88CC00",
                HataRenk = "#FF2244",
                ArkaplanRenk = "#061003",
                PanelRenk = "#0A1A08",
                IkonMetin = "◎"
            }
        };

        public static Tema MevcutTema =>
            Temalar.TryGetValue(_mevcutTemaAdi, out var tema)
                ? tema : Temalar["Cyan"];

        public static string MevcutTemaAdi => _mevcutTemaAdi;

        public static void TemaYukle()
        {
            try
            {
                if (File.Exists(_ayarYolu))
                {
                    var ad = File.ReadAllText(_ayarYolu).Trim();
                    if (Temalar.ContainsKey(ad))
                        _mevcutTemaAdi = ad;
                }
            }
            catch { }
        }

        public static void TemaUygula(string temaAdi,
            ResourceDictionary kaynaklar)
        {
            if (!Temalar.TryGetValue(temaAdi, out var tema)) return;

            _mevcutTemaAdi = temaAdi;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ayarYolu)!);
                File.WriteAllText(_ayarYolu, temaAdi);
            }
            catch { }

            kaynaklar["AnaRenk"] = new WpfSolidColorBrush(
                RenkCevir(tema.AnaRenk));
            kaynaklar["IkinciRenk"] = new WpfSolidColorBrush(
                RenkCevir(tema.IkinciRenk));
            kaynaklar["VurguRenk"] = new WpfSolidColorBrush(
                RenkCevir(tema.VurguRenk));
            kaynaklar["EkRenk1"] = new WpfSolidColorBrush(
                RenkCevir(tema.EkRenk1));
            kaynaklar["EkRenk2"] = new WpfSolidColorBrush(
                RenkCevir(tema.EkRenk2));
            kaynaklar["EkRenk3"] = new WpfSolidColorBrush(
                RenkCevir(tema.EkRenk3));
            kaynaklar["EkRenk4"] = new WpfSolidColorBrush(
                RenkCevir(tema.EkRenk4));
            kaynaklar["HataRenk"] = new WpfSolidColorBrush(
                RenkCevir(tema.HataRenk));
            kaynaklar["ArkaplanRenk"] = new WpfSolidColorBrush(
                RenkCevir(tema.ArkaplanRenk));
            kaynaklar["PanelRenk"] = new WpfSolidColorBrush(
                RenkCevir(tema.PanelRenk));
        }

        public static WpfColor RenkCevir(string hex)
            => (WpfColor)WpfColorConverter.ConvertFromString(hex);

        public static WpfColor AnaRenkColor =>
            RenkCevir(MevcutTema.AnaRenk);

        public static WpfColor IkinciRenkColor =>
            RenkCevir(MevcutTema.IkinciRenk);

        public static WpfColor VurguRenkColor =>
            RenkCevir(MevcutTema.VurguRenk);
    }
}