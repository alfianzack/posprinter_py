<?php
/**
 * Contoh Print HTML Content menggunakan API /api/print
 * 
 * File ini menunjukkan cara mengirim HTML content ke endpoint /api/print
 * untuk dicetak ke POS printer
 */

// Konfigurasi
$apiBaseUrl = 'http://localhost:8080'; // Sesuaikan dengan port aplikasi
$apiUrl = $apiBaseUrl . '/api/print';

// HTML content yang ingin dicetak
$htmlContent = '
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
';

// Fungsi untuk buka cash drawer via API
function openCashDrawerViaApi($apiBaseUrl, $pin = null) {
    $apiUrl = $apiBaseUrl . '/api/cashdrawer';
    
    // Prepare data
    $data = [];
    if ($pin !== null) {
        $data['pin'] = $pin;
    }
    
    // Convert to JSON
    $jsonData = json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    
    // Initialize cURL
    $ch = curl_init($apiUrl);
    
    // Set cURL options
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $jsonData);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        'Content-Type: application/json',
        'Content-Length: ' . strlen($jsonData)
    ]);
    
    // Execute request
    $response = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    $error = curl_error($ch);
    
    curl_close($ch);
    
    // Handle response
    if ($error) {
        return [
            'success' => false,
            'message' => 'cURL Error: ' . $error
        ];
    }
    
    if ($httpCode !== 200) {
        return [
            'success' => false,
            'message' => 'HTTP Error: ' . $httpCode
        ];
    }
    
    $result = json_decode($response, true);
    return $result;
}

// Fungsi untuk print dan buka cash drawer sekaligus
function printAndOpenDrawerViaApi($apiBaseUrl, $htmlContent, $cutPaper = true, $drawerPin = null, $drawerDelay = 500) {
    $apiUrl = $apiBaseUrl . '/api/print-and-drawer';
    
    // Prepare data
    $data = [
        'content' => $htmlContent,
        'cutPaper' => $cutPaper,
        'drawerDelay' => $drawerDelay
    ];
    
    if ($drawerPin !== null) {
        $data['drawerPin'] = $drawerPin;
    }
    
    // Convert to JSON
    $jsonData = json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    
    // Initialize cURL
    $ch = curl_init($apiUrl);
    
    // Set cURL options
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $jsonData);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        'Content-Type: application/json',
        'Content-Length: ' . strlen($jsonData)
    ]);
    
    // Execute request
    $response = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    $error = curl_error($ch);
    
    curl_close($ch);
    
    // Handle response
    if ($error) {
        return [
            'success' => false,
            'message' => 'cURL Error: ' . $error,
            'printSuccess' => false,
            'drawerSuccess' => false
        ];
    }
    
    if ($httpCode !== 200) {
        return [
            'success' => false,
            'message' => 'HTTP Error: ' . $httpCode,
            'printSuccess' => false,
            'drawerSuccess' => false
        ];
    }
    
    $result = json_decode($response, true);
    return $result;
}

// Fungsi untuk print via API
function printViaApi($apiUrl, $htmlContent, $cutPaper = true) {
    // Prepare data
    $data = [
        'content' => $htmlContent,
        'cutPaper' => $cutPaper
    ];
    
    // Convert to JSON
    $jsonData = json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    
    // Initialize cURL
    $ch = curl_init($apiUrl);
    
    // Set cURL options
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $jsonData);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        'Content-Type: application/json',
        'Content-Length: ' . strlen($jsonData)
    ]);
    
    // Execute request
    $response = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    $error = curl_error($ch);
    
    curl_close($ch);
    
    // Handle response
    if ($error) {
        return [
            'success' => false,
            'message' => 'cURL Error: ' . $error
        ];
    }
    
    if ($httpCode !== 200) {
        return [
            'success' => false,
            'message' => 'HTTP Error: ' . $httpCode
        ];
    }
    
    $result = json_decode($response, true);
    return $result;
}

// Contoh penggunaan
echo "<h1>Contoh Print HTML via API</h1>";

// Test 1: Print dengan cut paper
echo "<h2>Test 1: Print dengan Cut Paper</h2>";
$result = printViaApi($apiUrl, $htmlContent, true);
if ($result['success']) {
    echo "<p style='color: green;'>✓ " . htmlspecialchars($result['message']) . "</p>";
} else {
    echo "<p style='color: red;'>✗ " . htmlspecialchars($result['message']) . "</p>";
}

// Test 2: Print tanpa cut paper
echo "<h2>Test 2: Print tanpa Cut Paper</h2>";
$result = printViaApi($apiUrl, $htmlContent, false);
if ($result['success']) {
    echo "<p style='color: green;'>✓ " . htmlspecialchars($result['message']) . "</p>";
} else {
    echo "<p style='color: red;'>✗ " . htmlspecialchars($result['message']) . "</p>";
}

// Test 3: Print dan buka cash drawer sekaligus
echo "<h2>Test 3: Print dan Buka Cash Drawer Sekaligus</h2>";
$result = printAndOpenDrawerViaApi($apiBaseUrl, $htmlContent, true, 2, 500);
if ($result['success']) {
    echo "<p style='color: green;'>✓ " . htmlspecialchars($result['message']) . "</p>";
    echo "<p style='font-size: 12px;'>Print: " . ($result['printSuccess'] ? 'Berhasil' : 'Gagal') . " | Drawer: " . ($result['drawerSuccess'] ? 'Berhasil' : 'Gagal') . "</p>";
} else {
    echo "<p style='color: red;'>✗ " . htmlspecialchars($result['message']) . "</p>";
}

// Test 4: Print dengan data dinamis
echo "<h2>Test 4: Print dengan Data Dinamis</h2>";
$orderData = [
    'order_no' => 'ORD-' . date('YmdHis'),
    'date' => date('d/m/Y H:i:s'),
    'items' => [
        ['name' => 'Produk A', 'qty' => 2, 'price' => 10000],
        ['name' => 'Produk B', 'qty' => 1, 'price' => 15000],
    ],
    'total' => 35000
];

$dynamicHtml = '
<div style="text-align: center;">
    <h2>TOKO CONTOH</h2>
    <p>Order No: ' . htmlspecialchars($orderData['order_no']) . '</p>
    <p>Tanggal: ' . htmlspecialchars($orderData['date']) . '</p>
    <hr>
    <table style="width: 100%;">
';

foreach ($orderData['items'] as $item) {
    $subtotal = $item['qty'] * $item['price'];
    $dynamicHtml .= '
        <tr>
            <td>' . htmlspecialchars($item['name']) . ' x' . $item['qty'] . '</td>
            <td style="text-align: right;">Rp ' . number_format($subtotal, 0, ',', '.') . '</td>
        </tr>
    ';
}

$dynamicHtml .= '
    </table>
    <hr>
    <p style="text-align: right; font-weight: bold;">Total: Rp ' . number_format($orderData['total'], 0, ',', '.') . '</p>
    <p style="text-align: center; font-weight: bold;">Terima Kasih</p>
</div>
';

$result = printViaApi($apiUrl, $dynamicHtml, true);
if ($result['success']) {
    echo "<p style='color: green;'>✓ " . htmlspecialchars($result['message']) . "</p>";
} else {
    echo "<p style='color: red;'>✗ " . htmlspecialchars($result['message']) . "</p>";
}

?>

<!DOCTYPE html>
<html>
<head>
    <title>Test Print HTML via API</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
        }
        .test-section {
            margin: 20px 0;
            padding: 15px;
            border: 1px solid #ddd;
            border-radius: 5px;
        }
        button {
            background-color: #007bff;
            color: white;
            padding: 10px 20px;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            margin: 5px;
        }
        button:hover {
            background-color: #0056b3;
        }
        .result {
            margin-top: 10px;
            padding: 10px;
            background-color: #f5f5f5;
            border-radius: 5px;
        }
    </style>
</head>
<body>
    <div class="test-section">
        <h2>Test Print dari Browser</h2>
        <button onclick="testPrint()">Print HTML Content</button>
        <button onclick="testPrintAndDrawer()">Print & Buka Cash Drawer</button>
        <div id="result" class="result"></div>
    </div>

    <script>
        async function testPrint() {
            const apiUrl = '<?php echo $apiUrl; ?>';
            const htmlContent = `<?php echo addslashes($htmlContent); ?>`;
            
            const resultDiv = document.getElementById('result');
            resultDiv.textContent = 'Mengirim request...';
            
            try {
                const response = await fetch(apiUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        content: htmlContent,
                        cutPaper: true
                    })
                });
                
                const data = await response.json();
                
                if (data.success) {
                    resultDiv.innerHTML = '<span style="color: green;">✓ ' + data.message + '</span>';
                } else {
                    resultDiv.innerHTML = '<span style="color: red;">✗ ' + data.message + '</span>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<span style="color: red;">✗ Error: ' + error.message + '</span>';
            }
        }

        async function testPrintAndDrawer() {
            const apiBaseUrl = '<?php echo $apiBaseUrl; ?>';
            const htmlContent = `<?php echo addslashes($htmlContent); ?>`;
            
            const resultDiv = document.getElementById('result');
            resultDiv.textContent = 'Mengirim request...';
            
            try {
                const response = await fetch(apiBaseUrl + '/api/print-and-drawer', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        content: htmlContent,
                        cutPaper: true,
                        drawerPin: 2,
                        drawerDelay: 500
                    })
                });
                
                const data = await response.json();
                
                if (data.success) {
                    resultDiv.innerHTML = '<span style="color: green;">✓ ' + data.message + '</span><br>' +
                        '<span style="font-size: 12px;">Print: ' + (data.printSuccess ? 'Berhasil' : 'Gagal') + 
                        ' | Drawer: ' + (data.drawerSuccess ? 'Berhasil' : 'Gagal') + '</span>';
                } else {
                    resultDiv.innerHTML = '<span style="color: red;">✗ ' + data.message + '</span>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<span style="color: red;">✗ Error: ' + error.message + '</span>';
            }
        }
    </script>
</body>
</html>

