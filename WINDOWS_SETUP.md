# Windows Setup Instructions

Quick guide to install the ESP Flasher app on a new Windows laptop.

---

## Prerequisites

1. **Git for Windows** - [Download here](https://git-scm.com/download/win)
   - Install with default settings
   - This includes Git Bash terminal

2. **Visual Studio 2022** (Community Edition is free)
   - [Download here](https://visualstudio.microsoft.com/downloads/)
   - During installation, select: **.NET desktop development** workload
   - This includes .NET 8 SDK and all required build tools

---

## Installation Steps

### 1. Open Command Prompt or PowerShell

Press `Win + R`, type `cmd` or `powershell`, press Enter

### 2. Clone the Repository

**Option A: Public clone (no GitHub login needed)**
```cmd
cd C:\
git clone https://github.com/hillelvidal/datalogger-esp-flasher.git
cd datalogger-esp-flasher
```

**Option B: If repository is private (requires GitHub login)**
```cmd
cd C:\
git clone https://github.com/hillelvidal/datalogger-esp-flasher.git
```
- You'll be prompted to sign in to GitHub
- A browser window will open for authentication
- Complete the login and return to terminal

### 3. Run the Deployment Script

The `redeploy.bat` script does **everything automatically**:
- Restores NuGet packages
- Builds the project
- Creates the executable
- **Sets up shared bootloader/partitions files** in `Documents\ESP-Firmwares\_common\`
- No manual dependency installation or file copying needed!

```cmd
redeploy.bat
```

**What it does:**
1. Cleans previous builds
2. Restores all NuGet dependencies (Newtonsoft.Json, Google.Cloud.Firestore, etc.)
3. Builds the WinForms app in Release mode
4. Creates `winforms-flasher.exe` in `winforms-flasher\bin\Release\net8.0-windows\`
5. Copies shared `bootloader.bin` and `partitions.bin` to `%USERPROFILE%\Documents\ESP-Firmwares\_common\`

### 4. Run the Application

After successful build:

```cmd
cd winforms-flasher\bin\Release\net8.0-windows
winforms-flasher.exe
```

Or double-click `winforms-flasher.exe` from File Explorer.

---

## Troubleshooting

### "Git is not recognized"
- Install Git for Windows from link above
- Restart terminal after installation

### "MSBuild is not recognized"
- Install Visual Studio 2022 with .NET desktop development workload
- Restart terminal after installation
- Or add MSBuild to PATH: `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin`

### Build Errors
1. Make sure you have .NET 8 SDK installed (comes with VS 2022)
2. Check `redeploy.bat` output for specific errors
3. Try running as Administrator if permission issues occur

### First Run Setup
1. **Firmware Folder**: Click "Change..." to set where firmwares are stored
2. **Google Drive URL**: Enter your public firmware folder URL (optional)
3. **Scan Local**: Click to find local firmwares
4. **Scan Cloud**: Click to find Google Drive firmwares

---

## Quick Start After Installation

1. **Connect ESP32-S3** via USB
2. **Refresh Devices** - Click to detect COM port
3. **Scan Cloud** - Download latest firmware from Google Drive
4. **Select firmware** from list
5. **Download** if needed (cloud firmwares)
6. **Select device** from list
7. **Flash** - Put ESP32 in boot mode and flash!

---

## Updating the App

To get latest changes:

```cmd
cd C:\datalogger-esp-flasher
git pull
redeploy.bat
```

---

## File Structure After Installation

```
C:\datalogger-esp-flasher\
├── winforms-flasher\
│   ├── bin\
│   │   └── Release\
│   │       └── net8.0-windows\
│   │           └── winforms-flasher.exe  ← Run this!
│   ├── MainForm.cs
│   ├── Services\
│   └── ...
├── redeploy.bat  ← Build script
└── README.md
```

---

## Default Firmware Storage Location

**Automatically created by `redeploy.bat`:**
```
%USERPROFILE%\Documents\ESP-Firmwares\
```

Typically: `C:\Users\YourName\Documents\ESP-Firmwares\`

**Structure after first run:**
```
Documents\ESP-Firmwares\
├── _common\
│   ├── bootloader.bin      ← Auto-copied by redeploy.bat
│   └── partitions.bin      ← Auto-copied by redeploy.bat
├── firmware-20260108.4\    ← Downloaded from cloud
│   ├── scanin-datalogger-20260108.4.bin
│   └── build-info.txt
└── firmware-20260108.5\    ← Downloaded from cloud
    ├── scanin-datalogger-20260108.5.bin
    └── build-info.txt
```

The app will use this location by default. You can change it in the app settings if needed.

---

## Need Help?

- Check build output in terminal for specific errors
- Ensure all prerequisites are installed
- Run `redeploy.bat` as Administrator if needed
- Contact dev team if issues persist

---

**Last Updated:** January 8, 2026
