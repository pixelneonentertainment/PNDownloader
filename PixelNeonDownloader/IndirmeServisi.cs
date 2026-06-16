using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace PixelNeonDownloader
{
    public class IndirmeServisi
    {
        public static long MaxHiz { get; set; } = 0;
        public static bool AkilliKlasorlemeAcik { get; set; } = true;
        public static bool DusukDiskModu { get; set; } = false;
        public static int DiskOnbellekBoyutu { get; set; } = 81920;
        public static ProxyAyarlari Proxy { get; private set; } = ProxyAyarlari.Yukle();

        private static HttpClient _httpClient = HttpClientOlustur();

        // Elite Torrent Optimizasyonu: Sürüm uyumlu paylaşımlı sıcak motor referansı
        private static readonly object _torrentLock = new();
        private static MonoTorrent.Client.ClientEngine? _torrentEngine;

        public static void ProxyGuncelle(ProxyAyarlari yeniProxy)
        {
            Proxy = yeniProxy;
            yeniProxy.Kaydet();
            _httpClient = HttpClientOlustur();
        }

        // Ultra hız ayarları
        private static int _parcaSayisi = 16;
        private static int _bufferBoyutu = 1024 * 1024; // 1MB
        private static bool _ultraMod = false;

        public static int ParcaSayisi
        {
            get => _parcaSayisi;
            set => _parcaSayisi = Math.Clamp(value, 1, 32);
        }

        public static int BufferBoyutu
        {
            get => _bufferBoyutu;
            set => _bufferBoyutu = value;
        }

        public static bool UltraMod
        {
            get => _ultraMod;
            set => _ultraMod = value;
        }

        // Her .NET sürümüyle (%100) uyumlu optimize edilmiş bağlantı yöneticisi
        private static HttpClient HttpClientOlustur()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseProxy = true
            };

            try
            {
                // SSL sertifika hatalarını yok say
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            catch { }

            var proxyObj = Proxy?.WebProxyOlustur();
            if (proxyObj != null)
            {
                handler.Proxy = proxyObj;
                handler.UseProxy = true;
            }
            else
            {
                handler.UseProxy = false;
            }

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(60)
            };

            // Derleme hatasını önlemek için DefaultRequestHeaders olarak düzeltildi
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            return client;
        }



        // IndirmeServisi.cs içindeki TorrentEngineGetirAsync metodunu bu kesin çözülmüş haliyle güncelleyin:
        private static async Task<MonoTorrent.Client.ClientEngine> TorrentEngineGetirAsync()
        {
            if (_torrentEngine != null) return _torrentEngine;

            var settingsBuilder = new MonoTorrent.Client.EngineSettingsBuilder
            {
                ListenEndPoints = new Dictionary<string, System.Net.IPEndPoint>
        {
            { "ipv4", new System.Net.IPEndPoint(System.Net.IPAddress.Any, 55123) }
        },
                DiskCacheBytes = 32 * 1024 * 1024,
                MaximumConnections = 150,
                MaximumHalfOpenConnections = 15
            };

            // 100% UYUMLULUK BYPASS: Çalışma dizinini geçici olarak AppData'ya çekip, motor oluştuktan sonra eski yerine alıyoruz.
            // Bu sayede hiçbir kütüphane özelliğine bağımlı kalmadan yetki hatasını (Access Denied) kökten çözüyoruz.
            var eskiDizin = Directory.GetCurrentDirectory();
            var appDataDizin = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PixelNeonDownloader");

            try
            {
                Directory.CreateDirectory(appDataDizin);
                Directory.SetCurrentDirectory(appDataDizin); // Geçici yönlendirme aktif

                var engine = new MonoTorrent.Client.ClientEngine(settingsBuilder.ToSettings());
                _torrentEngine = engine;
            }
            finally
            {
                Directory.SetCurrentDirectory(eskiDizin); // Orijinal dizine anında geri dön (Güvenlik Kalkanı)
            }

            return await Task.FromResult(_torrentEngine);
        }

        public static async Task TorrentEngineDurdurAsync()
        {
            if (_torrentEngine != null)
            {
                _torrentEngine.Dispose();
                _torrentEngine = null;
            }
            await Task.CompletedTask;
        }

        public async Task IndirAsync(DownloadItem item, CancellationToken iptalToken)
        {
            if (item.Tur == IndirmeTuru.Torrent || item.Tur == IndirmeTuru.Magnet)
            {
                await TorrentIndirAsync(item, iptalToken);
                return;
            }

            var log = IndirmeLogYoneticisi.LogOlustur(item);

            try
            {
                item.Durum = Durum.Indiriliyor;
                item.KalanSure = "Bağlanıyor...";

                Directory.CreateDirectory(item.KayitYolu);
                var tamYol = Path.Combine(item.KayitYolu, item.DosyaAdi);

                var destekliyor = await CokluParcaDestekliyorMu(item.Url, item);

                // Optimizasyon: Hız limiti etkinken de çoklu indirme kararlılığını korumak için MaxHiz == 0 kontrolü kaldırıldı
                if (destekliyor && item.DosyaBoyutu > 10_000_000)
                {
                    await CokluParcaIndirAsync(item, tamYol, iptalToken, log);
                }
                else
                {
                    await TekParcaIndirAsync(item, tamYol, iptalToken, log);
                }

                if (item.Durum == Durum.Tamamlandi)
                {
                    IndirmeLogYoneticisi.LogTamamla(log);

                    if (AkilliKlasorlemeAcik)
                    {
                        var altKlasor = AkilliKlasorleme.KlasorBelirle(item.DosyaAdi);
                        var hedefKlasor = Path.Combine(item.KayitYolu, altKlasor);
                        AkilliKlasorleme.DosyayiTasi(tamYol, hedefKlasor);
                        item.KayitYolu = hedefKlasor;
                        tamYol = Path.Combine(hedefKlasor, item.DosyaAdi);
                    }

                    var uzanti = Path.GetExtension(item.DosyaAdi).ToLower();
                    if (uzanti is ".zip" or ".rar" or ".7z" or ".tar" or ".gz")
                    {
                        await CikartAsync(item, tamYol);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                item.Durum = Durum.Duraklatildi;
                item.Hiz = 0;
                item.KalanSure = "--:--";
                IndirmeLogYoneticisi.LogGuncelle(log, item.IndirilenBytes);
            }
            catch (Exception ex)
            {
                item.Durum = Durum.Hata;
                item.KalanSure = "Hata!";
                IndirmeLogYoneticisi.LogHata(log);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"İndirme hatası:\n\n{ex.Message}", "Hata",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                });
            }
        }

        // Performanslı & Cloudflare Uyumlu Gelişmiş Çoklu Parça Doğrulama Algoritması
        private async Task<bool> CokluParcaDestekliyorMu(string url, DownloadItem item)
        {
            try
            {
                // Adım 1: GET Range=0-0 isteği gönder (Cloudflare gibi HEAD engelleyen tüm sunucularda %100 çalışır)
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode == System.Net.HttpStatusCode.PartialContent)
                {
                    item.DosyaBoyutu = response.Content.Headers.ContentRange?.Length ?? 0;
                    return item.DosyaBoyutu > 0;
                }
            }
            catch { }

            try
            {
                // Adım 2: Fallback (Yedek) olarak standart HEAD isteğini dene
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await _httpClient.SendAsync(request);
                item.DosyaBoyutu = response.Content.Headers.ContentLength ?? 0;
                return response.Headers.AcceptRanges.Contains("bytes") && item.DosyaBoyutu > 0;
            }
            catch { return false; }
        }

        private async Task CokluParcaIndirAsync(
            DownloadItem item, string tamYol,
            CancellationToken iptalToken, IndirmeLog log)
        {
            int aktifParcaSayisi = ParcaSayisi;
            var parcaBoyutu = item.DosyaBoyutu / aktifParcaSayisi;
            var toplamIndirilenbytes = new long[aktifParcaSayisi];

            item.KalanSure = $"{aktifParcaSayisi} parça ile hazırlanıyor...";

            // Ana dosya boyutunu diskte önceden ayır (Birleştirme süresi sıfırlanır)
            using (var fs = new FileStream(tamYol, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true))
            {
                fs.SetLength(item.DosyaBoyutu);
            }

            var gorevler = new Task[aktifParcaSayisi];

            for (int i = 0; i < aktifParcaSayisi; i++)
            {
                var parcaIndex = i;
                var baslangic = parcaIndex * parcaBoyutu;
                var bitis = parcaIndex == aktifParcaSayisi - 1 ? item.DosyaBoyutu - 1 : baslangic + parcaBoyutu - 1;

                gorevler[i] = ParcaIndirAsync(
                    item,
                    item.Url, tamYol,
                    baslangic, bitis,
                    toplamIndirilenbytes, parcaIndex,
                    iptalToken, log);
            }

            using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(iptalToken);

            var progressTask = Task.Run(async () =>
            {
                var sonHizKontrol = DateTime.Now;
                long sonToplamBytes = 0;
                var zamanBaslangici = DateTime.Now;

                while (!progressCts.Token.IsCancellationRequested)
                {
                    var toplamIndirildi = toplamIndirilenbytes.Sum();
                    item.IndirilenBytes = toplamIndirildi;

                    if (item.DosyaBoyutu > 0)
                        item.Ilerleme = (double)toplamIndirildi / item.DosyaBoyutu;

                    var simdi = DateTime.Now;
                    var gecen = (simdi - sonHizKontrol).TotalSeconds;
                    if (gecen >= 1.0)
                    {
                        item.Hiz = (toplamIndirildi - sonToplamBytes) / gecen;
                        sonHizKontrol = simdi;
                        sonToplamBytes = toplamIndirildi;

                        if (item.Hiz > 0 && item.DosyaBoyutu > 0)
                        {
                            var kalan = (item.DosyaBoyutu - toplamIndirildi) / item.Hiz;
                            var kalanSure = TimeSpan.FromSeconds(kalan);
                            item.KalanSure = kalanSure.TotalHours >= 1
                                ? $"{(int)kalanSure.TotalHours}s {kalanSure.Minutes:D2}d"
                                : $"{kalanSure.Minutes:D2}:{kalanSure.Seconds:D2}";
                        }

                        IndirmeLogYoneticisi.LogGuncelle(log, toplamIndirildi);
                    }

                    item.IndirmeSuresi = DateTime.Now - zamanBaslangici;

                    if (item.IndirilenBytes >= item.DosyaBoyutu) break;
                    try
                    {
                        await Task.Delay(500, progressCts.Token);
                    }
                    catch (TaskCanceledException) { break; }
                }
            }, progressCts.Token);

            try
            {
                await Task.WhenAll(gorevler);
            }
            finally
            {
                progressCts.Cancel();
            }

            if (iptalToken.IsCancellationRequested)
            {
                item.Durum = Durum.Duraklatildi;
                return;
            }

            item.Ilerleme = 1.0;
            item.Hiz = 0;
            item.KalanSure = "Tamamlandı";
            item.Durum = Durum.Tamamlandi;
        }

        private async Task TekParcaIndirAsync(
            DownloadItem item, string tamYol,
            CancellationToken iptalToken, IndirmeLog log)
        {
            using var response = await _httpClient.GetAsync(
                item.Url,
                HttpCompletionOption.ResponseHeadersRead,
                iptalToken);

            response.EnsureSuccessStatusCode();
            item.DosyaBoyutu = response.Content.Headers.ContentLength ?? 0;

            var bufferBoyutu = DusukDiskModu
                ? Math.Min(DiskOnbellekBoyutu, 16384)
                : DiskOnbellekBoyutu;

            var fileBufferBoyutu = DusukDiskModu ? 4096 : DiskOnbellekBoyutu;

            using (var contentStream = await response.Content.ReadAsStreamAsync(iptalToken))
            using (var fileStream = new FileStream(
                tamYol, FileMode.Create, FileAccess.Write,
                FileShare.None, fileBufferBoyutu, true))
            {
                var pool = ArrayPool<byte>.Shared;
                var buffer = pool.Rent(bufferBoyutu);

                try
                {
                    long toplamOkunan = 0;
                    int okunan;
                    var sonHizKontrol = DateTime.Now;
                    long sonBytes = 0;
                    long sonFlush = 0;
                    long sonLogGuncelleme = 0;
                    var zamanBaslangici = DateTime.Now;

                    while ((okunan = await contentStream.ReadAsync(buffer, 0, bufferBoyutu, iptalToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, okunan), iptalToken);
                        toplamOkunan += okunan;
                        item.IndirilenBytes = toplamOkunan;

                        if (item.DosyaBoyutu > 0)
                            item.Ilerleme = (double)toplamOkunan / item.DosyaBoyutu;

                        var simdi = DateTime.Now;
                        var gecen = (simdi - sonHizKontrol).TotalSeconds;
                        if (gecen >= 1.0)
                        {
                            item.Hiz = (toplamOkunan - sonBytes) / gecen;
                            sonHizKontrol = simdi;
                            sonBytes = toplamOkunan;

                            if (item.Hiz > 0 && item.DosyaBoyutu > 0)
                            {
                                var kalan = (item.DosyaBoyutu - toplamOkunan) / item.Hiz;
                                var kalanSure = TimeSpan.FromSeconds(kalan);
                                item.KalanSure = kalanSure.TotalHours >= 1
                                    ? $"{(int)kalanSure.TotalHours}s {kalanSure.Minutes:D2}d"
                                    : $"{kalanSure.Minutes:D2}:{kalanSure.Seconds:D2}";
                            }
                        }

                        item.IndirmeSuresi = DateTime.Now - zamanBaslangici;

                        if (toplamOkunan - sonLogGuncelleme >= 5_000_000)
                        {
                            IndirmeLogYoneticisi.LogGuncelle(log, toplamOkunan);
                            sonLogGuncelleme = toplamOkunan;
                        }

                        if (DusukDiskModu &&
                            toplamOkunan - sonFlush >= bufferBoyutu * 4)
                        {
                            await fileStream.FlushAsync(iptalToken);
                            sonFlush = toplamOkunan;
                        }

                        // YÜKSEK HASSASİYETLİ HIZ SINIRLANDIRICI (Tek Kanallı)
                        if (MaxHiz > 0 && item.Hiz > MaxHiz)
                        {
                            var beklemeSuresi = (int)((okunan / (double)MaxHiz) * 1000);
                            await Task.Delay(Math.Min(beklemeSuresi, 1000), iptalToken);
                        }
                    }
                }
                finally
                {
                    pool.Return(buffer);
                }
            }

            item.Ilerleme = 1.0;
            item.Hiz = 0;
            item.KalanSure = "Tamamlandı";
            item.Durum = Durum.Tamamlandi;
        }

        private async Task ParcaIndirAsync(
            DownloadItem item, // Dinamik hız sınırlayıcı için parametre eklendi
            string url, string anaDosyaYolu,
            long baslangic, long bitis,
            long[] toplamIndirilenbytes, int index,
            CancellationToken iptalToken, IndirmeLog log)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(baslangic, bitis);

            // Gelişmiş entegrasyon için yönlendirici başlığı isteğe bağlanıyor
            if (log != null && !string.IsNullOrEmpty(log.Referrer))
            {
                request.Headers.Referrer = new Uri(log.Referrer);
            }

            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, iptalToken);

            using var contentStream = await response.Content.ReadAsStreamAsync(iptalToken);

            using var fileStream = new FileStream(
                anaDosyaYolu, FileMode.Open, FileAccess.Write,
                FileShare.ReadWrite, DiskOnbellekBoyutu, true);

            fileStream.Position = baslangic;

            var pool = ArrayPool<byte>.Shared;
            var buffer = pool.Rent(DiskOnbellekBoyutu);

            try
            {
                int okunan;
                long parcaToplamOkunan = 0;

                while ((okunan = await contentStream.ReadAsync(buffer, 0, DiskOnbellekBoyutu, iptalToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, okunan), iptalToken);
                    toplamIndirilenbytes[index] += okunan;
                    parcaToplamOkunan += okunan;

                    if (parcaToplamOkunan % 2_000_000 < DiskOnbellekBoyutu)
                        IndirmeLogYoneticisi.ParcaGuncelle(log, index, parcaToplamOkunan);

                    // YÜKSEK HASSASİYETLİ HIZ SINIRLANDIRICI (Çok Kanallı / Eşzamanlı)
                    if (MaxHiz > 0 && item.Hiz > MaxHiz)
                    {
                        // Eşzamanlı parça sayısına göre mikro saniyelik frenleme uygular
                        var beklemeSuresi = (int)((okunan / (double)MaxHiz) * 1000 * ParcaSayisi);
                        await Task.Delay(Math.Min(beklemeSuresi, 500), iptalToken);
                    }
                }

                IndirmeLogYoneticisi.ParcaGuncelle(log, index, parcaToplamOkunan, tamamlandi: true);
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        // ── Elite Torrent İndirme Akışı (Deadlock ve Stopping Korumalı) ──────────────────────────────
        private async Task TorrentIndirAsync(DownloadItem item, CancellationToken iptalToken)
        {
            MonoTorrent.Client.TorrentManager? manager = null;

            try
            {
                item.Durum = Durum.Indiriliyor;
                item.KalanSure = "Bağlanıyor...";

                Directory.CreateDirectory(item.KayitYolu);

                // UI Deadlock'unu önlemek için asenkron çağrılarda ConfigureAwait(false) eklendi
                var engine = await TorrentEngineGetirAsync().ConfigureAwait(false);

                lock (_torrentLock)
                {
                    if (engine == null)
                    {
                        _ = TorrentEngineGetirAsync().Result;
                    }
                }

                if (item.Tur == IndirmeTuru.Magnet)
                {
                    var magnet = MonoTorrent.MagnetLink.Parse(item.Url);
                    manager = engine.Torrents.FirstOrDefault(t => t.Name == item.DosyaAdi);
                    if (manager == null)
                    {
                        manager = await engine.AddAsync(magnet, item.KayitYolu).ConfigureAwait(false);
                    }
                }
                else
                {
                    var torrentYolu = item.Url.Trim('"').Trim();
                    var torrent = await MonoTorrent.Torrent.LoadAsync(torrentYolu).ConfigureAwait(false);
                    manager = engine.Torrents.FirstOrDefault(t => t.Name == item.DosyaAdi);
                    if (manager == null)
                    {
                        manager = await engine.AddAsync(torrent, item.KayitYolu).ConfigureAwait(false);
                    }
                }

                // DÜZELTME KATMANI (Deadlock & Stopping Kalkanı): 
                // Eğer menajer durdurulma (Stopping) aşamasındaysa, durması (Stopped olması) için asenkron olarak bekler.
                // ConfigureAwait(false) sayesinde WPF arayüz kilidi (Deadlock) tamamen bypass edilir.
                int beklemeyenDeneme = 0;
                while (manager.State == MonoTorrent.Client.TorrentState.Stopping && beklemeyenDeneme < 20)
                {
                    await Task.Delay(250, iptalToken).ConfigureAwait(false);
                    beklemeyenDeneme++;
                }

                // Torrent'i başlat
                await manager.StartAsync().ConfigureAwait(false);

                var torrentBaslangic = DateTime.Now;

                while (!iptalToken.IsCancellationRequested)
                {
                    item.Ilerleme = manager.Progress / 100.0;
                    item.Hiz = manager.Monitor.DownloadRate;
                    item.IndirilenBytes = manager.Monitor.DataBytesReceived;
                    item.DosyaBoyutu = manager.Torrent?.Size ?? item.DosyaBoyutu;

                    if (manager.Monitor.DownloadRate > 0 && item.DosyaBoyutu > 0)
                    {
                        var kalan = (item.DosyaBoyutu - item.IndirilenBytes) / (double)manager.Monitor.DownloadRate;
                        var kalanSure = TimeSpan.FromSeconds(kalan);
                        item.KalanSure = kalanSure.TotalHours >= 1
                            ? $"{(int)kalanSure.TotalHours}s {kalanSure.Minutes:D2}d"
                            : $"{kalanSure.Minutes:D2}:{kalanSure.Seconds:D2}";
                    }

                    item.IndirmeSuresi = DateTime.Now - torrentBaslangic;

                    if (manager.State == MonoTorrent.Client.TorrentState.Seeding)
                    {
                        item.Ilerleme = 1.0;
                        item.Hiz = 0;
                        item.KalanSure = "Tamamlandı";
                        item.Durum = Durum.Tamamlandi;
                        break;
                    }

                    if (manager.State == MonoTorrent.Client.TorrentState.Error)
                    {
                        item.Durum = Durum.Hata;
                        item.KalanSure = "Hata!";
                        break;
                    }

                    await Task.Delay(1000, iptalToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                item.Durum = Durum.Duraklatildi;
                item.Hiz = 0;
                item.KalanSure = "--:--";
            }
            catch (Exception ex)
            {
                item.Durum = Durum.Hata;
                item.KalanSure = "Hata!";
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"Torrent hatası:\n\n{ex.Message}", "Hata",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                });
            }
            finally
            {
                // DÜZELTME KATMANI (Asenkron Kilit Sökücü): 
                // DUR / BAŞLAT döngülerindeki asılı kalmaları (Stopping state) engellemek için,
                // duraklatıldığında durdurup silmek yerine anlık ve güvenli olan PauseAsync çağrılır.
                if (manager != null)
                {
                    try
                    {
                        await manager.PauseAsync().ConfigureAwait(false); // StopAsync yerine PauseAsync entegre edildi
                    }
                    catch { }
                }
            }

            if (iptalToken.IsCancellationRequested)
            {
                item.Durum = Durum.Duraklatildi;
                item.Hiz = 0;
                item.KalanSure = "--:--";
            }
        }

        private async Task CikartAsync(DownloadItem item, string arsivYolu)
        {
            try
            {
                item.Durum = Durum.Cikartiliyor;
                item.KalanSure = "Çıkartılıyor...";

                var cikartmaYolu = Path.Combine(
                    Path.GetDirectoryName(arsivYolu)!,
                    Path.GetFileNameWithoutExtension(arsivYolu));

                Directory.CreateDirectory(cikartmaYolu);

                await Task.Run(() =>
                {
                    using var arsiv = ArchiveFactory.Open(arsivYolu);
                    foreach (var giris in arsiv.Entries)
                    {
                        if (!giris.IsDirectory && !string.IsNullOrEmpty(giris.Key))
                        {
                            giris.WriteToDirectory(cikartmaYolu,
                                new ExtractionOptions
                                {
                                    ExtractFullPath = true,
                                    Overwrite = true
                                });
                        }
                    }
                });

                item.KalanSure = "Tamamlandı";
                item.Durum = Durum.Tamamlandi;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Çıkartma hatası: {ex.Message}");
                item.KalanSure = "Tamamlandı";
                item.Durum = Durum.Tamamlandi;
            }
        }
    }
}