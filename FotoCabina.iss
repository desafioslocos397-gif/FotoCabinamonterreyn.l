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
Source: "redist\ndp48-x86-x64-allos-enu.exe"; DestDir: "{tmp}"; Flags: dontcopy

[Icons]
Name: "{group}\FotoCabina"; Filename: "{app}\FotoCabina.exe"
Name: "{autodesktop}\FotoCabina"; Filename: "{app}\FotoCabina.exe"; Tasks: escritorio
Name: "{commonstartup}\FotoCabina"; Filename: "{app}\FotoCabina.exe"; Tasks: inicio

[Run]
Filename: "{app}\FotoCabina.exe"; Description: "Abrir FotoCabina"; Flags: nowait postinstall skipifsilent

[Code]
function Net48Instalado: Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release)
            and (Release >= 528040);
end;

function InitializeSetup: Boolean;
var
  Codigo: Integer;
begin
  Result := True;
  if Net48Instalado then Exit;

  if MsgBox('FotoCabina necesita .NET Framework 4.8 y no esta instalado.' + #13#10 + #13#10 +
            'Se instalara ahora (puede tardar varios minutos). Continuar?',
            mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := False;
    Exit;
  end;

  ExtractTemporaryFile('ndp48-x86-x64-allos-enu.exe');
  if not Exec(ExpandConstant('{tmp}\ndp48-x86-x64-allos-enu.exe'), '/passive /norestart', '',
              SW_SHOW, ewWaitUntilTerminated, Codigo) then
    Codigo := -1;

  if (Codigo = 3010) or (Codigo = 1641) then
  begin
    MsgBox('.NET Framework 4.8 se instalo, pero hay que REINICIAR la computadora.' + #13#10 +
           'Despues del reinicio, vuelve a ejecutar este instalador.', mbInformation, MB_OK);
    Result := False;
  end
  else if (Codigo <> 0) or (not Net48Instalado) then
  begin
    MsgBox('No se pudo instalar .NET Framework 4.8 (codigo ' + IntToStr(Codigo) + ').' + #13#10 + #13#10 +
           'En Windows 7 verifica lo siguiente:' + #13#10 +
           '1) Que la fecha y hora de la PC sean correctas.' + #13#10 +
           '2) Que tengas Service Pack 1.' + #13#10 +
           '3) Instala las actualizaciones KB4490628 y KB4474419 (version x86), reiniciando despues de cada una.' + #13#10 + #13#10 +
           'Despues vuelve a ejecutar este instalador.', mbError, MB_OK);
    Result := False;
  end;
end;
