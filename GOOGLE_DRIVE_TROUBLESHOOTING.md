# Google Drive Integration - Troubleshooting Guide

## Issue: "No subfolders found"

If the log shows "No subfolders found after trying all patterns", follow these steps:

### Step 1: Verify Folder Sharing

1. Open your Google Drive folder: https://drive.google.com/drive/folders/1cThzyaIsmPvt0CIrETppBEbOmRr1REr-
2. Click the **Share** button (top right)
3. Under "General access", it should say **"Anyone with the link"**
4. Permission should be **"Viewer"**
5. If not, click "Change" and set it to public

### Step 2: Check the Debug HTML File

The app saves the downloaded HTML to help diagnose issues:

1. Open the log file (it opens automatically after refresh)
2. Look for a line like: `[GoogleDrive] HTML saved to: C:\Users\...\Temp\gdrive_debug_1cThzyaIsmPvt0CIrETppBEbOmRr1REr-.html`
3. Open that HTML file in a text editor
4. Search for your subfolder names (e.g., "v1.0.0")
5. If you DON'T see your folder names → folder is not publicly shared or empty
6. If you DO see them → scraping pattern needs updating

### Step 3: Manual Configuration (Workaround)

If automatic scraping fails, you can manually configure folder IDs:

1. Open each subfolder in Google Drive
2. Copy the URL - it looks like: `https://drive.google.com/drive/folders/ABC123XYZ`
3. The folder ID is the part after `folders/` (e.g., `ABC123XYZ`)
4. Edit `google-drive-config.json` in the app folder
5. Add your folders:

```json
{
  "folderUrl": "https://drive.google.com/drive/folders/1cThzyaIsmPvt0CIrETppBEbOmRr1REr-",
  "manualFolders": [
    {
      "name": "v1.0.0",
      "folderId": "ABC123XYZ"
    },
    {
      "name": "v2.0.0", 
      "folderId": "DEF456UVW"
    }
  ]
}
```

6. Rebuild the app

### Step 4: Check Folder Structure

Make sure your Google Drive folder has this structure:

```
Main Folder (1cThzyaIsmPvt0CIrETppBEbOmRr-)
├── v1.0.0/           ← Subfolder (must be a folder, not a file)
│   ├── firmware.bin
│   ├── bootloader.bin
│   └── partitions.bin
├── v2.0.0/           ← Another subfolder
│   └── firmware.bin
```

**Common mistakes:**
- ❌ Files directly in main folder (no subfolders)
- ❌ Subfolders are shortcuts/links instead of real folders
- ❌ Folder names with special characters

### Step 5: Test with Simple Folder

Create a test:

1. Create a new Google Drive folder
2. Make it publicly shared
3. Add ONE subfolder named "test"
4. Put a dummy `firmware.bin` file in it
5. Update the folder URL in `MainForm.cs` line 76
6. Rebuild and test

### Step 6: Alternative - Use Direct File URLs

If scraping continues to fail, you can bypass folder scanning entirely:

**Option A: Use Firestore** (recommended)
- Store firmware metadata in Firestore
- Put direct download URLs in the `url` field
- This is more reliable than scraping

**Option B: Use local firmware folder**
- Click "Browse Folder" in the app
- Select a local folder with your firmware files
- No cloud dependency

## Common Error Messages

### "Google Drive service is NULL"
- Check the log for initialization errors
- Folder URL might be invalid
- Check `MainForm.cs` line 76

### "HTTP error: 403"
- Folder is not publicly shared
- Check sharing settings

### "Pattern 1 found 0 potential matches"
- Google Drive HTML structure has changed
- Check debug HTML file
- Use manual configuration workaround

### "No firmware.bin found in folder"
- Subfolder was found but doesn't contain firmware files
- Check file names (must be exactly `firmware.bin`)
- Files might be in nested subfolders

## Getting Help

When reporting issues, include:

1. **Log file** (opens automatically after refresh)
2. **Debug HTML file** (path shown in log)
3. **Folder URL** you're using
4. **Screenshot** of your Google Drive folder structure
5. **Sharing settings** screenshot

## Quick Diagnostics Checklist

- [ ] Folder is publicly shared (Anyone with link → Viewer)
- [ ] Folder contains subfolders (not just files)
- [ ] Subfolders contain `firmware.bin` files
- [ ] Log file shows HTML was downloaded (>1000 characters)
- [ ] Debug HTML file contains your subfolder names
- [ ] No special characters in folder names
- [ ] Internet connection is working

## Known Limitations

- **Scraping is fragile** - Google can change HTML structure anytime
- **No authentication** - Only works with public folders
- **Rate limiting** - Google may block too many requests
- **Large folders** - May be slow with 50+ subfolders

## Recommended Solution

For production use, consider switching to **Firebase Storage** instead of Google Drive:
- More reliable API
- Better security (signed URLs)
- Proper authentication
- No scraping needed

See `GOOGLE_DRIVE_INTEGRATION.md` for implementation details.
