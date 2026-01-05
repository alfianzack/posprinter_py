# Panduan Membuat Installer Windows untuk DXN POS Printer

Dokumen ini menjelaskan cara membuat installer Windows (.exe) untuk aplikasi DXN POS Printer.

## Persyaratan

1. **.NET 9.0 SDK** - Untuk build aplikasi
   - Download dari: https://dotnet.microsoft.com/download/dotnet/9.0

2. **Inno Setup 6** - Untuk membuat installer
   - Download dari: https://jrsoftware.org/isinfo.php
   - Install ke lokasi default: `C:\Program Files (x86)\Inno Setup 6`

## Cara Membuat Installer

### Metode 1: Menggunakan Script Batch (Windows)

1. Buka Command Prompt atau PowerShell
2. Navigate ke folder `installer`:
   ```bash
   cd installer
   ```
3. Jalankan script:
   ```bash
   build-installer.bat
   ```

### Metode 2: Menggunakan PowerShell Script

1. Buka PowerShell
2. Navigate ke folder `installer`:
   ```powershell
   cd installer
   ```
3. Jalankan script:
   ```powershell
   .\build-installer.ps1
   ```

### Metode 3: Manual Build

1. **Build aplikasi:**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\win-x64
   ```

2. **Buka Inno Setup Compiler:**
   - Buka Inno Setup Compiler
   - File > Open
   - Pilih file `installer\setup.iss`

3. **Build installer:**
   - Build > Compile (atau tekan F9)
   - Installer akan dibuat di folder `dist\DXN-POS-Printer-Setup.exe`

## Struktur File

```
installer/
├── setup.iss              # Script Inno Setup
├── build-installer.bat    # Batch script untuk build
├── build-installer.ps1    # PowerShell script untuk build
└── README.md              # Dokumentasi ini

publish/
└── win-x64/               # Output build aplikasi (dibuat otomatis)

dist/
└── DXN-POS-Printer-Setup.exe  # Installer final (dibuat otomatis)
```

## Konfigurasi Installer

File `setup.iss` berisi konfigurasi installer. Anda dapat mengedit:

- **AppName**: Nama aplikasi
- **AppVersion**: Versi aplikasi
- **AppPublisher**: Nama publisher
- **DefaultDirName**: Lokasi instalasi default
- **OutputBaseFilename**: Nama file installer

## Fitur Installer

- ✅ Install aplikasi ke Program Files
- ✅ Membuat shortcut di Start Menu
- ✅ Opsi shortcut di Desktop (opsional)
- ✅ Uninstaller terintegrasi
- ✅ Self-contained (tidak perlu install .NET terpisah)
- ✅ Cek WebView2 Runtime (warning jika tidak ada)

## Distribusi

Setelah build selesai, installer akan tersedia di:
```
dist\DXN-POS-Printer-Setup.exe
```

File ini dapat didistribusikan ke pengguna untuk instalasi.

## Catatan Penting

1. **WebView2 Runtime**: Installer akan memberikan warning jika WebView2 Runtime tidak terdeteksi, tapi tetap melanjutkan instalasi. Pastikan pengguna menginstall WebView2 Runtime setelah instalasi aplikasi.

2. **Administrator Rights**: Installer memerlukan hak administrator untuk install ke Program Files.

3. **Ukuran Installer**: Karena self-contained, installer akan cukup besar (~100-200 MB) karena include .NET runtime.

## Troubleshooting

### Error: Inno Setup tidak ditemukan
- Pastikan Inno Setup sudah terinstall
- Edit path di script (`build-installer.bat` atau `build-installer.ps1`) jika install di lokasi lain

### Error: Build failed
- Pastikan .NET 9.0 SDK sudah terinstall
- Pastikan semua dependencies sudah terinstall
- Cek error message untuk detail

### Installer tidak berjalan
- Pastikan menjalankan installer dengan hak administrator
- Cek apakah antivirus memblokir installer

## Update Versi

Untuk update versi aplikasi:

1. Edit `setup.iss`:
   ```ini
   #define MyAppVersion "1.0.1"  // Update versi di sini
   ```

2. Rebuild installer menggunakan script atau Inno Setup Compiler

