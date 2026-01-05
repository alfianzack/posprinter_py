@echo off
REM Script untuk build aplikasi dan membuat installer Windows
REM Pastikan Inno Setup sudah terinstall di: C:\Program Files (x86)\Inno Setup 6

echo ========================================
echo Building DXN POS Printer Installer
echo ========================================
echo.

REM Set variabel
set PROJECT_DIR=%~dp0..
set PUBLISH_DIR=%PROJECT_DIR%\publish\win-x64
set INSTALLER_DIR=%PROJECT_DIR%\installer
set DIST_DIR=%PROJECT_DIR%\dist

REM Clean previous builds
echo [1/4] Cleaning previous builds...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "%DIST_DIR%" rmdir /s /q "%DIST_DIR%"
echo Done.
echo.

REM Build aplikasi sebagai self-contained
echo [2/4] Building application (self-contained)...
cd /d "%PROJECT_DIR%"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true -o "%PUBLISH_DIR%"
if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo Done.
echo.

REM Cek apakah Inno Setup tersedia
echo [3/4] Checking Inno Setup...
set INNO_SETUP="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist %INNO_SETUP% (
    echo WARNING: Inno Setup tidak ditemukan di lokasi default.
    echo Silakan install Inno Setup dari: https://jrsoftware.org/isinfo.php
    echo Atau edit path di script ini.
    echo.
    echo Melanjutkan tanpa membuat installer...
    echo Aplikasi sudah di-build di: %PUBLISH_DIR%
    pause
    exit /b 0
)
echo Done.
echo.

REM Build installer
echo [4/4] Building installer...
cd /d "%INSTALLER_DIR%"
%INNO_SETUP% "setup.iss"
if errorlevel 1 (
    echo ERROR: Installer build failed!
    pause
    exit /b 1
)
echo Done.
echo.

echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Installer location: %DIST_DIR%\DXN-POS-Printer-Setup.exe
echo Application location: %PUBLISH_DIR%
echo.
pause

