# Google Drive Firmware - Quick Start Guide

## For You (Firmware Publisher)

### Step 1: Organize Your Google Drive Folder

Your folder structure should look like this:

```
📁 Your Public Folder
  📁 v1.0.0
    📄 firmware.bin
    📄 bootloader.bin
    📄 partitions.bin
  📁 v1.1.0
    📄 firmware.bin
    📄 bootloader.bin
    📄 partitions.bin
  📁 v2.0.0
    📄 firmware.bin
    📄 bootloader.bin
    📄 partitions.bin
```

### Step 2: Share the Folder Publicly

1. Right-click the folder in Google Drive
2. Click "Share"
3. Click "Change to anyone with the link"
4. Set permission to "Viewer"
5. Copy the link

### Step 3: Update the App (One-time)

The folder URL is already configured in your app:
```
https://drive.google.com/drive/folders/1cThzyaIsmPvt0CIrETppBEbOmRr1REr-?usp=sharing
```

If you want to change it, edit `MainForm.cs` line 76.

### Step 4: Build the App

```bash
cd winforms-flasher
dotnet build -c Release
```

### Log Files

All activity is logged to: `%LocalAppData%\ESPFlasher\Logs\flasher_YYYYMMDD_HHMMSS.log`

The log file automatically opens after clicking "Refresh Firmware" to show you what happened.

## For End Users

### Using the Flasher

1. **Launch ESPFlasher.exe**

2. **Click the "Refresh" button next to the FIRMWARE dropdown** (top section)
   - ⚠️ **IMPORTANT:** There are TWO "Refresh" buttons in the app!
   - Use the **Firmware Refresh** button (top section, next to firmware dropdown)
   - NOT the Device Refresh button (middle section, next to device list)
   - App scans Google Drive automatically
   - All firmware versions appear in dropdown
   - **Log file opens automatically** showing detailed scan progress

3. **Select firmware version**
   - Shows download status
   - "✓ Downloaded" or "⬇ Will download"

4. **Connect ESP32 device via USB**

5. **Click "Refresh" in the DEVICES section**
   - Detects connected ESP32

6. **Click "Flash Firmware"**
   - Downloads from Google Drive if needed
   - Flashes to device
   - Shows progress

## Adding New Firmware (Publisher)

1. Create new folder in Google Drive (e.g., "v2.1.0")
2. Upload your 3 files:
   - firmware.bin
   - bootloader.bin
   - partitions.bin
3. Done! Users will see it when they click "Refresh Firmware"

## What Changed?

### Before
- Firmware only from Firestore (required Firebase setup)
- Manual local folder selection

### After
- ✅ Google Drive (no auth, public folder)
- ✅ Firestore (optional, if configured)
- ✅ Local folders (browse manually)

All three sources work together!

## Troubleshooting

**"No firmware found"**
- Check folder is publicly shared
- Verify subfolders contain firmware.bin
- Check internet connection

**Download fails**
- Google Drive may rate limit
- Try again in a few minutes
- Check file is accessible manually

**Scraping fails**
- Google may have changed their HTML
- Check logs for errors
- May need to update regex patterns

## Current Configuration

Your Google Drive folder ID: `1cThzyaIsmPvt0CIrETppBEbOmRr1REr-`

The app will automatically:
- Scan this folder on "Refresh Firmware"
- List all subfolders as firmware versions
- Download files when needed
- Cache locally to avoid re-downloads
