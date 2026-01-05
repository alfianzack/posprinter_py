/**
 * Contoh Print HTML Content menggunakan API /api/print
 * Pastikan aplikasi DXN POS Printer sedang berjalan
 * 
 * Install dependencies:
 * npm install axios
 * atau
 * npm install node-fetch
 */

// Menggunakan axios (install: npm install axios)
const axios = require('axios');

// Atau menggunakan fetch (Node.js 18+ atau install: npm install node-fetch)
// const fetch = require('node-fetch');

// Konfigurasi
const API_BASE_URL = 'http://localhost:8080';
const API_URL = `${API_BASE_URL}/api/print`;

/**
 * Fungsi untuk buka cash drawer via API
 * @param {number|null} pin - Pin yang digunakan (1, 2, atau null untuk default)
 * @returns {Promise<Object>} Response dari API
 */
async function openCashDrawerViaApi(pin = null) {
    try {
        const data = {};
        if (pin !== null) {
            data.pin = pin;
        }
        
        const response = await axios.post(`${API_BASE_URL}/api/cashdrawer`, data, {
            headers: {
                'Content-Type': 'application/json'
            },
            timeout: 10000
        });
        
        return response.data;
    } catch (error) {
        return {
            success: false,
            message: error.response?.data?.message || error.message || 'Unknown error'
        };
    }
}

/**
 * Fungsi untuk print HTML content dan buka cash drawer sekaligus via API
 * @param {string} htmlContent - HTML content yang ingin dicetak
 * @param {boolean} cutPaper - Apakah kertas harus dipotong setelah print
 * @param {number|null} drawerPin - Pin yang digunakan untuk cash drawer (1, 2, atau null untuk default)
 * @param {number} drawerDelay - Delay dalam milliseconds sebelum buka drawer (default: 500)
 * @returns {Promise<Object>} Response dari API dengan detail print dan drawer success
 */
async function printAndOpenDrawerViaApi(htmlContent, cutPaper = true, drawerPin = null, drawerDelay = 500) {
    try {
        const data = {
            content: htmlContent,
            cutPaper: cutPaper,
            drawerDelay: drawerDelay
        };
        
        if (drawerPin !== null) {
            data.drawerPin = drawerPin;
        }
        
        const response = await axios.post(`${API_BASE_URL}/api/print-and-drawer`, data, {
            headers: {
                'Content-Type': 'application/json'
            },
            timeout: 15000  // Timeout lebih lama karena ada delay
        });
        
        return response.data;
    } catch (error) {
        return {
            success: false,
            message: error.response?.data?.message || error.message || 'Unknown error',
            printSuccess: false,
            drawerSuccess: false
        };
    }
}

/**
 * Fungsi untuk print HTML content via API
 * @param {string} htmlContent - HTML content yang ingin dicetak
 * @param {boolean} cutPaper - Apakah kertas harus dipotong setelah print
 * @returns {Promise<Object>} Response dari API
 */
async function printViaApi(htmlContent, cutPaper = true) {
    try {
        const response = await axios.post(API_URL, {
            content: htmlContent,
            cutPaper: cutPaper
        }, {
            headers: {
                'Content-Type': 'application/json'
            },
            timeout: 10000
        });
        
        return response.data;
    } catch (error) {
        return {
            success: false,
            message: error.response?.data?.message || error.message || 'Unknown error'
        };
    }
}

// Contoh 1: Print HTML sederhana
async function testPrintSimple() {
    console.log('Test 1: Print HTML sederhana...');
    
    const htmlContent = `
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
    `;
    
    const result = await printViaApi(htmlContent, true);
    if (result.success) {
        console.log(`✓ ${result.message}`);
    } else {
        console.log(`✗ ${result.message}`);
    }
    console.log();
}

// Contoh 2: Print dengan data dinamis
async function testPrintDynamic() {
    console.log('Test 2: Print dengan data dinamis...');
    
    const orderData = {
        order_no: `ORD-${Date.now()}`,
        date: new Date().toLocaleString('id-ID'),
        items: [
            { name: 'Produk A', qty: 2, price: 10000 },
            { name: 'Produk B', qty: 1, price: 15000 },
            { name: 'Produk C', qty: 3, price: 5000 }
        ]
    };
    
    // Calculate total
    const total = orderData.items.reduce((sum, item) => sum + (item.qty * item.price), 0);
    
    // Build items HTML
    const itemsHtml = orderData.items.map(item => {
        const subtotal = item.qty * item.price;
        return `
        <tr>
            <td>${item.name} x${item.qty}</td>
            <td style="text-align: right;">Rp ${subtotal.toLocaleString('id-ID')}</td>
        </tr>
        `;
    }).join('');
    
    const htmlContent = `
<div style="text-align: center;">
    <h2 style="font-size: 18px; font-weight: bold;">TOKO CONTOH</h2>
    <p style="font-size: 12px;">Jl. Contoh No. 123</p>
    <p style="font-size: 12px;">Telp: 081234567890</p>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <p style="font-size: 12px;"><strong>Order No:</strong> ${orderData.order_no}</p>
    <p style="font-size: 12px;"><strong>Tanggal:</strong> ${orderData.date}</p>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <table style="width: 100%; font-size: 12px;">
        ${itemsHtml}
    </table>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <p style="text-align: right; font-weight: bold; font-size: 14px;">Total: Rp ${total.toLocaleString('id-ID')}</p>
    <p style="text-align: center; font-weight: bold;">Terima Kasih</p>
    <p style="text-align: center; font-size: 10px;">Barang yang sudah dibeli tidak dapat ditukar/dikembalikan</p>
</div>
    `;
    
    const result = await printViaApi(htmlContent, true);
    if (result.success) {
        console.log(`✓ ${result.message}`);
    } else {
        console.log(`✗ ${result.message}`);
    }
    console.log();
}

// Contoh 3: Print dan buka cash drawer sekaligus
async function testPrintAndDrawer() {
    console.log('Test 3: Print dan buka cash drawer sekaligus...');
    
    const htmlContent = `
<div style="text-align: center;">
    <h2 style="font-size: 18px; font-weight: bold;">TOKO CONTOH</h2>
    <p style="font-size: 12px;">Jl. Contoh No. 123</p>
    <p style="font-size: 12px;">Telp: 081234567890</p>
    <hr style="border-top: 1px solid #000; margin: 10px 0;">
    <p style="font-size: 12px;"><strong>Tanggal:</strong> ${new Date().toLocaleString('id-ID')}</p>
    <p style="font-size: 12px;"><strong>Order No:</strong> ORD-${Date.now()}</p>
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
    `;
    
    const result = await printAndOpenDrawerViaApi(htmlContent, true, 2, 500);
    if (result.success) {
        console.log(`✓ ${result.message}`);
        console.log(`  Print: ${result.printSuccess ? '✓ Berhasil' : '✗ Gagal'}`);
        console.log(`  Drawer: ${result.drawerSuccess ? '✓ Berhasil' : '✗ Gagal'}`);
    } else {
        console.log(`✗ ${result.message}`);
        console.log(`  Print: ${result.printSuccess ? '✓ Berhasil' : '✗ Gagal'}`);
        console.log(`  Drawer: ${result.drawerSuccess ? '✓ Berhasil' : '✗ Gagal'}`);
    }
    console.log();
}

// Contoh 4: Print dari Express.js route
function createPrintRoute(app) {
    // Contoh untuk Express.js
    app.post('/api/print-receipt', async (req, res) => {
        try {
            const { orderData } = req.body;
            
            // Build HTML dari order data
            const itemsHtml = orderData.items.map(item => {
                const subtotal = item.qty * item.price;
                return `
                <tr>
                    <td>${item.name} x${item.qty}</td>
                    <td style="text-align: right;">Rp ${subtotal.toLocaleString('id-ID')}</td>
                </tr>
                `;
            }).join('');
            
            const total = orderData.items.reduce((sum, item) => sum + (item.qty * item.price), 0);
            
            const htmlContent = `
<div style="text-align: center;">
    <h2>${orderData.company_name || 'TOKO CONTOH'}</h2>
    <p>${orderData.address || ''}</p>
    <p>Telp: ${orderData.phone || ''}</p>
    <hr>
    <p><strong>Order No:</strong> ${orderData.order_no}</p>
    <p><strong>Tanggal:</strong> ${new Date().toLocaleString('id-ID')}</p>
    <hr>
    <table style="width: 100%;">
        ${itemsHtml}
    </table>
    <hr>
    <p style="text-align: right; font-weight: bold;">Total: Rp ${total.toLocaleString('id-ID')}</p>
    <p style="text-align: center; font-weight: bold;">Terima Kasih</p>
</div>
            `;
            
            // Print via API
            const printResult = await printViaApi(htmlContent, true);
            
            res.json({
                success: printResult.success,
                message: printResult.message
            });
        } catch (error) {
            res.status(500).json({
                success: false,
                message: error.message
            });
        }
    });
}

// Main execution
async function main() {
    console.log('='.repeat(60));
    console.log('Contoh Print HTML Content via API');
    console.log('='.repeat(60));
    console.log();
    
    try {
        await testPrintSimple();
        await testPrintDynamic();
        await testPrintAndDrawer();
        
        console.log('='.repeat(60));
        console.log('Semua test selesai!');
        console.log('='.repeat(60));
    } catch (error) {
        console.error('Error:', error.message);
    }
}

// Run jika dijalankan langsung
if (require.main === module) {
    main();
}

// Export untuk digunakan di file lain
module.exports = {
    printViaApi,
    openCashDrawerViaApi,
    printAndOpenDrawerViaApi,
    createPrintRoute
};

