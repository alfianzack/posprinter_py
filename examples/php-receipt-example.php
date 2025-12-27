<?php
/**
 * Contoh Modifikasi Kode PHP untuk Print ke POS Printer
 * 
 * File ini menunjukkan cara memodifikasi kode PHP yang menggunakan window.print()
 * untuk menggunakan JavaScript bridge POS Printer
 */

use yii\helpers\Url;
use yii\web\View;
use app\models\company\transaction\MultiPayment;

$multi_pay = MultiPayment::find()->where(['order_no' => $transHead->order_no])->one();
$row_id = $transHead->order_no;
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

<iframe id="iframePrint" frameborder="0" style="display:none"></iframe>

<div style="width:80mm; padding: 5px; margin: 0 auto; line-height: 22px; overflow: auto;background-color: #FFFFFF;">
    <!-- 
        Tambahkan data-auto-cash-drawer untuk auto-open cash drawer setelah print
        Nilai: "1" untuk Pin 1, "2" untuk Pin 2 (default), atau "false" untuk disable
    -->
    <div id="PrintContent" data-auto-cash-drawer="2" style="max-width: 500px;margin: auto;">
        <style>
            @page {
                margin:0;
                padding:0;
                width: 100%;
                position: fixed;
            }
            @media print {
                .page-break {
                    display:block;
                    page-break-before:always;
                    page-break-inside: avoid;
                }
            }
        </style>
        
        <table align="center" border="0" id="table_receipt" style="margin-top:0px;width:100%">
            <thead>
            <?php
            if ($printSetting->comp_name) echo '<tr><td align="center" colspan="6" style="font-size: 12px;font-weight:bold;">'.$company["company_name"].'</td></tr>';
            if ($printSetting->comp_regno) echo '<tr><td align="center" colspan="6" style="font-size: 12px;font-weight:bold;">'.$company["reg_no"].'</td></tr>';
            if ($printSetting->comp_addr) echo '<tr><td align="center" colspan="6" style="font-size: 12px" >'.$company["address"].'</td></tr>';
            if ($printSetting->comp_phone1) echo '<tr><td colspan="6" align="center" style="font-size: 12px" >'.$company["phone1"].'</td></tr>';   
            if ($printSetting->comp_email) echo '<tr><td colspan="6" align="center" style="font-size: 12px" >'.$company["email"].'</td></tr>';    
            if ($printSetting->comp_pic) echo '<tr><td colspan="6" align="center" style="font-size: 12px" >'.$company["contact_person"].'</td></tr>';
            if ($printSetting->queue_no) echo '<tr align="center" ><td colspan="6" style="font-size: 24px;font-weight:bold;border-top: 1px solid;border-bottom: 1px solid;">'.substr($transHead->order_no, 6).'</td></tr>';  
            echo '<tr align="center" ><td colspan="6" style="font-size: 12px;font-weight:bold;border-top: 1px solid;border-bottom: 1px solid;">'.strtoupper($printSetting->opt_type).'</td></tr>';
            if ($printSetting->cust_type) echo '<tr><td colspan="6" style="font-size: 12px">Cust. Type: '.$transHead->customerType->customer_type ?? ''. '</td></tr>';
            if ($transHead->customer_type != "CT240001" && $printSetting->cust_id) echo '<tr><td colspan="6" style="font-size: 12px">Cust. ID: '.$transHead->customer_id.'</td></tr>';
            if ($transHead->customer_name != "" && $printSetting->cust_name) echo '<tr><td colspan="6" style="font-size: 12px">Cust. Name: '.$transHead->customer_name ?? ''. '</td></tr>';
            if ($printSetting->sales_type) echo '<tr><td colspan="6" style="font-size: 12px">Sales Type: '.$transHead->salesType->sales_type.'</td></tr>';
            if ($printSetting->service_type) echo '<tr><td colspan="6" style="font-size: 12px">Service Type: '.$transHead->serviceType->service_type.'</td></tr>';
            if ($printSetting->table_no && $transHead->table_no != "") echo '<tr><td colspan="6" style="font-size: 12px">Table No: '.$transHead->table_no.'</td></tr>';
            if ($printSetting->order_no) echo '<tr><td colspan="6" style="font-size: 12px">Order No: '.$transHead->order_no.'</td></tr>';
            if ($printSetting->invoice_no) echo '<tr><td colspan="6" style="font-size: 12px">Invoice No: '.$transHead->invoice_no.'</td></tr>';
            if ($printSetting->receipt_no) echo '<tr><td colspan="6" style="font-size: 12px">Receipt No: '.$transHead->receipt_no.'</td></tr>';
            if ($company["company_id"] == 'dgrocer') { 
                echo '<tr><td colspan="6" style="font-size: 12px">Location: '.$transHead->location.'</td></tr>';
                echo '<tr><td colspan="6" style="font-size: 12px">Phone: '.$transHead->delv_hpno.'</td></tr>';
            } 
            if ($printSetting->cashier) echo '<tr><td colspan="6" style="font-size: 12px">Cashier: '.$transHead->cashier.'</td></tr>';
            if ($printSetting->date) {
                $dateTime = new DateTime($transHead->created_date);
                $dateTime->setTimezone(new DateTimeZone('Asia/Kuala_Lumpur'));
                if ((float)$company["tmzone"] < 0) {
                    $dateTime->sub(new DateInterval('PT'.abs($company["tmzone"]).'H'));
                } else {
                    $dateTime->add(new DateInterval('PT'.abs($company["tmzone"]).'H'));
                }
                echo '<tr><td colspan="6" style="font-size: 12px">Date: '.$dateTime->format('d/m/Y H:i:s').'</td></tr>';
            }
            echo '</thead>';
            ?> 
            
            <?php if ($printSetting->sales_price) { ?>
            <tr>
                <td colspan="4" style="font-size: 12px;font-weight:bold;border-top: 1px solid;border-bottom: 1px solid;">Product</td>
                <td colspan="2" align="center" style="font-size: 12px;font-weight:bold;border-top: 1px solid;border-bottom: 1px solid;">Price</td>
            </tr>
            
            <?php 
            $total_price = 0;
            $temp_seq = -1;
            foreach ($transDetail as $key => $value) { 
                $total_price += ($value->qty * ($value->price - $value->promo_amt));
                if ($temp_seq != $value->seq) {
                    $temp_seq = $value->seq;
            ?>
            
            <?php if ($printSetting->product) { ?>
            <tr>
                <td colspan="6" style="font-size: 12px;"><b><?= $value->product->product ?></b></td>
            </tr>
            <?php } ?>
            
            <?php 
                }
            ?>
            <?php if ($printSetting->selling_desc) { ?>
                <tr>
                    <td colspan="6" style="font-size: 12px;padding-left:10px"><?= $value->productSellInfo->selling_desc ?></td>
                </tr>
            <?php } ?>
                <tr>
                    <td colspan="4" style="font-size: 12px;padding-left:10px"><?= $value->qty ?> x <?= $value->price ?></td>
                    <td colspan="2" align="center" style="font-size: 12px;"><?= number_format($value->qty * ($value->price - $value->promo_amt), 2, '.', '') ?></td>
                </tr>
            <?php } ?>
            
            <tr>
                <td colspan="4" style="font-size: 12px;font-weight:bold;border-top: 1px solid;border-bottom: 1px solid;">Total</td>
                <td colspan="2" align="center" style="font-size: 12px;font-weight:bold;border-top: 1px solid;border-bottom: 1px solid;"><?= number_format($total_price, 2, '.', '') ?></td>
            </tr>
            
            <?php } else { ?>
            <!-- ... kode untuk mode tanpa sales_price ... -->
            <?php } ?>
            
            <?php if ($printSetting->footer_msg) { ?>
            <tr align="center" style="font-size: 12px;font-weight:bold;"><td colspan="6" >Thank you. Please come again.</td></tr>
            <?php } ?>
        </table>
        
        <table style="border-bottom:1px solid white" width="100%">
            <tr><td height="100"></td></tr>
        </table>
    </div>
</div>

<?php 
$js = <<<JS
    $(document).ready(function() {
        // Auto print jika diperlukan
        // $('.btnprint').trigger('click');
    });

    $(".btnprint").click(function() {
        // Metode 1: Gunakan window.print() - akan di-intercept oleh POS Printer bridge
        // Jika aplikasi POS Printer sedang berjalan, akan otomatis print ke POS printer
        // Jika tidak, akan fallback ke browser print dialog
        if (window.posPrinter) {
            // Metode 2: Langsung gunakan posPrinter.printFromElement() untuk lebih eksplisit
            window.posPrinter.printFromElement('#PrintContent', true);
            
            // Buka cash drawer setelah print (delay 500ms untuk memastikan print selesai)
            setTimeout(function() {
                window.posPrinter.openCashDrawer(2); // Pin 2 (default)
            }, 500);
        } else {
            // Fallback ke metode original jika POS Printer bridge tidak tersedia
            const oIframe = document.getElementById('iframePrint');
            const oContent = document.getElementById('PrintContent').innerHTML;
            let oDoc = (oIframe.contentWindow || oIframe.contentDocument);
            if (oDoc.document) oDoc = oDoc.document;
            oDoc.write('<body onload="this.focus(); this.print();" style="font-family:Arial !important;">');
            oDoc.write(oContent + '</body>');
            oDoc.close();
        }
        
        // Close window setelah print (opsional)
        setTimeout(function () {
            window.close()
        }, 2000);
    });
JS;

$this->registerJs($js);
?>

