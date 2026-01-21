using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace PosPrinterApp.Services
{
    /// <summary>
    /// Helper class untuk konversi bitmap ke ESC/POS commands
    /// </summary>
    public class BitmapPrinterHelper
    {
        /// <summary>
        /// Konversi bitmap ke ESC/POS bitmap printing commands
        /// </summary>
        /// <param name="bitmap">Bitmap yang akan dicetak</param>
        /// <param name="dither">Apakah menggunakan dithering untuk grayscale (default: true)</param>
        /// <returns>String ESC/POS commands untuk print bitmap</returns>
        public static string ConvertBitmapToEscPos(Bitmap bitmap, bool dither = true)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            var sb = new StringBuilder();
            
            Bitmap workingBitmap = bitmap;
            bool needsDispose = false;
            
            // Resize bitmap jika terlalu lebar (max 576 pixels untuk 80mm printer)
            int maxWidth = 576;
            if (bitmap.Width > maxWidth)
            {
                double ratio = (double)maxWidth / bitmap.Width;
                int newHeight = (int)(bitmap.Height * ratio);
                workingBitmap = new Bitmap(bitmap, maxWidth, newHeight);
                needsDispose = true;
            }

            try
            {
                // Konversi ke grayscale jika perlu
                Bitmap grayscaleBitmap = ConvertToGrayscale(workingBitmap);
                bool grayscaleNeedsDispose = (grayscaleBitmap != workingBitmap);
                
                try
                {
                    // Konversi ke monochrome (black & white) dengan threshold atau dithering
                    Bitmap monoBitmap;
                    if (dither)
                    {
                        monoBitmap = ConvertToMonochromeDither(grayscaleBitmap);
                    }
                    else
                    {
                        monoBitmap = ConvertToMonochrome(grayscaleBitmap);
                    }
                    
                    bool monoNeedsDispose = (monoBitmap != grayscaleBitmap);
                    
                    try
                    {
                        // Process bitmap
                        ProcessBitmap(monoBitmap, sb);
                    }
                    finally
                    {
                        if (monoNeedsDispose)
                        {
                            monoBitmap.Dispose();
                        }
                    }
                }
                finally
                {
                    if (grayscaleNeedsDispose)
                    {
                        grayscaleBitmap.Dispose();
                    }
                }
            }
            finally
            {
                if (needsDispose)
                {
                    workingBitmap.Dispose();
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Process bitmap dan tambahkan ESC/POS commands ke StringBuilder
        /// </summary>
        private static void ProcessBitmap(Bitmap monoBitmap, StringBuilder sb)
        {

            // Print bitmap menggunakan ESC/POS commands
            // ESC/POS menggunakan format: ESC * m nL nH d1...dk
            // m = mode (0 = 8-dot single-density, 1 = 8-dot double-density, 32 = 24-dot single-density, 33 = 24-dot double-density)
            // nL, nH = low dan high byte dari jumlah data
            // d1...dk = data bitmap

            int width = monoBitmap.Width;
            int height = monoBitmap.Height;
            
            // Set alignment center (opsional, bisa diubah)
            sb.Append((char)27); // ESC
            sb.Append("a");      // Select justification
            sb.Append((char)1);  // Center (0=left, 1=center, 2=right)

            // Print bitmap per baris (8 dots per baris untuk mode 0)
            int mode = 0; // 8-dot single-density
            int dotsPerLine = 8;
            
            for (int y = 0; y < height; y += dotsPerLine)
            {
                // Hitung jumlah baris yang akan diprint (maksimal 8 dots)
                int linesToPrint = Math.Min(dotsPerLine, height - y);
                
                // Set line spacing ke 0 untuk bitmap printing
                sb.Append((char)27); // ESC
                sb.Append("3");      // Set line spacing
                sb.Append((char)0);  // 0 dots
                
                // Hitung jumlah bytes per baris
                int bytesPerLine = (width + 7) / 8; // Round up to nearest byte
                int totalBytes = bytesPerLine * linesToPrint;
                
                // ESC * command untuk print bitmap
                sb.Append((char)27); // ESC
                sb.Append("*");      // Print raster image
                sb.Append((char)mode); // Mode
                
                // nL dan nH (low dan high byte dari totalBytes)
                sb.Append((char)(totalBytes & 0xFF));        // nL
                sb.Append((char)((totalBytes >> 8) & 0xFF)); // nH
                
                // Convert bitmap lines ke byte array
                for (int line = 0; line < linesToPrint; line++)
                {
                    int currentY = y + line;
                    if (currentY >= height) break;
                    
                    for (int byteIndex = 0; byteIndex < bytesPerLine; byteIndex++)
                    {
                        byte dataByte = 0;
                        int startX = byteIndex * 8;
                        
                        for (int bit = 0; bit < 8; bit++)
                        {
                            int x = startX + bit;
                            if (x < width)
                            {
                                Color pixel = monoBitmap.GetPixel(x, currentY);
                                // Jika pixel hitam (R, G, B < 128), set bit
                                if (pixel.R < 128)
                                {
                                    dataByte |= (byte)(1 << (7 - bit));
                                }
                            }
                        }
                        
                        sb.Append((char)dataByte);
                    }
                }
                
                // Line feed
                sb.Append((char)10); // LF
            }
            
            // Reset line spacing
            sb.Append((char)27); // ESC
            sb.Append("2");      // Set line spacing to default
            
            // Reset alignment
            sb.Append((char)27); // ESC
            sb.Append("a");      // Select justification
            sb.Append((char)0);  // Left alignment
        }

        /// <summary>
        /// Konversi bitmap ke grayscale
        /// </summary>
        private static Bitmap ConvertToGrayscale(Bitmap original)
        {
            if (original.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                // Sudah grayscale
                return new Bitmap(original);
            }

            Bitmap grayscale = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb);
            
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color originalColor = original.GetPixel(x, y);
                    int grayValue = (int)(0.299 * originalColor.R + 0.587 * originalColor.G + 0.114 * originalColor.B);
                    Color grayColor = Color.FromArgb(grayValue, grayValue, grayValue);
                    grayscale.SetPixel(x, y, grayColor);
                }
            }
            
            return grayscale;
        }

        /// <summary>
        /// Konversi grayscale bitmap ke monochrome dengan threshold
        /// </summary>
        private static Bitmap ConvertToMonochrome(Bitmap grayscale, int threshold = 128)
        {
            // Gunakan Format24bppRgb untuk memudahkan SetPixel, lalu konversi ke Format1bppIndexed
            Bitmap mono = new Bitmap(grayscale.Width, grayscale.Height, PixelFormat.Format24bppRgb);
            
            for (int y = 0; y < grayscale.Height; y++)
            {
                for (int x = 0; x < grayscale.Width; x++)
                {
                    Color pixel = grayscale.GetPixel(x, y);
                    Color monoColor = (pixel.R < threshold) ? Color.Black : Color.White;
                    mono.SetPixel(x, y, monoColor);
                }
            }
            
            return mono;
        }

        /// <summary>
        /// Konversi grayscale bitmap ke monochrome dengan dithering (Floyd-Steinberg)
        /// </summary>
        private static Bitmap ConvertToMonochromeDither(Bitmap grayscale)
        {
            // Buat copy untuk error diffusion
            int[,] error = new int[grayscale.Width, grayscale.Height];
            
            // Initialize dengan nilai grayscale
            for (int y = 0; y < grayscale.Height; y++)
            {
                for (int x = 0; x < grayscale.Width; x++)
                {
                    Color pixel = grayscale.GetPixel(x, y);
                    error[x, y] = pixel.R;
                }
            }
            
            // Gunakan Format24bppRgb untuk memudahkan SetPixel
            Bitmap mono = new Bitmap(grayscale.Width, grayscale.Height, PixelFormat.Format24bppRgb);
            
            // Floyd-Steinberg dithering
            for (int y = 0; y < grayscale.Height; y++)
            {
                for (int x = 0; x < grayscale.Width; x++)
                {
                    int oldPixel = error[x, y];
                    int newPixel = (oldPixel < 128) ? 0 : 255;
                    int quantError = oldPixel - newPixel;
                    
                    mono.SetPixel(x, y, (newPixel == 0) ? Color.Black : Color.White);
                    
                    // Distribute error
                    if (x + 1 < grayscale.Width)
                        error[x + 1, y] += (quantError * 7) / 16;
                    if (x - 1 >= 0 && y + 1 < grayscale.Height)
                        error[x - 1, y + 1] += (quantError * 3) / 16;
                    if (y + 1 < grayscale.Height)
                        error[x, y + 1] += (quantError * 5) / 16;
                    if (x + 1 < grayscale.Width && y + 1 < grayscale.Height)
                        error[x + 1, y + 1] += (quantError * 1) / 16;
                }
            }
            
            return mono;
        }
    }
}

