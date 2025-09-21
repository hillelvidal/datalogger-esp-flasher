# ESP Datalogger Flasher - Complete Setup Guide

This guide will walk you through setting up the ESP Datalogger Flasher application from scratch.

## Step 1: Install Prerequisites

### 1.1 Install .NET 8.0 SDK
1. Go to [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Download and install the .NET 8.0 SDK for Windows
3. Verify installation by opening Command Prompt and running: `dotnet --version`

### 1.2 Install Visual Studio (Optional)
For development and debugging:
1. Download Visual Studio 2022 Community (free)
2. During installation, select ".NET desktop development" workload

## Step 2: Set Up Firebase Project

### 2.1 Create Firebase Project
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Create a project"
3. Enter project name (e.g., "esp-datalogger-firmware")
4. Disable Google Analytics (optional)
5. Click "Create project"

### 2.2 Enable Firestore Database
1. In your Firebase project, go to "Firestore Database"
2. Click "Create database"
3. Choose "Start in test mode" (for development)
4. Select a location close to your users
5. Click "Done"

### 2.3 Create Service Account
1. Go to Project Settings (gear icon) → "Service accounts"
2. Click "Generate new private key"
3. Click "Generate key" - this downloads a JSON file
4. Rename the file to `firebase-config.json`
5. Keep this file secure - it contains sensitive credentials

### 2.4 Set Up Firestore Collection
1. In Firestore Database, click "Start collection"
2. Collection ID: `firmware_versions`
3. Add your first document with these fields:

```
Document ID: (auto-generated)
Fields:
- version (string): "1.0.0"
- description (string): "Initial release"
- storageUrl (string): "https://your-storage-url/firmware_v1.0.0.bin"
- releaseDate (timestamp): (current date/time)
- fileSize (number): 2097152
- checksum (string): "your-sha256-hash"
- isLatest (boolean): true
```

## Step 3: Download Required Tools

### 3.1 Download esptool
1. Go to [esptool releases](https://github.com/espressif/esptool/releases)
2. Download the latest `esptool-vX.X.X-win64.zip`
3. Extract `esptool.exe` from the archive
4. Place it in your application directory

### 3.2 Install ESP32 Drivers (if needed)
Most modern Windows systems have built-in drivers, but you may need:
- **CP210x drivers**: [Silicon Labs website](https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers)
- **CH340 drivers**: Search for "CH340 Windows driver"

## Step 4: Build the Application

### 4.1 Clone/Download the Code
Place all the application files in a directory, e.g., `C:\ESP-Flasher\`

### 4.2 Configure Firebase
1. Copy your `firebase-config.json` to the application directory
2. Edit `MainForm.cs` line 67 and replace `"your-project-id"` with your actual Firebase project ID

### 4.3 Build the Application
1. Open Command Prompt in the application directory
2. Run: `build.bat`
3. Or manually run:
   ```cmd
   dotnet restore
   dotnet build --configuration Release
   dotnet publish --configuration Release --output publish
   ```

## Step 5: Set Up Firmware Storage

### 5.1 Choose Storage Solution
You can store firmware files in:
- **Firebase Storage** (recommended)
- **Google Cloud Storage**
- **Amazon S3**
- **Any web server with direct download links**

### 5.2 Firebase Storage Setup (Recommended)
1. In Firebase Console, go to "Storage"
2. Click "Get started"
3. Choose security rules (start in test mode)
4. Upload your firmware files
5. Get the download URLs for each file
6. Use these URLs in your Firestore documents

### 5.3 Update Firestore with Real URLs
Update your `firmware_versions` documents with actual storage URLs:
```
storageUrl: "https://firebasestorage.googleapis.com/v0/b/your-project.appspot.com/o/firmware%2Ffirmware_v1.0.0.bin?alt=media&token=your-token"
```

## Step 6: Test the Application

### 6.1 Connect ESP32 Device
1. Connect ESP32 to your computer via USB
2. Put device in download mode (hold BOOT button while connecting)
3. Verify device appears in Device Manager under "Ports (COM & LPT)"

### 6.2 Run the Application
1. Navigate to the `publish` directory
2. Run `ESPFlasher.exe`
3. Check that:
   - Firestore connection is successful
   - Firmware versions load in dropdown
   - ESP device is detected in the list

### 6.3 Test Flash Operation
1. Select a firmware version
2. Select your ESP device
3. Click "Flash Firmware"
4. Monitor progress and verify successful completion

## Step 7: Deployment

### 7.1 Create Installer (Optional)
You can create an installer using tools like:
- **Inno Setup** (free)
- **WiX Toolset** (free)
- **Advanced Installer** (commercial)

### 7.2 Distribute Application
Include these files in your distribution:
- `ESPFlasher.exe`
- `firebase-config.json`
- `esptool.exe`
- All DLL dependencies (automatically included by publish)
- `README.md`

## Troubleshooting Common Issues

### Issue: "Firebase connection failed"
**Solutions:**
- Verify `firebase-config.json` is correct and in the right location
- Check internet connection
- Ensure Firestore rules allow read access
- Verify project ID in code matches Firebase project

### Issue: "No ESP devices found"
**Solutions:**
- Install proper USB drivers (CP210x or CH340)
- Try different USB cable/port
- Put ESP32 in download mode
- Check Device Manager for COM ports

### Issue: "esptool.exe not found"
**Solutions:**
- Download esptool.exe and place in application directory
- Ensure file is not blocked by Windows (Properties → Unblock)
- Run application as Administrator if needed

### Issue: "Flash operation failed"
**Solutions:**
- Ensure ESP32 is in download mode
- Close other applications using the COM port
- Try lower baud rate
- Reset ESP32 and try again

## Security Considerations

### Firebase Security
- Use Firestore security rules to restrict access
- Consider using Firebase Authentication for user management
- Keep service account credentials secure
- Rotate keys periodically

### Application Security
- Code sign your executable for distribution
- Consider implementing update mechanisms
- Validate firmware files before flashing
- Log security-relevant events

## Advanced Configuration

### Custom Firestore Rules
```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /firmware_versions/{document} {
      allow read: if true; // Adjust based on your security needs
      allow write: if false; // Prevent unauthorized updates
    }
  }
}
```

### Environment-Specific Configuration
Create different Firebase projects for:
- Development
- Testing
- Production

Use different configuration files for each environment.

## Support and Maintenance

### Logging
The application logs to the console. For production, consider:
- File-based logging
- Centralized logging (e.g., Application Insights)
- Error reporting services

### Updates
Plan for:
- Application updates
- Firmware version management
- Database schema changes
- Security updates

This completes the setup guide. Your ESP Datalogger Flasher should now be ready for use!
