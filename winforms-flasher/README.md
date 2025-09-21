# ESP Datalogger Flasher - Windows Forms Application

A modern Windows Forms application for flashing ESP32 firmware with Firestore integration, automatic device detection, and progress tracking.

## Features

- **Firestore Integration**: Automatically fetches firmware versions from your Firestore database
- **Smart Device Detection**: Automatically detects connected ESP32 devices via USB
- **Firmware Management**: Downloads and caches firmware files locally
- **Progress Tracking**: Real-time progress updates during download and flashing
- **Modern UI**: Clean, intuitive Windows Forms interface
- **Error Handling**: Comprehensive error handling with user-friendly messages

## Prerequisites

- Windows 10 or later
- .NET 8.0 Runtime
- ESP32 device with USB connection
- Firebase project with Firestore database
- esptool.exe (included in the application)

## Setup Instructions

### 1. Firebase Configuration

1. Create a Firebase project at [https://console.firebase.google.com/](https://console.firebase.google.com/)
2. Enable Firestore Database
3. Create a service account:
   - Go to Project Settings → Service Accounts
   - Click "Generate new private key"
   - Download the JSON file
4. Rename the downloaded file to `firebase-config.json` and place it in the application directory
5. Update the project ID in `MainForm.cs` (line 67):
   ```csharp
   _firestoreService = new FirestoreService("your-actual-project-id", configPath, _logger);
   ```

### 2. Firestore Database Structure

Create a collection named `firmware_versions` with documents containing:

```json
{
  "version": "2.1.3",
  "description": "Bug fixes and performance improvements",
  "storageUrl": "https://your-storage-url/firmware_v2.1.3.bin",
  "releaseDate": "2024-01-15T10:30:00Z",
  "fileSize": 2097152,
  "checksum": "sha256-hash-of-the-file",
  "isLatest": true
}
```

### 3. ESP Tool Setup

1. Download `esptool.exe` from [https://github.com/espressif/esptool/releases](https://github.com/espressif/esptool/releases)
2. Place `esptool.exe` in the application directory
3. Ensure the file is not blocked by Windows (Right-click → Properties → Unblock)

## Building the Application

1. Install .NET 8.0 SDK
2. Open terminal in the project directory
3. Run the following commands:

```bash
dotnet restore
dotnet build --configuration Release
```

## Running the Application

1. Ensure all configuration files are in place
2. Connect your ESP32 device via USB
3. Run the application:

```bash
dotnet run
```

Or run the compiled executable from the `bin/Release/net8.0-windows/` directory.

## Usage

1. **Launch the Application**: The app will automatically connect to Firestore and load firmware versions
2. **Select Firmware**: Choose the desired firmware version from the dropdown (latest is selected by default)
3. **Detect Devices**: Click "Refresh" to scan for connected ESP devices
4. **Select Device**: Choose the target ESP device from the list
5. **Flash Firmware**: Click "Flash Firmware" to begin the process

The application will:
- Download the firmware if not already cached
- Erase the ESP flash memory
- Write the new firmware
- Verify the installation
- Show progress throughout the process

## Troubleshooting

### No ESP Devices Found
- Check USB cable connection
- Ensure ESP32 is in download mode (hold BOOT button while connecting)
- Install CP210x or CH340 drivers if needed
- Try a different USB port

### Firestore Connection Issues
- Verify `firebase-config.json` is correctly configured
- Check internet connection
- Ensure Firestore rules allow read access
- Verify project ID is correct

### Flash Operation Fails
- Ensure device is not in use by another application
- Try a different USB cable
- Reset the ESP32 device
- Check if esptool.exe is present and not blocked

### Permission Issues
- Run the application as Administrator
- Ensure antivirus software isn't blocking the application
- Check Windows Defender exclusions

## File Structure

```
ESPFlasher/
├── Models/
│   ├── FirmwareVersion.cs      # Firmware version data model
│   └── EspDevice.cs            # ESP device data model
├── Services/
│   ├── FirestoreService.cs     # Firestore integration
│   ├── DeviceDetectionService.cs # ESP device detection
│   ├── FirmwareDownloadService.cs # Firmware download/cache
│   └── EspFlashingService.cs   # ESP flashing operations
├── MainForm.cs                 # Main application form
├── MainForm.Designer.cs        # Form designer code
├── Program.cs                  # Application entry point
├── ESPFlasher.csproj          # Project file
├── firebase-config.json        # Firebase configuration
├── esptool.exe                # ESP flashing tool
└── README.md                  # This file
```

## Dependencies

- **Google.Cloud.Firestore**: Firestore database integration
- **System.IO.Ports**: Serial port communication
- **Newtonsoft.Json**: JSON serialization
- **Microsoft.Extensions.Logging**: Logging framework

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review the application logs
3. Ensure all prerequisites are met
4. Contact your system administrator for Firebase/network issues
