# Cash Drawer Controller

Aplikasi sederhana untuk membuka cash drawer (laci kasir) menggunakan perintah ESC/POS melalui koneksi Serial/USB, Bluetooth, atau WiFi.

## Fitur

- GUI sederhana dan mudah digunakan
- **Dukungan penuh untuk printer Bluetooth** (akan muncul sebagai COM port virtual)
- **Dukungan penuh untuk printer WiFi** (koneksi TCP/IP)
- Deteksi otomatis port COM yang tersedia (USB/Serial dan Bluetooth)
- Deteksi otomatis tipe koneksi (Bluetooth atau Serial/USB)
- Dukungan berbagai baudrate (9600, 19200, 38400, 57600, 115200)
- **Test Print** - Menguji printer dengan mencetak informasi koneksi dan karakter test
- Mengirim perintah ESC/POS standar untuk membuka cash drawer

## Persyaratan

- Python 3.6 atau lebih baru
- Printer POS dengan cash drawer yang terhubung via:
  - **USB/Serial** (kabel USB atau Serial)
  - **Bluetooth** (printer harus sudah dipasangkan dengan komputer)
  - **WiFi** (printer harus terhubung ke jaringan WiFi yang sama dengan komputer)

## Instalasi

1. Clone atau download repository ini

2. Install dependencies:
```bash
pip install -r requirements.txt
```

## Penggunaan

### Untuk Printer USB/Serial:
1. Hubungkan printer POS Anda ke komputer via USB atau Serial
2. Jalankan aplikasi (lihat langkah di bawah)

### Untuk Printer Bluetooth:
1. **Pasangkan printer Bluetooth dengan komputer Anda terlebih dahulu:**
   - Buka Settings > Devices > Bluetooth & other devices (Windows)
   - Aktifkan Bluetooth jika belum aktif
   - Tambahkan printer Bluetooth Anda
   - Setelah dipasangkan, Windows akan membuat COM port virtual untuk printer

### Untuk Printer WiFi:
1. **Pastikan printer WiFi sudah terhubung ke jaringan WiFi yang sama dengan komputer:**
   - Konfigurasi printer WiFi melalui menu printer atau aplikasi konfigurasi
   - Catat IP address printer (biasanya dapat dilihat dari menu printer atau aplikasi konfigurasi)
   - Pastikan printer dan komputer berada di jaringan WiFi yang sama

2. **Jalankan aplikasi:**
```bash
python cash_drawer_app.py
```

3. **Pilih tipe koneksi:**
   - Pilih **"Serial"** untuk USB/Serial atau Bluetooth
   - Pilih **"WiFi"** untuk printer WiFi

4. **Konfigurasi sesuai tipe koneksi:**

   **Untuk Serial/Bluetooth:**
   - Pilih port COM printer Anda dari dropdown
   - Aplikasi akan otomatis mendeteksi apakah port adalah Bluetooth atau Serial/USB
   - Info tipe koneksi akan ditampilkan di bawah dropdown
   - Pilih baudrate yang sesuai (default: 9600)
   - Untuk Bluetooth, biasanya menggunakan 9600 atau 115200

   **Untuk WiFi:**
   - Masukkan IP address printer (contoh: 192.168.1.100)
   - Masukkan port number (default: 9100 untuk Raw TCP/IP)
   - Port 9100 adalah port standar untuk printer ESC/POS melalui WiFi

5. **Gunakan fitur yang diinginkan:**
   - **Klik "Test Print"** untuk menguji printer dan mencetak informasi koneksi
   - **Klik "Buka Cash Drawer"** untuk membuka laci kasir

6. **Cash drawer akan terbuka atau test print akan tercetak!**

## Catatan

- **Untuk Printer Bluetooth:**
  - Pastikan printer sudah dipasangkan dengan komputer sebelum menjalankan aplikasi
  - Printer Bluetooth akan muncul sebagai COM port virtual di Windows
  - Aplikasi akan otomatis mendeteksi dan menampilkan info jika port adalah Bluetooth
  - Pastikan Bluetooth printer dalam jangkauan dan sudah terhubung

- **Untuk Printer USB/Serial:**
  - Pastikan printer sudah terhubung dan terdeteksi di sistem sebelum menjalankan aplikasi
  - Pastikan driver printer sudah terinstall dengan benar

- **Untuk Printer WiFi:**
  - Pastikan printer dan komputer berada di jaringan WiFi yang sama
  - IP address printer biasanya dapat ditemukan melalui:
    - Menu printer (biasanya ada opsi "Network Status" atau "Print Network Configuration")
    - Aplikasi konfigurasi printer dari vendor
    - Router admin panel (lihat daftar perangkat yang terhubung)
  - Port default adalah 9100 (Raw TCP/IP), yang merupakan standar untuk printer ESC/POS
  - Beberapa printer mungkin menggunakan port lain (9101, 515, dll) - cek dokumentasi printer
  - Pastikan firewall tidak memblokir koneksi ke port printer

- **Umum:**
  - Jika port COM tidak muncul, klik tombol "Refresh"
  - Baudrate default adalah 9600, tetapi beberapa printer mungkin menggunakan baudrate yang berbeda
  - Perintah yang dikirim menggunakan format ESC/POS standar: `ESC p 1 0 1`

## Troubleshooting

**Test Print tidak muncul:**
- Pastikan kertas printer sudah terpasang dengan benar
- Cek apakah port COM yang dipilih sudah benar
- Coba baudrate yang berbeda
- Pastikan tidak ada aplikasi lain yang menggunakan port yang sama
- Gunakan tombol "Test Print" untuk memverifikasi printer bekerja dengan baik sebelum membuka cash drawer

**Cash drawer tidak terbuka:**
- Pastikan kabel cash drawer terhubung dengan benar ke printer
- Cek apakah port COM yang dipilih sudah benar
- Coba baudrate yang berbeda
- Pastikan tidak ada aplikasi lain yang menggunakan port yang sama
- Gunakan "Test Print" terlebih dahulu untuk memastikan komunikasi dengan printer berjalan dengan baik

**Port COM tidak muncul (Bluetooth):**
- Pastikan printer Bluetooth sudah dipasangkan dengan komputer
- Buka Settings > Devices > Bluetooth & other devices dan pastikan printer terhubung
- Cek Device Manager (Windows) > Ports (COM & LPT) untuk melihat port COM virtual
- Klik tombol "Refresh" untuk memperbarui daftar port
- Pastikan printer Bluetooth dalam jangkauan dan tidak dalam mode sleep

**Port COM tidak muncul (USB/Serial):**
- Pastikan printer sudah terhubung ke komputer
- Cek Device Manager (Windows) untuk melihat port COM yang tersedia
- Klik tombol "Refresh" untuk memperbarui daftar port

**Error saat membuka port (Serial/Bluetooth):**
- Pastikan tidak ada aplikasi lain yang menggunakan port yang sama
- Coba restart aplikasi
- Pastikan driver printer sudah terinstall dengan benar

**Error koneksi WiFi:**
- Pastikan IP address dan port number sudah benar
- Pastikan printer dan komputer berada di jaringan WiFi yang sama
- Cek apakah printer WiFi aktif dan terhubung ke jaringan
- Coba ping IP address printer dari command prompt: `ping 192.168.1.100` (ganti dengan IP printer Anda)
- Pastikan firewall tidak memblokir koneksi ke port printer
- Coba port lain jika port 9100 tidak bekerja (cek dokumentasi printer)
- Pastikan tidak ada aplikasi lain yang menggunakan koneksi ke printer yang sama

## Lisensi

Aplikasi ini dibuat untuk keperluan internal dan dapat digunakan secara bebas.

