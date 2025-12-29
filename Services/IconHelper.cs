using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PosPrinterApp.Services
{
    /// <summary>
    /// Helper untuk konversi PNG ke Icon untuk Windows Forms
    /// </summary>
    public static class IconHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr DestroyIcon(IntPtr handle);

        /// <summary>
        /// Konversi PNG file ke Icon
        /// </summary>
        public static Icon PngToIcon(string pngPath, Size? size = null)
        {
            if (!File.Exists(pngPath))
                throw new FileNotFoundException("PNG file not found", pngPath);

            using (var bitmap = new Bitmap(pngPath))
            {
                return BitmapToIcon(bitmap, size ?? new Size(32, 32));
            }
        }

        /// <summary>
        /// Konversi Bitmap ke Icon
        /// </summary>
        public static Icon BitmapToIcon(Bitmap bitmap, Size size)
        {
            // Resize bitmap jika diperlukan
            Bitmap resizedBitmap = bitmap;
            if (bitmap.Width != size.Width || bitmap.Height != size.Height)
            {
                resizedBitmap = new Bitmap(bitmap, size);
            }

            // Konversi ke Icon
            IntPtr hIcon = resizedBitmap.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            
            // Clone icon karena FromHandle tidak membuat copy
            Icon clonedIcon = (Icon)icon.Clone();
            
            // Cleanup
            if (resizedBitmap != bitmap)
            {
                resizedBitmap.Dispose();
            }
            
            return clonedIcon;
        }

        /// <summary>
        /// Load icon dari PNG dengan berbagai ukuran
        /// </summary>
        public static Icon LoadIconFromPng(string fileName = "logo_pos.png")
        {
            try
            {
                // Coba berbagai lokasi
                string[] paths = new[]
                {
                    fileName, // Path langsung jika sudah full path
                    Path.Combine(Application.StartupPath, "image", fileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", fileName),
                    Path.Combine(Directory.GetCurrentDirectory(), "image", fileName),
                    Path.Combine(Application.StartupPath, fileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName)
                };

                foreach (string path in paths)
                {
                    if (File.Exists(path))
                    {
                        return PngToIcon(path, new Size(32, 32));
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

