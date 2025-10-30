@echo off
echo ========================================
echo Building ESP Flasher Windows App
echo ========================================
echo.

cd /d "%~dp0"

echo Cleaning previous build...
if exist bin\Release rmdir /s /q bin\Release
if exist obj rmdir /s /q obj

echo.
echo Building Release version...
dotnet build --configuration Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================
    echo BUILD FAILED!
    echo ========================================
    pause
    exit /b 1
)

echo.
echo ========================================
echo BUILD SUCCESSFUL!
echo ========================================
echo.
echo Executable location:
echo %CD%\bin\Release\net8.0-windows\ESPFlasher.exe
echo.
echo Press any key to run the app...
pause > nul

start "" "bin\Release\net8.0-windows\ESPFlasher.exe"
