@echo off
cd /d "%~dp0"
echo === 1/2 Compilando FotoCabina ===
dotnet build FotoCabina\FotoCabina.csproj -c Release
if errorlevel 1 (
  echo.
  echo ERROR al compilar. Instala el .NET SDK: https://dotnet.microsoft.com/download
  pause
  exit /b 1
)

echo.
echo === 2/2 Creando instalador ===
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
  echo Falta Inno Setup 6: https://jrsoftware.org/isdl.php
  pause
  exit /b 1
)
if not exist "Instalador\redist\ndp48-x86-x64-allos-enu.exe" (
  echo Falta copiar ndp48-x86-x64-allos-enu.exe en Instalador\redist
  pause
  exit /b 1
)
"%ISCC%" "Instalador\FotoCabina.iss"
echo.
echo Listo: Instalador\salida\Instalador_FotoCabina.exe
pause
