@echo off
cd /d "%~dp0"
fltmc >nul 2>&1
if errorlevel 1 (
  echo Asking for administrator permission...
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs -Wait"
  exit /b
)
echo Installing ViGEmBus virtual gamepad driver...
msiexec /i "%~dp0app\ViGEmBusSetup_x64.msi" /passive /norestart
echo Done.
