; FiveM Police Broadcast Overlay - Inno Setup Installer Script
; Requires Inno Setup 6.x (https://jrsoftware.org/isinfo.php)

#define MyAppName "HOPE PD SkyNews"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Xtra Manager Softwares Community"
#define MyAppExeName "HopePDSkyNews.exe"
#define MyAppURL "https://github.com/Gustyx-Power/HPD-Tools"

[Setup]
AppId={{A7E8F9D1-B2C3-4D5E-6F7A-8B9C0D1E2F3A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputBaseFilename=FiveMPoliceOverlay_Setup_v{#MyAppVersion}
OutputDir=installer_output
Compression=lzma2/ultra64
SolidCompression=yes
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest
WizardStyle=modern
DisableProgramGroupPage=yes
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0

[Languages]
Name: "indonesian"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
indonesian.WelcomeLabel=Selamat datang di instalasi {#MyAppName}
indonesian.FinishedLabel=Instalasi {#MyAppName} telah selesai.

[Tasks]
Name: "desktopicon"; Description: "Buat shortcut di Desktop"; GroupDescription: "Shortcut:"; Flags: unchecked
Name: "autostart"; Description: "Jalankan otomatis saat Windows startup"; GroupDescription: "Opsi tambahan:"

[Files]
; Main application files from publish output
Source: "bin\Release\net6.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Auto-start registry entry
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "HopePDSkyNews"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Jalankan {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{userappdata}\FiveMPoliceOverlay"

[UninstallRun]
Filename: "taskkill"; Parameters: "/F /IM HopePDSkyNews.exe"; Flags: runhidden; RunOnceId: "KillApp"
