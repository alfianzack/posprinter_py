using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace PosPrinterApp.Services
{
    /// <summary>
    /// Helper class untuk render HTML ke bitmap menggunakan WebView2
    /// </summary>
    public class HtmlRendererHelper
    {
        /// <summary>
        /// Render HTML content ke bitmap dengan ukuran tertentu
        /// </summary>
        /// <param name="htmlContent">Konten HTML yang akan di-render</param>
        /// <param name="width">Lebar bitmap dalam pixels (default: 576 untuk 80mm printer)</param>
        /// <param name="height">Tinggi bitmap dalam pixels (akan dihitung otomatis jika 0)</param>
        /// <returns>Bitmap hasil render</returns>
        public static async Task<Bitmap> RenderHtmlToBitmapAsync(string htmlContent, int width = 576, int height = 0)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                throw new ArgumentException("HTML content tidak boleh kosong", nameof(htmlContent));

            // Buat form tersembunyi untuk WebView2
            Form renderForm = null;
            WebView2 webView = null;
            Bitmap result = null;

            try
            {
                // Buat form tersembunyi
                renderForm = new Form
                {
                    WindowState = FormWindowState.Minimized,
                    ShowInTaskbar = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Size = new Size(width, height > 0 ? height : 1000),
                    Visible = false
                };

                // Buat WebView2
                webView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    Size = new Size(width, height > 0 ? height : 1000)
                };

                renderForm.Controls.Add(webView);
                renderForm.Show();

                // Initialize WebView2
                await webView.EnsureCoreWebView2Async();

                // Set ukuran virtual untuk rendering
                webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                // Load HTML content
                webView.NavigateToString(htmlContent);

                // Tunggu sampai page loaded
                bool pageLoaded = false;
                webView.NavigationCompleted += (sender, e) =>
                {
                    if (e.IsSuccess)
                    {
                        pageLoaded = true;
                    }
                };

                // Tunggu maksimal 10 detik untuk page load
                int waitCount = 0;
                while (!pageLoaded && waitCount < 100)
                {
                    await Task.Delay(100);
                    Application.DoEvents();
                    waitCount++;
                }

                // Tunggu sedikit lagi untuk memastikan rendering selesai
                await Task.Delay(500);
                Application.DoEvents();

                // Get content height menggunakan JavaScript
                string heightScript = @"
                    (function() {
                        return Math.max(
                            document.body.scrollHeight,
                            document.body.offsetHeight,
                            document.documentElement.clientHeight,
                            document.documentElement.scrollHeight,
                            document.documentElement.offsetHeight
                        );
                    })();
                ";

                var heightResult = await webView.CoreWebView2.ExecuteScriptAsync(heightScript);
                int contentHeight = 1000; // default
                
                if (!string.IsNullOrEmpty(heightResult))
                {
                    // Parse hasil (biasanya dalam format seperti "1000")
                    heightResult = heightResult.Trim('"');
                    if (int.TryParse(heightResult, out int parsedHeight))
                    {
                        contentHeight = parsedHeight;
                    }
                }

                // Jika height tidak ditentukan, gunakan content height
                if (height == 0)
                {
                    height = contentHeight;
                    renderForm.Size = new Size(width, height);
                    webView.Size = new Size(width, height);
                    await Task.Delay(300);
                    Application.DoEvents();
                }

                // Gunakan pendekatan screenshot dari control
                // WebView2 perlu visible untuk screenshot, jadi kita buat visible sebentar
                renderForm.Visible = true;
                renderForm.BringToFront();
                renderForm.Location = new Point(-10000, -10000); // Pindahkan ke luar layar
                await Task.Delay(500); // Tunggu rendering selesai
                Application.DoEvents();

                // Capture screenshot dari form
                result = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.CopyFromScreen(renderForm.PointToScreen(Point.Empty), Point.Empty, new Size(width, height));
                }

                renderForm.Visible = false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error rendering HTML to bitmap: {ex.Message}", ex);
            }
            finally
            {
                // Cleanup
                if (webView != null)
                {
                    webView.Dispose();
                }
                if (renderForm != null)
                {
                    renderForm.Close();
                    renderForm.Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// Render HTML content ke bitmap dengan ukuran otomatis berdasarkan content
        /// </summary>
        public static async Task<Bitmap> RenderHtmlToBitmapAutoSizeAsync(string htmlContent, int maxWidth = 576)
        {
            // Render dengan height besar dulu untuk mendapatkan actual height
            var tempBitmap = await RenderHtmlToBitmapAsync(htmlContent, maxWidth, 5000);
            
            // Hitung height yang sebenarnya (cari baris terakhir yang tidak putih)
            int actualHeight = GetActualContentHeight(tempBitmap);
            
            // Render ulang dengan height yang tepat
            tempBitmap.Dispose();
            return await RenderHtmlToBitmapAsync(htmlContent, maxWidth, actualHeight);
        }

        /// <summary>
        /// Dapatkan tinggi konten yang sebenarnya dari bitmap (menghindari area putih di bawah)
        /// </summary>
        private static int GetActualContentHeight(Bitmap bitmap)
        {
            int height = bitmap.Height;
            
            // Cari dari bawah ke atas untuk menemukan baris terakhir yang tidak putih
            for (int y = bitmap.Height - 1; y >= 0; y--)
            {
                bool hasContent = false;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    // Jika pixel tidak putih (dengan toleransi)
                    if (pixel.R < 250 || pixel.G < 250 || pixel.B < 250)
                    {
                        hasContent = true;
                        break;
                    }
                }
                
                if (hasContent)
                {
                    height = y + 1;
                    break;
                }
            }
            
            // Tambahkan sedikit margin di bawah
            return Math.Min(height + 20, bitmap.Height);
        }
    }
}

