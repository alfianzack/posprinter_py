using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PosPrinterApp.Services
{
    public class PosPrinterService
    {
        private string _printerName;

        public PosPrinterService(string printerName = null)
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
            try
            {
                // ESC/POS commands
                var sb = new StringBuilder();
                
                // Initialize printer
                sb.Append((char)27); // ESC
                sb.Append("@");      // Initialize printer
                
                // Set alignment center
                sb.Append((char)27);
                sb.Append("a");
                sb.Append((char)1);  // Center alignment
                
                // Set text size (double width and height)
                sb.Append((char)29);
                sb.Append("!");
                sb.Append((char)17); // Double width and height
                
                // Add content
                sb.Append(content);
                
                // Reset text size
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

        public bool PrintTestReceipt()
        {
            var receipt = new StringBuilder();
            receipt.AppendLine("=================================");
            receipt.AppendLine("       TOKO CONTOH");
            receipt.AppendLine("   Jl. Contoh No. 123");
            receipt.AppendLine("   Telp: 081234567890");
            receipt.AppendLine("=================================");
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

        public bool PrintCustomText(string text, bool cutPaper = true)
        {
            return PrintReceipt(text, cutPaper);
        }
    }
}

