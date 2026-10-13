@echo off
rem Builds the portable Co-op Warrior folder.
rem   Build.bat                 -> dist\CoopWarrior
rem   Build.bat "D:\Some\Path"  -> updates that folder in place
rem Replaces app\ and the files from package\. Leaves profiles\ and CoopWarrior.ini alone.
setlocal
cd /d "%~dp0"

set "OUT=%~1"
if "%OUT%"=="" set "OUT=%~dp0dist\CoopWarrior"

where dotnet >nul 2>&1
if errorlevel 1 goto nosdk
dotnet --list-sdks | findstr /b "9. 1" >nul
if errorlevel 1 goto nosdk

tasklist /fi "imagename eq CoopWarrior.exe" | find /i "CoopWarrior.exe" >nul
if not errorlevel 1 (
  echo Close Co-op Warrior first, then run Build.bat again.
  pause
  exit /b 1
)

echo Building Co-op Warrior into "%OUT%". The first build downloads packages and takes a minute or two...
if exist "%OUT%\app" rmdir /s /q "%OUT%\app"
dotnet publish src -c Release -o "%OUT%\app" -nologo
if errorlevel 1 (
  echo.
  echo Build failed. Please send me the messages above.
  pause
  exit /b 1
)
xcopy /y /q "package\*" "%OUT%" >nul
echo.
echo Done. Start Co-op Warrior with "%OUT%\Start.bat".
pause
exit /b 0

:nosdk
echo Building needs the .NET 9 SDK (or newer):
echo   https://dotnet.microsoft.com/download/dotnet/9.0
pause
exit /b 1
