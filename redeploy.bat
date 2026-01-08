@echo off
REM ============================================
REM ESP Datalogger Flasher - Redeploy
REM ============================================
REM Pull latest changes, restore, and build
REM ============================================

echo.
echo ========================================
echo ESP Datalogger Flasher - Redeploy
echo ========================================
echo.

REM Check if Git is installed
git --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Git is not installed or not in PATH
    echo Please install Git from https://git-scm.com/download/win
    pause
    exit /b 1
)

REM Check if .NET 8 SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET 8 SDK is not installed or not in PATH
    echo Please install .NET 8 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [1/4] Pulling latest changes from Git...
echo.
git pull
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Git pull failed
    echo Please resolve any conflicts and try again
    pause
    exit /b 1
)

echo.
echo [2/4] Navigating to winforms-flasher directory...
cd winforms-flasher
if %errorlevel% neq 0 (
    echo [ERROR] Could not find winforms-flasher directory
    pause
    exit /b 1
)

echo.
echo [3/4] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Failed to restore packages
    pause
    exit /b 1
)

echo.
echo [4/5] Building application (Release)...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build failed
    pause
    exit /b 1
)

echo.
echo Publishing application...
dotnet publish --configuration Release --output "publish" --no-build --self-contained false
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Publish failed
    pause
    exit /b 1
)

echo.
echo [5/5] Setting up shared ESP32 files...
cd ..

REM Get firmware folder from app settings or use default
set "FIRMWARE_DIR=%LOCALAPPDATA%\ESPFlasher\Firmware"
if not exist "%FIRMWARE_DIR%" mkdir "%FIRMWARE_DIR%"
if not exist "%FIRMWARE_DIR%\_common" mkdir "%FIRMWARE_DIR%\_common"

REM Only copy if files don't already exist
if exist "src\_common\bootloader.bin" (
    if not exist "%FIRMWARE_DIR%\_common\bootloader.bin" (
        copy "src\_common\bootloader.bin" "%FIRMWARE_DIR%\_common\bootloader.bin" >nul 2>&1
        echo Copied bootloader.bin to %FIRMWARE_DIR%\_common\
    ) else (
        echo bootloader.bin already exists, skipping
    )
)
if exist "src\_common\partitions.bin" (
    if not exist "%FIRMWARE_DIR%\_common\partitions.bin" (
        copy "src\_common\partitions.bin" "%FIRMWARE_DIR%\_common\partitions.bin" >nul 2>&1
        echo Copied partitions.bin to %FIRMWARE_DIR%\_common\
    ) else (
        echo partitions.bin already exists, skipping
    )
)

cd winforms-flasher

echo.
echo Copying additional files...
if exist "firebase-config.json.template" copy "firebase-config.json.template" "publish\firebase-config.json.template" >nul 2>&1
if exist "README.md" copy "README.md" "publish\README.md" >nul 2>&1
if exist "flash.ico" copy "flash.ico" "publish\flash.ico" >nul 2>&1

REM Check if esptool.exe exists and copy to both locations
if not exist "esptool.exe" (
    echo [WARNING] esptool.exe not found in winforms-flasher directory
) else (
    copy "esptool.exe" "publish\esptool.exe" >nul 2>&1
    copy "esptool.exe" "bin\Release\net8.0-windows\esptool.exe" >nul 2>&1
    echo Copied esptool.exe to output directories
)

echo.
echo ========================================
echo SUCCESS! Redeploy completed
echo ========================================
echo.
echo Executable: %cd%\publish\ESPFlasher.exe
echo Firmware folder: %FIRMWARE_DIR%
echo Shared files: %FIRMWARE_DIR%\_common\
echo.
pause
