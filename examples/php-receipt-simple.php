<?php
/**
 * Contoh Sederhana: Modifikasi Minimal untuk Print ke POS Printer
 * 
 * Hanya perlu menambahkan beberapa baris JavaScript untuk mendukung POS Printer
 */

// ... kode PHP Anda yang sudah ada ...

?>

<div class="row" style="padding-bottom:10px">
    <div class="col-md-12" style="text-align:center ">
        <a href="javascript:window.close()" class="btn btn-danger" ><i class="fa fa-times" style="padding-right:10px"></i>Close</a>    
        <a href="javascript:void(0)" class="btn btn-primary btnprint" ><i class="fa fa-print" style="padding-right:10px"></i>Print</a>
    </div>
</div>

<!-- ... konten receipt Anda ... -->
<!-- 
    Tambahkan data-auto-cash-drawer untuk auto-open cash drawer setelah print
    Nilai: "1" untuk Pin 1, "2" untuk Pin 2 (default), atau "false" untuk disable
-->
<div id="PrintContent" data-auto-cash-drawer="2">
    <!-- ... konten receipt ... -->
</div>

<?php 
$js = <<<JS
    $(".btnprint").click(function() {
        // Cek apakah POS Printer bridge tersedia
        if (window.posPrinter) {
            // Print ke POS printer menggunakan printFromElement
            window.posPrinter.printFromElement('#PrintContent', true);
            
            // Buka cash drawer setelah print
            setTimeout(function() {
                window.posPrinter.openCashDrawer(2); // Pin 2 (default)
            }, 500);
        } else {
            // Fallback ke metode original (iframe print)
            const oIframe = document.getElementById('iframePrint');
            const oContent = document.getElementById('PrintContent').innerHTML;
            let oDoc = (oIframe.contentWindow || oIframe.contentDocument);
            if (oDoc.document) oDoc = oDoc.document;
            oDoc.write('<body onload="this.focus(); this.print();" style="font-family:Arial !important;">');
            oDoc.write(oContent + '</body>');
            oDoc.close();
        }
        
        setTimeout(function () {
            window.close()
        }, 2000);
    });
JS;

$this->registerJs($js);
?>

