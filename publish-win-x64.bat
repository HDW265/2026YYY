@echo off
setlocal
cd /d "%~dp0"
dotnet publish "src\LanMonitor.Receiver\LanMonitor.Receiver.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableWindowsTargeting=true -o "publish\win-x64"
echo.
echo Output: publish\win-x64\局域网监控接收端.exe
pause
