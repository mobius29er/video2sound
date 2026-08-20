; Inno Setup script for video2sound
; Build with:  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\video2sound.iss

#define AppName    "video2sound"
#define AppVersion "2.0.0"
#define AppExe     "video2sound.exe"
#define Publisher  "Jeremy Foxx"
#define AppUrl     "https://github.com/mobius29er/video2sound"

[Setup]
AppId={{8F3A1C42-6D5B-4E79-9A21-0C7E5B4D8A16}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#Publisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\dist\video2sound-{#AppVersion}-win64\LICENSE.txt
InfoAfterFile=..\dist\video2sound-{#AppVersion}-win64\README.txt
OutputDir=..\dist
OutputBaseFilename=video2sound-{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Per-user install by default so no UAC prompt is needed, but let the user
; choose a machine-wide install if they want one.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

UninstallDisplayName={#AppName} {#AppVersion}
UninstallDisplayIcon={app}\{#AppExe}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"
Name: "contextmenu"; Description: "Add ""Convert with video2sound"" to the right-click menu for video files"; GroupDescription: "Explorer integration:"

[Files]
Source: "..\dist\video2sound-{#AppVersion}-win64\{#AppExe}";        DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\video2sound-{#AppVersion}-win64\ffmpeg.exe";       DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\video2sound-{#AppVersion}-win64\LICENSE.txt";      DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\video2sound-{#AppVersion}-win64\LICENSE-ffmpeg.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\video2sound-{#AppVersion}-win64\README.txt";       DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mp4\shell\video2sound"; ValueType: string; ValueName: ""; ValueData: "Convert with video2sound"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mp4\shell\video2sound\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mkv\shell\video2sound"; ValueType: string; ValueName: ""; ValueData: "Convert with video2sound"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mkv\shell\video2sound\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mov\shell\video2sound"; ValueType: string; ValueName: ""; ValueData: "Convert with video2sound"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mov\shell\video2sound\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.webm\shell\video2sound"; ValueType: string; ValueName: ""; ValueData: "Convert with video2sound"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.webm\shell\video2sound\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.avi\shell\video2sound"; ValueType: string; ValueName: ""; ValueData: "Convert with video2sound"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.avi\shell\video2sound\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m4v\shell\video2sound"; ValueType: string; ValueName: ""; ValueData: "Convert with video2sound"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m4v\shell\video2sound\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wmv\shell\video2sound"; ValueType: string; ValueName: ""; ValueData: "Convert with video2sound"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wmv\shell\video2sound\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.flv\shell\video2sound"; ValueType: string; ValueName: ""; ValueData: "Convert with video2sound"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.flv\shell\video2sound\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExe}"" ""%1"""; Flags: uninsdeletekey; Tasks: contextmenu
