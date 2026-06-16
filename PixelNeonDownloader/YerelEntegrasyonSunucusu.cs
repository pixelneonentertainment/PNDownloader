using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace PixelNeonDownloader
{
    public class EntegrasyonVerisi
    {
        public string Url { get; set; } = "";
        public string Referrer { get; set; } = "";
    }

    public class YerelEntegrasyonSunucusu
    {
        private HttpListener? _listener;
        private readonly Action<string, string> _linkEkleCallback;
        private bool _calisiyor = false;
        private const int PORT = 55122;

        public YerelEntegrasyonSunucusu(Action<string, string> linkEkleCallback)
        {
            _linkEkleCallback = linkEkleCallback;
        }

        public void Baslat()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{PORT}/");
                _listener.Start();
                _calisiyor = true;
                _ = DinlemeDongusu();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Entegrasyon sunucusu başlatılamadı: {ex.Message}");
            }
        }

        private async Task DinlemeDongusu()
        {
            while (_calisiyor && _listener != null)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    var req = context.Request;
                    var res = context.Response;

                    res.Headers.Add("Access-Control-Allow-Origin", "*");
                    res.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                    res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                    if (req.HttpMethod == "OPTIONS")
                    {
                        res.StatusCode = 200;
                        res.Close();
                        continue;
                    }

                    if (req.HttpMethod == "POST" && req.Url?.LocalPath == "/add")
                    {
                        using var reader = new StreamReader(req.InputStream);
                        var json = await reader.ReadToEndAsync();

                        try
                        {
                            var veri = JsonSerializer.Deserialize<EntegrasyonVerisi>(json);

                            if (veri != null && !string.IsNullOrEmpty(veri.Url))
                            {
                                // CS0104 hatasını önlemek için System.Windows.Application olarak tam adıyla çağrıldı
                                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    _linkEkleCallback?.Invoke(veri.Url, veri.Referrer);
                                }));

                                res.StatusCode = 200;
                                using var writer = new StreamWriter(res.OutputStream);
                                await writer.WriteAsync("OK");
                            }
                            else
                            {
                                res.StatusCode = 400;
                            }
                        }
                        catch
                        {
                            res.StatusCode = 400;
                        }
                    }
                    else
                    {
                        res.StatusCode = 404;
                    }

                    res.Close();
                }
                catch { }
            }
        }

        public void Durdur()
        {
            _calisiyor = false;
            try
            {
                _listener?.Stop();
                _listener?.Close();
            }
            catch { }
        }
    }
}