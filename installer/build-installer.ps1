# PowerShell script untuk build aplikasi dan membuat installer Windows
# Pastikan Inno Setup sudah terinstall

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building DXN POS Printer Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Set variabel
$ProjectDir = Split-Path -Parent $PSScriptRoot
$PublishDir = Join-Path $ProjectDir "publish\win-x64"
$InstallerDir = $PSScriptRoot
$DistDir = Join-Path $ProjectDir "dist"
$InnoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# Clean previous builds
Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force
}
if (Test-Path $DistDir) {
    Remove-Item -Path $DistDir -Recurse -Force
}
Write-Host "Done." -ForegroundColor Green
Write-Host ""

# Build aplikasi sebagai self-contained
Write-Host "[2/4] Building application (self-contained)..." -ForegroundColor Yellow
Set-Location $ProjectDir
$buildResult = dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "Done." -ForegroundColor Green
Write-Host ""

# Cek apakah Inno Setup tersedia
Write-Host "[3/4] Checking Inno Setup..." -ForegroundColor Yellow
if (-not (Test-Path $InnoSetup)) {
    Write-Host "WARNING: Inno Setup tidak ditemukan di lokasi default." -ForegroundColor Yellow
    Write-Host "Silakan install Inno Setup dari: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    Write-Host "Atau edit path di script ini." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Melanjutkan tanpa membuat installer..." -ForegroundColor Yellow
    Write-Host "Aplikasi sudah di-build di: $PublishDir" -ForegroundColor Green
    Read-Host "Press Enter to exit"
    exit 0
}
Write-Host "Done." -ForegroundColor Green
Write-Host ""

# Build installer
Write-Host "[4/4] Building installer..." -ForegroundColor Yellow
Set-Location $InstallerDir
$setupScript = Join-Path $InstallerDir "setup.iss"
$buildResult = & $InnoSetup $setupScript

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Installer build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "Done." -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Installer location: $DistDir\DXN-POS-Printer-Setup.exe" -ForegroundColor Green
Write-Host "Application location: $PublishDir" -ForegroundColor Green
Write-Host ""
Read-Host "Press Enter to exit"

