using System;
using System.Windows.Forms;
using PosPrinterApp.Services;

namespace PosPrinterApp.Services
{
    public class CashDrawerService
    {
        private string _printerName;

        public CashDrawerService(string? printerName = null)
        {
            _printerName = printerName ?? GetDefaultPrinter();
        }

        public string GetDefaultPrinter()
        {
            System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
            return pd.PrinterSettings.PrinterName;
        }

        public void SetPrinter(string printerName)
        {
            _printerName = printerName;
        }

        public bool OpenCashDrawer(bool showMessage = true)
        {
            try
            {
                // ESC/POS command untuk membuka cash drawer
                // ESC p 0 25 250 (decimal)
                // atau ESC p 0 50 250 untuk pin 2
                var command = new System.Text.StringBuilder();
                
                // Method 1: Pin 2 (umum digunakan)
                command.Append((char)27);  // ESC
                command.Append("p");        // Print and feed
                command.Append((char)0);   // Pin number (0 = pin 2)
                command.Append((char)50);  // On time (50 * 2ms = 100ms)
                command.Append((char)250); // Off time (250 * 2ms = 500ms)

                // Alternatif: Pin 1 (beberapa printer menggunakan ini)
                // command.Append((char)27);
                // command.Append("p");
                // command.Append((char)1);
                // command.Append((char)50);
                // command.Append((char)250);

                bool result = RawPrinterHelper.SendStringToPrinter(_printerName, command.ToString());
                
                if (showMessage)
                {
                    if (result)
                    {
                        MessageBox.Show("Cash drawer berhasil dibuka!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Gagal membuka cash drawer. Pastikan printer terhubung dan cash drawer terpasang.", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                if (showMessage)
                {
                    MessageBox.Show($"Error membuka cash drawer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
        }

        public bool OpenCashDrawerPin1(bool showMessage = false)
        {
            try
            {
                var command = new System.Text.StringBuilder();
                command.Append((char)27);  // ESC
                command.Append("p");       // Print and feed
                command.Append((char)1);   // Pin number (1 = pin 1)
                command.Append((char)50);  // On time
                command.Append((char)250); // Off time

                return RawPrinterHelper.SendStringToPrinter(_printerName, command.ToString());
            }
            catch (Exception ex)
            {
                if (showMessage)
                {
                    MessageBox.Show($"Error membuka cash drawer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
        }

        public bool OpenCashDrawerPin2(bool showMessage = false)
        {
            try
            {
                var command = new System.Text.StringBuilder();
                command.Append((char)27);  // ESC
                command.Append("p");       // Print and feed
                command.Append((char)0);   // Pin number (0 = pin 2)
                command.Append((char)50);  // On time
                command.Append((char)250); // Off time

                return RawPrinterHelper.SendStringToPrinter(_printerName, command.ToString());
            }
            catch (Exception ex)
            {
                if (showMessage)
                {
                    MessageBox.Show($"Error membuka cash drawer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }
        }
    }
}

