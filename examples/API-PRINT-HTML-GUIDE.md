# API Print HTML - Panduan Lengkap

## Endpoint
```
POST http://localhost:7080/api/print-html
Content-Type: application/json
```

## Penggunaan

**Endpoint ini sekarang menggunakan ESC/POS library untuk print dengan format kaya (bold, font size, line).**

Client hanya perlu mengirim **data saja** (tidak perlu HTML). Sistem akan:
1. Menerima data dari client
2. Convert data ke format ReceiptContent
3. Print menggunakan EscPosPrinterService dengan format kaya (bold, font size, line)

---

## Kirim Data (Print dengan ESC/POS)

### Request Body
```json
{
  "data": {
    "company": { ... },
    "transHead": { ... },
    "transDetail": [ ... ],
    "printSetting": { ... }
  },
  "cutPaper": true
}
```

### Parameter
- **data** (required): Object data receipt (PrintReceiptDataRequest)
- **cutPaper** (optional, default: true): Apakah kertas dipotong setelah print

**Catatan**: Endpoint ini menggunakan ESC/POS library yang otomatis akan:
- Print company name dengan **bold** dan **large font** (center aligned)
- Print order info dengan **bold** untuk label
- Draw **lines** sebagai separator
- Print total dengan **bold** dan **large font**
- Print footer dengan **center alignment**

### Struktur Data

#### Company Data
```json
{
  "companyId": "COMP001",
  "companyName": "DXN COMPANY",
  "regNo": "REG123456",
  "address": "123 Main Street",
  "phone1": "123-456-7890",
  "email": "info@dxn.com",
  "contactPerson": "John Manager",
  "taxRegistrant": true,
  "serviceCharge": true
}
```

#### Transaction Head Data
```json
{
  "orderNo": "ORD001",
  "invoiceNo": "INV001",
  "receiptNo": "RCP001",
  "customerType": "CT240001",
  "customerTypeName": "Member",
  "customerId": "CUST001",
  "customerName": "John Doe",
  "serviceType": "ST001",
  "serviceTypeName": "Dine In",
  "salesType": "SALES001",
  "salesTypeName": "Retail",
  "tableNo": "T01",
  "cashier": "Cashier 1",
  "createdDate": "2024-01-01 10:00:00",
  "totalPrice": 150.50,
  "subtotalPrice": 140.00,
  "totalTax": 10.00,
  "serviceCharge": 5.00,
  "delvCharge": 10.00,
  "processingFee": 2.50,
  "totalSpecDisc": 5.00,
  "totalVoucher": 0.00,
  "rndPay": 0.00,
  "payAmt": 200.00,
  "change": 49.50,
  "totalPv": 15.0,
  "totalSv": 5.0,
  "paidStatus": "1",
  "stampDuty": 0.00,
  "paymentMethod": "Cash",
  "referenceNo": "REF001"
}
```

#### Transaction Detail Data (Array)
```json
[
  {
    "rowId": 1,
    "seq": 1,
    "prodCode": "PROD001",
    "productName": "Product A",
    "qty": 2,
    "price": 50.00,
    "promoAmt": 0.00,
    "sellingDesc": "Size: Large, Color: Red"
  }
]
```

#### Print Setting Data
```json
{
  "optType": "ORDER",
  "compName": true,
  "compRegno": true,
  "compAddr": true,
  "compPhone1": true,
  "compEmail": true,
  "compPic": true,
  "queueNo": true,
  "custType": true,
  "custId": true,
  "custName": true,
  "salesType": true,
  "serviceType": true,
  "tableNo": true,
  "orderNo": true,
  "invoiceNo": true,
  "receiptNo": true,
  "cashier": true,
  "date": true,
  "salesPrice": true,
  "product": true,
  "sellingDesc": true,
  "paymentInfo": true,
  "pvInfo": true,
  "svInfo": true,
  "footerMsg": true
}
```

**Catatan**: Field boolean di `printSetting` menentukan field mana yang akan ditampilkan:
- `true` = tampilkan field tersebut
- `false` atau `null` = tidak tampilkan

---

## Contoh Lengkap Mode 2

```json
{
  "data": {
    "company": {
      "companyName": "DXN COMPANY",
      "address": "123 Main Street, Jakarta",
      "phone1": "123-456-7890"
    },
    "transHead": {
      "orderNo": "ORD001",
      "customerName": "John Doe",
      "cashier": "Cashier 1",
      "createdDate": "2024-01-01 10:00:00",
      "totalPrice": 80.00,
      "payAmt": 100.00,
      "change": 20.00,
      "paymentMethod": "Cash"
    },
    "transDetail": [
      {
        "seq": 1,
        "productName": "Product 1",
        "qty": 2,
        "price": 25.00,
        "sellingDesc": "Size: Large"
      },
      {
        "seq": 2,
        "productName": "Product 2",
        "qty": 1,
        "price": 30.00,
        "sellingDesc": "Size: Medium"
      }
    ],
    "printSetting": {
      "optType": "ORDER",
      "compName": true,
      "compAddr": true,
      "compPhone1": true,
      "orderNo": true,
      "custName": true,
      "cashier": true,
      "date": true,
      "salesPrice": true,
      "product": true,
      "sellingDesc": true,
      "paymentInfo": true,
      "footerMsg": true
    }
  },
  "cutPaper": true,
  "useExactLayout": true,
  "width": 576,
  "dither": true
}
```

---

## Response

### Success Response
```json
{
  "success": true,
  "message": "Print HTML berhasil"
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error message"
}
```

---

## Tips

1. **Gunakan Mode 1** jika Anda sudah punya HTML yang siap pakai
2. **Gunakan Mode 2** jika Anda hanya punya data dan ingin sistem yang generate HTML
3. **useExactLayout: true** = print dengan layout yang tepat (sebagai gambar bitmap)
4. **useExactLayout: false** = print sebagai plain text (lebih cepat, tapi layout mungkin kurang tepat)
5. **width: 576** = untuk printer 80mm (default)
6. **dither: true** = hasil print lebih halus untuk grayscale

---

## Testing dengan cURL

### Mode 1 (HTML Langsung)
```bash
curl -X POST http://localhost:7080/api/print-html \
  -H "Content-Type: application/json" \
  -d @api-print-html-direct-example.json
```

### Mode 2 (Generate dari Data)
```bash
curl -X POST http://localhost:7080/api/print-html \
  -H "Content-Type: application/json" \
  -d @api-print-html-simple-example.json
```

---

## Testing dengan JavaScript

```javascript
// Mode 1: HTML Langsung
fetch('http://localhost:7080/api/print-html', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    htmlContent: '<html><body><h1>Test</h1></body></html>',
    cutPaper: true,
    useExactLayout: true
  })
})
.then(res => res.json())
.then(data => console.log(data));

// Mode 2: Generate dari Data
fetch('http://localhost:7080/api/print-html', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    data: {
      company: {
        companyName: 'DXN COMPANY',
        address: '123 Main Street'
      },
      transHead: {
        orderNo: 'ORD001',
        totalPrice: 100.00
      },
      transDetail: [{
        productName: 'Product 1',
        qty: 1,
        price: 100.00
      }],
      printSetting: {
        compName: true,
        orderNo: true,
        salesPrice: true,
        product: true
      }
    },
    cutPaper: true,
    useExactLayout: true
  })
})
.then(res => res.json())
.then(data => console.log(data));
```
