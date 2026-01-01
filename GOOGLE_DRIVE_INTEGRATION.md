# Google Drive Firmware Integration

## Overview

The ESP Flasher now supports pulling firmware directly from Google Drive public folders! This allows you to:
- Upload firmware folders to Google Drive
- Automatically scan and download new firmware versions
- No authentication required (uses public folder access)
- Works alongside existing Firestore and local firmware options

## Setup Instructions

### 1. Google Drive Folder Structure

Your Google Drive folder should be organized like this:

```
Your Public Folder (1cThzyaIsmPvt0CIrETppBEbOmRr1REr-)
├── v1.0.0/
│   ├── firmware.bin
│   ├── bootloader.bin
│   └── partitions.bin
├── v1.1.0/
│   ├── firmware.bin
│   ├── bootloader.bin
│   └── partitions.bin
└── v2.0.0/
    ├── firmware.bin
    ├── bootloader.bin
    └── partitions.bin
```

**Important:**
- Each subfolder name becomes the version name (e.g., "v1.0.0")
- Each subfolder MUST contain at least `firmware.bin`
- Optionally include `bootloader.bin` and `partitions.bin`
- Folder must be publicly shared (Anyone with the link can view)

### 2. Configure Your Folder URL

The Google Drive folder URL is currently hardcoded in `MainForm.cs` line 76:

```csharp
var googleDriveFolderUrl = "https://drive.google.com/drive/folders/1cThzyaIsmPvt0CIrETppBEbOmRr1REr-?usp=sharing";
```

To change it:
1. Open your Google Drive folder
2. Click "Share" → "Copy link"
3. Replace the URL in `MainForm.cs`
4. Rebuild the application

## How It Works

### Architecture

1. **GoogleDriveService** (`Services/GoogleDriveService.cs`)
   - Scrapes the public Google Drive folder page
   - Extracts subfolder names and file IDs
   - Creates `FirmwareVersion` objects for each valid firmware folder
   - Generates direct download URLs for each file

2. **FirmwareDownloadService** (Enhanced)
   - Detects Google Drive URLs
   - Handles Google Drive redirect pages (virus scan warnings)
   - Downloads files to local cache
   - Validates checksums and file sizes

3. **MainForm** (Enhanced)
   - Initializes Google Drive service on startup
   - "Refresh Firmware" button now scans:
     - Local firmware folders
     - Google Drive (fast, no auth)
     - Firestore (optional, if configured)
   - Displays all firmware in dropdown

### Download Flow

```
User clicks "Refresh Firmware"
    ↓
Scan Google Drive folder for subfolders
    ↓
For each subfolder, scan for .bin files
    ↓
Create FirmwareVersion with download URLs
    ↓
Add to dropdown
    ↓
User selects firmware and clicks "Flash"
    ↓
Download files from Google Drive (if not cached)
    ↓
Flash to ESP32
```

## Usage

### For End Users

1. **Launch the application**
2. **Click the "Refresh" button in the FIRMWARE section** (top section, next to firmware dropdown)
   - **IMPORTANT:** There are TWO "Refresh" buttons - use the one next to the firmware dropdown, NOT the device refresh button
   - App will scan Google Drive for firmware
   - All available versions appear in dropdown
   - Check the logs/console for detailed scanning progress
3. **Select desired firmware version**
   - Status shows if already downloaded or will download
4. **Select ESP device**
5. **Click "Flash Firmware"**
   - Firmware downloads automatically if needed
   - Progress bar shows download/flash progress

### For Firmware Developers

1. **Create a new version folder** in Google Drive
   - Name it with version number (e.g., "v2.1.0")
2. **Upload the firmware files**:
   - `firmware.bin` (required)
   - `bootloader.bin` (optional)
   - `partitions.bin` (optional)
3. **Done!** Users can now see and download this version

## Technical Details

### Web Scraping Approach

Since Google Drive API requires authentication even for public folders, we use web scraping:

- Fetches the public folder HTML page
- Extracts embedded JSON data using regex patterns
- Parses folder/file IDs and names
- Constructs direct download URLs

**Pros:**
- No API key required
- No authentication needed
- Works with any public folder
- Simple to implement

**Cons:**
- May break if Google changes HTML structure
- Slower than API calls
- No metadata like upload dates (uses current date)

### Direct Download URLs

Google Drive direct download URL format:
```
https://drive.google.com/uc?export=download&id={FILE_ID}
```

For large files (>100MB), Google shows a virus scan warning. The `FirmwareDownloadService` automatically:
1. Detects the warning page
2. Extracts the confirmation URL
3. Follows the redirect to download

### Security Considerations

**Current Implementation:**
- ✅ Public folder access (no credentials exposed)
- ✅ HTTPS downloads
- ✅ SHA256 checksum validation (if provided in metadata)
- ✅ File size validation
- ⚠️ No signature verification (firmware not cryptographically signed)
- ⚠️ URLs can be shared/leaked
- ⚠️ No access control (anyone with link can download)

**Recommendations for Production:**
1. Consider adding firmware signature verification
2. Use Firebase Storage with signed URLs for better security
3. Implement version pinning to prevent rollback attacks
4. Add rate limiting to prevent abuse

## Troubleshooting

### No Firmware Found

**Possible causes:**
- Google Drive folder is not publicly shared
- Folder URL is incorrect
- Subfolders don't contain `firmware.bin`
- Internet connection issues

**Solutions:**
1. Verify folder sharing settings (Anyone with link → Viewer)
2. Check folder URL in `MainForm.cs`
3. Ensure each subfolder has `firmware.bin`
4. Check application logs for errors

### Download Fails

**Possible causes:**
- Google Drive rate limiting
- Large file virus scan redirect not handled
- Network issues

**Solutions:**
1. Wait a few minutes and retry
2. Check logs for specific error messages
3. Try downloading manually to verify file accessibility

### Scraping Pattern Broken

If Google changes their HTML structure:

1. Open `GoogleDriveService.cs`
2. Update regex patterns in:
   - `GetSubfoldersByScraping()` - line 86
   - `GetFilesInFolderAsync()` - line 183
3. Test with your folder
4. Rebuild application

## Files Modified

### New Files
- `Services/GoogleDriveService.cs` - Google Drive integration service

### Modified Files
- `Services/FirmwareDownloadService.cs` - Added Google Drive redirect handling
- `MainForm.cs` - Integrated Google Drive scanning into firmware refresh

## Future Enhancements

Potential improvements:
- [ ] Make folder URL configurable via settings UI
- [ ] Support multiple Google Drive folders
- [ ] Add firmware signature verification
- [ ] Cache folder listings to reduce scraping
- [ ] Add manual refresh for specific folders
- [ ] Support Google Drive API with OAuth (optional)
- [ ] Add progress indicator for folder scanning
- [ ] Support nested folder structures

## License

Same as main project (MIT License)
