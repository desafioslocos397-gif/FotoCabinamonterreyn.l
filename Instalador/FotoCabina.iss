; Instalador compatible con Windows 7 SP1, Windows 10 y Windows 11
; En Windows 7 se requiere Service Pack 1.
#define AppName "FotoCabina"
#define AppVersion "1.0"
#define Origen "..\FotoCabina\bin\Release\net48"

[Setup]
AppId={{B3C1A6E2-5D4F-4C7A-9E21-7F0A2D8C1B11}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=salida
OutputBaseFilename=Instalador_FotoCabina
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
MinVersion=6.1sp1
PrivilegesRequired=admin
UninstallDisplayIcon={app}\FotoCabina.exe

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "escritorio"; Description: "Crear icono en el escritorio"; Flags: checkedonce
Name: "inicio"; Description: "Iniciar FotoCabina automaticamente al encender Windows"; Flags: unchecked

[Files]
Source: "{#Origen}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "redist\ndp48-x86-x64-allos-enu.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not Net48Instalado

[Icons]
Name: "{group}\FotoCabina"; Filename: "{app}\FotoCabina.exe"
Name: "{autodesktop}\FotoCabina"; Filename: "{app}\FotoCabina.exe"; Tasks: escritorio
Name: "{commonstartup}\FotoCabina"; Filename: "{app}\FotoCabina.exe"; Tasks: inicio

[Run]
Filename: "{tmp}\ndp48-x86-x64-allos-enu.exe"; Parameters: "/q /norestart"; StatusMsg: "Instalando .NET Framework 4.8..."; Flags: waituntilterminated; Check: not Net48Instalado; AfterInstall: VerificarNet48
Filename: "{app}\FotoCabina.exe"; Description: "Abrir FotoCabina"; Flags: nowait postinstall skipifsilent

[Code]
function Net48Instalado: Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
end;

procedure VerificarNet48;
begin
  if not Net48Instalado then
  begin
    MsgBox('No se pudo confirmar la instalacion de .NET Framework 4.8.' + #13#10 + #13#10 +
      'En Windows 7 verifica que sea Service Pack 1, instala las actualizaciones de Windows necesarias y reinicia el equipo. ' +
      'Despues vuelve a ejecutar este instalador. No se iniciara FotoCabina hasta que .NET 4.8 este instalado.',
      mbError, MB_OK);
    Abort;
  end;
end;
