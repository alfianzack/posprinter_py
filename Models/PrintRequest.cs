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

    public class PrintAndDrawerRequest
    {
        public string Content { get; set; } = string.Empty;
        public bool CutPaper { get; set; } = true;
        public int? DrawerPin { get; set; } // null = default, 1 = pin 1, 2 = pin 2
        public int? DrawerDelay { get; set; } // Delay dalam milliseconds sebelum buka drawer (default: 500)
    }

    public class PrintAndDrawerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool PrintSuccess { get; set; }
        public bool DrawerSuccess { get; set; }
    }

    public class PrintHtmlRequest
    {
        public string HtmlContent { get; set; } = string.Empty;
        public bool CutPaper { get; set; } = true;
    }

    public class PrintHtmlResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
