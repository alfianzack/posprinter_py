# Panduan Print HTML Content via API `/api/print`

Dokumen ini menjelaskan cara menggunakan endpoint `/api/print` untuk mencetak HTML content ke POS printer.

## Endpoint

**URL:** `http://localhost:8080/api/print`  
**Method:** `POST`  
**Content-Type:** `application/json`

## Request Format

```json
{
  "content": "<div>HTML content di sini</div>",
  "cutPaper": true
}
```

### Parameters

- **content** (string, required): HTML content yang ingin dicetak
- **cutPaper** (boolean, optional): Apakah kertas harus dipotong setelah print. Default: `true`

## Response Format

```json
{
  "success": true,
  "message": "Print berhasil"
}
```

atau jika error:

```json
{
  "success": false,
  "message": "Error message"
}
```

## Contoh Penggunaan

### 1. PHP

Lihat file: `examples/api-print-html-php.php`

```php
<?php
$apiUrl = 'http://localhost:8080/api/print';
$htmlContent = '<div style="text-align: center;"><h2>TOKO CONTOH</h2><p>Test Print</p></div>';

$data = [
    'content' => $htmlContent,
    'cutPaper' => true
];

$ch = curl_init($apiUrl);
curl_setopt($ch, CURLOPT_POST, true);
curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);

$response = curl_exec($ch);
$result = json_decode($response, true);

if ($result['success']) {
    echo "Print berhasil!";
} else {
    echo "Print gagal: " . $result['message'];
}
?>
```

### 2. JavaScript (Browser/Fetch API)

Lihat file: `examples/api-print-html-javascript.html`

```javascript
async function printViaApi(htmlContent, cutPaper = true) {
    const response = await fetch('http://localhost:8080/api/print', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            content: htmlContent,
            cutPaper: cutPaper
        })
    });
    
    const result = await response.json();
    return result;
}

// Penggunaan
const htmlContent = `
<div style="text-align: center;">
    <h2>TOKO CONTOH</h2>
    <p>Test Print</p>
</div>
`;

const result = await printViaApi(htmlContent, true);
if (result.success) {
    console.log('Print berhasil!');
} else {
    console.log('Print gagal:', result.message);
}
```

### 3. cURL

Lihat file: `examples/api-print-html-curl.sh`

```bash
curl -X POST "http://localhost:8080/api/print" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "<div style=\"text-align: center;\"><h2>TOKO CONTOH</h2><p>Test Print</p></div>",
    "cutPaper": true
  }'
```

### 4. Python

Lihat file: `examples/api-print-html-python.py`

```python
import requests
import json

api_url = "http://localhost:8080/api/print"

html_content = """
<div style="text-align: center;">
    <h2>TOKO CONTOH</h2>
    <p>Test Print</p>
</div>
"""

data = {
    "content": html_content,
    "cutPaper": True
}

response = requests.post(api_url, json=data, headers={"Content-Type": "application/json"})
result = response.json()

if result["success"]:
    print("Print berhasil!")
else:
    print(f"Print gagal: {result['message']}")
```

### 5. Node.js

Lihat file: `examples/api-print-html-nodejs.js`

```javascript
const axios = require('axios');

async function printViaApi(htmlContent, cutPaper = true) {
    const response = await axios.post('http://localhost:8080/api/print', {
        content: htmlContent,
        cutPaper: cutPaper
    }, {
        headers: {
            'Content-Type': 'application/json'
        }
    });
    
    return response.data;
}

// Penggunaan
const htmlContent = `
<div style="text-align: center;">
    <h2>TOKO CONTOH</h2>
    <p>Test Print</p>
</div>
`;

const result = await printViaApi(htmlContent, true);
if (result.success) {
    console.log('Print berhasil!');
} else {
    console.log('Print gagal:', result.message);
}
```

## Contoh HTML Content untuk Receipt

### Receipt Sederhana

```html
<div style="text-align: center;">
    <h2 style="font-size: 18px; font-weight: bold;">TOKO CONTOH</h2>
    <p style="font-size: 12px;">Jl. Contoh No. 123</p>
    <p style="font-size: 12px;">Telp: 081234567890</p>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <table style="width: 100%; font-size: 12px;">
        <tr>
            <td>Item 1</td>
            <td style="text-align: right;">Rp 10.000</td>
        </tr>
        <tr>
            <td>Item 2</td>
            <td style="text-align: right;">Rp 15.000</td>
        </tr>
    </table>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <p style="text-align: right; font-weight: bold;">Total: Rp 25.000</p>
    <p style="text-align: center; font-weight: bold;">Terima Kasih</p>
</div>
```

### Receipt dengan Data Dinamis

```html
<div style="text-align: center;">
    <h2 style="font-size: 18px; font-weight: bold;">TOKO CONTOH</h2>
    <p style="font-size: 12px;">Jl. Contoh No. 123</p>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <p style="font-size: 12px;"><strong>Order No:</strong> ORD-20240101120000</p>
    <p style="font-size: 12px;"><strong>Tanggal:</strong> 01/01/2024 12:00:00</p>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <table style="width: 100%; font-size: 12px;">
        <tr>
            <td>Produk A x2</td>
            <td style="text-align: right;">Rp 20.000</td>
        </tr>
        <tr>
            <td>Produk B x1</td>
            <td style="text-align: right;">Rp 15.000</td>
        </tr>
    </table>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <p style="text-align: right; font-weight: bold;">Total: Rp 35.000</p>
    <p style="text-align: center; font-weight: bold;">Terima Kasih</p>
</div>
```

## Tips Format HTML untuk Print

1. **Gunakan Inline Styles**: POS printer akan mengekstrak teks dari HTML, jadi gunakan inline styles untuk hasil terbaik.

2. **Gunakan Tabel untuk Alignment**: Untuk alignment yang konsisten, gunakan tabel:
   ```html
   <table style="width: 100%;">
       <tr>
           <td>Item</td>
           <td style="text-align: right;">Rp 10.000</td>
       </tr>
   </table>
   ```

3. **Gunakan `<hr>` atau Border untuk Garis Pemisah**:
   ```html
   <hr style="border-top: 1px solid #000; margin: 10px 0;">
   ```

4. **Hindari CSS Kompleks**: POS printer menggunakan plain text, jadi format HTML kompleks mungkin tidak terkonversi dengan sempurna.

5. **Gunakan Font Size yang Sesuai**:
   ```html
   <div style="font-size: 12px;">Teks normal</div>
   <div style="font-size: 14px; font-weight: bold;">Teks besar dan tebal</div>
   ```

## Catatan Penting

1. **HTML akan dikonversi ke Plain Text**: Endpoint ini menerima HTML, tapi akan dikonversi ke plain text oleh printer service. Format HTML kompleks mungkin tidak terkonversi dengan sempurna.

2. **Escape Karakter Khusus**: Saat mengirim HTML dalam JSON, pastikan untuk escape karakter khusus seperti `"`, `\`, dll.

3. **CORS**: Endpoint mendukung CORS, jadi bisa dipanggil dari browser atau server lain.

4. **Error Handling**: Selalu cek response `success` untuk mengetahui apakah print berhasil atau tidak.

5. **Timeout**: Set timeout yang sesuai untuk request (default: 10 detik).

## Troubleshooting

### Print tidak bekerja

1. **Pastikan aplikasi DXN POS Printer sedang berjalan**
2. **Pastikan HTTP Server sudah di-start** (default port: 8080)
3. **Cek URL API** - pastikan port sesuai dengan yang dikonfigurasi
4. **Cek printer sudah dipilih** di aplikasi

### Error: Connection refused

- Pastikan aplikasi DXN POS Printer sedang berjalan
- Pastikan HTTP Server sudah di-start
- Cek firewall tidak memblokir port 8080

### Format print tidak sesuai

- HTML akan dikonversi ke plain text
- Gunakan struktur HTML sederhana
- Hindari CSS kompleks
- Gunakan tabel untuk alignment

## File Contoh

- `examples/api-print-html-php.php` - Contoh PHP lengkap
- `examples/api-print-html-javascript.html` - Contoh JavaScript/Browser
- `examples/api-print-html-curl.sh` - Contoh cURL/Bash
- `examples/api-print-html-python.py` - Contoh Python
- `examples/api-print-html-nodejs.js` - Contoh Node.js

