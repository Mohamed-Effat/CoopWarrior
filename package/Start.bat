@echo off
setlocal
cd /d "%~dp0"

if exist "app\CoopWarrior.exe" goto run

echo The "app" folder is missing or incomplete.
echo Copy the whole Co-op Warrior folder again, or rebuild it from the source code with Build.bat.
pause
exit /b 1

:run
start "" "%~dp0app\CoopWarrior.exe"
