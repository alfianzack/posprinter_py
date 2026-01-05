; Script Inno Setup untuk DXN POS
; Kompilasi: Inno Setup Compiler (https://jrsoftware.org/isinfo.php)

#define MyAppName "DXN POS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "DXN"
#define MyAppURL "https://dxnpos-train.dxn2u.com"
#define MyAppExeName "DXNPosApp.exe"
#define MyAppId "{{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}"

[Setup]
; Informasi aplikasi
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
OutputDir=..\dist
OutputBaseFilename=DXN-POS-Printer-Setup
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

; Tampilan installer
WizardImageFile=
WizardSmallImageFile=

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "indonesian"; MessagesFile: "compiler:Languages\Indonesian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; File aplikasi dari publish output
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Contoh file (opsional)
Source: "..\examples\*"; DestDir: "{app}\examples"; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: ; Languages: 

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Custom code untuk cek WebView2 Runtime
function InitializeSetup(): Boolean;
var
  Version: String;
begin
  Result := True;
  
  // Cek apakah WebView2 Runtime sudah terinstall
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE, 
    'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 
    'pv', Version) then
  begin
    if MsgBox('Microsoft Edge WebView2 Runtime tidak terdeteksi.' + #13#10 +
      'Aplikasi memerlukan WebView2 Runtime untuk berjalan.' + #13#10 +
      'Apakah Anda ingin melanjutkan instalasi?', 
      mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end
    else
    begin
      MsgBox('Setelah instalasi, pastikan untuk menginstall WebView2 Runtime dari:' + #13#10 +
        'https://developer.microsoft.com/microsoft-edge/webview2/', 
        mbInformation, MB_OK);
    end;
  end;
end;

