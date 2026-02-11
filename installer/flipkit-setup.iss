; FlipKit Windows Installer Script
; Inno Setup 6.x required: https://jrsoftware.org/isdl.php

#define MyAppName "FlipKit"
#define MyAppVersion "3.0.0"
#define MyAppPublisher "FlipKit Contributors"
#define MyAppURL "https://github.com/mthous72/FlipKit"
#define MyAppExeName "FlipKit.Desktop.exe"

[Setup]
; NOTE: Generate a new GUID for AppId using an online GUID generator
AppId={{F1A2B3C4-D5E6-7890-1234-567890ABCDEF}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=..\releases\installer
OutputBaseFilename=FlipKit-Setup-v{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\Desktop\{#MyAppExeName}
UninstallDisplayName={#MyAppName} v{#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} Setup

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full Installation (Desktop + Web + API)"
Name: "desktop"; Description: "Desktop Only"
Name: "web"; Description: "Web Server Only (for mobile access)"
Name: "custom"; Description: "Custom"; Flags: iscustom

[Components]
Name: "desktop"; Description: "Desktop Application"; Types: full desktop custom; Flags: fixed
Name: "web"; Description: "Web Server (mobile browser access)"; Types: full web
Name: "api"; Description: "API Server (remote access via Tailscale)"; Types: full
Name: "docs"; Description: "Documentation"; Types: full custom

[Files]
; Desktop App
Source: "..\releases\temp\desktop-win-x64\*"; DestDir: "{app}\Desktop"; Components: desktop; Flags: recursesubdirs ignoreversion

; Web Server
Source: "..\releases\temp\web-win-x64\*"; DestDir: "{app}\Web"; Components: web; Flags: recursesubdirs ignoreversion

; API Server
Source: "..\releases\temp\api-win-x64\*"; DestDir: "{app}\API"; Components: api; Flags: recursesubdirs ignoreversion

; Documentation
Source: "..\Docs\*.md"; DestDir: "{app}\Docs"; Components: docs; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Components: docs; Flags: ignoreversion
Source: "..\CHANGELOG.md"; DestDir: "{app}"; Components: docs; Flags: ignoreversion
Source: "..\release-notes-v3.0.0.md"; DestDir: "{app}"; Components: docs; Flags: ignoreversion

; Launcher scripts
Source: "..\releases\temp\web-win-x64\StartWeb.bat"; DestDir: "{app}\Web"; Components: web; Flags: ignoreversion
Source: "..\releases\temp\api-win-x64\StartAPI.bat"; DestDir: "{app}\API"; Components: api; Flags: ignoreversion

[Icons]
; Desktop app shortcuts
Name: "{group}\FlipKit"; Filename: "{app}\Desktop\{#MyAppExeName}"; Components: desktop
Name: "{autodesktop}\FlipKit"; Filename: "{app}\Desktop\{#MyAppExeName}"; Tasks: desktopicon; Components: desktop

; Web server shortcuts
Name: "{group}\FlipKit Web Server"; Filename: "{app}\Web\StartWeb.bat"; Components: web
Name: "{group}\Open FlipKit Web (Browser)"; Filename: "http://localhost:5001"; Components: web

; API server shortcuts
Name: "{group}\FlipKit API Server"; Filename: "{app}\API\StartAPI.bat"; Components: api

; Documentation shortcuts
Name: "{group}\FlipKit User Guide"; Filename: "{app}\Docs\USER-GUIDE.md"; Components: docs
Name: "{group}\FlipKit Web Guide"; Filename: "{app}\Docs\WEB-USER-GUIDE.md"; Components: docs

; Uninstaller
Name: "{group}\Uninstall FlipKit"; Filename: "{uninstallexe}"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Components: desktop

[Run]
; Launch Desktop app after install
Name: "{app}\Desktop\{#MyAppExeName}"; Description: "Launch FlipKit Desktop"; Flags: nowait postinstall skipifsilent; Components: desktop

; Option to start Web server
Name: "{app}\Web\StartWeb.bat"; Description: "Start FlipKit Web Server"; Flags: nowait postinstall skipifsilent unchecked; Components: web

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nFlipKit is a sports card inventory and pricing tool for sellers. It includes:%n%n• Desktop App - Full-featured Windows application%n• Web Server - Mobile browser access (optional)%n• API Server - Remote access via Tailscale (optional)%n%nIt is recommended that you close all other applications before continuing.

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Check if user is upgrading from CardLister
    if DirExists(ExpandConstant('{localappdata}\CardLister')) then
    begin
      if MsgBox('FlipKit installer detected existing CardLister data. Your data will be automatically migrated when you launch FlipKit for the first time. Your original CardLister data will be preserved as a backup.' + #13#10#13#10 + 'Continue?', mbInformation, MB_OK) = IDOK then
      begin
        // User acknowledged - migration will happen on first launch
      end;
    end;
  end;
end;
