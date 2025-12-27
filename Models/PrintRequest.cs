namespace PosPrinterApp.Models
{
    public class PrintRequest
    {
        public string Content { get; set; } = string.Empty;
        public bool CutPaper { get; set; } = true;
    }

    public class PrintResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CashDrawerRequest
    {
        public int? Pin { get; set; } // null = default, 1 = pin 1, 2 = pin 2
    }

    public class CashDrawerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
