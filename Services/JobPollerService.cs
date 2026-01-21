using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PosPrinterApp.Models;

namespace PosPrinterApp.Services
{
    public class JobPollerService : IDisposable
    {
        private readonly PosPrinterService _printerService;
        private readonly CashDrawerService _cashDrawerService;
        private readonly Action<string>? _log;
        private readonly HttpClient _http;
        private Timer? _timer;
        private int _isRunning = 0;
        private PosPrinterPollSettings _settings;

        public JobPollerService(PosPrinterPollSettings settings, PosPrinterService printerService, CashDrawerService cashDrawerService, Action<string>? log = null)
        {
            _settings = settings;
            _printerService = printerService;
            _cashDrawerService = cashDrawerService;
            _log = log;

            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(10);
        }

        public void UpdateSettings(PosPrinterPollSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Fetch settings dari API Yii2 berdasarkan session cookie (atau API key untuk backward compatibility)
        /// </summary>
        public async Task<bool> FetchSettingsFromApiAsync()
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                Log("Session cookie/API key atau BaseUrl kosong, tidak bisa fetch settings");
                return false;
            }

            try
            {
                var url = CombineUrl(_settings.BaseUrl, "/pos-printer-api/get-settings");
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Jika ApiKey berisi "PHPSESSID=" berarti session cookie, jika tidak berarti API key
                if (_settings.ApiKey.StartsWith("PHPSESSID=", StringComparison.OrdinalIgnoreCase))
                {
                    req.Headers.Add("Cookie", _settings.ApiKey);
                }
                else
                {
                    req.Headers.Add("X-PosPrinter-Key", _settings.ApiKey);
                }

                using var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    Log($"Fetch settings failed HTTP {(int)resp.StatusCode}: {body}");
                    return false;
                }

                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("success", out var successEl) || !successEl.GetBoolean())
                {
                    Log("Fetch settings failed: API returned success=false");
                    return false;
                }

                // Update interval dan max_jobs dari API jika ada
                if (doc.RootElement.TryGetProperty("default_interval_ms", out var intervalEl))
                {
                    _settings.IntervalMs = intervalEl.GetInt32();
                }
                if (doc.RootElement.TryGetProperty("default_max_jobs", out var maxJobsEl))
                {
                    _settings.MaxJobs = maxJobsEl.GetInt32();
                }

                Log($"Settings fetched: interval={_settings.IntervalMs}ms, max_jobs={_settings.MaxJobs}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error fetching settings: {ex.Message}");
                return false;
            }
        }

        public void Start()
        {
            if (!_settings.Enabled) return;
            if (_timer != null) return;
            if (string.IsNullOrWhiteSpace(_settings.CompanyId) || string.IsNullOrWhiteSpace(_settings.UserId))
            {
                Log("CompanyId atau UserId kosong, tidak bisa start polling");
                return;
            }

            _timer = new Timer(async _ => await TickAsync(), null, 1000, _settings.IntervalMs);
            Log($"Job poller started (interval {_settings.IntervalMs}ms, company={_settings.CompanyId}, user={_settings.UserId})");
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            Log("Job poller stopped");
        }

        private void Log(string msg) => _log?.Invoke($"[POLL] {msg}");

        private async Task TickAsync()
        {
            if (!_settings.Enabled) return;
            if (Interlocked.Exchange(ref _isRunning, 1) == 1) return;

            try
            {
                await PollAndProcessAsync();
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        private async Task PollAndProcessAsync()
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                string.IsNullOrWhiteSpace(_settings.CompanyId) ||
                string.IsNullOrWhiteSpace(_settings.UserId) ||
                string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                return;
            }

            var pollUrl = CombineUrl(_settings.BaseUrl, "/pos-printer-api/poll");
            using var req = new HttpRequestMessage(HttpMethod.Post, pollUrl);
            AddAuthHeader(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                company_id = _settings.CompanyId,
                user_id = _settings.UserId,
                max_jobs = _settings.MaxJobs
            }), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                Log($"Poll failed HTTP {(int)resp.StatusCode}: {body}");
                return;
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("success", out var successEl) || !successEl.GetBoolean())
                return;

            if (!doc.RootElement.TryGetProperty("jobs", out var jobsEl) || jobsEl.ValueKind != JsonValueKind.Array)
                return;

            foreach (var job in jobsEl.EnumerateArray())
            {
                int jobId = job.GetProperty("job_id").GetInt32();
                string jobType = job.GetProperty("job_type").GetString() ?? "";
                string orderNo = job.GetProperty("order_no").GetString() ?? "";

                bool ok = false;
                string err = "";

                try
                {
                    if (jobType == "open_drawer")
                    {
                        ok = _cashDrawerService.OpenCashDrawer(showMessage: false);
                    }
                    else
                    {
                        var content = await GetJobContentAsync(jobId);
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            ok = false;
                            err = "Empty content";
                        }
                        else
                        {
                            ok = _printerService.PrintReceipt(content, cutPaper: true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ok = false;
                    err = ex.Message;
                }

                await CompleteJobAsync(jobId, ok, err);
                Log($"Processed job_id={jobId} type={jobType} order_no={orderNo} ok={ok}");
            }
        }

        private async Task<string> GetJobContentAsync(int jobId)
        {
            var url = CombineUrl(_settings.BaseUrl, $"/pos-printer-api/job-content?job_id={jobId}&company_id={Uri.EscapeDataString(_settings.CompanyId)}&user_id={Uri.EscapeDataString(_settings.UserId)}");
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            AddAuthHeader(req);

            using var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return "";

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("success", out var successEl) || !successEl.GetBoolean())
                return "";

            if (!doc.RootElement.TryGetProperty("content", out var contentEl))
                return "";

            return contentEl.GetString() ?? "";
        }

        private async Task CompleteJobAsync(int jobId, bool ok, string err)
        {
            var url = CombineUrl(_settings.BaseUrl, "/pos-printer-api/job-complete");
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            AddAuthHeader(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                company_id = _settings.CompanyId,
                user_id = _settings.UserId,
                job_id = jobId,
                success = ok,
                error = err
            }), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req);
            _ = await resp.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Helper untuk menambahkan auth header (cookie atau API key)
        /// </summary>
        private void AddAuthHeader(HttpRequestMessage req)
        {
            // Jika ApiKey berisi "PHPSESSID=" berarti session cookie, jika tidak berarti API key
            if (_settings.ApiKey.StartsWith("PHPSESSID=", StringComparison.OrdinalIgnoreCase))
            {
                req.Headers.Add("Cookie", _settings.ApiKey);
            }
            else
            {
                req.Headers.Add("X-PosPrinter-Key", _settings.ApiKey);
            }
        }

        private static string CombineUrl(string baseUrl, string path)
        {
            baseUrl = baseUrl.TrimEnd('/');
            if (!path.StartsWith("/")) path = "/" + path;
            return baseUrl + path;
        }

        public void Dispose()
        {
            Stop();
            _http.Dispose();
        }
    }
}

