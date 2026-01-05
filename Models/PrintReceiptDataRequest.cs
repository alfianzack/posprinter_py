using System.Collections.Generic;

namespace PosPrinterApp.Models
{
    public class PrintReceiptDataRequest
    {
        public CompanyData? Company { get; set; }
        public TransHeadData? TransHead { get; set; }
        public List<TransDetailData>? TransDetail { get; set; }
        public PrintSettingData? PrintSetting { get; set; }
        public bool CutPaper { get; set; } = true;
    }

    public class CompanyData
    {
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? RegNo { get; set; }
        public string? Address { get; set; }
        public string? Phone1 { get; set; }
        public string? Email { get; set; }
        public string? ContactPerson { get; set; }
        public bool? TaxRegistrant { get; set; }
        public bool? ServiceCharge { get; set; }
        public double? Tmzone { get; set; }
    }

    public class TransHeadData
    {
        public string? OrderNo { get; set; }
        public string? InvoiceNo { get; set; }
        public string? ReceiptNo { get; set; }
        public string? CustomerType { get; set; }
        public string? CustomerTypeName { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceTypeName { get; set; }
        public string? SalesType { get; set; }
        public string? SalesTypeName { get; set; }
        public string? TableNo { get; set; }
        public string? Cashier { get; set; }
        public string? CreatedDate { get; set; }
        public double? TotalPrice { get; set; }
        public double? SubtotalPrice { get; set; }
        public double? TotalTax { get; set; }
        public double? TotalRnd { get; set; }
        public double? ServiceCharge { get; set; }
        public double? DelvCharge { get; set; }
        public double? ProcessingFee { get; set; }
        public double? TotalSpecDisc { get; set; }
        public double? TotalVoucher { get; set; }
        public double? RndPay { get; set; }
        public double? PayAmt { get; set; }
        public double? Change { get; set; }
        public double? TotalPv { get; set; }
        public double? TotalSv { get; set; }
        public string? PaidStatus { get; set; }
        public double? StampDuty { get; set; }
        public string? Location { get; set; }
        public string? DelvHpno { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ReferenceNo { get; set; }
    }

    public class TransDetailData
    {
        public int? RowId { get; set; }
        public int? Seq { get; set; }
        public string? ProdCode { get; set; }
        public double? Qty { get; set; }
        public double? Price { get; set; }
        public double? PromoAmt { get; set; }
        public string? ProductName { get; set; }
        public string? SellingDesc { get; set; }
    }

    public class PrintSettingData
    {
        public string? OptType { get; set; }
        public bool? CompName { get; set; }
        public bool? CompRegno { get; set; }
        public bool? CompAddr { get; set; }
        public bool? CompPhone1 { get; set; }
        public bool? CompEmail { get; set; }
        public bool? CompPic { get; set; }
        public bool? QueueNo { get; set; }
        public bool? CustType { get; set; }
        public bool? CustId { get; set; }
        public bool? CustName { get; set; }
        public bool? SalesType { get; set; }
        public bool? ServiceType { get; set; }
        public bool? TableNo { get; set; }
        public bool? OrderNo { get; set; }
        public bool? InvoiceNo { get; set; }
        public bool? ReceiptNo { get; set; }
        public bool? Cashier { get; set; }
        public bool? Date { get; set; }
        public bool? SalesPrice { get; set; }
        public bool? Product { get; set; }
        public bool? SellingDesc { get; set; }
        public bool? PaymentInfo { get; set; }
        public bool? PvInfo { get; set; }
        public bool? SvInfo { get; set; }
        public bool? FooterMsg { get; set; }
    }

    public class PrintReceiptDataResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

