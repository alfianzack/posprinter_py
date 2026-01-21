using System;
using System.Text;
using PosPrinterApp.Models;

namespace PosPrinterApp.Services
{
    /// <summary>
    /// Service untuk generate HTML dari data receipt
    /// </summary>
    public class HtmlGeneratorService
    {
        /// <summary>
        /// Generate HTML receipt dari data
        /// </summary>
        /// <param name="data">Data receipt</param>
        /// <returns>HTML string</returns>
        public string GenerateHtmlFromData(PrintReceiptDataRequest data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var html = new StringBuilder();
            int receiptWidth = 576; // 80mm printer width in pixels (72 DPI * 8 inches)
            
            // Start HTML
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"UTF-8\">");
            html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine("<style>");
            html.AppendLine(GetReceiptStyles(receiptWidth));
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class=\"receipt\">");
            
            // Company Info
            if (data.Company != null)
            {
                html.AppendLine("<div class=\"company-info\">");
                
                if (data.PrintSetting?.CompName == true && !string.IsNullOrEmpty(data.Company.CompanyName))
                {
                    html.AppendLine($"<div class=\"company-name\">{EscapeHtml(data.Company.CompanyName)}</div>");
                }
                if (data.PrintSetting?.CompRegno == true && !string.IsNullOrEmpty(data.Company.RegNo))
                {
                    html.AppendLine($"<div class=\"company-regno\">{EscapeHtml(data.Company.RegNo)}</div>");
                }
                if (data.PrintSetting?.CompAddr == true && !string.IsNullOrEmpty(data.Company.Address))
                {
                    html.AppendLine($"<div class=\"company-address\">{EscapeHtml(data.Company.Address)}</div>");
                }
                if (data.PrintSetting?.CompPhone1 == true && !string.IsNullOrEmpty(data.Company.Phone1))
                {
                    html.AppendLine($"<div class=\"company-phone\">{EscapeHtml(data.Company.Phone1)}</div>");
                }
                if (data.PrintSetting?.CompEmail == true && !string.IsNullOrEmpty(data.Company.Email))
                {
                    html.AppendLine($"<div class=\"company-email\">{EscapeHtml(data.Company.Email)}</div>");
                }
                if (data.PrintSetting?.CompPic == true && !string.IsNullOrEmpty(data.Company.ContactPerson))
                {
                    html.AppendLine($"<div class=\"company-contact\">{EscapeHtml(data.Company.ContactPerson)}</div>");
                }
                
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"separator\">================================</div>");
            }
            
            // Queue No
            if (data.PrintSetting?.QueueNo == true && data.TransHead != null && !string.IsNullOrEmpty(data.TransHead.OrderNo))
            {
                string queueNo = data.TransHead.OrderNo.Length > 6 
                    ? data.TransHead.OrderNo.Substring(6) 
                    : data.TransHead.OrderNo;
                html.AppendLine($"<div class=\"queue-no\">{EscapeHtml(queueNo)}</div>");
                html.AppendLine("<div class=\"separator\">================================</div>");
            }
            
            // Document Type
            if (data.PrintSetting?.OptType != null)
            {
                html.AppendLine($"<div class=\"doc-type\">{EscapeHtml(data.PrintSetting.OptType.ToUpper())}</div>");
                html.AppendLine("<div class=\"separator\">--------------------------------</div>");
            }
            
            // Customer Info
            if (data.TransHead != null)
            {
                html.AppendLine("<div class=\"customer-info\">");
                
                if (data.PrintSetting?.CustType == true && !string.IsNullOrEmpty(data.TransHead.CustomerTypeName))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Cust. Type:</span> <span class=\"value\">{EscapeHtml(data.TransHead.CustomerTypeName)}</span></div>");
                }
                if (data.TransHead.CustomerType != "CT240001" && data.PrintSetting?.CustId == true && !string.IsNullOrEmpty(data.TransHead.CustomerId))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Cust. ID:</span> <span class=\"value\">{EscapeHtml(data.TransHead.CustomerId)}</span></div>");
                }
                if (data.PrintSetting?.CustName == true && !string.IsNullOrEmpty(data.TransHead.CustomerName))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Cust. Name:</span> <span class=\"value\">{EscapeHtml(data.TransHead.CustomerName)}</span></div>");
                }
                if (data.PrintSetting?.SalesType == true && !string.IsNullOrEmpty(data.TransHead.SalesTypeName))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Sales Type:</span> <span class=\"value\">{EscapeHtml(data.TransHead.SalesTypeName)}</span></div>");
                }
                if (data.PrintSetting?.ServiceType == true && !string.IsNullOrEmpty(data.TransHead.ServiceTypeName))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Service Type:</span> <span class=\"value\">{EscapeHtml(data.TransHead.ServiceTypeName)}</span></div>");
                }
                if (data.PrintSetting?.TableNo == true && !string.IsNullOrEmpty(data.TransHead.TableNo))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Table No:</span> <span class=\"value\">{EscapeHtml(data.TransHead.TableNo)}</span></div>");
                }
                if (data.PrintSetting?.OrderNo == true && !string.IsNullOrEmpty(data.TransHead.OrderNo))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Order No:</span> <span class=\"value\">{EscapeHtml(data.TransHead.OrderNo)}</span></div>");
                }
                if (data.PrintSetting?.InvoiceNo == true && !string.IsNullOrEmpty(data.TransHead.InvoiceNo))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Invoice No:</span> <span class=\"value\">{EscapeHtml(data.TransHead.InvoiceNo)}</span></div>");
                }
                if (data.PrintSetting?.ReceiptNo == true && !string.IsNullOrEmpty(data.TransHead.ReceiptNo))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Receipt No:</span> <span class=\"value\">{EscapeHtml(data.TransHead.ReceiptNo)}</span></div>");
                }
                if (data.PrintSetting?.Cashier == true && !string.IsNullOrEmpty(data.TransHead.Cashier))
                {
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Cashier:</span> <span class=\"value\">{EscapeHtml(data.TransHead.Cashier)}</span></div>");
                }
                if (data.PrintSetting?.Date == true && !string.IsNullOrEmpty(data.TransHead.CreatedDate))
                {
                    string dateStr = data.TransHead.CreatedDate;
                    if (DateTime.TryParse(data.TransHead.CreatedDate, out DateTime date))
                    {
                        dateStr = date.ToString("dd/MM/yyyy HH:mm:ss");
                    }
                    html.AppendLine($"<div class=\"info-line\"><span class=\"label\">Date:</span> <span class=\"value\">{EscapeHtml(dateStr)}</span></div>");
                }
                
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"separator\">--------------------------------</div>");
            }
            
            // Product Header
            html.AppendLine("<div class=\"product-header\">");
            if (data.PrintSetting?.SalesPrice == true)
            {
                html.AppendLine("<div class=\"header-line\"><span class=\"header-left\">Product</span><span class=\"header-right\">Price</span></div>");
            }
            else
            {
                html.AppendLine("<div class=\"header-line\"><span class=\"header-left\">Product</span><span class=\"header-right\">Qty</span></div>");
            }
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"separator\">--------------------------------</div>");
            
            // Items
            if (data.TransDetail != null && data.TransDetail.Count > 0)
            {
                html.AppendLine("<div class=\"items\">");
                
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
                            html.AppendLine($"<div class=\"product-name\">{EscapeHtml(detail.ProductName)}</div>");
                        }
                    }
                    
                    if (data.PrintSetting?.SellingDesc == true && !string.IsNullOrEmpty(detail.SellingDesc))
                    {
                        html.AppendLine($"<div class=\"selling-desc\">{EscapeHtml(detail.SellingDesc)}</div>");
                    }
                    
                    if (data.PrintSetting?.SalesPrice == true)
                    {
                        double itemPrice = (detail.Qty ?? 0) * ((detail.Price ?? 0) - (detail.PromoAmt ?? 0));
                        totalPrice += itemPrice;
                        string qtyPrice = $"{detail.Qty} x {detail.Price:F2}";
                        string itemTotal = itemPrice.ToString("F2");
                        html.AppendLine($"<div class=\"item-line\"><span class=\"item-left\">{EscapeHtml(qtyPrice)}</span><span class=\"item-right\">{EscapeHtml(itemTotal)}</span></div>");
                    }
                    else
                    {
                        totalQty += (int)(detail.Qty ?? 0);
                        html.AppendLine($"<div class=\"item-line\"><span class=\"item-left\">{EscapeHtml(detail.SellingDesc ?? "")}</span><span class=\"item-right\">{EscapeHtml(detail.Qty?.ToString() ?? "0")}</span></div>");
                    }
                }
                
                html.AppendLine("</div>");
                html.AppendLine("<div class=\"separator\">--------------------------------</div>");
                
                // Total
                if (data.PrintSetting?.SalesPrice == true)
                {
                    html.AppendLine($"<div class=\"total-line\"><span class=\"total-left\">Total</span><span class=\"total-right\">{totalPrice:F2}</span></div>");
                    
                    // Charges
                    if (data.TransHead != null)
                    {
                        if (data.TransHead.DelvCharge.HasValue && data.TransHead.DelvCharge.Value > 0)
                        {
                            html.AppendLine($"<div class=\"charge-line\"><span class=\"charge-left\">Delivery Charge</span><span class=\"charge-right\">{data.TransHead.DelvCharge.Value:F2}</span></div>");
                        }
                        if (data.TransHead.ProcessingFee.HasValue && data.TransHead.ProcessingFee.Value > 0)
                        {
                            html.AppendLine($"<div class=\"charge-line\"><span class=\"charge-left\">Processing Fee</span><span class=\"charge-right\">{data.TransHead.ProcessingFee.Value:F2}</span></div>");
                        }
                        if (data.Company?.TaxRegistrant == true && data.TransHead.TotalTax.HasValue && data.TransHead.TotalTax.Value > 0)
                        {
                            html.AppendLine($"<div class=\"charge-line\"><span class=\"charge-left\">Tax Amount</span><span class=\"charge-right\">{data.TransHead.TotalTax.Value:F2}</span></div>");
                        }
                        if (data.TransHead.StampDuty.HasValue && data.TransHead.StampDuty.Value > 0)
                        {
                            html.AppendLine($"<div class=\"charge-line\"><span class=\"charge-left\">Stamp Duty</span><span class=\"charge-right\">{data.TransHead.StampDuty.Value:F2}</span></div>");
                        }
                        if (data.Company?.ServiceCharge == true && data.TransHead.ServiceCharge.HasValue && data.TransHead.ServiceCharge.Value > 0)
                        {
                            html.AppendLine($"<div class=\"charge-line\"><span class=\"charge-left\">Service Charge</span><span class=\"charge-right\">{data.TransHead.ServiceCharge.Value:F2}</span></div>");
                        }
                        if (data.TransHead.TotalSpecDisc.HasValue && data.TransHead.TotalSpecDisc.Value > 0)
                        {
                            html.AppendLine($"<div class=\"charge-line\"><span class=\"charge-left\">Special Disc.</span><span class=\"charge-right\">{data.TransHead.TotalSpecDisc.Value:F2}</span></div>");
                        }
                        if (data.TransHead.TotalVoucher.HasValue && data.TransHead.TotalVoucher.Value > 0)
                        {
                            html.AppendLine($"<div class=\"charge-line\"><span class=\"charge-left\">Voucher</span><span class=\"charge-right\">{data.TransHead.TotalVoucher.Value:F2}</span></div>");
                        }
                        if (data.TransHead.RndPay.HasValue && data.TransHead.RndPay.Value != 0)
                        {
                            html.AppendLine($"<div class=\"charge-line\"><span class=\"charge-left\">Rounding</span><span class=\"charge-right\">{data.TransHead.RndPay.Value:F2}</span></div>");
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
                            html.AppendLine("<div class=\"separator\">================================</div>");
                            html.AppendLine($"<div class=\"grand-total-line\"><span class=\"grand-total-left\">Grand Total</span><span class=\"grand-total-right\">{data.TransHead.TotalPrice.Value:F2}</span></div>");
                        }
                    }
                }
                else
                {
                    html.AppendLine($"<div class=\"total-line\"><span class=\"total-left\">Total</span><span class=\"total-right\">{totalQty}</span></div>");
                }
            }
            
            html.AppendLine("<div class=\"separator\">================================</div>");
            
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
                    html.AppendLine($"<div class=\"payment-line\"><span class=\"payment-left\">{EscapeHtml(paymentInfo)}</span><span class=\"payment-right\">{data.TransHead.PayAmt?.ToString("F2") ?? "0.00"}</span></div>");
                }
                html.AppendLine($"<div class=\"payment-line\"><span class=\"payment-left\">Change</span><span class=\"payment-right\">{data.TransHead.Change?.ToString("F2") ?? "0.00"}</span></div>");
            }
            
            // PV Info
            if (data.PrintSetting?.PvInfo == true && data.TransHead != null && 
                data.TransHead.TotalPv.HasValue && data.TransHead.TotalPv.Value > 0 && data.TransHead.PaidStatus == "1")
            {
                html.AppendLine("<div class=\"separator\">================================</div>");
                html.AppendLine($"<div class=\"pv-line\"><span class=\"pv-left\">PV</span><span class=\"pv-right\">{data.TransHead.TotalPv.Value:F1}</span></div>");
            }
            
            // SV Info
            if (data.PrintSetting?.SvInfo == true && data.TransHead != null && 
                data.TransHead.TotalSv.HasValue && data.TransHead.TotalSv.Value > 0 && data.TransHead.PaidStatus == "1")
            {
                html.AppendLine("<div class=\"separator\">================================</div>");
                html.AppendLine($"<div class=\"sv-line\"><span class=\"sv-left\">SV</span><span class=\"sv-right\">{data.TransHead.TotalSv.Value:F1}</span></div>");
            }
            
            // Footer
            if (data.PrintSetting?.FooterMsg == true)
            {
                html.AppendLine("<div class=\"footer\">Thank you. Please come again.</div>");
            }
            
            html.AppendLine("</div>"); // receipt
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }
        
        /// <summary>
        /// Get CSS styles untuk receipt
        /// </summary>
        private string GetReceiptStyles(int width)
        {
            return $@"
                * {{
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }}
                body {{
                    font-family: 'Courier New', monospace;
                    font-size: 12px;
                    line-height: 1.4;
                    background: white;
                    padding: 10px;
                    width: {width}px;
                    margin: 0 auto;
                }}
                .receipt {{
                    width: 100%;
                    max-width: {width}px;
                }}
                .company-info {{
                    text-align: center;
                    margin-bottom: 10px;
                }}
                .company-name {{
                    font-weight: bold;
                    font-size: 14px;
                    margin-bottom: 5px;
                }}
                .company-regno, .company-address, .company-phone, .company-email, .company-contact {{
                    font-size: 11px;
                    margin-bottom: 3px;
                }}
                .separator {{
                    text-align: center;
                    margin: 8px 0;
                    font-weight: bold;
                }}
                .queue-no {{
                    text-align: center;
                    font-size: 18px;
                    font-weight: bold;
                    margin: 10px 0;
                }}
                .doc-type {{
                    text-align: center;
                    font-weight: bold;
                    font-size: 13px;
                    margin: 8px 0;
                }}
                .customer-info {{
                    margin: 8px 0;
                }}
                .info-line {{
                    margin: 4px 0;
                    display: flex;
                    justify-content: space-between;
                }}
                .label {{
                    font-weight: normal;
                }}
                .value {{
                    font-weight: normal;
                }}
                .product-header {{
                    margin: 8px 0;
                }}
                .header-line {{
                    display: flex;
                    justify-content: space-between;
                    font-weight: bold;
                    margin: 4px 0;
                }}
                .header-left, .header-right {{
                    font-weight: bold;
                }}
                .items {{
                    margin: 8px 0;
                }}
                .product-name {{
                    font-weight: bold;
                    margin: 6px 0 2px 0;
                }}
                .selling-desc {{
                    margin-left: 10px;
                    margin-bottom: 2px;
                }}
                .item-line {{
                    display: flex;
                    justify-content: space-between;
                    margin: 3px 0;
                }}
                .item-left {{
                    flex: 1;
                }}
                .item-right {{
                    text-align: right;
                }}
                .total-line {{
                    display: flex;
                    justify-content: space-between;
                    font-weight: bold;
                    margin: 6px 0;
                }}
                .total-left, .total-right {{
                    font-weight: bold;
                }}
                .charge-line {{
                    display: flex;
                    justify-content: space-between;
                    margin: 3px 0;
                }}
                .charge-left {{
                    flex: 1;
                }}
                .charge-right {{
                    text-align: right;
                }}
                .grand-total-line {{
                    display: flex;
                    justify-content: space-between;
                    font-weight: bold;
                    font-size: 14px;
                    margin: 8px 0;
                }}
                .grand-total-left, .grand-total-right {{
                    font-weight: bold;
                }}
                .payment-line {{
                    display: flex;
                    justify-content: space-between;
                    margin: 4px 0;
                }}
                .payment-left {{
                    flex: 1;
                }}
                .payment-right {{
                    text-align: right;
                }}
                .pv-line, .sv-line {{
                    display: flex;
                    justify-content: space-between;
                    font-weight: bold;
                    margin: 6px 0;
                }}
                .pv-left, .pv-right, .sv-left, .sv-right {{
                    font-weight: bold;
                }}
                .footer {{
                    text-align: center;
                    margin-top: 15px;
                    font-style: italic;
                }}
            ";
        }
        
        /// <summary>
        /// Escape HTML characters
        /// </summary>
        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}

