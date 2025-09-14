@echo off
REM Windows Build Script for ESP32 Firmware Flasher
REM Creates standalone executable with all dependencies

echo ESP32 Firmware Flasher - Windows Build
echo =====================================

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python 3.8+ from https://python.org
    pause
    exit /b 1
)

REM Install build dependencies
echo Installing build dependencies...
pip install -r requirements-build.txt
if errorlevel 1 (
    echo ERROR: Failed to install dependencies
    pause
    exit /b 1
)

REM Run the build script
echo Building executable...
python build_windows.py
if errorlevel 1 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo Executable location: dist\esp32-flasher.exe
echo.
echo To create installer:
echo 1. Install NSIS from https://nsis.sourceforge.io/
echo 2. Run: makensis installer.nsi
echo.
pause
