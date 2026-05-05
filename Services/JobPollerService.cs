using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PosPrinterApp.Models;

namespace PosPrinterApp.Services
{
    public class JobPollerService : IDisposable
    {
        /// <summary>
        /// Extract error message dari HTML atau JSON response, filter HTML tags
        /// </summary>
        private static string ExtractErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "";

            // Jika response adalah JSON, coba parse
            if (body.TrimStart().StartsWith("{") || body.TrimStart().StartsWith("["))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var msgEl))
                    {
                        return msgEl.GetString() ?? "";
                    }
                    if (doc.RootElement.TryGetProperty("error", out var errEl))
                    {
                        return errEl.GetString() ?? "";
                    }
                }
                catch { /* Bukan JSON valid, lanjut ke HTML parsing */ }
            }

            // Jika response adalah HTML, extract pesan error
            if (body.Contains("<html") || body.Contains("<!DOCTYPE"))
            {
                // Extract dari tag <h2>, <h3>, atau <title>
                var patterns = new[]
                {
                    @"<h2[^>]*>(.*?)</h2>",
                    @"<h3[^>]*>(.*?)</h3>",
                    @"<title[^>]*>(.*?)</title>",
                    @"Unauthorized",
                    @"the page you requested was protected"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(body, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (match.Success)
                    {
                        string extracted = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                        // Remove HTML tags
                        extracted = Regex.Replace(extracted, "<[^>]+>", "").Trim();
                        if (!string.IsNullOrWhiteSpace(extracted))
                        {
                            return extracted;
                        }
                    }
                }

                // Fallback: cari status code di HTML
                var statusMatch = Regex.Match(body, @"(\d{3})");
                if (statusMatch.Success)
                {
                    return $"HTTP {statusMatch.Value}";
                }

                return "Error (HTML response)";
            }

            // Jika bukan HTML atau JSON, return as-is (tapi limit length)
            return body.Length > 200 ? body.Substring(0, 200) + "..." : body;
        }
        private readonly PosPrinterService _printerService;
        private readonly CashDrawerService _cashDrawerService;
        private readonly Action<string>? _log;
        private readonly HttpClient _http;
        private System.Threading.Timer? _timer;
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
                AddAuthHeader(req);

                using var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    string errorMsg = ExtractErrorMessage(body);
                    Log($"Fetch settings failed HTTP {(int)resp.StatusCode}: {errorMsg}");
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
            if (!_settings.Enabled)
            {
                Log("Job poller not started: Enabled=false");
                return;
            }
            if (_timer != null)
            {
                Log("Job poller already started (timer exists)");
                return;
            }
            if (string.IsNullOrWhiteSpace(_settings.CompanyId) || string.IsNullOrWhiteSpace(_settings.UserId))
            {
                Log($"CompanyId atau UserId kosong, tidak bisa start polling - CompanyId={_settings.CompanyId}, UserId={_settings.UserId}");
                return;
            }

            try
            {
                _timer = new System.Threading.Timer(async _ => await TickAsync(), null, 1000, _settings.IntervalMs);
                Log($"Job poller started (interval {_settings.IntervalMs}ms, company={_settings.CompanyId}, user={_settings.UserId})");
            }
            catch (Exception ex)
            {
                Log($"Error starting job poller: {ex.Message}");
            }
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
                Log("Poll skipped: Settings tidak lengkap (ApiKey, CompanyId, UserId, atau BaseUrl kosong)");
                return;
            }

            var pollUrl = CombineUrl(_settings.BaseUrl, "/pos-printer-api/poll");
            Log($"Polling: {pollUrl} (company={_settings.CompanyId}, user={_settings.UserId}, max_jobs={_settings.MaxJobs})");
            
            using var req = new HttpRequestMessage(HttpMethod.Post, pollUrl);
            AddAuthHeader(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                company_id = _settings.CompanyId,
                user_id = _settings.UserId,
                max_jobs = _settings.MaxJobs
            }), Encoding.UTF8, "application/json");

            try
            {
                using var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                
                if (!resp.IsSuccessStatusCode)
                {
                    // Extract error message, filter HTML
                    string errorMsg = ExtractErrorMessage(body);
                    Log($"Poll failed HTTP {(int)resp.StatusCode}: {errorMsg}");
                    if (body.Length > 0 && body.Length < 500)
                    {
                        Log($"Response body: {body}");
                    }
                    return;
                }

                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("success", out var successEl) || !successEl.GetBoolean())
                {
                    string message = "Unknown error";
                    if (doc.RootElement.TryGetProperty("message", out var msgEl))
                    {
                        message = msgEl.GetString() ?? message;
                    }
                    Log($"Poll failed: API returned success=false, message={message}");
                    return;
                }

                if (!doc.RootElement.TryGetProperty("jobs", out var jobsEl) || jobsEl.ValueKind != JsonValueKind.Array)
                {
                    Log("Poll: Tidak ada jobs atau format jobs tidak valid");
                    return;
                }

                int jobCount = 0;
                foreach (var job in jobsEl.EnumerateArray())
                {
                    jobCount++;
                }

                if (jobCount == 0)
                {
                    Log("Poll: Tidak ada job yang tersedia");
                    return;
                }

                Log($"Poll: Ditemukan {jobCount} job(s)");

                foreach (var job in jobsEl.EnumerateArray())
                {
                    int jobId = job.GetProperty("job_id").GetInt32();
                    string jobType = job.GetProperty("job_type").GetString() ?? "";
                    string orderNo = job.GetProperty("order_no").GetString() ?? "";

                    Log($"Processing job_id={jobId} type={jobType} order_no={orderNo}");

                    bool ok = false;
                    string err = "";

                    try
                    {
                        if (jobType == "open_drawer")
                        {
                            Log($"Opening cash drawer for job_id={jobId}");
                            ok = _cashDrawerService.OpenCashDrawer(showMessage: false);
                            Log($"Cash drawer result: {ok}");
                        }
                        else
                        {
                            Log($"Getting content for job_id={jobId} type={jobType}");
                            var content = await GetJobContentAsync(jobId);
                            if (string.IsNullOrWhiteSpace(content))
                            {
                                ok = false;
                                err = "Empty content";
                                Log($"Job content is empty for job_id={jobId}");
                            }
                            else
                            {
                                Log($"Printing content for job_id={jobId} (content length={content.Length})");
                                ok = _printerService.PrintReceipt(content, cutPaper: true);
                                Log($"Print result for job_id={jobId}: {ok}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ok = false;
                        err = ex.Message;
                        Log($"Error processing job_id={jobId}: {ex.Message}\n{ex.StackTrace}");
                    }

                    await CompleteJobAsync(jobId, ok, err);
                    Log($"Completed job_id={jobId} type={jobType} order_no={orderNo} ok={ok} error={err}");
                }
            }
            catch (Exception ex)
            {
                Log($"Exception during poll: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task<string> GetJobContentAsync(int jobId)
        {
            var url = CombineUrl(_settings.BaseUrl, $"/pos-printer-api/job-content?job_id={jobId}&company_id={Uri.EscapeDataString(_settings.CompanyId)}&user_id={Uri.EscapeDataString(_settings.UserId)}");
            Log($"Getting job content: {url}");
            
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            AddAuthHeader(req);

            try
            {
                using var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                
                if (!resp.IsSuccessStatusCode)
                {
                    string errorMsg = ExtractErrorMessage(body);
                    Log($"Get job content failed HTTP {(int)resp.StatusCode}: {errorMsg}");
                    return "";
                }

                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("success", out var successEl) || !successEl.GetBoolean())
                {
                    string message = "Unknown error";
                    if (doc.RootElement.TryGetProperty("message", out var msgEl))
                    {
                        message = msgEl.GetString() ?? message;
                    }
                    Log($"Get job content failed: API returned success=false, message={message}");
                    return "";
                }

                if (!doc.RootElement.TryGetProperty("content", out var contentEl))
                {
                    Log($"Get job content: No content property in response");
                    return "";
                }

                string content = contentEl.GetString() ?? "";
                Log($"Get job content: Success, content length={content.Length}");
                return content;
            }
            catch (Exception ex)
            {
                Log($"Exception getting job content: {ex.Message}\n{ex.StackTrace}");
                return "";
            }
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
            // Cek apakah ApiKey adalah session cookie (format: "CookieName=CookieValue" dan mengandung "sess" atau "session")
            bool isSessionCookie = _settings.ApiKey.Contains("=") && 
                                   (_settings.ApiKey.ToLower().Contains("sess") || _settings.ApiKey.ToLower().Contains("session"));
            
            if (isSessionCookie)
            {
                // Session cookie - gunakan header Cookie
                req.Headers.Add("Cookie", _settings.ApiKey);
            }
            else
            {
                // API key - gunakan header X-PosPrinter-Key
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

