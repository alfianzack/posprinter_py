# Aplikasi POS Printer & Cash Drawer

Aplikasi Windows Forms C# untuk mengontrol POS printer dan cash drawer menggunakan ESC/POS commands.

## Fitur

- ✅ Print test receipt dengan format standar
- ✅ Print teks kustom
- ✅ Kontrol cash drawer (Pin 1 dan Pin 2)
- ✅ Pilih printer dari daftar printer yang terinstall
- ✅ **HTTP Server API untuk integrasi dengan server eksternal**
- ✅ **WebView2 untuk menampilkan website dan print dari website**
- ✅ **JavaScript Bridge untuk komunikasi antara website dan aplikasi**
- ✅ Interface yang user-friendly dengan TabControl

## Persyaratan

- .NET 9.0
- Windows OS
- **Microsoft Edge WebView2 Runtime** (akan diunduh otomatis atau download dari [Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/))
- POS Printer yang mendukung ESC/POS commands
- Cash drawer yang terhubung ke printer

## Cara Menggunakan

1. **Build aplikasi:**
   ```bash
   dotnet build
   ```

2. **Run aplikasi:**
   ```bash
   dotnet run
   ```

3. **Atau build executable:**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```

## Penggunaan

### Aplikasi Desktop

#### Tab 1: Kontrol Printer
1. **Pilih Printer:** Pilih printer POS dari dropdown
2. **Print Test Receipt:** Klik tombol untuk mencetak struk test
3. **Print Teks Kustom:** Masukkan teks di textbox dan klik tombol print
4. **Buka Cash Drawer:** Klik tombol untuk membuka cash drawer (menggunakan Pin 2 secara default)
5. **Buka dengan Pin Spesifik:** Gunakan tombol Pin 1 atau Pin 2 jika printer Anda menggunakan pin tertentu

#### Tab 2: Web POS
1. **Masukkan URL:** Masukkan URL website yang ingin ditampilkan (default: https://dxnpos-train.dxn2u.com)
2. **Klik Go:** Untuk memuat website
3. **Refresh:** Untuk me-reload halaman
4. **Print dari Website:** Website dapat menggunakan JavaScript bridge untuk print dan buka cash drawer

### HTTP Server API

1. **Start Server:** Masukkan port (default: 8080) dan klik "Start Server"
2. **Server akan berjalan di:** `http://localhost:{port}/`
3. **Gunakan API endpoint untuk mengirim perintah dari server eksternal**

#### API Endpoints

##### 1. Print Receipt
**POST** `/api/print`

Request Body:
```json
{
  "content": "Teks yang ingin dicetak\nBaris kedua",
  "cutPaper": true
}
```

Response:
```json
{
  "success": true,
  "message": "Print berhasil"
}
```

Contoh menggunakan cURL:
```bash
curl -X POST http://localhost:8080/api/print \
  -H "Content-Type: application/json" \
  -d "{\"content\":\"Test Print\",\"cutPaper\":true}"
```

##### 2. Buka Cash Drawer
**POST** `/api/cashdrawer`

Request Body:
```json
{
  "pin": 2
}
```
- `pin`: `null` atau tidak ada = default (Pin 2)
- `pin`: `1` = Pin 1
- `pin`: `2` = Pin 2

Response:
```json
{
  "success": true,
  "message": "Cash drawer berhasil dibuka"
}
```

Contoh menggunakan cURL:
```bash
curl -X POST http://localhost:8080/api/cashdrawer \
  -H "Content-Type: application/json" \
  -d "{\"pin\":2}"
```

##### 3. Status Server
**GET** `/api/status`

Response:
```json
{
  "status": "running",
  "port": 8080,
  "timestamp": "2024-01-01T12:00:00"
}
```

#### Contoh Integrasi dengan JavaScript/Node.js

```javascript
// Print receipt
async function printReceipt(content) {
  const response = await fetch('http://localhost:8080/api/print', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      content: content,
      cutPaper: true
    })
  });
  return await response.json();
}

// Buka cash drawer
async function openCashDrawer(pin = 2) {
  const response = await fetch('http://localhost:8080/api/cashdrawer', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ pin: pin })
  });
  return await response.json();
}
```

#### Contoh Integrasi dengan PHP

```php
// Print receipt
function printReceipt($content, $cutPaper = true) {
    $url = 'http://localhost:8080/api/print';
    $data = json_encode([
        'content' => $content,
        'cutPaper' => $cutPaper
    ]);
    
    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_POST, 1);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
    curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    
    $result = curl_exec($ch);
    curl_close($ch);
    
    return json_decode($result, true);
}

// Buka cash drawer
function openCashDrawer($pin = 2) {
    $url = 'http://localhost:8080/api/cashdrawer';
    $data = json_encode(['pin' => $pin]);
    
    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_POST, 1);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
    curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    
    $result = curl_exec($ch);
    curl_close($ch);
    
    return json_decode($result, true);
}
```

### Integrasi dari Website (JavaScript Bridge)

Aplikasi menyediakan JavaScript bridge yang dapat digunakan oleh website yang ditampilkan di WebView2. Bridge ini tersedia melalui object `window.posPrinter`.

#### Cara Menggunakan di Website

1. **Print Receipt:**
```javascript
if (window.posPrinter) {
    window.posPrinter.print('Teks yang ingin dicetak\nBaris kedua', true);
}
```

2. **Buka Cash Drawer:**
```javascript
// Default (Pin 2)
if (window.posPrinter) {
    window.posPrinter.openCashDrawer();
}

// Dengan pin spesifik
if (window.posPrinter) {
    window.posPrinter.openCashDrawer(2); // Pin 2
    window.posPrinter.openCashDrawer(1); // Pin 1
}
```

3. **Mendengarkan Response (Optional):**
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

#### Contoh Lengkap

Lihat file `examples/web-integration-example.html` untuk contoh lengkap penggunaan JavaScript bridge.

#### Integrasi dengan Website PHP (Yii2)

Aplikasi mendukung intercept `window.print()` secara otomatis. Jika website memanggil `window.print()` dan ada elemen dengan ID `PrintContent`, kontennya akan otomatis dicetak ke POS printer.

**Contoh Kode PHP:**
```php
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
```

**Atau gunakan fungsi eksplisit dengan cash drawer:**
```php
$js = <<<JS
    $(".btnprint").click(function() {
        if (window.posPrinter) {
            window.posPrinter.printFromElement('#PrintContent', true);
            // Buka cash drawer setelah print
            setTimeout(function() {
                window.posPrinter.openCashDrawer(2);
            }, 500);
        } else {
            // Fallback ke metode original
            window.print();
        }
    });
JS;
$this->registerJs($js);
```

**Atau gunakan atribut data untuk auto-open cash drawer:**
```html
<!-- Auto-open cash drawer dengan Pin 2 setelah print -->
<div id="PrintContent" data-auto-cash-drawer="2">
    <!-- konten receipt -->
</div>
```

Lihat file `examples/INTEGRASI-PHP.md` untuk panduan lengkap integrasi dengan website PHP.

**Catatan:** 
- Bridge hanya tersedia ketika website dimuat di dalam aplikasi POS Printer
- Pastikan aplikasi sudah memilih printer yang benar sebelum menggunakan fungsi print
- Website harus menggunakan HTTPS atau HTTP untuk keamanan
- Aplikasi secara otomatis mengintercept `window.print()` jika ada elemen `#PrintContent`

## Catatan Teknis

- Aplikasi menggunakan ESC/POS commands untuk komunikasi dengan printer
- Cash drawer dibuka menggunakan command `ESC p` dengan parameter pin dan timing
- Beberapa printer menggunakan Pin 1, beberapa menggunakan Pin 2
- Jika cash drawer tidak terbuka, coba gunakan pin yang berbeda

## Struktur Proyek

```
PosPrinterApp/
├── Program.cs                 # Entry point aplikasi
├── MainForm.cs               # Form utama dengan UI (TabControl)
├── Models/
│   └── PrintRequest.cs       # Model untuk request/response API
├── Services/
│   ├── PosPrinterService.cs  # Service untuk print receipt
│   ├── CashDrawerService.cs  # Service untuk kontrol cash drawer
│   ├── RawPrinterHelper.cs   # Helper untuk raw printing ke Windows
│   └── HttpServerService.cs  # HTTP server untuk API
├── examples/
│   ├── test-api.html         # Contoh testing API
│   ├── web-integration-example.html  # Contoh JavaScript bridge
│   ├── php-receipt-example.php  # Contoh lengkap integrasi PHP
│   ├── php-receipt-simple.php   # Contoh sederhana integrasi PHP
│   ├── php-cashdrawer-example.php  # Contoh penggunaan cash drawer
│   └── INTEGRASI-PHP.md      # Panduan integrasi dengan PHP
└── PosPrinterApp.csproj      # File proyek
```

## Troubleshooting

- **Printer tidak terdeteksi:** Pastikan printer sudah terinstall di Windows
- **Cash drawer tidak terbuka:** Coba gunakan pin yang berbeda (Pin 1 atau Pin 2)
- **Print gagal:** Pastikan printer mendukung ESC/POS commands dan dalam kondisi siap
- **Server tidak bisa start:** Pastikan port tidak digunakan oleh aplikasi lain. Coba gunakan port lain (misalnya 8081, 8082)
- **Server tidak bisa diakses dari komputer lain:** Server default hanya listen di localhost. Untuk akses dari jaringan, modifikasi kode untuk menggunakan `http://*:{port}/` atau `http://0.0.0.0:{port}/` (perlu permission administrator)
- **WebView2 tidak bisa dimuat:** Pastikan Microsoft Edge WebView2 Runtime sudah terinstall. Download dari [Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/)
- **JavaScript bridge tidak bekerja:** Pastikan website sudah fully loaded. Bridge di-inject setelah navigation completed. Cek console browser untuk melihat apakah bridge sudah tersedia
- **Print dari website tidak bekerja:** Pastikan printer sudah dipilih di tab "Kontrol Printer" sebelum menggunakan fungsi print dari website

