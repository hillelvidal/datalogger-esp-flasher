# ESP Datalogger Flasher (Windows)

This repository now contains a Windows-only WinForms application for flashing ESP32-based dataloggers. The previous Python/CLI implementation has been deprecated and removed.

## Project

- Main app: `winforms-flasher/`
  - Build: `.\winforms-flasher\build.bat` or `dotnet build .\winforms-flasher\ESPFlasher.csproj -c Release`
  - Run: `Start-Process ".\winforms-flasher\bin\Debug\net8.0-windows\ESPFlasher.exe"`
  - Docs: `winforms-flasher/README.md`

## Key Features

- Firestore integration to fetch firmware metadata (version, URL, notes, timestamp)
- Windows device detection (COM ports) for ESP boards
- Firmware download with local caching
- Flashing via `esptool.exe` with progress reporting

## Quick Start

1) Place your Firestore service account JSON in `winforms-flasher/` (do not commit this).
2) Put `esptool.exe` in `winforms-flasher/`.
3) Build and run the app from `winforms-flasher/`.

See `winforms-flasher/README.md` and `winforms-flasher/setup-guide.md` for details.

## License

MIT License - see [LICENSE](LICENSE) for details.
