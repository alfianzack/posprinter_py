<?php
/**
 * Contoh Penggunaan Logo DXN di Receipt Print
 * 
 * File ini menunjukkan cara menambahkan logo DXN ke receipt print
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
    </div>
</div>

<div id="PrintContent" data-auto-cash-drawer="2">
    <div style="text-align: center;">
        <!-- Logo DXN untuk print -->
        <div style="font-weight: bold; font-size: 14px; margin-bottom: 10px;">
            =================================<br>
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;DXN POS SYSTEM&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<br>
            =================================<br>
        </div>
        
        <!-- Atau gunakan ASCII art logo -->
        <!--
        <div style="font-family: monospace; font-size: 10px; margin-bottom: 10px;">
            &nbsp;&nbsp;_____&nbsp;&nbsp;__&nbsp;&nbsp;__&nbsp;&nbsp;_&nbsp;&nbsp;&nbsp;_&nbsp;<br>
            &nbsp;|&nbsp;&nbsp;__&nbsp;\\|&nbsp;&nbsp;\\/&nbsp;&nbsp;||&nbsp;\\&nbsp;|&nbsp;<br>
            &nbsp;|&nbsp;|&nbsp;&nbsp;|&nbsp;|&nbsp;\\&nbsp;&nbsp;/&nbsp;||&nbsp;&nbsp;\\|&nbsp;|<br>
            &nbsp;|&nbsp;|&nbsp;&nbsp;|&nbsp;|&nbsp;\\/|&nbsp;||&nbsp;.&nbsp;`&nbsp;|<br>
            &nbsp;|&nbsp;|__|&nbsp;|&nbsp;|&nbsp;&nbsp;|&nbsp;||&nbsp;|\\&nbsp;&nbsp;|<br>
            &nbsp;|_____/|_|&nbsp;&nbsp;|_||_|&nbsp;\\_|<br>
            <br>
            &nbsp;&nbsp;&nbsp;&nbsp;POS&nbsp;SYSTEM<br>
        </div>
        -->
        
        <!-- Informasi perusahaan -->
        <div style="font-size: 12px; margin-bottom: 10px;">
            <?php if ($company["company_name"]) { ?>
                <?= $company["company_name"] ?><br>
            <?php } ?>
            <?php if ($company["address"]) { ?>
                <?= $company["address"] ?><br>
            <?php } ?>
            <?php if ($company["phone1"]) { ?>
                Telp: <?= $company["phone1"] ?><br>
            <?php } ?>
        </div>
        
        <div style="border-top: 1px solid; margin: 10px 0;"></div>
        
        <!-- Konten receipt -->
        <div style="text-align: left; font-size: 12px;">
            <p><strong>Tanggal:</strong> <?= date('d/m/Y H:i:s') ?></p>
            <p><strong>Order No:</strong> <?= $transHead->order_no ?? 'N/A' ?></p>
            
            <div style="border-top: 1px solid; margin: 10px 0;"></div>
            
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
            
            <div style="border-top: 1px solid; margin: 10px 0;"></div>
            
            <p style="text-align: right;"><strong>Total: Rp 25.000</strong></p>
        </div>
        
        <div style="border-top: 1px solid; margin: 10px 0;"></div>
        
        <div style="text-align: center; font-size: 12px; margin-top: 10px;">
            <p><strong>Terima Kasih</strong></p>
            <p style="font-size: 10px;">DXN POS SYSTEM</p>
        </div>
        
        <div style="border-top: 1px solid; margin: 10px 0;"></div>
    </div>
</div>

<?php 
$js = <<<JS
    $(".btnprint").click(function() {
        if (window.posPrinter) {
            window.posPrinter.printFromElement('#PrintContent', true);
            setTimeout(function() {
                window.posPrinter.openCashDrawer(2);
            }, 500);
        } else {
            window.print();
        }
        
        setTimeout(function() {
            window.close();
        }, 2000);
    });
JS;

$this->registerJs($js);
?>

