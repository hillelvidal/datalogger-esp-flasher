# ESP32 Firmware Flasher - Usage Guide

## Quick Start

### 1. Installation
```bash
# Install dependencies
pip install -r requirements.txt

# Or install as a package
pip install -e .
```

### 2. Basic Usage

#### Flash firmware (auto-detect device)
```bash
python -m src.main flash
```

#### List available ESP32 devices
```bash
python -m src.main list-devices
```

#### Verify firmware file
```bash
python -m src.main verify firmware/scanin_firmware_v2.1.3.bin
```

## Command Reference

### Flash Command
```bash
python -m src.main flash [OPTIONS]

Options:
  -p, --port TEXT     Specific serial port (e.g., COM3, /dev/ttyUSB0)
  -f, --firmware TEXT Path to firmware file
  -b, --baud INTEGER  Baud rate for flashing (default: 460800)
  --erase-all         Erase entire flash before writing
  --no-verify         Skip verification after flashing
  -y, --yes           Skip confirmation prompt
```

### Examples
```bash
# Flash with specific port
python -m src.main flash --port /dev/ttyUSB0

# Flash with custom firmware
python -m src.main flash --firmware my_firmware.bin

# Flash with full erase
python -m src.main flash --erase-all

# Flash without confirmation
python -m src.main flash --yes
```

### Other Commands

#### Get device information
```bash
python -m src.main info [--port PORT]
```

#### Erase flash memory
```bash
python -m src.main erase [--port PORT]
```

#### List all serial ports (debug)
```bash
python -m src.main list-devices --all
```

## Supported Devices

### ESP32 Chip Types
- ESP32 (original)
- ESP32-S2
- ESP32-S3
- ESP32-C3
- ESP32-C6

### USB-to-UART Bridges
- Silicon Labs CP210x series
- WCH CH340/CH341 series
- FTDI chips (FT232R, FT2232, etc.)
- Espressif native USB (S2/S3/C3)

## Troubleshooting

### No devices detected
1. Check USB cable connection
2. Ensure device is powered on
3. Install USB-to-UART drivers:
   - **CP210x**: Download from Silicon Labs
   - **CH340**: Download from WCH
   - **FTDI**: Usually included in OS
4. Try a different USB port
5. Put device in download mode (hold BOOT button while pressing RESET)

### Flash operation fails
1. Try lower baud rate: `--baud 115200`
2. Use full erase: `--erase-all`
3. Check firmware file validity: `verify firmware.bin`
4. Ensure device is in download mode
5. Try different USB cable

### Permission errors (Linux/macOS)
```bash
# Add user to dialout group (Linux)
sudo usermod -a -G dialout $USER

# Or use sudo
sudo python -m src.main flash
```

## Configuration

The application stores configuration in:
- **Windows**: `%APPDATA%/ESP32Flasher/`
- **macOS**: `~/Library/Application Support/ESP32Flasher/`
- **Linux**: `~/.config/esp32-flasher/`

## Logs

Logs are stored in:
- **Windows**: `%APPDATA%/ESP32Flasher/logs/`
- **macOS**: `~/Library/Logs/ESP32Flasher/`
- **Linux**: `~/.local/share/esp32-flasher/logs/`

## Firmware Files

Place firmware files in the `firmware/` directory. The application will automatically detect:
- `scanin_firmware*.bin` (preferred)
- Any `.bin` or `.elf` files

Firmware files are validated for:
- File size (1KB - 16MB)
- File format
- Checksum integrity
- Version detection from filename
