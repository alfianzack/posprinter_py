using System;
using System.Collections.Generic;
using System.Text;
using PosPrinterApp.Services;

namespace PosPrinterApp.Examples
{
    /// <summary>
    /// Contoh penggunaan EscPosPrinterService
    /// </summary>
    public class EscPosPrinterExample
    {
        public static void Example1_SimplePrint()
        {
            var printer = new EscPosPrinterService();
            
            // Print sederhana
            printer.PrintReceipt("Hello World\nThis is a test receipt", cutPaper: true);
        }

        public static void Example2_FormattedReceipt()
        {
            var printer = new EscPosPrinterService();
            
            // Buat receipt content
            var receipt = new ReceiptContent
            {
                CompanyName = "DXN COMPANY",
                CompanyAddress = "123 Main Street, Jakarta",
                CompanyPhone = "123-456-7890",
                OrderNo = "ORD001",
                Date = "2024-01-01 10:00:00",
                Items = new List<ReceiptItem>
                {
                    new ReceiptItem
                    {
                        ProductName = "Product 1",
                        Description = "Size: Large",
                        Price = 50.00,
                        Quantity = 2
                    },
                    new ReceiptItem
                    {
                        ProductName = "Product 2",
                        Description = "Size: Medium",
                        Price = 30.00,
                        Quantity = 1
                    }
                },
                Total = 130.00,
                Footer = "Thank you. Please come again."
            };
            
            // Print formatted receipt
            printer.PrintFormattedReceipt(receipt, cutPaper: true);
        }

        public static void Example3_ManualFormatting()
        {
            var printer = new EscPosPrinterService();
            
            var sb = new StringBuilder();
            
            // Initialize
            sb.Append(printer.InitializePrinter());
            
            // Header (bold, center, large)
            sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Center));
            sb.Append(printer.SetBold(true));
            sb.Append(printer.SetFontSize(2, 2)); // Double width and height
            sb.Append("DXN COMPANY");
            sb.Append(printer.SetFontSize(1, 1));
            sb.Append(printer.SetBold(false));
            sb.Append(printer.FeedLines(1));
            
            // Draw line
            sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Left));
            sb.Append(printer.DrawLine('=', 48));
            sb.Append(printer.FeedLines(1));
            
            // Order info (bold)
            sb.Append(printer.SetBold(true));
            sb.Append("Order No: ORD001");
            sb.Append(printer.SetBold(false));
            sb.Append(printer.FeedLines(1));
            
            // Draw line
            sb.Append(printer.DrawLine('-', 48));
            sb.Append(printer.FeedLines(1));
            
            // Items
            sb.Append("Product 1");
            sb.Append(printer.FeedLines(1));
            sb.Append("  Size: Large");
            sb.Append(printer.FeedLines(1));
            sb.Append("Product 2");
            sb.Append(printer.FeedLines(1));
            
            // Total (bold, large)
            sb.Append(printer.DrawLine('-', 48));
            sb.Append(printer.FeedLines(1));
            sb.Append(printer.SetBold(true));
            sb.Append(printer.SetFontSize(1, 2)); // Double height
            sb.Append("Total: Rp 100.00");
            sb.Append(printer.SetFontSize(1, 1));
            sb.Append(printer.SetBold(false));
            sb.Append(printer.FeedLines(1));
            
            // Draw line
            sb.Append(printer.DrawLine('=', 48));
            sb.Append(printer.FeedLines(2));
            
            // Footer (center)
            sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Center));
            sb.Append("Thank you!");
            sb.Append(printer.FeedLines(2));
            
            // Cut paper
            sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Left));
            sb.Append(printer.CutPaper());
            
            // Print
            RawPrinterHelper.SendStringToPrinter(printer.GetDefaultPrinter(), sb.ToString());
        }
    }
}
