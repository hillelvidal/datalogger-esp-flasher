# Shared ESP32 Files

This folder contains the shared bootloader and partitions files required for ESP32-S3 flashing.

## Files

- `bootloader.bin` - ESP32-S3 bootloader (required for flashing)
- `partitions.bin` - Partition table (required for flashing)

## Usage

These files are automatically copied to the firmware folder's `_common/` directory when you run `redeploy.bat`.

The flasher app uses these shared files for all firmware versions, so they only need to be stored once.

## Important

**DO NOT DELETE** these files - they are critical for ESP32 flashing to work!

If you need to update these files, replace them here in the repo and run `redeploy.bat` again.
