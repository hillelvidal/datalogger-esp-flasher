# ESP32 Datalogger Flasher

A tool for flashing firmware to ESP32-based datalogger devices.

## Features

- Simple and intuitive command-line interface
- Support for multiple ESP32 models
- Automatic port detection
- Progress feedback during flashing
- Configurable flash settings

## Requirements

- Python 3.7+
- esptool.py
- pyserial
- PlatformIO (optional, for building from source)

## Installation

1. Clone this repository:
   ```bash
   git clone https://github.com/yourusername/datalogger-esp-flasher.git
   cd datalogger-esp-flasher
   ```

2. Install the required Python packages:
   ```bash
   pip install -r requirements.txt
   ```

## Usage

```bash
python esp_flasher.py [options]
```

### Options

- `-p, --port`: Specify the serial port
- `-b, --baud`: Set the baud rate (default: 460800)
- `-f, --firmware`: Path to the firmware binary
- `--erase`: Perform a full chip erase before flashing
- `-v, --verbose`: Enable verbose output

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) before submitting pull requests.
