<?php
/**
 * Contoh Lengkap Print HTML Content ke POS Printer
 * 
 * File ini menunjukkan berbagai cara untuk mencetak konten HTML ke POS printer
 */

use yii\helpers\Url;
use yii\web\View;
?>

<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Contoh Print HTML Content</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            padding: 20px;
        }
        .btn {
            padding: 10px 20px;
            margin: 5px;
            cursor: pointer;
            border: none;
            border-radius: 5px;
        }
        .btn-primary {
            background-color: #007bff;
            color: white;
        }
        .btn-danger {
            background-color: #dc3545;
            color: white;
        }
        .btn-success {
            background-color: #28a745;
            color: white;
        }
        #PrintContent {
            width: 80mm;
            margin: 0 auto;
            padding: 10px;
            background-color: #fff;
        }
    </style>
</head>
<body>
    <div class="row" style="padding-bottom:10px">
        <div class="col-md-12" style="text-align:center">
            <button class="btn btn-danger" onclick="window.close()">
                <i class="fa fa-times" style="padding-right:10px"></i>Close
            </button>
            
            <!-- Metode 1: Print dari Element HTML -->
            <button class="btn btn-primary" onclick="printFromElement()">
                <i class="fa fa-print" style="padding-right:10px"></i>Print dari Element
            </button>
            
            <!-- Metode 2: Print menggunakan window.print() -->
            <button class="btn btn-success" onclick="printWithWindowPrint()">
                <i class="fa fa-print" style="padding-right:10px"></i>Print (window.print)
            </button>
        </div>
    </div>

    <!-- 
        Elemen HTML yang akan dicetak
        Pastikan memiliki ID "PrintContent" untuk metode window.print() intercept
        Atribut data-auto-cash-drawer untuk auto-open cash drawer setelah print
        Nilai: "1" untuk Pin 1, "2" untuk Pin 2 (default), atau "false" untuk disable
    -->
    <div id="PrintContent" data-auto-cash-drawer="2">
        <div style="text-align: center;">
            <!-- Header dengan Logo/Title -->
            <div style="font-weight: bold; font-size: 16px; margin-bottom: 10px;">
                =================================<br>
                &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;TOKO CONTOH&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<br>
                =================================<br>
            </div>
            
            <!-- Informasi Perusahaan -->
            <div style="font-size: 12px; margin-bottom: 10px;">
                <strong>PT. CONTOH PERUSAHAAN</strong><br>
                Jl. Contoh No. 123<br>
                Jakarta 12345<br>
                Telp: 081234567890<br>
                Email: info@contoh.com<br>
            </div>
            
            <div style="border-top: 1px solid #000; margin: 10px 0;"></div>
            
            <!-- Informasi Transaksi -->
            <div style="text-align: left; font-size: 12px; margin-bottom: 10px;">
                <p><strong>Tanggal:</strong> <?= date('d/m/Y H:i:s') ?></p>
                <p><strong>Invoice No:</strong> INV-<?= date('YmdHis') ?></p>
                <p><strong>Kasir:</strong> Admin</p>
            </div>
            
            <div style="border-top: 1px solid #000; margin: 10px 0;"></div>
            
            <!-- Daftar Item dalam Tabel -->
            <table style="width: 100%; font-size: 12px; border-collapse: collapse;">
                <thead>
                    <tr style="border-bottom: 1px solid #000;">
                        <th style="text-align: left; padding: 5px 0;">Item</th>
                        <th style="text-align: center; padding: 5px 0;">Qty</th>
                        <th style="text-align: right; padding: 5px 0;">Harga</th>
                        <th style="text-align: right; padding: 5px 0;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td style="padding: 5px 0;">Produk A</td>
                        <td style="text-align: center; padding: 5px 0;">2</td>
                        <td style="text-align: right; padding: 5px 0;">Rp 10.000</td>
                        <td style="text-align: right; padding: 5px 0;">Rp 20.000</td>
                    </tr>
                    <tr>
                        <td style="padding: 5px 0;">Produk B</td>
                        <td style="text-align: center; padding: 5px 0;">1</td>
                        <td style="text-align: right; padding: 5px 0;">Rp 15.000</td>
                        <td style="text-align: right; padding: 5px 0;">Rp 15.000</td>
                    </tr>
                    <tr>
                        <td style="padding: 5px 0;">Produk C</td>
                        <td style="text-align: center; padding: 5px 0;">3</td>
                        <td style="text-align: right; padding: 5px 0;">Rp 5.000</td>
                        <td style="text-align: right; padding: 5px 0;">Rp 15.000</td>
                    </tr>
                </tbody>
            </table>
            
            <div style="border-top: 1px solid #000; margin: 10px 0;"></div>
            
            <!-- Total -->
            <div style="text-align: right; font-size: 12px; margin-bottom: 10px;">
                <p><strong>Subtotal: Rp 50.000</strong></p>
                <p>Diskon: Rp 5.000</p>
                <p style="font-size: 14px; font-weight: bold; border-top: 1px solid #000; padding-top: 5px;">
                    TOTAL: Rp 45.000
                </p>
            </div>
            
            <div style="border-top: 1px solid #000; margin: 10px 0;"></div>
            
            <!-- Footer -->
            <div style="text-align: center; font-size: 12px; margin-top: 10px;">
                <p><strong>Terima Kasih</strong></p>
                <p style="font-size: 10px;">Barang yang sudah dibeli tidak dapat ditukar/dikembalikan</p>
                <p style="font-size: 10px;">www.contoh.com</p>
            </div>
            
            <div style="border-top: 1px solid #000; margin: 10px 0;"></div>
        </div>
    </div>

    <script>
        /**
         * METODE 1: Print dari Element HTML menggunakan printFromElement()
         * 
         * Metode ini lebih eksplisit dan memberikan kontrol lebih baik
         */
        function printFromElement() {
            // Cek apakah POS Printer bridge tersedia
            if (window.posPrinter) {
                // Print konten dari elemen #PrintContent
                // Parameter 1: selector atau element DOM
                // Parameter 2: cutPaper (true = potong kertas setelah print, false = tidak potong)
                window.posPrinter.printFromElement('#PrintContent', true);
                
                // Buka cash drawer setelah print (delay 500ms untuk memastikan print selesai)
                setTimeout(function() {
                    // Pin 2 (default) - sesuaikan dengan konfigurasi printer Anda
                    window.posPrinter.openCashDrawer(2);
                }, 500);
                
                console.log('Print berhasil dikirim ke POS printer');
            } else {
                // Fallback jika POS Printer bridge tidak tersedia
                alert('POS Printer bridge tidak tersedia. Pastikan aplikasi POS Printer sedang berjalan.');
                console.log('POS Printer bridge tidak tersedia');
            }
            
            // Opsional: Close window setelah print
            // setTimeout(function() {
            //     window.close();
            // }, 2000);
        }

        /**
         * METODE 2: Print menggunakan window.print() yang di-intercept otomatis
         * 
         * Metode ini lebih sederhana - cukup panggil window.print()
         * Aplikasi POS Printer akan otomatis mengintercept dan print ke POS printer
         * jika elemen dengan ID "PrintContent" ditemukan
         */
        function printWithWindowPrint() {
            // Cukup panggil window.print()
            // Aplikasi akan otomatis:
            // 1. Mencari elemen dengan ID "PrintContent"
            // 2. Extract text dari elemen tersebut
            // 3. Print ke POS printer
            // 4. Auto-open cash drawer jika ada atribut data-auto-cash-drawer
            window.print();
            
            // Opsional: Close window setelah print
            // setTimeout(function() {
            //     window.close();
            // }, 2000);
        }

        /**
         * METODE 3: Print teks langsung (bukan dari HTML)
         * 
         * Jika Anda ingin format teks secara manual tanpa HTML
         */
        function printDirectText() {
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
                
                // Print teks langsung
                // Parameter 1: teks yang ingin dicetak
                // Parameter 2: cutPaper (true = potong kertas setelah print)
                window.posPrinter.print(receipt, true);
                
                // Buka cash drawer
                setTimeout(function() {
                    window.posPrinter.openCashDrawer(2);
                }, 500);
            } else {
                alert('POS Printer bridge tidak tersedia');
            }
        }

        /**
         * METODE 4: Print dengan callback untuk mengetahui hasil print
         */
        function printWithCallback() {
            if (window.posPrinter) {
                // Set callback untuk mendengarkan response
                window.onPosPrinterResponse = function(response) {
                    console.log('Type:', response.type);
                    console.log('Success:', response.success);
                    console.log('Message:', response.message);
                    
                    if (response.type === 'print' && response.success) {
                        alert('Print berhasil!');
                    } else if (response.type === 'print' && !response.success) {
                        alert('Print gagal: ' + response.message);
                    }
                };
                
                // Print
                window.posPrinter.printFromElement('#PrintContent', true);
                
                // Buka cash drawer
                setTimeout(function() {
                    window.posPrinter.openCashDrawer(2);
                }, 500);
            } else {
                alert('POS Printer bridge tidak tersedia');
            }
        }

        // Auto print saat halaman dimuat (opsional)
        // Uncomment baris di bawah jika ingin auto print
        // $(document).ready(function() {
        //     setTimeout(function() {
        //         printFromElement();
        //     }, 500);
        // });
    </script>
</body>
</html>

