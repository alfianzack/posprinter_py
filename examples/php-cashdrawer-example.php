<?php
/**
 * Contoh Penggunaan Cash Drawer dari Website PHP
 * 
 * File ini menunjukkan berbagai cara menggunakan cash drawer dari website PHP
 */

use yii\helpers\Url;
use yii\web\View;
?>

<div class="row" style="padding-bottom:10px">
    <div class="col-md-12" style="text-align:center ">
        <a href="javascript:window.close()" class="btn btn-danger" >
            <i class="fa fa-times" style="padding-right:10px"></i>Close
        </a>    
        <a href="javascript:void(0)" class="btn btn-primary btnprint" >
            <i class="fa fa-print" style="padding-right:10px"></i>Print
        </a>
        <a href="javascript:void(0)" class="btn btn-success btncashdrawer" >
            <i class="fa fa-money" style="padding-right:10px"></i>Buka Cash Drawer
        </a>
    </div>
</div>

<div id="PrintContent">
    <!-- Konten receipt Anda di sini -->
    <div style="text-align: center;">
        <h3>TOKO CONTOH</h3>
        <p>Jl. Contoh No. 123</p>
        <p>Telp: 081234567890</p>
        <hr>
        <p>Terima Kasih</p>
    </div>
</div>

<?php 
$js = <<<JS
    // Contoh 1: Print dan buka cash drawer secara terpisah
    $(".btnprint").click(function() {
        if (window.posPrinter) {
            // Print receipt
            window.posPrinter.printFromElement('#PrintContent', true);
            
            // Buka cash drawer setelah print (delay untuk memastikan print selesai)
            setTimeout(function() {
                window.posPrinter.openCashDrawer(2); // Pin 2 (default)
            }, 500);
        } else {
            alert('POS Printer bridge tidak tersedia');
        }
    });
    
    // Contoh 2: Buka cash drawer secara manual (tombol terpisah)
    $(".btncashdrawer").click(function() {
        if (window.posPrinter) {
            // Buka cash drawer dengan Pin 2 (default)
            window.posPrinter.openCashDrawer(2);
            
            // Atau gunakan Pin 1 jika printer Anda menggunakan Pin 1
            // window.posPrinter.openCashDrawer(1);
        } else {
            alert('POS Printer bridge tidak tersedia');
        }
    });
    
    // Contoh 3: Auto print dan buka cash drawer saat halaman dimuat
    // Uncomment kode di bawah jika ingin auto print saat halaman dimuat
    /*
    $(document).ready(function() {
        setTimeout(function() {
            if (window.posPrinter) {
                window.posPrinter.printFromElement('#PrintContent', true);
                setTimeout(function() {
                    window.posPrinter.openCashDrawer(2);
                }, 500);
            }
        }, 1000);
    });
    */
    
    // Contoh 4: Mendengarkan response dari cash drawer
    window.onPosPrinterResponse = function(response) {
        if (response.type === 'cashDrawer') {
            if (response.success) {
                console.log('Cash drawer berhasil dibuka');
                // Optional: Tampilkan notifikasi
                // alert('Cash drawer berhasil dibuka');
            } else {
                console.error('Cash drawer gagal dibuka:', response.message);
                alert('Cash drawer gagal dibuka: ' + response.message);
            }
        } else if (response.type === 'print') {
            if (response.success) {
                console.log('Print berhasil');
            } else {
                console.error('Print gagal:', response.message);
            }
        }
    };
JS;

$this->registerJs($js);
?>

