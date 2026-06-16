using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace PixelNeonDownloader
{
    public class IndirmeKuyrugu
    {
        private readonly Queue<DownloadItem> _kuyruk = new();
        private readonly IndirmeServisi _servis;
        private readonly ObservableCollection<DownloadItem> _indirmeler;
        private readonly Dictionary<DownloadItem, CancellationTokenSource> _iptalTokenlari;

        private bool _calisiyor = false;
        private DownloadItem? _mevcutIndirme = null;
        private CancellationTokenSource? _mevcutCts = null;

        public bool KuyrukModu { get; set; } = false;
        public int KuyrukSayisi => _kuyruk.Count;
        public DownloadItem? MevcutIndirme => _mevcutIndirme;

        public event Action<DownloadItem>? IndirmeBasladi;
        public event Action<DownloadItem>? IndirmeTamamlandi;
        public event Action<string>? DurumDegisti;

        public IndirmeKuyrugu(
            IndirmeServisi servis,
            ObservableCollection<DownloadItem> indirmeler,
            Dictionary<DownloadItem, CancellationTokenSource> iptalTokenlari)
        {
            _servis = servis;
            _indirmeler = indirmeler;
            _iptalTokenlari = iptalTokenlari;
        }

        public void Ekle(DownloadItem item)
        {
            if (!KuyrukModu) return;

            item.Durum = Durum.Bekliyor;
            _kuyruk.Enqueue(item);
            DurumDegisti?.Invoke(
                $"📋 Kuyruğa eklendi: {item.DosyaAdi} ({_kuyruk.Count} bekliyor)");

            if (!_calisiyor)
                _ = KuyrukDongusu();
        }

        private async Task KuyrukDongusu()
        {
            _calisiyor = true;

            // Özyineleme (recursion) yerine performanslı ve güvenli while döngüsü
            while (_kuyruk.Count > 0)
            {
                var item = _kuyruk.Dequeue();
                _mevcutIndirme = item;

                var cts = new CancellationTokenSource();
                _mevcutCts = cts;
                _iptalTokenlari[item] = cts;

                IndirmeBasladi?.Invoke(item);
                DurumDegisti?.Invoke(
                    $"⬇ İndiriliyor: {item.DosyaAdi} ({_kuyruk.Count} bekliyor)");

                try
                {
                    // Bir indirme hata verse bile sonraki indirmelere güvenle geçebilmek için try-catch
                    await _servis.IndirAsync(item, cts.Token);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Kuyruk indirme hatası: {ex.Message}");
                }
                finally
                {
                    // Bellek ve handle sızıntılarını önlemek için temizlik
                    cts.Dispose();
                    _iptalTokenlari.Remove(item);
                }

                IndirmeTamamlandi?.Invoke(item);
            }

            _calisiyor = false;
            _mevcutIndirme = null;
            _mevcutCts = null;
            DurumDegisti?.Invoke("✅ Kuyruk tamamlandı!");
        }

        public void MevcuduDuraklat()
        {
            _mevcutCts?.Cancel();
            DurumDegisti?.Invoke("⏸ Kuyruk duraklatıldı.");
        }

        public void Temizle()
        {
            _kuyruk.Clear();
            _mevcutCts?.Cancel();
            _calisiyor = false;
            _mevcutIndirme = null;
            DurumDegisti?.Invoke("🗑 Kuyruk temizlendi.");
        }

        public List<DownloadItem> KuyrukListesi()
            => new List<DownloadItem>(_kuyruk);
    }
}