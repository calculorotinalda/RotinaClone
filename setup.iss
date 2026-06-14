; Inno Setup Script for Rotina Clone Enterprise Edition
; Defines installer variables, shortcuts, file packaging, and custom application icon

[Setup]
AppName=Rotina Clone
AppVersion=1.0.0
AppPublisher=Rotina Clone Enterprise
AppPublisherURL=https://www.rotinaclone.com
DefaultDirName={autopf}\Rotina Clone
DefaultGroupName=Rotina Clone
DisableProgramGroupPage=yes
; Icon specifications
SetupIconFile=icons\icon.ico
UninstallDisplayIcon={app}\icons\icon.ico
OutputDir=publish_release
OutputBaseFilename=rotina_clone_setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Grab files directly from the published portable build directory
Source: "publish_portable\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
Source: "icons\icon.ico"; DestDir: "{app}\icons"; Flags: ignoreversion

[Icons]
Name: "{group}\Rotina Clone"; Filename: "{app}\RotinaClone.App.exe"; IconFilename: "{app}\icons\icon.ico"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,Rotina Clone}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Rotina Clone"; Filename: "{app}\RotinaClone.App.exe"; IconFilename: "{app}\icons\icon.ico"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
Filename: "{app}\RotinaClone.App.exe"; Description: "{cm:LaunchProgram,Rotina Clone}"; Flags: nowait postinstall skipifsilent runascurrentuser
