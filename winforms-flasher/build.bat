@echo off
echo Building ESP Datalogger Flasher...

REM Check if .NET 8 SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: .NET 8 SDK is not installed or not in PATH
    echo Please install .NET 8 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Restore packages
echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo Error: Failed to restore packages
    pause
    exit /b 1
)

REM Build the application
echo Building application...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo Error: Build failed
    pause
    exit /b 1
)

REM Create publish directory
echo Publishing application...
dotnet publish --configuration Release --output "publish" --no-build --self-contained false
if %errorlevel% neq 0 (
    echo Error: Publish failed
    pause
    exit /b 1
)

REM Copy additional files
echo Copying additional files...
copy "firebase-config.json.template" "publish\firebase-config.json.template" >nul 2>&1
copy "README.md" "publish\README.md" >nul 2>&1

REM Check if esptool.exe exists
if not exist "esptool.exe" (
    echo Warning: esptool.exe not found in current directory
    echo Please download esptool.exe and place it in the publish directory
) else (
    copy "esptool.exe" "publish\esptool.exe" >nul 2>&1
    echo Copied esptool.exe to publish directory
)

echo.
echo Build completed successfully!
echo.
echo Output directory: %cd%\publish
echo.
echo Next steps:
echo 1. Download esptool.exe if not already present
echo 2. Configure firebase-config.json with your Firebase credentials
echo 3. Run ESPFlasher.exe from the publish directory
echo.
pause
