@echo off
REM Quick test script to flash ESP32-S3 using esptool directly

echo === ESP32-S3 Test Flash Script ===
echo.

REM Check if firmware files exist
if not exist "Datalogger Firmware Test\bootloader.bin" (
    echo ERROR: bootloader.bin not found
    pause
    exit /b 1
)
if not exist "Datalogger Firmware Test\partitions.bin" (
    echo ERROR: partitions.bin not found
    pause
    exit /b 1
)
if not exist "Datalogger Firmware Test\firmware.bin" (
    echo ERROR: firmware.bin not found
    pause
    exit /b 1
)

echo Firmware files found:
dir "Datalogger Firmware Test\*.bin" /b
echo.

REM Ask for COM port
set /p COMPORT="Enter COM port (e.g., COM4): "

echo.
echo WARNING: This will erase all data on %COMPORT%
set /p CONFIRM="Continue? (y/N): "
if /i not "%CONFIRM%"=="y" (
    echo Cancelled.
    pause
    exit /b 0
)

echo.
echo === Starting Flash ===
echo.

REM Run esptool
python -m esptool --port %COMPORT% --chip esp32s3 --baud 460800 --before default_reset --after hard_reset write_flash --flash_mode dio --flash_freq 80m --flash_size detect 0x0 "Datalogger Firmware Test\bootloader.bin" 0x8000 "Datalogger Firmware Test\partitions.bin" 0x10000 "Datalogger Firmware Test\firmware.bin"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo === Flash Successful! ===
    echo You can now press RESET on your ESP32-S3 or open serial monitor.
) else (
    echo.
    echo === Flash Failed ===
    echo Exit code: %ERRORLEVEL%
)

echo.
pause
