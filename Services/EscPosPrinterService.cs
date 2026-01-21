using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PosPrinterApp.Models;

namespace PosPrinterApp.Services
{
    /// <summary>
    /// Service untuk print receipt dengan ESC/POS commands
    /// Mendukung bold text, font size, dan draw line
    /// </summary>
    public class EscPosPrinterService
    {
        private string _printerName;
        private const int ReceiptWidth = 48; // Standard 80mm printer width in characters

        public EscPosPrinterService(string? printerName = null)
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

        /// <summary>
        /// Print receipt dengan format yang lebih kaya (bold, font size, line)
        /// </summary>
        public bool PrintReceipt(string content, bool cutPaper = true)
        {
            try
            {
                var sb = new StringBuilder();
                
                // Initialize printer
                sb.Append(InitializePrinter());
                
                // Add content
                sb.Append(content);
                
                // Feed lines
                sb.Append(FeedLines(2));
                
                // Cut paper if requested
                if (cutPaper)
                {
                    sb.Append(CutPaper());
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
        /// Print receipt dengan format kaya (bold, font size, line)
        /// </summary>
        public bool PrintFormattedReceipt(ReceiptContent content, bool cutPaper = true)
        {
            try
            {
                var sb = new StringBuilder();
                
                // Initialize printer
                sb.Append(InitializePrinter());
                
                // Company header (bold, center)
                if (!string.IsNullOrEmpty(content.CompanyName))
                {
                    sb.Append(SetAlignment(Alignment.Center));
                    sb.Append(SetBold(true));
                    sb.Append(content.CompanyName);
                    sb.Append(SetBold(false));
                    sb.Append(FeedLines(1));
                }
                
                if (!string.IsNullOrEmpty(content.CompanyAddress))
                {
                    sb.Append(SetAlignment(Alignment.Center));
                    sb.Append(content.CompanyAddress);
                    sb.Append(FeedLines(1));
                }
                
                if (!string.IsNullOrEmpty(content.CompanyPhone))
                {
                    sb.Append(SetAlignment(Alignment.Center));
                    sb.Append(content.CompanyPhone);
                    sb.Append(FeedLines(1));
                }
                
                // Draw line
                sb.Append(SetAlignment(Alignment.Left));
                sb.Append(DrawLine('=', ReceiptWidth));
                sb.Append(FeedLines(1));
                
                // Queue No (font size 2 and bold, center)
                if (!string.IsNullOrEmpty(content.QueueNo))
                {
                    sb.Append(SetAlignment(Alignment.Center));
                    sb.Append(SetBold(true));
                    sb.Append(SetFontSize(2, 2)); // Double width and height
                    sb.Append(content.QueueNo);
                    sb.Append(SetFontSize(1, 1));
                    sb.Append(SetBold(false));
                    sb.Append(FeedLines(1));
                    sb.Append(SetAlignment(Alignment.Left));
                    sb.Append(DrawLine('=', ReceiptWidth));
                    sb.Append(FeedLines(1));
                }
                
                // Document Type (bold, center)
                if (!string.IsNullOrEmpty(content.DocType))
                {
                    sb.Append(SetAlignment(Alignment.Center));
                    sb.Append(SetBold(true));
                    sb.Append(content.DocType);
                    sb.Append(SetBold(false));
                    sb.Append(FeedLines(1));
                    sb.Append(SetAlignment(Alignment.Left));
                    sb.Append(DrawLine('-', ReceiptWidth));
                    sb.Append(FeedLines(1));
                }
                
                // Order info
                if (!string.IsNullOrEmpty(content.OrderNo))
                {
                    sb.Append(SetBold(true));
                    sb.Append($"Order No: {content.OrderNo}");
                    sb.Append(SetBold(false));
                    sb.Append(FeedLines(1));
                }
                
                if (!string.IsNullOrEmpty(content.Date))
                {
                    sb.Append($"Date: {content.Date}");
                    sb.Append(FeedLines(1));
                }
                
                // Draw line
                sb.Append(DrawLine('-', ReceiptWidth));
                sb.Append(FeedLines(1));
                
                // Items
                if (content.Items != null && content.Items.Count > 0)
                {
                    foreach (var item in content.Items)
                    {
                        // Product name (bold)
                        if (!string.IsNullOrEmpty(item.ProductName))
                        {
                            sb.Append(SetBold(true));
                            sb.Append(item.ProductName);
                            sb.Append(SetBold(false));
                            sb.Append(FeedLines(1));
                        }
                        
                        // Description
                        if (!string.IsNullOrEmpty(item.Description))
                        {
                            sb.Append($"  {item.Description}");
                            sb.Append(FeedLines(1));
                        }
                        
                        // Price (right aligned)
                        if (item.Price.HasValue)
                        {
                            string priceStr = item.Price.Value.ToString("F2");
                            string productName = item.ProductName ?? "";
                            int padding = ReceiptWidth - productName.Length - priceStr.Length;
                            if (padding > 0)
                            {
                                sb.Append(productName);
                                sb.Append(new string(' ', padding));
                                sb.Append(priceStr);
                            }
                            else
                            {
                                sb.Append(productName);
                                sb.Append(FeedLines(1));
                                sb.Append(priceStr);
                            }
                            sb.Append(FeedLines(1));
                        }
                    }
                }
                
                // Draw line
                sb.Append(DrawLine('-', ReceiptWidth));
                sb.Append(FeedLines(1));
                
                // Total
                if (content.Total.HasValue)
                {
                    sb.Append(SetBold(true));
                    sb.Append($"Total: {content.Total.Value:F2}");
                    sb.Append(SetBold(false));
                    sb.Append(FeedLines(1));
                }
                
                // Grand Total (font size 2 and bold)
                if (content.GrandTotal.HasValue)
                {
                    sb.Append(DrawLine('=', ReceiptWidth));
                    sb.Append(FeedLines(1));
                    sb.Append(SetBold(true));
                    sb.Append(SetFontSize(2, 2)); // Double width and height
                    sb.Append($"Grand Total: {content.GrandTotal.Value:F2}");
                    sb.Append(SetFontSize(1, 1));
                    sb.Append(SetBold(false));
                    sb.Append(FeedLines(1));
                }
                else if (content.Total.HasValue)
                {
                    // Jika tidak ada GrandTotal, gunakan Total dengan format besar
                    sb.Append(DrawLine('=', ReceiptWidth));
                    sb.Append(FeedLines(1));
                    sb.Append(SetBold(true));
                    sb.Append(SetFontSize(2, 2)); // Double width and height
                    sb.Append($"Total: {content.Total.Value:F2}");
                    sb.Append(SetFontSize(1, 1));
                    sb.Append(SetBold(false));
                    sb.Append(FeedLines(1));
                }
                
                // Draw line
                sb.Append(DrawLine('=', ReceiptWidth));
                sb.Append(FeedLines(1));
                
                // Footer
                if (!string.IsNullOrEmpty(content.Footer))
                {
                    sb.Append(SetAlignment(Alignment.Center));
                    sb.Append(content.Footer);
                    sb.Append(FeedLines(2));
                }
                
                // Feed and cut
                sb.Append(SetAlignment(Alignment.Left));
                sb.Append(FeedLines(2));
                
                if (cutPaper)
                {
                    sb.Append(CutPaper());
                }
                
                // Print using RawPrinterHelper
                return RawPrinterHelper.SendStringToPrinter(_printerName, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing formatted receipt: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Print HTML content dengan format kaya
        /// </summary>
        public bool PrintHtmlFormatted(string htmlContent, bool cutPaper = true)
        {
            try
            {
                // Parse HTML dan convert ke formatted receipt
                var receiptContent = ParseHtmlToReceiptContent(htmlContent);
                return PrintFormattedReceipt(receiptContent, cutPaper);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing HTML formatted: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Print receipt dari data structured
        /// Format receipt sesuai dengan format standar POS printer 80mm
        /// </summary>
        /// <param name="data">Data receipt yang akan dicetak</param>
        /// <param name="cutPaper">Apakah kertas harus dipotong setelah print</param>
        /// <returns>True jika berhasil, False jika gagal</returns>
        public bool PrintReceiptFromData(PrintReceiptDataRequest data, bool cutPaper = true)
        {
            try
            {
                var sb = new StringBuilder();
                
                // Initialize printer
                sb.Append(InitializePrinter());
                
                // Helper function untuk format line dengan left dan right
                string FormatLine(string left, string right)
                {
                    int totalWidth = ReceiptWidth;
                    int leftWidth = left.Length;
                    int rightWidth = right.Length;
                    int spaceWidth = totalWidth - leftWidth - rightWidth;
                    
                    if (spaceWidth < 1) spaceWidth = 1;
                    return left + new string(' ', spaceWidth) + right;
                }
                
                // Company Info
                if (data.Company != null)
                {
                    if (data.PrintSetting?.CompName == true && !string.IsNullOrEmpty(data.Company.CompanyName))
                    {
                        sb.Append(SetAlignment(Alignment.Center));
                        sb.Append(SetBold(true));
                        sb.Append(data.Company.CompanyName);
                        sb.Append(SetBold(false));
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.CompRegno == true && !string.IsNullOrEmpty(data.Company.RegNo))
                    {
                        sb.Append(SetAlignment(Alignment.Center));
                        sb.Append(data.Company.RegNo);
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.CompAddr == true && !string.IsNullOrEmpty(data.Company.Address))
                    {
                        sb.Append(SetAlignment(Alignment.Center));
                        sb.Append(data.Company.Address);
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.CompPhone1 == true && !string.IsNullOrEmpty(data.Company.Phone1))
                    {
                        sb.Append(SetAlignment(Alignment.Center));
                        sb.Append(data.Company.Phone1);
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.CompEmail == true && !string.IsNullOrEmpty(data.Company.Email))
                    {
                        sb.Append(SetAlignment(Alignment.Center));
                        sb.Append(data.Company.Email);
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.CompPic == true && !string.IsNullOrEmpty(data.Company.ContactPerson))
                    {
                        sb.Append(SetAlignment(Alignment.Center));
                        sb.Append(data.Company.ContactPerson);
                        sb.Append(FeedLines(1));
                    }
                }
                
                sb.Append(SetAlignment(Alignment.Left));
                sb.Append(DrawLine('=', ReceiptWidth));
                sb.Append(FeedLines(1));
                
                // Queue No (font size 2x2 and bold, center)
                if (data.PrintSetting?.QueueNo == true && data.TransHead != null && !string.IsNullOrEmpty(data.TransHead.OrderNo))
                {
                    string queueNo = data.TransHead.OrderNo.Length > 6 
                        ? data.TransHead.OrderNo.Substring(6) 
                        : data.TransHead.OrderNo;
                    sb.Append(SetAlignment(Alignment.Center));
                    sb.Append(SetBold(true));
                    sb.Append(SetFontSize(2, 2)); // Double width and height
                    sb.Append(queueNo);
                    sb.Append(SetFontSize(1, 1)); // Reset to 1x1
                    sb.Append(SetBold(false));
                    sb.Append(FeedLines(1));
                    sb.Append(SetAlignment(Alignment.Left));
                    sb.Append(DrawLine('=', ReceiptWidth));
                    sb.Append(FeedLines(1));
                }
                
                // Document Type (ORDER/RECEIPT/INVOICE) - bold
                if (data.PrintSetting?.OptType != null)
                {
                    sb.Append(SetAlignment(Alignment.Center));
                    sb.Append(SetBold(true));
                    sb.Append(data.PrintSetting.OptType.ToUpper());
                    sb.Append(SetBold(false));
                    sb.Append(FeedLines(1));
                    sb.Append(SetAlignment(Alignment.Left));
                    sb.Append(DrawLine('-', ReceiptWidth));
                    sb.Append(FeedLines(1));
                }
                
                // Customer Info
                if (data.TransHead != null)
                {
                    if (data.PrintSetting?.CustType == true && !string.IsNullOrEmpty(data.TransHead.CustomerTypeName))
                    {
                        sb.Append($"Cust. Type: {data.TransHead.CustomerTypeName}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.TransHead.CustomerType != "CT240001" && data.PrintSetting?.CustId == true && !string.IsNullOrEmpty(data.TransHead.CustomerId))
                    {
                        sb.Append($"Cust. ID: {data.TransHead.CustomerId}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.CustName == true && !string.IsNullOrEmpty(data.TransHead.CustomerName))
                    {
                        sb.Append($"Cust. Name: {data.TransHead.CustomerName}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.SalesType == true && !string.IsNullOrEmpty(data.TransHead.SalesTypeName))
                    {
                        sb.Append($"Sales Type: {data.TransHead.SalesTypeName}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.ServiceType == true && !string.IsNullOrEmpty(data.TransHead.ServiceTypeName))
                    {
                        sb.Append($"Service Type: {data.TransHead.ServiceTypeName}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.TableNo == true && !string.IsNullOrEmpty(data.TransHead.TableNo))
                    {
                        sb.Append($"Table No: {data.TransHead.TableNo}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.OrderNo == true && !string.IsNullOrEmpty(data.TransHead.OrderNo))
                    {
                        sb.Append($"Order No: {data.TransHead.OrderNo}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.InvoiceNo == true && !string.IsNullOrEmpty(data.TransHead.InvoiceNo))
                    {
                        sb.Append($"Invoice No: {data.TransHead.InvoiceNo}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.ReceiptNo == true && !string.IsNullOrEmpty(data.TransHead.ReceiptNo))
                    {
                        sb.Append($"Receipt No: {data.TransHead.ReceiptNo}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.Cashier == true && !string.IsNullOrEmpty(data.TransHead.Cashier))
                    {
                        sb.Append($"Cashier: {data.TransHead.Cashier}");
                        sb.Append(FeedLines(1));
                    }
                    if (data.PrintSetting?.Date == true && !string.IsNullOrEmpty(data.TransHead.CreatedDate))
                    {
                        if (DateTime.TryParse(data.TransHead.CreatedDate, out DateTime date))
                        {
                            sb.Append($"Date: {date:dd/MM/yyyy HH:mm:ss}");
                        }
                        else
                        {
                            sb.Append($"Date: {data.TransHead.CreatedDate}");
                        }
                        sb.Append(FeedLines(1));
                    }
                }
                
                sb.Append(DrawLine('-', ReceiptWidth));
                sb.Append(FeedLines(1));
                
                // Product Header
                if (data.PrintSetting?.SalesPrice == true)
                {
                    sb.Append(FormatLine("Product", "Price"));
                    sb.Append(FeedLines(1));
                }
                else
                {
                    sb.Append(FormatLine("Product", "Qty"));
                    sb.Append(FeedLines(1));
                }
                
                sb.Append(DrawLine('-', ReceiptWidth));
                sb.Append(FeedLines(1));
                
                // Items
                if (data.TransDetail != null && data.TransDetail.Count > 0)
                {
                    int tempSeq = -1;
                    double totalPrice = 0;
                    int totalQty = 0;
                    
                    foreach (var detail in data.TransDetail)
                    {
                        if (detail.Seq.HasValue && tempSeq != detail.Seq.Value)
                        {
                            tempSeq = detail.Seq.Value;
                            if (data.PrintSetting?.Product == true && !string.IsNullOrEmpty(detail.ProductName))
                            {
                                sb.Append(SetBold(true));
                                sb.Append(detail.ProductName);
                                sb.Append(SetBold(false));
                                sb.Append(FeedLines(1));
                            }
                        }
                        
                        if (data.PrintSetting?.SellingDesc == true && !string.IsNullOrEmpty(detail.SellingDesc))
                        {
                            sb.Append($"  {detail.SellingDesc}");
                            sb.Append(FeedLines(1));
                        }
                        
                        if (data.PrintSetting?.SalesPrice == true)
                        {
                            double itemPrice = (detail.Qty ?? 0) * ((detail.Price ?? 0) - (detail.PromoAmt ?? 0));
                            totalPrice += itemPrice;
                            string qtyPrice = $"{detail.Qty} x {detail.Price:F2}";
                            string itemTotal = itemPrice.ToString("F2");
                            sb.Append(FormatLine($"  {qtyPrice}", itemTotal));
                            sb.Append(FeedLines(1));
                        }
                        else
                        {
                            totalQty += (int)(detail.Qty ?? 0);
                            sb.Append(FormatLine($"  {detail.SellingDesc ?? ""}", detail.Qty?.ToString() ?? "0"));
                            sb.Append(FeedLines(1));
                        }
                    }
                    
                    // Total
                    sb.Append(DrawLine('-', ReceiptWidth));
                    sb.Append(FeedLines(1));
                    if (data.PrintSetting?.SalesPrice == true)
                    {
                        sb.Append(FormatLine("Total", totalPrice.ToString("F2")));
                        sb.Append(FeedLines(1));
                    }
                    else
                    {
                        sb.Append(FormatLine("Total", totalQty.ToString()));
                        sb.Append(FeedLines(1));
                    }
                    
                    // Charges (hanya jika sales_price = true)
                    if (data.PrintSetting?.SalesPrice == true && data.TransHead != null)
                    {
                        if (data.TransHead.DelvCharge.HasValue && data.TransHead.DelvCharge.Value > 0)
                        {
                            sb.Append(FormatLine("Delivery Charge", data.TransHead.DelvCharge.Value.ToString("F2")));
                            sb.Append(FeedLines(1));
                        }
                        if (data.TransHead.ProcessingFee.HasValue && data.TransHead.ProcessingFee.Value > 0)
                        {
                            sb.Append(FormatLine("Processing Fee", data.TransHead.ProcessingFee.Value.ToString("F2")));
                            sb.Append(FeedLines(1));
                        }
                        if (data.Company?.TaxRegistrant == true && data.TransHead.TotalTax.HasValue && data.TransHead.TotalTax.Value > 0)
                        {
                            sb.Append(FormatLine("Tax Amount", data.TransHead.TotalTax.Value.ToString("F2")));
                            sb.Append(FeedLines(1));
                        }
                        if (data.TransHead.StampDuty.HasValue && data.TransHead.StampDuty.Value > 0)
                        {
                            sb.Append(FormatLine("Stamp Duty", data.TransHead.StampDuty.Value.ToString("F2")));
                            sb.Append(FeedLines(1));
                        }
                        if (data.Company?.ServiceCharge == true && data.TransHead.ServiceCharge.HasValue && data.TransHead.ServiceCharge.Value > 0)
                        {
                            sb.Append(FormatLine("Service Charge", data.TransHead.ServiceCharge.Value.ToString("F2")));
                            sb.Append(FeedLines(1));
                        }
                        if (data.TransHead.TotalSpecDisc.HasValue && data.TransHead.TotalSpecDisc.Value > 0)
                        {
                            sb.Append(FormatLine("Special Disc.", data.TransHead.TotalSpecDisc.Value.ToString("F2")));
                            sb.Append(FeedLines(1));
                        }
                        if (data.TransHead.TotalVoucher.HasValue && data.TransHead.TotalVoucher.Value > 0)
                        {
                            sb.Append(FormatLine("Voucher", data.TransHead.TotalVoucher.Value.ToString("F2")));
                            sb.Append(FeedLines(1));
                        }
                        if (data.TransHead.RndPay.HasValue && data.TransHead.RndPay.Value != 0)
                        {
                            sb.Append(FormatLine("Rounding", data.TransHead.RndPay.Value.ToString("F2")));
                            sb.Append(FeedLines(1));
                        }
                        
                        // Grand Total
                        bool hasCharges = (data.Company?.TaxRegistrant == true) || 
                                         (data.Company?.ServiceCharge == true) || 
                                         (data.TransHead.TotalSpecDisc.HasValue && data.TransHead.TotalSpecDisc.Value > 0) || 
                                         (data.TransHead.TotalVoucher.HasValue && data.TransHead.TotalVoucher.Value > 0) || 
                                         (data.TransHead.RndPay.HasValue && data.TransHead.RndPay.Value != 0) || 
                                         (data.TransHead.DelvCharge.HasValue && data.TransHead.DelvCharge.Value > 0) || 
                                         (data.TransHead.ProcessingFee.HasValue && data.TransHead.ProcessingFee.Value > 0);
                        
                        if (hasCharges && data.TransHead.TotalPrice.HasValue)
                        {
                            sb.Append(DrawLine('=', ReceiptWidth));
                            sb.Append(FeedLines(1));
                            sb.Append(SetBold(true));
                            sb.Append(SetFontSize(2, 2)); // Double width and height
                            sb.Append(FormatLine("Grand Total", data.TransHead.TotalPrice.Value.ToString("F2")));
                            sb.Append(SetFontSize(1, 1)); // Reset to 1x1
                            sb.Append(SetBold(false));
                            sb.Append(FeedLines(1));
                        }
                    }
                }
                
                sb.Append(DrawLine('=', ReceiptWidth));
                sb.Append(FeedLines(1));
                
                // Payment Info
                if (data.PrintSetting?.PaymentInfo == true && data.TransHead != null)
                {
                    string paymentInfo = "";
                    if (!string.IsNullOrEmpty(data.TransHead.PaymentMethod))
                    {
                        paymentInfo = data.TransHead.PaymentMethod;
                    }
                    if (!string.IsNullOrEmpty(data.TransHead.ReferenceNo))
                    {
                        paymentInfo += " " + data.TransHead.ReferenceNo;
                    }
                    if (!string.IsNullOrEmpty(paymentInfo))
                    {
                        sb.Append(FormatLine(paymentInfo, data.TransHead.PayAmt?.ToString("F2") ?? "0.00"));
                        sb.Append(FeedLines(1));
                    }
                    sb.Append(FormatLine("Change", data.TransHead.Change?.ToString("F2") ?? "0.00"));
                    sb.Append(FeedLines(1));
                }
                
                // PV Info
                if (data.PrintSetting?.PvInfo == true && data.TransHead != null && 
                    data.TransHead.TotalPv.HasValue && data.TransHead.TotalPv.Value > 0 && data.TransHead.PaidStatus == "1")
                {
                    sb.Append(DrawLine('=', ReceiptWidth));
                    sb.Append(FeedLines(1));
                    sb.Append(FormatLine("PV", data.TransHead.TotalPv.Value.ToString("F1")));
                    sb.Append(FeedLines(1));
                }
                
                // SV Info
                if (data.PrintSetting?.SvInfo == true && data.TransHead != null && 
                    data.TransHead.TotalSv.HasValue && data.TransHead.TotalSv.Value > 0 && data.TransHead.PaidStatus == "1")
                {
                    sb.Append(DrawLine('=', ReceiptWidth));
                    sb.Append(FeedLines(1));
                    sb.Append(FormatLine("SV", data.TransHead.TotalSv.Value.ToString("F1")));
                    sb.Append(FeedLines(1));
                }
                
                // Footer
                if (data.PrintSetting?.FooterMsg == true)
                {
                    sb.Append(FeedLines(1));
                    sb.Append(SetAlignment(Alignment.Center));
                    sb.Append("Thank you. Please come again.");
                    sb.Append(FeedLines(2));
                }
                
                sb.Append(SetAlignment(Alignment.Left));
                sb.Append(FeedLines(2));
                
                // Cut paper if requested
                if (cutPaper)
                {
                    sb.Append(CutPaper());
                }
                
                // Print using RawPrinterHelper
                return RawPrinterHelper.SendStringToPrinter(_printerName, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing receipt from data: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Parse HTML ke ReceiptContent (sederhana)
        /// </summary>
        private ReceiptContent ParseHtmlToReceiptContent(string html)
        {
            // Implementasi sederhana - bisa diperluas
            var content = new ReceiptContent();
            
            // Remove HTML tags sederhana
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", "");
            html = System.Net.WebUtility.HtmlDecode(html);
            
            // Split by lines
            var lines = html.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                
                // Simple parsing - bisa diperbaiki
                if (trimmed.Contains("Order No") || trimmed.Contains("ORDER"))
                {
                    content.OrderNo = trimmed;
                }
                else if (trimmed.Contains("Total"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"[\d.]+");
                    if (match.Success && double.TryParse(match.Value, out double total))
                    {
                        content.Total = total;
                    }
                }
            }
            
            return content;
        }

        // ========== ESC/POS Command Helpers ==========
        
        /// <summary>
        /// Initialize printer (ESC @)
        /// </summary>
        public string InitializePrinter()
        {
            return $"{(char)27}@";
        }

        /// <summary>
        /// Set text bold (ESC E n)
        /// </summary>
        public string SetBold(bool bold)
        {
            return $"{(char)27}E{(char)(bold ? 1 : 0)}";
        }

        /// <summary>
        /// Set font size (GS ! n)
        /// width: 1=normal, 2=double width
        /// height: 1=normal, 2=double height
        /// </summary>
        public string SetFontSize(int width, int height)
        {
            byte size = 0;
            if (width == 2) size |= 0x10; // Double width
            if (height == 2) size |= 0x01; // Double height
            return $"{(char)29}!{(char)size}";
        }

        /// <summary>
        /// Set alignment (ESC a n)
        /// </summary>
        public string SetAlignment(Alignment alignment)
        {
            byte align = (byte)alignment;
            return $"{(char)27}a{(char)align}";
        }

        /// <summary>
        /// Draw line dengan karakter tertentu
        /// </summary>
        public string DrawLine(char character = '-', int width = 48)
        {
            return new string(character, width) + "\n";
        }

        /// <summary>
        /// Feed lines (LF)
        /// </summary>
        public string FeedLines(int count = 1)
        {
            return new string('\n', count);
        }

        /// <summary>
        /// Cut paper (GS V n)
        /// </summary>
        public string CutPaper()
        {
            return $"{(char)29}V{(char)66}{(char)0}";
        }

        /// <summary>
        /// Underline (ESC - n)
        /// </summary>
        public string SetUnderline(bool underline, int thickness = 1)
        {
            byte value = (byte)(underline ? thickness : 0);
            return $"{(char)27}-{(char)value}";
        }

        /// <summary>
        /// Reverse colors (GS B n)
        /// </summary>
        public string SetReverseColors(bool reverse)
        {
            return $"{(char)29}B{(char)(reverse ? 1 : 0)}";
        }

        /// <summary>
        /// Enum untuk alignment
        /// </summary>
        public enum Alignment : byte
        {
            Left = 0,
            Center = 1,
            Right = 2
        }
    }

    /// <summary>
    /// Model untuk receipt content
    /// </summary>
    public class ReceiptContent
    {
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public string? CompanyPhone { get; set; }
        public string? QueueNo { get; set; }
        public string? DocType { get; set; }
        public string? OrderNo { get; set; }
        public string? Date { get; set; }
        public List<ReceiptItem>? Items { get; set; }
        public double? Total { get; set; }
        public double? GrandTotal { get; set; }
        public string? Footer { get; set; }
    }

    /// <summary>
    /// Model untuk receipt item
    /// </summary>
    public class ReceiptItem
    {
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public int? Quantity { get; set; }
    }
}
