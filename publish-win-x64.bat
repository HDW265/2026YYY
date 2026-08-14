@echo off
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet SDK not found. Install .NET 8 SDK first.
  pause
  exit /b 1
)

dotnet publish src\LanMonitor.Receiver\LanMonitor.Receiver.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableWindowsTargeting=true -o publish\win-x64
if errorlevel 1 (
  echo PUBLISH FAILED
  pause
  exit /b 1
)

echo.
echo DONE. EXE is in folder publish\win-x64
dir /b publish\win-x64\*.exe
pause
