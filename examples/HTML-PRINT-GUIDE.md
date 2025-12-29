# Panduan Print HTML Content ke POS Printer

Dokumen ini menjelaskan berbagai cara untuk mencetak konten HTML ke POS printer dengan contoh lengkap.

## Metode Print HTML Content

Ada 3 metode utama untuk print HTML content:

### 1. Menggunakan `window.posPrinter.printFromElement()` (Direkomendasikan)

Metode ini paling eksplisit dan memberikan kontrol penuh.

**Keuntungan:**
- Kontrol lebih baik
- Dapat menambahkan logika tambahan (misalnya buka cash drawer)
- Fallback yang jelas jika POS Printer tidak tersedia

**Contoh:**

```javascript
function printFromElement() {
    if (window.posPrinter) {
        // Print konten dari elemen HTML
        window.posPrinter.printFromElement('#PrintContent', true);
        
        // Buka cash drawer setelah print
        setTimeout(function() {
            window.posPrinter.openCashDrawer(2);
        }, 500);
    } else {
        alert('POS Printer bridge tidak tersedia');
    }
}
```

**Parameter:**
- `selector` (string atau Element): CSS selector atau DOM element yang ingin dicetak
- `cutPaper` (boolean, optional): Apakah kertas harus dipotong setelah print. Default: `true`

### 2. Menggunakan `window.print()` (Paling Mudah)

Metode ini paling sederhana - cukup panggil `window.print()`. Aplikasi POS Printer akan otomatis mengintercept dan print ke POS printer jika elemen dengan ID `PrintContent` ditemukan.

**Keuntungan:**
- Perubahan minimal pada kode
- Tetap kompatibel dengan browser biasa (akan fallback ke print dialog browser)
- Otomatis bekerja jika aplikasi POS Printer sedang berjalan

**Contoh:**

```javascript
function printWithWindowPrint() {
    // Cukup panggil window.print()
    // Aplikasi akan otomatis:
    // 1. Mencari elemen dengan ID "PrintContent"
    // 2. Extract text dari elemen tersebut
    // 3. Print ke POS printer
    // 4. Auto-open cash drawer jika ada atribut data-auto-cash-drawer
    window.print();
}
```

**Syarat:**
- Elemen yang ingin dicetak harus memiliki ID `PrintContent`
- Untuk auto-open cash drawer, tambahkan atribut `data-auto-cash-drawer="2"` pada elemen

### 3. Menggunakan `window.posPrinter.print()` dengan Format Manual

Jika Anda ingin format teks secara manual tanpa HTML:

**Contoh:**

```javascript
function printDirectText() {
    if (window.posPrinter) {
        var receipt = '';
        receipt += 'TOKO CONTOH\n';
        receipt += 'Jl. Contoh No. 123\n';
        receipt += 'Telp: 081234567890\n';
        receipt += '=================================\n';
        receipt += 'Tanggal: ' + new Date().toLocaleString('id-ID') + '\n';
        receipt += '---------------------------------\n';
        receipt += 'Item 1              Rp 10.000\n';
        receipt += 'Item 2              Rp 15.000\n';
        receipt += '---------------------------------\n';
        receipt += 'Total               Rp 25.000\n';
        receipt += '\n';
        receipt += 'Terima Kasih\n';
        receipt += '=================================\n';
        
        window.posPrinter.print(receipt, true);
        
        setTimeout(function() {
            window.posPrinter.openCashDrawer(2);
        }, 500);
    }
}
```

## Struktur HTML yang Disarankan

Pastikan elemen yang ingin dicetak memiliki ID `PrintContent`:

```html
<!-- 
    Tambahkan data-auto-cash-drawer untuk auto-open cash drawer setelah print
    Nilai: "1" untuk Pin 1, "2" untuk Pin 2 (default), atau "false" untuk disable
-->
<div id="PrintContent" data-auto-cash-drawer="2">
    <!-- Konten receipt Anda di sini -->
    <div style="text-align: center;">
        <h2>TOKO CONTOH</h2>
        <p>Jl. Contoh No. 123</p>
        <p>Telp: 081234567890</p>
        
        <hr>
        
        <table style="width: 100%;">
            <tr>
                <td>Item 1</td>
                <td style="text-align: right;">Rp 10.000</td>
            </tr>
            <tr>
                <td>Item 2</td>
                <td style="text-align: right;">Rp 15.000</td>
            </tr>
        </table>
        
        <hr>
        
        <p style="text-align: right;"><strong>Total: Rp 25.000</strong></p>
        
        <p><strong>Terima Kasih</strong></p>
    </div>
</div>
```

**Atribut `data-auto-cash-drawer`:**
- `"1"` - Auto-open cash drawer dengan Pin 1 setelah print
- `"2"` - Auto-open cash drawer dengan Pin 2 setelah print (default)
- `"false"` atau tidak ada - Tidak auto-open cash drawer

## Tips Format HTML untuk Print

### 1. Gunakan Inline Styles

POS printer akan mengekstrak teks dari HTML, jadi gunakan inline styles untuk hasil terbaik:

```html
<div style="text-align: center; font-size: 14px; font-weight: bold;">
    TOKO CONTOH
</div>
```

### 2. Gunakan Tabel untuk Alignment

Untuk alignment yang konsisten, gunakan tabel:

```html
<table style="width: 100%;">
    <tr>
        <td>Item</td>
        <td style="text-align: right;">Rp 10.000</td>
    </tr>
</table>
```

### 3. Gunakan `<hr>` atau Border untuk Garis Pemisah

```html
<hr style="border-top: 1px solid #000; margin: 10px 0;">
```

atau

```html
<div style="border-top: 1px solid #000; margin: 10px 0;"></div>
```

### 4. Hindari CSS Kompleks

POS printer menggunakan plain text, jadi format HTML kompleks mungkin tidak terkonversi dengan sempurna. Gunakan struktur HTML sederhana.

### 5. Gunakan Font Size yang Sesuai

```html
<div style="font-size: 12px;">Teks normal</div>
<div style="font-size: 14px; font-weight: bold;">Teks besar dan tebal</div>
<div style="font-size: 10px;">Teks kecil</div>
```

## Contoh Lengkap

Lihat file:
- `examples/php-html-print-example.php` - Contoh lengkap dengan semua metode
- `examples/php-html-print-simple.php` - Contoh sederhana dengan perubahan minimal
- `examples/php-receipt-example.php` - Contoh receipt lengkap
- `examples/php-logo-example.php` - Contoh dengan logo

## API JavaScript Bridge

### window.posPrinter.print(content, cutPaper)

Print teks langsung ke POS printer.

**Parameters:**
- `content` (string): Teks yang ingin dicetak
- `cutPaper` (boolean, optional): Apakah kertas harus dipotong setelah print. Default: `true`

**Contoh:**
```javascript
window.posPrinter.print('Hello World\nLine 2', true);
```

### window.posPrinter.printFromElement(selector, cutPaper)

Print konten dari elemen HTML ke POS printer.

**Parameters:**
- `selector` (string atau Element): CSS selector atau DOM element
- `cutPaper` (boolean, optional): Apakah kertas harus dipotong setelah print. Default: `true`

**Contoh:**
```javascript
window.posPrinter.printFromElement('#PrintContent', true);
window.posPrinter.printFromElement(document.getElementById('PrintContent'), true);
```

### window.posPrinter.openCashDrawer(pin)

Buka cash drawer.

**Parameters:**
- `pin` (number, optional): Pin yang digunakan (1 atau 2). Default: `2`

**Contoh:**
```javascript
window.posPrinter.openCashDrawer();      // Pin 2 (default)
window.posPrinter.openCashDrawer(2);     // Pin 2
window.posPrinter.openCashDrawer(1);     // Pin 1
```

### window.onPosPrinterResponse(callback)

Mendengarkan response dari operasi print atau cash drawer.

**Contoh:**
```javascript
window.onPosPrinterResponse = function(response) {
    console.log('Type:', response.type);
    console.log('Success:', response.success);
    console.log('Message:', response.message);
    
    if (response.type === 'print' && response.success) {
        alert('Print berhasil!');
    }
};
```

## Troubleshooting

### Print tidak bekerja

1. **Pastikan aplikasi POS Printer sedang berjalan**
   - Buka aplikasi POS Printer
   - Pastikan WebView sudah memuat website Anda

2. **Cek apakah bridge tersedia**
   ```javascript
   if (window.posPrinter) {
       console.log('POS Printer bridge tersedia');
   } else {
       console.log('POS Printer bridge tidak tersedia');
   }
   ```

3. **Pastikan elemen PrintContent ada**
   ```javascript
   var element = document.getElementById('PrintContent');
   if (element) {
       console.log('PrintContent ditemukan');
   } else {
       console.log('PrintContent tidak ditemukan');
   }
   ```

### Format print tidak sesuai

- Aplikasi mengekstrak teks dari HTML, jadi format HTML kompleks mungkin tidak terkonversi dengan sempurna
- Gunakan tabel sederhana untuk hasil terbaik
- Atau gunakan `window.posPrinter.print()` dengan format manual untuk kontrol penuh

### Print ke browser print dialog bukan POS printer

- Pastikan aplikasi POS Printer sedang berjalan
- Pastikan website dimuat di WebView aplikasi POS Printer, bukan di browser biasa
- Cek apakah `window.posPrinter` tersedia dengan `console.log(window.posPrinter)`

## Catatan Penting

1. **Bridge hanya tersedia di WebView aplikasi POS Printer**
   - Jika website dibuka di browser biasa, `window.posPrinter` akan `undefined`
   - Selalu sediakan fallback ke metode print biasa

2. **Format Teks**
   - POS printer menggunakan plain text, bukan HTML
   - Format HTML kompleks akan dikonversi ke teks sederhana
   - Gunakan `\n` untuk line break dalam format manual

3. **Auto Print**
   - Jika Anda ingin auto print saat halaman dimuat:
   ```javascript
   $(document).ready(function() {
       setTimeout(function() {
           $('.btnprint').trigger('click');
       }, 500);
   });
   ```

