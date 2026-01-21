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
        // Mode 1: Langsung kirim HTML
        public string? HtmlContent { get; set; }
        
        // Mode 2: Kirim data, sistem yang generate HTML
        public PrintReceiptDataRequest? Data { get; set; }
        
        public bool CutPaper { get; set; } = true;
        
        // Untuk mode data: apakah menggunakan exact layout (default: true)
        public bool? UseExactLayout { get; set; } = true;
        
        // Untuk mode data: lebar bitmap (default: 576 untuk 80mm printer)
        public int? Width { get; set; }
        
        // Untuk mode data: apakah menggunakan dithering (default: true)
        public bool? Dither { get; set; }
    }

    public class PrintHtmlResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PrintHtmlExactRequest
    {
        public string HtmlContent { get; set; } = string.Empty;
        public bool CutPaper { get; set; } = true;
        public int? Width { get; set; } // Lebar bitmap dalam pixels (default: 576 untuk 80mm printer)
        public bool? Dither { get; set; } // Apakah menggunakan dithering (default: true)
    }

    public class PrintHtmlExactResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
