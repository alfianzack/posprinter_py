using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using PosPrinterApp.Resources;
using PosPrinterApp.Models;

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

        /// <summary>
        /// Print HTML content dengan konversi ke plain text
        /// HTML akan di-parse dan dikonversi ke format plain text yang sesuai untuk printer ESC/POS
        /// </summary>
        /// <param name="htmlContent">Konten HTML yang akan dicetak</param>
        /// <param name="cutPaper">Apakah kertas harus dipotong setelah print</param>
        /// <returns>True jika berhasil, False jika gagal</returns>
        public bool PrintHtml(string htmlContent, bool cutPaper = true)
        {
            try
            {
                // Convert HTML ke plain text
                string plainText = ConvertHtmlToPlainText(htmlContent);
                
                // Print plain text yang sudah dikonversi
                return PrintReceipt(plainText, cutPaper, FontSize.Normal);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing HTML: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Convert HTML content ke plain text untuk printer ESC/POS
        /// </summary>
        /// <param name="html">HTML content</param>
        /// <returns>Plain text yang sudah diformat</returns>
        private string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var result = new StringBuilder();
            
            // Remove script dan style tags beserta isinya
            html = System.Text.RegularExpressions.Regex.Replace(html, 
                @"<(script|style)[^>]*>.*?</\1>", 
                "", 
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Replace common HTML entities
            html = html.Replace("&nbsp;", " ");
            html = html.Replace("&amp;", "&");
            html = html.Replace("&lt;", "<");
            html = html.Replace("&gt;", ">");
            html = html.Replace("&quot;", "\"");
            html = html.Replace("&#39;", "'");

            // Replace line breaks
            html = html.Replace("<br>", "\n");
            html = html.Replace("<br/>", "\n");
            html = html.Replace("<br />", "\n");
            html = html.Replace("</p>", "\n");
            html = html.Replace("</div>", "\n");
            html = html.Replace("</tr>", "\n");
            html = html.Replace("</td>", " ");

            // Remove HTML tags
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", "");

            // Decode HTML entities
            html = System.Net.WebUtility.HtmlDecode(html);

            // Clean up whitespace
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\n\s*\n", "\n");

            // Split into lines and process
            string[] lines = html.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    // Center alignment detection (untuk header)
                    if (trimmedLine.Length < 35 && (trimmedLine.ToUpper() == trimmedLine || trimmedLine.Contains("=")))
                    {
                        // Center align untuk header
                        int padding = (32 - trimmedLine.Length) / 2;
                        result.AppendLine(new string(' ', Math.Max(0, padding)) + trimmedLine);
                    }
                    else
                    {
                        // Left align untuk content biasa
                        result.AppendLine(trimmedLine);
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Print receipt dari data structured
        /// Format receipt sesuai dengan format standar POS printer 80mm
        /// </summary>
        /// <param name="data">Data receipt yang akan dicetak</param>
        /// <param name="cutPaper">Apakah kertas harus dipotong setelah print</param>
        /// <returns>True jika berhasil, False jika gagal</returns>
        public bool PrintReceiptFromData(Models.PrintReceiptDataRequest data, bool cutPaper = true)
        {
            try
            {
                var receipt = new StringBuilder();
                int lineWidth = 32; // Standard 80mm printer width
                
                // Helper function untuk center text
                string CenterText(string text)
                {
                    if (string.IsNullOrEmpty(text)) return "";
                    int padding = (lineWidth - text.Length) / 2;
                    return new string(' ', Math.Max(0, padding)) + text;
                }
                
                // Helper function untuk format line dengan left dan right
                string FormatLine(string left, string right)
                {
                    int totalWidth = lineWidth;
                    int leftWidth = left.Length;
                    int rightWidth = right.Length;
                    int spaceWidth = totalWidth - leftWidth - rightWidth;
                    
                    if (spaceWidth < 1) spaceWidth = 1;
                    return left + new string(' ', spaceWidth) + right;
                }
                
                // Helper function untuk separator line
                string SeparatorLine(char character = '=')
                {
                    return new string(character, lineWidth);
                }
                
                // Company Info
                if (data.Company != null)
                {
                    if (data.PrintSetting?.CompName == true && !string.IsNullOrEmpty(data.Company.CompanyName))
                    {
                        receipt.AppendLine(CenterText(data.Company.CompanyName));
                    }
                    if (data.PrintSetting?.CompRegno == true && !string.IsNullOrEmpty(data.Company.RegNo))
                    {
                        receipt.AppendLine(CenterText(data.Company.RegNo));
                    }
                    if (data.PrintSetting?.CompAddr == true && !string.IsNullOrEmpty(data.Company.Address))
                    {
                        receipt.AppendLine(CenterText(data.Company.Address));
                    }
                    if (data.PrintSetting?.CompPhone1 == true && !string.IsNullOrEmpty(data.Company.Phone1))
                    {
                        receipt.AppendLine(CenterText(data.Company.Phone1));
                    }
                    if (data.PrintSetting?.CompEmail == true && !string.IsNullOrEmpty(data.Company.Email))
                    {
                        receipt.AppendLine(CenterText(data.Company.Email));
                    }
                    if (data.PrintSetting?.CompPic == true && !string.IsNullOrEmpty(data.Company.ContactPerson))
                    {
                        receipt.AppendLine(CenterText(data.Company.ContactPerson));
                    }
                }
                
                receipt.AppendLine(SeparatorLine('='));
                
                // Queue No (large, center, bold)
                if (data.PrintSetting?.QueueNo == true && data.TransHead != null && !string.IsNullOrEmpty(data.TransHead.OrderNo))
                {
                    string queueNo = data.TransHead.OrderNo.Length > 6 
                        ? data.TransHead.OrderNo.Substring(6) 
                        : data.TransHead.OrderNo;
                    receipt.AppendLine(CenterText(queueNo));
                }
                
                receipt.AppendLine(SeparatorLine('='));
                
                // Document Type (ORDER/RECEIPT/INVOICE)
                if (data.PrintSetting?.OptType != null)
                {
                    receipt.AppendLine(CenterText(data.PrintSetting.OptType.ToUpper()));
                }
                
                receipt.AppendLine(SeparatorLine('-'));
                
                // Customer Info
                if (data.TransHead != null)
                {
                    if (data.PrintSetting?.CustType == true && !string.IsNullOrEmpty(data.TransHead.CustomerTypeName))
                    {
                        receipt.AppendLine($"Cust. Type: {data.TransHead.CustomerTypeName}");
                    }
                    if (data.TransHead.CustomerType != "CT240001" && data.PrintSetting?.CustId == true && !string.IsNullOrEmpty(data.TransHead.CustomerId))
                    {
                        receipt.AppendLine($"Cust. ID: {data.TransHead.CustomerId}");
                    }
                    if (data.PrintSetting?.CustName == true && !string.IsNullOrEmpty(data.TransHead.CustomerName))
                    {
                        receipt.AppendLine($"Cust. Name: {data.TransHead.CustomerName}");
                    }
                    if (data.PrintSetting?.SalesType == true && !string.IsNullOrEmpty(data.TransHead.SalesTypeName))
                    {
                        receipt.AppendLine($"Sales Type: {data.TransHead.SalesTypeName}");
                    }
                    if (data.PrintSetting?.ServiceType == true && !string.IsNullOrEmpty(data.TransHead.ServiceTypeName))
                    {
                        receipt.AppendLine($"Service Type: {data.TransHead.ServiceTypeName}");
                    }
                    if (data.PrintSetting?.TableNo == true && !string.IsNullOrEmpty(data.TransHead.TableNo))
                    {
                        receipt.AppendLine($"Table No: {data.TransHead.TableNo}");
                    }
                    if (data.PrintSetting?.OrderNo == true && !string.IsNullOrEmpty(data.TransHead.OrderNo))
                    {
                        receipt.AppendLine($"Order No: {data.TransHead.OrderNo}");
                    }
                    if (data.PrintSetting?.InvoiceNo == true && !string.IsNullOrEmpty(data.TransHead.InvoiceNo))
                    {
                        receipt.AppendLine($"Invoice No: {data.TransHead.InvoiceNo}");
                    }
                    if (data.PrintSetting?.ReceiptNo == true && !string.IsNullOrEmpty(data.TransHead.ReceiptNo))
                    {
                        receipt.AppendLine($"Receipt No: {data.TransHead.ReceiptNo}");
                    }
                    if (data.PrintSetting?.Cashier == true && !string.IsNullOrEmpty(data.TransHead.Cashier))
                    {
                        receipt.AppendLine($"Cashier: {data.TransHead.Cashier}");
                    }
                    if (data.PrintSetting?.Date == true && !string.IsNullOrEmpty(data.TransHead.CreatedDate))
                    {
                        if (DateTime.TryParse(data.TransHead.CreatedDate, out DateTime date))
                        {
                            receipt.AppendLine($"Date: {date:dd/MM/yyyy HH:mm:ss}");
                        }
                        else
                        {
                            receipt.AppendLine($"Date: {data.TransHead.CreatedDate}");
                        }
                    }
                }
                
                receipt.AppendLine(SeparatorLine('-'));
                
                // Product Header
                if (data.PrintSetting?.SalesPrice == true)
                {
                    receipt.AppendLine(FormatLine("Product", "Price"));
                }
                else
                {
                    receipt.AppendLine(FormatLine("Product", "Qty"));
                }
                
                receipt.AppendLine(SeparatorLine('-'));
                
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
                                receipt.AppendLine(detail.ProductName);
                            }
                        }
                        
                        if (data.PrintSetting?.SellingDesc == true && !string.IsNullOrEmpty(detail.SellingDesc))
                        {
                            receipt.AppendLine($"  {detail.SellingDesc}");
                        }
                        
                        if (data.PrintSetting?.SalesPrice == true)
                        {
                            double itemPrice = (detail.Qty ?? 0) * ((detail.Price ?? 0) - (detail.PromoAmt ?? 0));
                            totalPrice += itemPrice;
                            string qtyPrice = $"{detail.Qty} x {detail.Price:F2}";
                            string itemTotal = itemPrice.ToString("F2");
                            receipt.AppendLine(FormatLine($"  {qtyPrice}", itemTotal));
                        }
                        else
                        {
                            totalQty += (int)(detail.Qty ?? 0);
                            receipt.AppendLine(FormatLine($"  {detail.SellingDesc ?? ""}", detail.Qty?.ToString() ?? "0"));
                        }
                    }
                    
                    // Total
                    receipt.AppendLine(SeparatorLine('-'));
                    if (data.PrintSetting?.SalesPrice == true)
                    {
                        receipt.AppendLine(FormatLine("Total", totalPrice.ToString("F2")));
                    }
                    else
                    {
                        receipt.AppendLine(FormatLine("Total", totalQty.ToString()));
                    }
                    
                    // Charges (hanya jika sales_price = true)
                    if (data.PrintSetting?.SalesPrice == true && data.TransHead != null)
                    {
                        if (data.TransHead.DelvCharge > 0)
                        {
                            receipt.AppendLine(FormatLine("Delivery Charge", data.TransHead.DelvCharge.Value.ToString("F2")));
                        }
                        if (data.TransHead.ProcessingFee > 0)
                        {
                            receipt.AppendLine(FormatLine("Processing Fee", data.TransHead.ProcessingFee.Value.ToString("F2")));
                        }
                        if (data.Company?.TaxRegistrant == true && data.TransHead.TotalTax > 0)
                        {
                            receipt.AppendLine(FormatLine("Tax Amount", data.TransHead.TotalTax.Value.ToString("F2")));
                        }
                        if (data.TransHead.StampDuty > 0)
                        {
                            receipt.AppendLine(FormatLine("Stamp Duty", data.TransHead.StampDuty.Value.ToString("F2")));
                        }
                        if (data.Company?.ServiceCharge == true && data.TransHead.ServiceCharge > 0)
                        {
                            receipt.AppendLine(FormatLine("Service Charge", data.TransHead.ServiceCharge.Value.ToString("F2")));
                        }
                        if (data.TransHead.TotalSpecDisc > 0)
                        {
                            receipt.AppendLine(FormatLine("Special Disc.", data.TransHead.TotalSpecDisc.Value.ToString("F2")));
                        }
                        if (data.TransHead.TotalVoucher > 0)
                        {
                            receipt.AppendLine(FormatLine("Voucher", data.TransHead.TotalVoucher.Value.ToString("F2")));
                        }
                        if (data.TransHead.RndPay != 0)
                        {
                            receipt.AppendLine(FormatLine("Rounding", data.TransHead.RndPay.Value.ToString("F2")));
                        }
                        
                        // Grand Total
                        bool hasCharges = (data.Company?.TaxRegistrant == true) || 
                                         (data.Company?.ServiceCharge == true) || 
                                         (data.TransHead.TotalSpecDisc > 0) || 
                                         (data.TransHead.TotalVoucher > 0) || 
                                         (data.TransHead.RndPay != 0) || 
                                         (data.TransHead.DelvCharge > 0) || 
                                         (data.TransHead.ProcessingFee > 0);
                        
                        if (hasCharges && data.TransHead.TotalPrice.HasValue)
                        {
                            receipt.AppendLine(SeparatorLine('='));
                            receipt.AppendLine(FormatLine("Grand Total", data.TransHead.TotalPrice.Value.ToString("F2")));
                        }
                    }
                }
                
                receipt.AppendLine(SeparatorLine('='));
                
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
                        receipt.AppendLine(FormatLine(paymentInfo, data.TransHead.PayAmt?.ToString("F2") ?? "0.00"));
                    }
                    receipt.AppendLine(FormatLine("Change", data.TransHead.Change?.ToString("F2") ?? "0.00"));
                }
                
                // PV Info
                if (data.PrintSetting?.PvInfo == true && data.TransHead != null && 
                    data.TransHead.TotalPv > 0 && data.TransHead.PaidStatus == "1")
                {
                    receipt.AppendLine(SeparatorLine('='));
                    receipt.AppendLine(FormatLine("PV", data.TransHead.TotalPv.Value.ToString("F1")));
                }
                
                // SV Info
                if (data.PrintSetting?.SvInfo == true && data.TransHead != null && 
                    data.TransHead.TotalSv > 0 && data.TransHead.PaidStatus == "1")
                {
                    receipt.AppendLine(SeparatorLine('='));
                    receipt.AppendLine(FormatLine("SV", data.TransHead.TotalSv.Value.ToString("F1")));
                }
                
                // Footer
                if (data.PrintSetting?.FooterMsg == true)
                {
                    receipt.AppendLine();
                    receipt.AppendLine(CenterText("Thank you. Please come again."));
                }
                
                receipt.AppendLine();
                receipt.AppendLine();
                
                // Print receipt
                return PrintReceipt(receipt.ToString(), cutPaper, FontSize.Normal);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing receipt from data: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}

