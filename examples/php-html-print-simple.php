<?php
/**
 * Contoh Sederhana Print HTML Content
 * 
 * Contoh minimal untuk print HTML content dengan perubahan kode yang sangat sedikit
 */

use yii\helpers\Url;
use yii\web\View;
?>

<div class="row" style="padding-bottom:10px">
    <div class="col-md-12" style="text-align:center">
        <a href="javascript:window.close()" class="btn btn-danger">
            <i class="fa fa-times" style="padding-right:10px"></i>Close
        </a>
        <a href="javascript:void(0)" class="btn btn-primary btnprint">
            <i class="fa fa-print" style="padding-right:10px"></i>Print
        </a>
    </div>
</div>

<!-- 
    Elemen HTML yang akan dicetak
    - ID harus "PrintContent" untuk metode window.print() intercept
    - data-auto-cash-drawer="2" untuk auto-open cash drawer setelah print
-->
<div id="PrintContent" data-auto-cash-drawer="2">
    <div style="text-align: center; width: 80mm; margin: 0 auto;">
        <!-- Header -->
        <h2 style="margin: 10px 0;">TOKO CONTOH</h2>
        
        <!-- Informasi -->
        <div style="font-size: 12px; margin-bottom: 10px;">
            <p>Jl. Contoh No. 123</p>
            <p>Telp: 081234567890</p>
        </div>
        
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        
        <!-- Konten Receipt -->
        <div style="text-align: left; font-size: 12px;">
            <p><strong>Tanggal:</strong> <?= date('d/m/Y H:i:s') ?></p>
            <p><strong>Invoice:</strong> INV-<?= date('YmdHis') ?></p>
            
            <hr style="border-top: 1px solid #000; margin: 10px 0;">
            
            <!-- Items -->
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
            
            <p style="text-align: right;"><strong>Total: Rp 25.000</strong></p>
        </div>
        
        <hr style="border-top: 1px solid #000; margin: 10px 0;">
        
        <!-- Footer -->
        <div style="text-align: center; font-size: 12px; margin-top: 10px;">
            <p><strong>Terima Kasih</strong></p>
        </div>
    </div>
</div>

<?php 
$js = <<<JS
    $(".btnprint").click(function() {
        // Metode paling sederhana: cukup panggil window.print()
        // Aplikasi POS Printer akan otomatis mengintercept dan print ke POS printer
        // jika elemen dengan ID "PrintContent" ditemukan
        window.print();
        
        // Close window setelah print (opsional)
        setTimeout(function() {
            window.close();
        }, 2000);
    });
JS;

$this->registerJs($js);
?>

