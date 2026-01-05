# Panduan Deployment dan Distribusi Aplikasi

Dokumen ini menjelaskan cara build dan menjalankan aplikasi DXN POS Printer di komputer lain.

## Build Self-Contained

### Apa itu Self-Contained?

**Self-contained** berarti aplikasi sudah include semua yang diperlukan untuk berjalan, termasuk:
- ✅ .NET Runtime (tidak perlu install .NET terpisah)
- ✅ Semua dependencies
- ✅ Native libraries

**Keuntungan:**
- Tidak perlu install .NET di komputer target
- Semua sudah include dalam satu folder
- Mudah didistribusikan

**Kekurangan:**
- Ukuran lebih besar (~100-200 MB)
- Perlu build untuk setiap platform (win-x64, win-x86, win-arm64)

### Build Self-Contained

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

Atau dengan opsi lebih lengkap:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish/win-x64
```

**Parameter:**
- `-c Release`: Build mode Release (optimized)
- `-r win-x64`: Target platform Windows 64-bit
- `--self-contained`: Include .NET runtime
- `-p:PublishSingleFile=false`: Output sebagai multiple files (lebih mudah untuk debugging)
- `-o publish/win-x64`: Output directory

## Cara Menjalankan di Komputer Lain

### Langkah 1: Copy Folder

Setelah build, folder `publish/win-x64` berisi semua file yang diperlukan. Copy seluruh folder ini ke komputer target.

**Struktur folder yang perlu di-copy:**
```
publish/win-x64/
├── PosPrinterApp.exe          ← File utama (double-click untuk run)
├── PosPrinterApp.dll
├── PosPrinterApp.dll.config
├── PosPrinterApp.runtimeconfig.json
├── PosPrinterApp.deps.json
├── image/
│   └── logo_pos.png
├── Microsoft.Web.WebView2.*.dll
├── runtimes/
│   └── win-x64/
│       └── native/
│           └── WebView2Loader.dll
└── ... (file-file lainnya)
```

### Langkah 2: Jalankan di Komputer Target

**Cara 1: Double-click**
- Buka folder `publish/win-x64`
- Double-click `PosPrinterApp.exe`
- Aplikasi akan langsung berjalan

**Cara 2: Command Line**
```bash
cd publish/win-x64
PosPrinterApp.exe
```

### Langkah 3: Persyaratan di Komputer Target

**Wajib:**
- ✅ Windows 10/11 (64-bit)
- ✅ Microsoft Edge WebView2 Runtime
  - Download: https://developer.microsoft.com/microsoft-edge/webview2/
  - Atau akan otomatis terinstall jika sudah ada Microsoft Edge

**Tidak Perlu:**
- ❌ .NET 9.0 Runtime (sudah include)
- ❌ Visual Studio
- ❌ .NET SDK

## Distribusi Aplikasi

### Opsi 1: Copy Folder Langsung

1. **Zip folder `publish/win-x64`:**
   ```bash
   # Di Windows, right-click folder > Send to > Compressed (zipped) folder
   # Atau gunakan 7-Zip/WinRAR
   ```

2. **Kirim ke komputer target:**
   - Via USB drive
   - Via network share
   - Via email (jika ukuran memungkinkan)
   - Via cloud storage (Google Drive, OneDrive, dll)

3. **Extract dan jalankan:**
   - Extract zip file
   - Double-click `PosPrinterApp.exe`

### Opsi 2: Buat Installer (Recommended)

Gunakan installer yang sudah dibuat (lihat `installer/README.md`):

1. Build installer:
   ```bash
   cd installer
   build-installer.bat
   ```

2. Distribusikan file `dist/DXN-POS-Printer-Setup.exe`

3. User install dengan double-click installer

**Keuntungan installer:**
- ✅ Install ke Program Files
- ✅ Shortcut di Start Menu
- ✅ Uninstaller terintegrasi
- ✅ Lebih profesional

### Opsi 3: Publish Single File (Alternatif)

Jika ingin satu file executable saja:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64-single
```

**Output:**
- Satu file `PosPrinterApp.exe` (lebih besar, ~150-200 MB)
- Semua dependencies di-extract saat pertama kali run

**Keuntungan:**
- ✅ Satu file saja
- ✅ Mudah didistribusikan

**Kekurangan:**
- ❌ Startup lebih lambat (extract dulu)
- ❌ Perlu write permission di folder executable

## Platform yang Didukung

### Windows x64 (64-bit) - Recommended
```bash
dotnet publish -c Release -r win-x64 --self-contained
```
- Windows 10/11 64-bit
- Komputer modern (2010+)

### Windows x86 (32-bit)
```bash
dotnet publish -c Release -r win-x86 --self-contained
```
- Windows 10/11 32-bit
- Komputer lama (32-bit only)

### Windows ARM64
```bash
dotnet publish -c Release -r win-arm64 --self-contained
```
- Windows on ARM devices
- Surface Pro X, dll

## Troubleshooting

### Error: "The application failed to start"

**Kemungkinan penyebab:**
1. **WebView2 Runtime tidak terinstall**
   - Install dari: https://developer.microsoft.com/microsoft-edge/webview2/
   - Atau install Microsoft Edge

2. **Windows version tidak support**
   - Minimal Windows 10 version 1607
   - Update Windows ke versi terbaru

3. **Antivirus memblokir**
   - Tambahkan exception di antivirus
   - Atau disable sementara untuk test

### Error: "Missing DLL"

**Solusi:**
- Pastikan copy semua file dari folder `publish/win-x64`
- Jangan hanya copy `.exe` saja
- Pastikan folder structure sama

### Aplikasi tidak bisa print

**Cek:**
1. Printer sudah terinstall di Windows
2. Printer driver sudah terinstall
3. Printer support ESC/POS commands
4. Aplikasi berjalan dengan permission yang cukup

### WebView tidak muncul

**Solusi:**
1. Install WebView2 Runtime
2. Restart aplikasi
3. Cek Windows version (minimal Windows 10)

## Checklist Deployment

Sebelum distribusi, pastikan:

- [ ] Build dengan `--self-contained`
- [ ] Test di komputer yang tidak ada .NET SDK
- [ ] Test print functionality
- [ ] Test WebView functionality
- [ ] Test cash drawer
- [ ] Cek WebView2 Runtime requirement
- [ ] Buat installer (opsional tapi recommended)
- [ ] Dokumentasi untuk user

## Contoh Script Build Lengkap

```bash
# Build untuk distribusi
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=false `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:Version=1.0.0 `
  -o publish/win-x64

# Zip untuk distribusi
Compress-Archive -Path publish/win-x64 -DestinationPath DXN-POS-Printer-v1.0.0-win-x64.zip -Force
```

## Catatan Penting

1. **Ukuran:** Self-contained build akan besar (~100-200 MB) karena include .NET runtime
2. **Platform:** Harus build terpisah untuk setiap platform (x64, x86, arm64)
3. **WebView2:** Tetap perlu WebView2 Runtime (tidak bisa di-bundle)
4. **Update:** Untuk update aplikasi, cukup replace folder atau reinstall

