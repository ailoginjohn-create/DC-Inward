; Inno Setup script for Inward & DC.
; Build with: iscc installer.iss   (or run scripts\build-installer.ps1)
; Publish the app first (scripts\publish.ps1) so publish\win-x64 exists.

#define MyAppName "Inward & DC"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "InwardDC"
#define MyAppExeName "InwardDC.exe"
#define SourceDir "..\publish\win-x64"

[Setup]
AppId={{C8E6F3B4-1A2B-4C5D-9E8F-0A1B2C3D4E5F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\InwardDC
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.\output
OutputBaseFilename=InwardDC-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
SetupIconFile=..\src\InwardDC.App\Resources\app.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    if MsgBox('Remove the application data folder too (backups, database, attachments)?' + #13#10 +
              'Choose No to keep your data for a future reinstall.', mbConfirmation, MB_YESNO) = IDYES then
      DelTree(ExpandConstant('{localappdata}\InwardDC'), True, True, True);
end;
