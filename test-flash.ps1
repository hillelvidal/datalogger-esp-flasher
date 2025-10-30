# Quick test script to flash ESP32-S3 using esptool directly
# This bypasses the Windows app to test if flashing works at all

$ErrorActionPreference = "Stop"

Write-Host "=== ESP32-S3 Test Flash Script ===" -ForegroundColor Cyan
Write-Host ""

# Paths
$firmwareDir = "Datalogger Firmware Test"
$bootloader = Join-Path $firmwareDir "bootloader.bin"
$partitions = Join-Path $firmwareDir "partitions.bin"
$firmware = Join-Path $firmwareDir "firmware.bin"

# Check files
Write-Host "Checking firmware files..." -ForegroundColor Yellow
if (-not (Test-Path $bootloader)) { Write-Host "ERROR: bootloader.bin not found" -ForegroundColor Red; exit 1 }
if (-not (Test-Path $partitions)) { Write-Host "ERROR: partitions.bin not found" -ForegroundColor Red; exit 1 }
if (-not (Test-Path $firmware)) { Write-Host "ERROR: firmware.bin not found" -ForegroundColor Red; exit 1 }

Write-Host "  ✓ bootloader.bin ($([math]::Round((Get-Item $bootloader).Length/1KB, 1)) KB)" -ForegroundColor Green
Write-Host "  ✓ partitions.bin ($([math]::Round((Get-Item $partitions).Length/1KB, 1)) KB)" -ForegroundColor Green
Write-Host "  ✓ firmware.bin ($([math]::Round((Get-Item $firmware).Length/1KB, 1)) KB)" -ForegroundColor Green
Write-Host ""

# Detect COM port
Write-Host "Detecting COM ports..." -ForegroundColor Yellow
$ports = [System.IO.Ports.SerialPort]::GetPortNames()
if ($ports.Count -eq 0) {
    Write-Host "ERROR: No COM ports found. Please connect your ESP32-S3." -ForegroundColor Red
    exit 1
}

Write-Host "Available ports:" -ForegroundColor Yellow
foreach ($port in $ports) {
    Write-Host "  - $port" -ForegroundColor Cyan
}

# Use first port or ask user
if ($ports.Count -eq 1) {
    $comPort = $ports[0]
    Write-Host "Using: $comPort" -ForegroundColor Green
} else {
    $comPort = Read-Host "Enter COM port (e.g., COM4)"
}

Write-Host ""
Write-Host "⚠️  WARNING: This will erase all data on $comPort" -ForegroundColor Red
$confirm = Read-Host "Continue? (y/N)"
if ($confirm -ne "y") {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "=== Starting Flash ===" -ForegroundColor Cyan
Write-Host ""

# Build esptool command
$cmd = "python"
$args = @(
    "-m", "esptool",
    "--port", $comPort,
    "--chip", "esp32s3",
    "--baud", "460800",
    "--before", "default_reset",
    "--after", "hard_reset",
    "write_flash",
    "--flash_mode", "dio",
    "--flash_freq", "80m",
    "--flash_size", "detect",
    "0x0", $bootloader,
    "0x8000", $partitions,
    "0x10000", $firmware
)

Write-Host "Running command:" -ForegroundColor Yellow
Write-Host "$cmd $($args -join ' ')" -ForegroundColor Gray
Write-Host ""

# Run esptool
& $cmd $args

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=== Flash Successful! ===" -ForegroundColor Green
    Write-Host "You can now press RESET on your ESP32-S3 or open serial monitor." -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "=== Flash Failed ===" -ForegroundColor Red
    Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
