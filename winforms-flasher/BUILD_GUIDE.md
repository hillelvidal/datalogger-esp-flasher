# Windows App Build Guide

## Prerequisites

1. **.NET 8.0 SDK** - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Choose "Windows x64" installer
   - Verify installation: `dotnet --version` (should show 8.0.x)

2. **Visual Studio 2022** (Optional, but recommended)
   - Download Community Edition (free): https://visualstudio.microsoft.com/downloads/
   - During installation, select ".NET desktop development" workload

## Build Methods

### Method 1: Command Line (No Visual Studio Required)

1. Open PowerShell or Command Prompt
2. Navigate to the winforms-flasher directory:
   ```powershell
   cd c:\Users\user\Documents\GitHub\datalogger-esp-flasher\winforms-flasher
   ```

3. Build the project:
   ```powershell
   dotnet build --configuration Release
   ```

4. The executable will be in:
   ```
   bin\Release\net8.0-windows\ESPFlasher.exe
   ```

5. Run it:
   ```powershell
   .\bin\Release\net8.0-windows\ESPFlasher.exe
   ```

### Method 2: Visual Studio 2022

1. Open `ESPFlasher.sln` (or `ESPFlasher.csproj`) in Visual Studio
2. Select "Release" configuration from the dropdown (top toolbar)
3. Press `Ctrl+Shift+B` to build, or click **Build → Build Solution**
4. Press `F5` to run, or click the green "Start" button

### Method 3: VS Code (with C# extension)

1. Install the **C# Dev Kit** extension in VS Code
2. Open the `winforms-flasher` folder in VS Code
3. Press `Ctrl+Shift+B` and select "build"
4. Or use the terminal:
   ```powershell
   dotnet build --configuration Release
   ```

## Publishing (Creating Standalone Executable)

To create a self-contained executable that doesn't require .NET to be installed:

```powershell
cd c:\Users\user\Documents\GitHub\datalogger-esp-flasher\winforms-flasher
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true
```

The standalone executable will be in:
```
bin\Release\net8.0-windows\win-x64\publish\ESPFlasher.exe
```

This single file can be copied to any Windows machine without requiring .NET installation.

## Quick Build Script

Use the provided `build.bat` script:

```cmd
cd c:\Users\user\Documents\GitHub\datalogger-esp-flasher\winforms-flasher
build.bat
```

This will build and create a standalone executable automatically.

## Troubleshooting

### "dotnet command not found"
- Install .NET 8.0 SDK (see Prerequisites)
- Restart your terminal after installation

### "The project file could not be loaded"
- Make sure you're in the `winforms-flasher` directory
- Check that `ESPFlasher.csproj` exists

### "Could not find a part of the path"
- Ensure all NuGet packages are restored: `dotnet restore`

### Build errors about missing packages
- Run: `dotnet restore`
- If still failing, delete `bin` and `obj` folders and rebuild

## Running the App

After building, the app requires:
1. **esptool.exe** in the same directory (already included)
2. **Firebase service account JSON** (place as `firebase-config.json` or any `*.json` file with `project_id`)
3. **ESP32-S3 connected** to a USB port

## Logging

Logs are written to the console when running from terminal. To see detailed logs:

```powershell
$env:DOTNET_ENVIRONMENT="Development"
.\bin\Release\net8.0-windows\ESPFlasher.exe
```

Or check Windows Event Viewer for application logs.

## Next Steps

After successful build:
1. Place your firmware files (`bootloader.bin`, `partitions.bin`, `firmware.bin`) in a folder
2. Configure Firebase credentials
3. Run the app and test flashing to ESP32-S3
