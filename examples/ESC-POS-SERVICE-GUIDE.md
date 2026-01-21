# EscPosPrinterService - Panduan Lengkap

## Overview

`EscPosPrinterService` adalah service untuk print receipt dengan format yang lebih kaya menggunakan ESC/POS commands. Service ini mendukung:

- ✅ **Bold Text** - Teks tebal
- ✅ **Font Size** - Ukuran font (normal, double width, double height, double width & height)
- ✅ **Draw Line** - Garis horizontal dengan karakter tertentu
- ✅ **Alignment** - Left, Center, Right
- ✅ **Underline** - Garis bawah
- ✅ **Reverse Colors** - Warna terbalik

## Fitur Utama

### 1. Bold Text
```csharp
var printer = new EscPosPrinterService();
var sb = new StringBuilder();
sb.Append(printer.SetBold(true));
sb.Append("This is bold text");
sb.Append(printer.SetBold(false));
```

### 2. Font Size
```csharp
// Double width and height
sb.Append(printer.SetFontSize(2, 2));

// Double height only
sb.Append(printer.SetFontSize(1, 2));

// Double width only
sb.Append(printer.SetFontSize(2, 1));

// Normal size
sb.Append(printer.SetFontSize(1, 1));
```

### 3. Draw Line
```csharp
// Draw line dengan karakter '='
sb.Append(printer.DrawLine('=', 48));

// Draw line dengan karakter '-'
sb.Append(printer.DrawLine('-', 48));
```

### 4. Alignment
```csharp
// Center
sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Center));

// Left
sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Left));

// Right
sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Right));
```

## Contoh Penggunaan

### Contoh 1: Print Sederhana
```csharp
var printer = new EscPosPrinterService();
printer.PrintReceipt("Hello World\nThis is a test", cutPaper: true);
```

### Contoh 2: Print Formatted Receipt
```csharp
var printer = new EscPosPrinterService();

var receipt = new ReceiptContent
{
    CompanyName = "DXN COMPANY",
    CompanyAddress = "123 Main Street",
    CompanyPhone = "123-456-7890",
    OrderNo = "ORD001",
    Date = "2024-01-01 10:00:00",
    Items = new List<ReceiptItem>
    {
        new ReceiptItem
        {
            ProductName = "Product 1",
            Description = "Size: Large",
            Price = 50.00
        }
    },
    Total = 50.00,
    Footer = "Thank you!"
};

printer.PrintFormattedReceipt(receipt, cutPaper: true);
```

### Contoh 3: Manual Formatting
```csharp
var printer = new EscPosPrinterService();
var sb = new StringBuilder();

// Initialize
sb.Append(printer.InitializePrinter());

// Header (bold, center, large)
sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Center));
sb.Append(printer.SetBold(true));
sb.Append(printer.SetFontSize(2, 2));
sb.Append("DXN COMPANY");
sb.Append(printer.SetFontSize(1, 1));
sb.Append(printer.SetBold(false));
sb.Append(printer.FeedLines(1));

// Draw line
sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Left));
sb.Append(printer.DrawLine('=', 48));
sb.Append(printer.FeedLines(1));

// Content
sb.Append("Order No: ORD001");
sb.Append(printer.FeedLines(1));
sb.Append("Total: Rp 100.00");
sb.Append(printer.FeedLines(2));

// Cut paper
sb.Append(printer.CutPaper());

// Print
RawPrinterHelper.SendStringToPrinter(printer.GetDefaultPrinter(), sb.ToString());
```

## Method Reference

### InitializePrinter()
Initialize printer (ESC @)

### SetBold(bool bold)
Set text bold (ESC E n)
- `true` = bold on
- `false` = bold off

### SetFontSize(int width, int height)
Set font size (GS ! n)
- `width`: 1 = normal, 2 = double width
- `height`: 1 = normal, 2 = double height

### SetAlignment(Alignment alignment)
Set alignment (ESC a n)
- `Alignment.Left` = 0
- `Alignment.Center` = 1
- `Alignment.Right` = 2

### DrawLine(char character, int width)
Draw horizontal line
- `character`: karakter untuk line (default: '-')
- `width`: lebar line dalam karakter (default: 48)

### FeedLines(int count)
Feed lines (LF)
- `count`: jumlah baris (default: 1)

### CutPaper()
Cut paper (GS V n)

### SetUnderline(bool underline, int thickness)
Set underline (ESC - n)
- `underline`: true = on, false = off
- `thickness`: 1 = thin, 2 = thick

### SetReverseColors(bool reverse)
Set reverse colors (GS B n)
- `reverse`: true = reverse, false = normal

## ReceiptContent Model

```csharp
public class ReceiptContent
{
    public string? CompanyName { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? OrderNo { get; set; }
    public string? Date { get; set; }
    public List<ReceiptItem>? Items { get; set; }
    public double? Total { get; set; }
    public string? Footer { get; set; }
}
```

## ReceiptItem Model

```csharp
public class ReceiptItem
{
    public string? ProductName { get; set; }
    public string? Description { get; set; }
    public double? Price { get; set; }
    public int? Quantity { get; set; }
}
```

## ESC/POS Commands Reference

| Command | Hex | Description |
|---------|-----|-------------|
| ESC @ | 1B 40 | Initialize printer |
| ESC E n | 1B 45 n | Bold (n=1 on, n=0 off) |
| ESC a n | 1B 61 n | Alignment (n=0 left, n=1 center, n=2 right) |
| GS ! n | 1D 21 n | Font size (bit 0-3 height, bit 4-7 width) |
| ESC - n | 1B 2D n | Underline (n=0 off, n=1 thin, n=2 thick) |
| GS B n | 1D 42 n | Reverse colors (n=1 on, n=0 off) |
| GS V n | 1D 56 n | Cut paper (n=66 full cut) |
| LF | 0A | Line feed |

## Tips

1. **Selalu initialize printer** di awal dengan `InitializePrinter()`
2. **Reset format** setelah menggunakan format khusus (bold, size, dll)
3. **Gunakan DrawLine** untuk separator yang konsisten
4. **Set alignment** sebelum text yang perlu di-align
5. **FeedLines** untuk spacing yang konsisten

## Contoh Receipt Lengkap

```csharp
var printer = new EscPosPrinterService();
var sb = new StringBuilder();

sb.Append(printer.InitializePrinter());

// Company Header (Bold, Center, Large)
sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Center));
sb.Append(printer.SetBold(true));
sb.Append(printer.SetFontSize(2, 2));
sb.Append("DXN COMPANY");
sb.Append(printer.SetFontSize(1, 1));
sb.Append(printer.SetBold(false));
sb.Append(printer.FeedLines(1));

sb.Append("123 Main Street");
sb.Append(printer.FeedLines(1));
sb.Append("Phone: 123-456-7890");
sb.Append(printer.FeedLines(1));

// Separator
sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Left));
sb.Append(printer.DrawLine('=', 48));
sb.Append(printer.FeedLines(1));

// Order Info
sb.Append(printer.SetBold(true));
sb.Append("Order No: ORD001");
sb.Append(printer.SetBold(false));
sb.Append(printer.FeedLines(1));
sb.Append("Date: 2024-01-01 10:00:00");
sb.Append(printer.FeedLines(1));

// Separator
sb.Append(printer.DrawLine('-', 48));
sb.Append(printer.FeedLines(1));

// Items
sb.Append("Product 1");
sb.Append(printer.FeedLines(1));
sb.Append("  Size: Large");
sb.Append(printer.FeedLines(1));
sb.Append("Product 2");
sb.Append(printer.FeedLines(1));

// Separator
sb.Append(printer.DrawLine('-', 48));
sb.Append(printer.FeedLines(1));

// Total (Bold, Large)
sb.Append(printer.SetBold(true));
sb.Append(printer.SetFontSize(1, 2));
sb.Append("Total: Rp 100.00");
sb.Append(printer.SetFontSize(1, 1));
sb.Append(printer.SetBold(false));
sb.Append(printer.FeedLines(1));

// Separator
sb.Append(printer.DrawLine('=', 48));
sb.Append(printer.FeedLines(1));

// Footer (Center)
sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Center));
sb.Append("Thank you. Please come again.");
sb.Append(printer.FeedLines(2));

// Cut
sb.Append(printer.SetAlignment(EscPosPrinterService.Alignment.Left));
sb.Append(printer.CutPaper());

// Print
RawPrinterHelper.SendStringToPrinter(printer.GetDefaultPrinter(), sb.ToString());
```
