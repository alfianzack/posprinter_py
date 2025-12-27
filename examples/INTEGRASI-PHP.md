# Panduan Integrasi Print dan Cash Drawer dari Website PHP ke POS Printer

Dokumen ini menjelaskan cara mengintegrasikan print dan cash drawer dari website PHP (Yii2) ke aplikasi POS Printer.

## Cara Kerja

Aplikasi POS Printer menyediakan JavaScript bridge yang dapat diakses dari website yang ditampilkan di WebView. Bridge ini dapat:

1. **Intercept `window.print()`** - Secara otomatis menangkap panggilan `window.print()` dan mengarahkannya ke POS printer
2. **Print langsung** - Menggunakan fungsi `window.posPrinter.print()` atau `window.posPrinter.printFromElement()`
3. **Buka Cash Drawer** - Menggunakan fungsi `window.posPrinter.openCashDrawer()` atau auto-open dengan atribut `data-auto-cash-drawer`

## Metode 1: Menggunakan Intercept window.print() (Paling Mudah)

Aplikasi POS Printer secara otomatis mengintercept panggilan `window.print()`. Jika ada elemen dengan ID `PrintContent`, kontennya akan diekstrak dan dicetak ke POS printer.

### Kode PHP (Minimal Changes)

```php
<?php
// ... kode PHP Anda yang sudah ada ...

$js = <<<JS
    $(".btnprint").click(function() {
        // Cukup panggil window.print() - akan otomatis di-intercept
        window.print();
        
        setTimeout(function () {
            window.close()
        }, 2000);
    });
JS;

$this->registerJs($js);
?>
```

**Keuntungan:**
- Perubahan minimal pada kode PHP
- Tetap kompatibel dengan browser biasa (akan fallback ke print dialog browser)
- Otomatis bekerja jika aplikasi POS Printer sedang berjalan

## Metode 2: Menggunakan posPrinter.printFromElement() (Lebih Eksplisit)

Gunakan fungsi `printFromElement()` untuk lebih eksplisit dan kontrol yang lebih baik.

### Kode PHP

```php
<?php
// ... kode PHP Anda yang sudah ada ...

$js = <<<JS
    $(".btnprint").click(function() {
        // Cek apakah POS Printer bridge tersedia
        if (window.posPrinter) {
            // Print ke POS printer
            window.posPrinter.printFromElement('#PrintContent', true);
            
            // Optional: Buka cash drawer setelah print
            // window.posPrinter.openCashDrawer(2);
        } else {
            // Fallback ke metode original jika POS Printer tidak tersedia
            const oIframe = document.getElementById('iframePrint');
            const oContent = document.getElementById('PrintContent').innerHTML;
            let oDoc = (oIframe.contentWindow || oIframe.contentDocument);
            if (oDoc.document) oDoc = oDoc.document;
            oDoc.write('<body onload="this.focus(); this.print();" style="font-family:Arial !important;">');
            oDoc.write(oContent + '</body>');
            oDoc.close();
        }
        
        setTimeout(function () {
            window.close()
        }, 2000);
    });
JS;

$this->registerJs($js);
?>
```

**Keuntungan:**
- Kontrol lebih baik
- Dapat menambahkan logika tambahan (misalnya buka cash drawer)
- Fallback yang jelas jika POS Printer tidak tersedia

## Metode 3: Menggunakan posPrinter.print() dengan Format Manual

Jika Anda ingin mengontrol format teks secara manual:

### Kode PHP

```php
<?php
// ... kode PHP Anda yang sudah ada ...

$js = <<<JS
    $(".btnprint").click(function() {
        if (window.posPrinter) {
            // Format teks secara manual
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
        } else {
            // Fallback ke metode original
            window.print();
        }
        
        setTimeout(function () {
            window.close()
        }, 2000);
    });
JS;

$this->registerJs($js);
?>
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
    <table>
        <tr>
            <td>Item</td>
            <td>Price</td>
        </tr>
        <!-- ... -->
    </table>
</div>
```

**Atribut `data-auto-cash-drawer`:**
- `"1"` - Auto-open cash drawer dengan Pin 1 setelah print
- `"2"` - Auto-open cash drawer dengan Pin 2 setelah print (default)
- `"false"` atau tidak ada - Tidak auto-open cash drawer

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

**Auto-Open Cash Drawer Setelah Print:**

Anda dapat menggunakan atribut `data-auto-cash-drawer` pada elemen `PrintContent` untuk auto-open cash drawer setelah print:

```html
<!-- Auto-open dengan Pin 2 (default) -->
<div id="PrintContent" data-auto-cash-drawer="2">
    <!-- konten receipt -->
</div>

<!-- Auto-open dengan Pin 1 -->
<div id="PrintContent" data-auto-cash-drawer="1">
    <!-- konten receipt -->
</div>

<!-- Tidak auto-open -->
<div id="PrintContent" data-auto-cash-drawer="false">
    <!-- konten receipt -->
</div>
```

Atau gunakan JavaScript untuk membuka cash drawer setelah print:

```javascript
window.posPrinter.printFromElement('#PrintContent', true);
setTimeout(function() {
    window.posPrinter.openCashDrawer(2); // Pin 2
}, 500); // Delay 500ms untuk memastikan print selesai
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

## Contoh Lengkap

Lihat file:
- `examples/php-receipt-example.php` - Contoh lengkap dengan semua fitur (termasuk cash drawer)
- `examples/php-receipt-simple.php` - Contoh sederhana dengan perubahan minimal
- `examples/php-cashdrawer-example.php` - Contoh lengkap penggunaan cash drawer

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

4. **Cek console browser untuk error**
   - Buka Developer Tools di WebView
   - Lihat tab Console untuk error messages

### Print ke browser print dialog bukan POS printer

- Pastikan aplikasi POS Printer sedang berjalan
- Pastikan website dimuat di WebView aplikasi POS Printer, bukan di browser biasa
- Cek apakah `window.posPrinter` tersedia dengan `console.log(window.posPrinter)`

### Format print tidak sesuai

- Aplikasi mengekstrak teks dari HTML, jadi format HTML kompleks mungkin tidak terkonversi dengan sempurna
- Gunakan tabel sederhana untuk hasil terbaik
- Atau gunakan `window.posPrinter.print()` dengan format manual untuk kontrol penuh

## Catatan Penting

1. **Bridge hanya tersedia di WebView aplikasi POS Printer**
   - Jika website dibuka di browser biasa, `window.posPrinter` akan `undefined`
   - Selalu sediakan fallback ke metode print biasa

2. **Format Teks**
   - POS printer menggunakan plain text, bukan HTML
   - Format HTML kompleks akan dikonversi ke teks sederhana
   - Gunakan `\n` untuk line break

3. **Auto Print**
   - Jika Anda ingin auto print saat halaman dimuat, gunakan:
   ```javascript
   $(document).ready(function() {
       setTimeout(function() {
           $('.btnprint').trigger('click');
       }, 500);
   });
   ```

## Dukungan

Jika mengalami masalah, pastikan:
- Aplikasi POS Printer sudah terinstall dan berjalan
- WebView2 Runtime sudah terinstall
- Printer sudah dipilih di aplikasi POS Printer
- Website menggunakan HTTPS atau HTTP (bukan file://)

