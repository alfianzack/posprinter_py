using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PosPrinterApp.Models;
using PosPrinterApp.Services;

namespace PosPrinterApp.Services
{
    public class HttpServerService
    {
        private HttpListener? _listener;
        private bool _isRunning = false;
        private int _port = 8080;
        private PosPrinterService _printerService;
        private CashDrawerService _cashDrawerService;
        private Action<string>? _onLog;

        public HttpServerService(PosPrinterService printerService, CashDrawerService cashDrawerService)
        {
            _printerService = printerService;
            _cashDrawerService = cashDrawerService;
        }

        public int Port
        {
            get => _port;
            set
            {
                if (!_isRunning)
                {
                    _port = value;
                }
            }
        }

        public bool IsRunning => _isRunning;

        public void SetLogCallback(Action<string> onLog)
        {
            _onLog = onLog;
        }

        private void Log(string message)
        {
            _onLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public async Task StartAsync()
        {
            if (_isRunning)
            {
                Log("Server sudah berjalan");
                return;
            }

            try
            {
                _listener = new HttpListener();
                string url = $"http://localhost:{_port}/";
                _listener.Prefixes.Add(url);
                _listener.Start();
                _isRunning = true;

                Log($"Server HTTP berjalan di {url}");
                Log("Endpoint tersedia:");
                Log("  POST /api/print - Print receipt");
                Log("  POST /api/cashdrawer - Buka cash drawer");

                _ = Task.Run(async () => await ListenAsync());
            }
            catch (Exception ex)
            {
                Log($"Error starting server: {ex.Message}");
                _isRunning = false;
                throw;
            }
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            try
            {
                _listener?.Stop();
                _listener?.Close();
                _isRunning = false;
                Log("Server HTTP dihentikan");
            }
            catch (Exception ex)
            {
                Log($"Error stopping server: {ex.Message}");
            }
        }

        private async Task ListenAsync()
        {
            while (_isRunning && _listener != null)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(async () => await ProcessRequestAsync(context));
                }
                catch (HttpListenerException)
                {
                    // Server stopped
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Error listening: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                string path = request.Url?.AbsolutePath ?? "";
                string method = request.HttpMethod;

                Log($"{method} {path}");

                // Enable CORS
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                if (method == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                string responseText = "";
                response.ContentType = "application/json; charset=utf-8";

                if (path == "/api/print" && method == "POST")
                {
                    responseText = await HandlePrintRequestAsync(request);
                    response.StatusCode = 200;
                }
                else if (path == "/api/cashdrawer" && method == "POST")
                {
                    responseText = await HandleCashDrawerRequestAsync(request);
                    response.StatusCode = 200;
                }
                else if (path == "/api/status" && method == "GET")
                {
                    responseText = HandleStatusRequest();
                    response.StatusCode = 200;
                }
                else
                {
                    responseText = System.Text.Json.JsonSerializer.Serialize(new { error = "Endpoint tidak ditemukan" });
                    response.StatusCode = 404;
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Log($"Error processing request: {ex.Message}");
                string errorResponse = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
                byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
                response.StatusCode = 500;
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        private async Task<string> HandlePrintRequestAsync(HttpListenerRequest request)
        {
            try
            {
                // Baca request body dengan encoding UTF-8 (default untuk JSON)
                Encoding encoding = request.ContentEncoding ?? Encoding.UTF8;
                if (encoding == null || encoding.CodePage == 0)
                {
                    encoding = Encoding.UTF8;
                }
                
                using var reader = new StreamReader(request.InputStream, encoding);
                string body = await reader.ReadToEndAsync();
                
                Log($"Request body: {body}");

                // Configure JSON options untuk case-insensitive dan allow trailing commas
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var printRequest = System.Text.Json.JsonSerializer.Deserialize<PrintRequest>(body, jsonOptions);
                
                if (printRequest == null)
                {
                    Log("Deserialisasi gagal - printRequest is null");
                    return System.Text.Json.JsonSerializer.Serialize(new PrintResponse
                    {
                        Success = false,
                        Message = "Format JSON tidak valid"
                    });
                }
                
                if (string.IsNullOrWhiteSpace(printRequest.Content))
                {
                    Log($"Content kosong atau null. Content length: {printRequest.Content?.Length ?? 0}");
                    return System.Text.Json.JsonSerializer.Serialize(new PrintResponse
                    {
                        Success = false,
                        Message = "Content tidak boleh kosong"
                    });
                }

                Log($"Printing content: {printRequest.Content.Substring(0, Math.Min(50, printRequest.Content.Length))}...");
                bool success = _printerService.PrintCustomText(printRequest.Content, printRequest.CutPaper);
                
                return System.Text.Json.JsonSerializer.Serialize(new PrintResponse
                {
                    Success = success,
                    Message = success ? "Print berhasil" : "Print gagal"
                });
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                Log($"JSON parsing error: {jsonEx.Message}");
                return System.Text.Json.JsonSerializer.Serialize(new PrintResponse
                {
                    Success = false,
                    Message = $"Format JSON tidak valid: {jsonEx.Message}"
                });
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}\nStack: {ex.StackTrace}");
                return System.Text.Json.JsonSerializer.Serialize(new PrintResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        private async Task<string> HandleCashDrawerRequestAsync(HttpListenerRequest request)
        {
            try
            {
                // Baca request body dengan encoding UTF-8 (default untuk JSON)
                Encoding encoding = request.ContentEncoding ?? Encoding.UTF8;
                if (encoding == null || encoding.CodePage == 0)
                {
                    encoding = Encoding.UTF8;
                }
                
                using var reader = new StreamReader(request.InputStream, encoding);
                string body = await reader.ReadToEndAsync();

                // Configure JSON options untuk case-insensitive
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var drawerRequest = System.Text.Json.JsonSerializer.Deserialize<CashDrawerRequest>(body, jsonOptions);
                
                bool success = false;
                if (drawerRequest?.Pin == 1)
                {
                    success = _cashDrawerService.OpenCashDrawerPin1(showMessage: false);
                }
                else if (drawerRequest?.Pin == 2)
                {
                    success = _cashDrawerService.OpenCashDrawerPin2(showMessage: false);
                }
                else
                {
                    success = _cashDrawerService.OpenCashDrawer(showMessage: false);
                }

                return System.Text.Json.JsonSerializer.Serialize(new CashDrawerResponse
                {
                    Success = success,
                    Message = success ? "Cash drawer berhasil dibuka" : "Cash drawer gagal dibuka"
                });
            }
            catch (Exception ex)
            {
                return System.Text.Json.JsonSerializer.Serialize(new CashDrawerResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        private string HandleStatusRequest()
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                status = "running",
                port = _port,
                timestamp = DateTime.Now
            });
        }
    }
}

