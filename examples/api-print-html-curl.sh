#!/bin/bash
# Contoh Print HTML Content menggunakan cURL
# Pastikan aplikasi DXN POS Printer sedang berjalan

API_URL="http://localhost:8080/api/print"

# Contoh 1: Print HTML sederhana
echo "Test 1: Print HTML sederhana..."
curl -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "<div style=\"text-align: center;\"><h2>TOKO CONTOH</h2><p>Jl. Contoh No. 123</p><p>Telp: 081234567890</p><hr><table style=\"width: 100%;\"><tr><td>Item 1</td><td style=\"text-align: right;\">Rp 10.000</td></tr><tr><td>Item 2</td><td style=\"text-align: right;\">Rp 15.000</td></tr></table><hr><p style=\"text-align: right; font-weight: bold;\">Total: Rp 25.000</p><p style=\"text-align: center; font-weight: bold;\">Terima Kasih</p></div>",
    "cutPaper": true
  }'

echo -e "\n\n"

# Contoh 2: Print tanpa cut paper
echo "Test 2: Print tanpa cut paper..."
curl -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "<div style=\"text-align: center;\"><h2>TOKO CONTOH</h2><p>Test Print</p></div>",
    "cutPaper": false
  }'

echo -e "\n\n"

# Contoh 3: Print dengan HTML multiline (menggunakan file)
echo "Test 3: Print dari file..."
cat > /tmp/receipt.html << 'EOF'
<div style="text-align: center;">
    <h2 style="font-size: 18px; font-weight: bold;">TOKO CONTOH</h2>
    <p style="font-size: 12px;">Jl. Contoh No. 123</p>
    <p style="font-size: 12px;">Telp: 081234567890</p>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <p style="font-size: 12px;"><strong>Tanggal:</strong> 2024-01-01 12:00:00</p>
    <p style="font-size: 12px;"><strong>Order No:</strong> ORD-20240101120000</p>
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
EOF

HTML_CONTENT=$(cat /tmp/receipt.html | jq -Rs .)
curl -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d "{\"content\": $HTML_CONTENT, \"cutPaper\": true}"

echo -e "\n\n"

