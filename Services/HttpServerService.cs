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
        private int _port = 7080;
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
                Log("  POST /api/print - Print receipt (plain text)");
                Log("  POST /api/print-html - Print receipt (HTML format)");
                Log("  POST /api/print-receipt - Print receipt dari data structured");
                Log("  POST /api/cashdrawer - Buka cash drawer");
                Log("  POST /api/print-and-drawer - Print dan buka cash drawer");

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

                // Enable CORS - Set headers untuk semua request
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS, PUT, DELETE");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
                response.Headers.Add("Access-Control-Max-Age", "3600");

                // Handle preflight OPTIONS request
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
                else if (path == "/api/print-html" && method == "POST")
                {
                    responseText = await HandlePrintHtmlRequestAsync(request);
                    response.StatusCode = 200;
                }
                else if (path == "/api/print-receipt" && method == "POST")
                {
                    responseText = await HandlePrintReceiptDataRequestAsync(request);
                    response.StatusCode = 200;
                }
                else if (path == "/api/cashdrawer" && method == "POST")
                {
                    responseText = await HandleCashDrawerRequestAsync(request);
                    response.StatusCode = 200;
                }
                else if (path == "/api/print-and-drawer" && method == "POST")
                {
                    responseText = await HandlePrintAndDrawerRequestAsync(request);
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
                
                // Pastikan CORS headers juga ada di error response
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS, PUT, DELETE");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
                
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

        private async Task<string> HandlePrintHtmlRequestAsync(HttpListenerRequest request)
        {
            try
            {
                // Baca request body dengan encoding UTF-8
                Encoding encoding = request.ContentEncoding ?? Encoding.UTF8;
                if (encoding == null || encoding.CodePage == 0)
                {
                    encoding = Encoding.UTF8;
                }
                
                using var reader = new StreamReader(request.InputStream, encoding);
                string body = await reader.ReadToEndAsync();
                
                Log($"Print HTML Request body length: {body.Length}");

                // Configure JSON options untuk case-insensitive dan allow trailing commas
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var printRequest = System.Text.Json.JsonSerializer.Deserialize<PrintHtmlRequest>(body, jsonOptions);
                
                if (printRequest == null)
                {
                    Log("Deserialisasi gagal - printRequest is null");
                    return System.Text.Json.JsonSerializer.Serialize(new PrintHtmlResponse
                    {
                        Success = false,
                        Message = "Format JSON tidak valid"
                    });
                }
                
                if (string.IsNullOrWhiteSpace(printRequest.HtmlContent))
                {
                    Log("HTML Content kosong atau null");
                    return System.Text.Json.JsonSerializer.Serialize(new PrintHtmlResponse
                    {
                        Success = false,
                        Message = "HTML Content tidak boleh kosong"
                    });
                }

                Log($"Printing HTML content (length: {printRequest.HtmlContent.Length})");
                bool success = _printerService.PrintHtml(printRequest.HtmlContent, printRequest.CutPaper);
                
                return System.Text.Json.JsonSerializer.Serialize(new PrintHtmlResponse
                {
                    Success = success,
                    Message = success ? "Print HTML berhasil" : "Print HTML gagal"
                });
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                Log($"JSON parsing error: {jsonEx.Message}");
                return System.Text.Json.JsonSerializer.Serialize(new PrintHtmlResponse
                {
                    Success = false,
                    Message = $"Format JSON tidak valid: {jsonEx.Message}"
                });
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}\nStack: {ex.StackTrace}");
                return System.Text.Json.JsonSerializer.Serialize(new PrintHtmlResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        private async Task<string> HandlePrintReceiptDataRequestAsync(HttpListenerRequest request)
        {
            try
            {
                // Baca request body dengan encoding UTF-8
                Encoding encoding = request.ContentEncoding ?? Encoding.UTF8;
                if (encoding == null || encoding.CodePage == 0)
                {
                    encoding = Encoding.UTF8;
                }
                
                using var reader = new StreamReader(request.InputStream, encoding);
                string body = await reader.ReadToEndAsync();
                
                Log($"Print Receipt Data Request body length: {body.Length}");

                // Configure JSON options untuk case-insensitive dan allow trailing commas
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                };

                var printRequest = System.Text.Json.JsonSerializer.Deserialize<PrintReceiptDataRequest>(body, jsonOptions);
                
                if (printRequest == null)
                {
                    Log("Deserialisasi gagal - printRequest is null");
                    return System.Text.Json.JsonSerializer.Serialize(new PrintReceiptDataResponse
                    {
                        Success = false,
                        Message = "Format JSON tidak valid"
                    });
                }
                
                if (printRequest.Company == null && printRequest.TransHead == null)
                {
                    Log("Data Company dan TransHead kosong");
                    return System.Text.Json.JsonSerializer.Serialize(new PrintReceiptDataResponse
                    {
                        Success = false,
                        Message = "Data tidak boleh kosong"
                    });
                }

                Log($"Printing receipt from data (Order No: {printRequest.TransHead?.OrderNo ?? "N/A"})");
                bool success = _printerService.PrintReceiptFromData(printRequest, printRequest.CutPaper);
                
                return System.Text.Json.JsonSerializer.Serialize(new PrintReceiptDataResponse
                {
                    Success = success,
                    Message = success ? "Print receipt berhasil" : "Print receipt gagal"
                });
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                Log($"JSON parsing error: {jsonEx.Message}");
                return System.Text.Json.JsonSerializer.Serialize(new PrintReceiptDataResponse
                {
                    Success = false,
                    Message = $"Format JSON tidak valid: {jsonEx.Message}"
                });
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}\nStack: {ex.StackTrace}");
                return System.Text.Json.JsonSerializer.Serialize(new PrintReceiptDataResponse
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

        private async Task<string> HandlePrintAndDrawerRequestAsync(HttpListenerRequest request)
        {
            try
            {
                // Baca request body dengan encoding UTF-8
                Encoding encoding = request.ContentEncoding ?? Encoding.UTF8;
                if (encoding == null || encoding.CodePage == 0)
                {
                    encoding = Encoding.UTF8;
                }
                
                using var reader = new StreamReader(request.InputStream, encoding);
                string body = await reader.ReadToEndAsync();
                
                Log($"Print and Drawer Request body: {body}");

                // Configure JSON options
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var printDrawerRequest = System.Text.Json.JsonSerializer.Deserialize<PrintAndDrawerRequest>(body, jsonOptions);
                
                if (printDrawerRequest == null)
                {
                    return System.Text.Json.JsonSerializer.Serialize(new PrintAndDrawerResponse
                    {
                        Success = false,
                        Message = "Format JSON tidak valid",
                        PrintSuccess = false,
                        DrawerSuccess = false
                    });
                }
                
                if (string.IsNullOrWhiteSpace(printDrawerRequest.Content))
                {
                    return System.Text.Json.JsonSerializer.Serialize(new PrintAndDrawerResponse
                    {
                        Success = false,
                        Message = "Content tidak boleh kosong",
                        PrintSuccess = false,
                        DrawerSuccess = false
                    });
                }

                // Print dulu
                Log($"Printing content: {printDrawerRequest.Content.Substring(0, Math.Min(50, printDrawerRequest.Content.Length))}...");
                bool printSuccess = _printerService.PrintCustomText(printDrawerRequest.Content, printDrawerRequest.CutPaper);
                
                // Delay sebelum buka drawer (default: 500ms)
                int delay = printDrawerRequest.DrawerDelay ?? 500;
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }
                
                // Buka cash drawer
                bool drawerSuccess = false;
                if (printDrawerRequest.DrawerPin == 1)
                {
                    drawerSuccess = _cashDrawerService.OpenCashDrawerPin1(showMessage: false);
                }
                else if (printDrawerRequest.DrawerPin == 2)
                {
                    drawerSuccess = _cashDrawerService.OpenCashDrawerPin2(showMessage: false);
                }
                else
                {
                    drawerSuccess = _cashDrawerService.OpenCashDrawer(showMessage: false);
                }
                
                bool overallSuccess = printSuccess && drawerSuccess;
                string message = "";
                if (overallSuccess)
                {
                    message = "Print dan buka cash drawer berhasil";
                }
                else if (printSuccess && !drawerSuccess)
                {
                    message = "Print berhasil, tapi cash drawer gagal dibuka";
                }
                else if (!printSuccess && drawerSuccess)
                {
                    message = "Print gagal, tapi cash drawer berhasil dibuka";
                }
                else
                {
                    message = "Print dan cash drawer gagal";
                }
                
                return System.Text.Json.JsonSerializer.Serialize(new PrintAndDrawerResponse
                {
                    Success = overallSuccess,
                    Message = message,
                    PrintSuccess = printSuccess,
                    DrawerSuccess = drawerSuccess
                });
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                Log($"JSON parsing error: {jsonEx.Message}");
                return System.Text.Json.JsonSerializer.Serialize(new PrintAndDrawerResponse
                {
                    Success = false,
                    Message = $"Format JSON tidak valid: {jsonEx.Message}",
                    PrintSuccess = false,
                    DrawerSuccess = false
                });
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}\nStack: {ex.StackTrace}");
                return System.Text.Json.JsonSerializer.Serialize(new PrintAndDrawerResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    PrintSuccess = false,
                    DrawerSuccess = false
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

