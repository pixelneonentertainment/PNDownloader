chrome.downloads.onCreated.addListener((downloadItem) => {
    try {
        const url = downloadItem.url || downloadItem.finalUrl;
        if (!url) return;

        const urlObj = new URL(url);
        const temizYol = urlObj.pathname.toLowerCase();

        const izlenenUzantilar = [".rar", ".zip", ".7z", ".tar", ".gz", ".exe", ".msi", ".mp4", ".iso", ".torrent", ".apk", ".dmg"];
        const dosyaAdi = downloadItem.filename ? downloadItem.filename.toLowerCase() : "";

        const eslesmeVar = izlenenUzantilar.some(ext =>
            temizYol.endsWith(ext) ||
            dosyaAdi.endsWith(ext) ||
            url.toLowerCase().includes(ext)
        );

        if (eslesmeVar) {
            chrome.downloads.cancel(downloadItem.id);

            // JSON formatında URL ve Yönlendirici (Referrer) adresini paketleyip gönderiyoruz
            const entegrasyonVerisi = {
                Url: url,
                Referrer: downloadItem.referrer || ""
            };

            fetch("http://localhost:55122/add", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(entegrasyonVerisi)
            }).catch(err => {
                console.log("Pixel Neon Downloader uygulaması kapalı veya sunucu aktif değil.");
            });
        }
    } catch (e) {
        console.error("Eklenti hatası:", e);
    }
});