# Windows Deployment Guide

## Building Standalone Executable

### Prerequisites
- Python 3.8+ installed on Windows
- Git for Windows
- NSIS (Nullsoft Scriptable Install System) for installer creation

### Quick Build Process

1. **Clone the repository:**
   ```bash
   git clone https://github.com/hillelvidal/datalogger-esp-flasher.git
   cd datalogger-esp-flasher
   ```

2. **Install build dependencies:**
   ```bash
   pip install -r requirements-build.txt
   ```

3. **Run the build script:**
   ```bash
   python build_windows.py
   ```

This creates:
- `dist/esp32-flasher.exe` - Standalone executable (~50-80MB)
- `installer.nsi` - NSIS installer script
- `esp32-flasher.spec` - PyInstaller configuration

### Creating Windows Installer

1. **Install NSIS:**
   - Download from: https://nsis.sourceforge.io/
   - Install with default settings

2. **Build installer:**
   ```bash
   makensis installer.nsi
   ```

3. **Distribute:**
   - Share `ESP32-Firmware-Flasher-Setup.exe` with end users

## Driver Requirements

Windows users may need USB-to-UART drivers:

### CP210x Drivers (Silicon Labs)
- Download: https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers
- Supports: ESP32 DevKits, NodeMCU, Wemos D1

### CH340 Drivers
- Download: http://www.wch-ic.com/downloads/CH341SER_ZIP.html
- Supports: Many Chinese ESP32 boards

### FTDI Drivers
- Download: https://ftdichip.com/drivers/vcp-drivers/
- Usually pre-installed on Windows 10/11

## Installation Instructions for End Users

### Method 1: Installer (Recommended)
1. Download `ESP32-Firmware-Flasher-Setup.exe`
2. Right-click → "Run as administrator"
3. Follow installation wizard
4. Launch from Start Menu or Desktop shortcut

### Method 2: Portable Executable
1. Download `esp32-flasher.exe`
2. Place in desired folder
3. Run directly (no installation required)

## Usage on Windows

### Command Line
```cmd
# Flash firmware (auto-detect device)
esp32-flasher.exe flash

# List available devices
esp32-flasher.exe list-devices

# Flash with specific options
esp32-flasher.exe flash --port COM3 --baud 921600

# Get help
esp32-flasher.exe --help
```

### GUI Mode (Future Enhancement)
- Consider adding tkinter-based GUI for non-technical users
- File browser for firmware selection
- Visual device selection

## Troubleshooting

### Common Issues

**"Device not found"**
- Install appropriate USB drivers
- Check Device Manager for COM ports
- Try different USB cable/port

**"Access denied to COM port"**
- Close Arduino IDE, PuTTY, or other serial programs
- Run as administrator if needed

**"Flash failed"**
- Put ESP32 in download mode manually:
  1. Hold BOOT button
  2. Press RESET button
  3. Release BOOT button
- Try lower baud rate: `--baud 115200`

### Windows-Specific Notes
- COM ports use Windows naming (COM1, COM3, etc.)
- Some antivirus may flag the executable (false positive)
- Windows Defender SmartScreen may warn on first run

## Build Automation (GitHub Actions)

Future enhancement: Automated builds on every release tag.

```yaml
# .github/workflows/build-windows.yml
name: Build Windows Executable
on:
  release:
    types: [published]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Set up Python
        uses: actions/setup-python@v4
        with:
          python-version: '3.11'
      - name: Install dependencies
        run: pip install -r requirements-build.txt
      - name: Build executable
        run: python build_windows.py
      - name: Upload artifact
        uses: actions/upload-artifact@v3
        with:
          name: esp32-flasher-windows
          path: dist/esp32-flasher.exe
```

## Distribution Checklist

- [ ] Test executable on clean Windows machine
- [ ] Verify all firmware files are bundled
- [ ] Test COM port detection
- [ ] Verify driver installation instructions
- [ ] Create release notes
- [ ] Upload to GitHub releases
- [ ] Test installer creation and installation
- [ ] Document known limitations
