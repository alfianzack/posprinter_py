using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using PosPrinterApp.Resources;

namespace PosPrinterApp.Services
{
    public class PosPrinterService
    {
        private string _printerName;

        public PosPrinterService(string? printerName = null)
        {
            _printerName = printerName ?? GetDefaultPrinter();
        }

        public string GetDefaultPrinter()
        {
            PrintDocument pd = new PrintDocument();
            return pd.PrinterSettings.PrinterName;
        }

        public void SetPrinter(string printerName)
        {
            _printerName = printerName;
        }

        public bool PrintReceipt(string content, bool cutPaper = true)
        {
            return PrintReceipt(content, cutPaper, FontSize.Normal);
        }

        /// <summary>
        /// Print receipt dengan kontrol font size
        /// </summary>
        /// <param name="content">Konten yang akan dicetak</param>
        /// <param name="cutPaper">Apakah kertas harus dipotong setelah print</param>
        /// <param name="fontSize">Ukuran font (Normal, Small, Medium, Large)</param>
        public bool PrintReceipt(string content, bool cutPaper, FontSize fontSize)
        {
            try
            {
                // ESC/POS commands
                var sb = new StringBuilder();
                
                // Initialize printer
                sb.Append((char)27); // ESC
                sb.Append("@");      // Initialize printer
                
                // Set alignment left (default, bisa diubah per baris jika perlu)
                sb.Append((char)27);
                sb.Append("a");
                sb.Append((char)0);  // Left alignment (0=left, 1=center, 2=right)
                
                // Set font size sesuai parameter
                sb.Append((char)29); // GS
                sb.Append("!");      // Select character size
                sb.Append((char)GetFontSizeValue(fontSize));
                
                // Add content
                sb.Append(content);
                
                // Reset text size ke normal
                sb.Append((char)29);
                sb.Append("!");
                sb.Append((char)0);
                
                // Line feed
                sb.Append((char)10);
                sb.Append((char)10);
                
                // Cut paper if requested
                if (cutPaper)
                {
                    sb.Append((char)29);
                    sb.Append("V");
                    sb.Append((char)66);
                    sb.Append((char)0);
                }
                
                // Print using RawPrinterHelper
                return RawPrinterHelper.SendStringToPrinter(_printerName, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Enum untuk ukuran font
        /// </summary>
        public enum FontSize
        {
            /// <summary>
            /// Ukuran normal (1x1)
            /// </summary>
            Normal = 0,
            /// <summary>
            /// Ukuran kecil (belum didukung, fallback ke normal)
            /// </summary>
            Small = 0,
            /// <summary>
            /// Double width (2x1)
            /// </summary>
            Medium = 16,
            /// <summary>
            /// Double width and height (2x2)
            /// </summary>
            Large = 17
        }

        /// <summary>
        /// Get ESC/POS font size value
        /// </summary>
        private int GetFontSizeValue(FontSize fontSize)
        {
            return (int)fontSize;
        }

        public bool PrintTestReceipt()
        {
            var receipt = new StringBuilder();
            
            // Tambahkan logo DXN
            receipt.Append(DxnLogo.GetReceiptLogo());
            receipt.AppendLine();
            receipt.AppendLine($"Tanggal: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            receipt.AppendLine("---------------------------------");
            receipt.AppendLine("Item 1              Rp 10.000");
            receipt.AppendLine("Item 2              Rp 15.000");
            receipt.AppendLine("Item 3              Rp 20.000");
            receipt.AppendLine("---------------------------------");
            receipt.AppendLine($"Total               Rp 45.000");
            receipt.AppendLine();
            receipt.AppendLine("Terima Kasih");
            receipt.AppendLine("=================================");

            return PrintReceipt(receipt.ToString());
        }
        
        /// <summary>
        /// Print receipt dengan logo DXN
        /// </summary>
        public bool PrintReceiptWithLogo(string content, bool includeLogo = true, bool cutPaper = true)
        {
            var receipt = new StringBuilder();
            
            if (includeLogo)
            {
                receipt.Append(DxnLogo.GetReceiptLogo());
                receipt.AppendLine();
            }
            
            receipt.Append(content);
            
            return PrintReceipt(receipt.ToString(), cutPaper);
        }

        public bool PrintCustomText(string text, bool cutPaper = true)
        {
            return PrintReceipt(text, cutPaper, FontSize.Normal);
        }
    }
}

