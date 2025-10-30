# Known Limitations

## Progress Bar During Flash

### Issue
The progress bar does not update in real-time during the firmware write phase (~60 seconds). Instead, it shows "Verifying firmware..." for the entire duration, then completes.

### Root Cause
- We use `esptool.exe` (PyInstaller-compiled executable)
- PyInstaller embeds the Python runtime and buffers all output
- Even with `PYTHONUNBUFFERED=1`, the output is not flushed until the process completes
- esptool detects it's not in a TTY and buffers stderr/stdout
- Windows ConPTY (pseudo-terminal) approach also didn't work with the PyInstaller executable

### What We Tried
1. ✅ `PYTHONUNBUFFERED=1` environment variable - **No effect**
2. ✅ Character-by-character reading with `\r` handling - **No effect**
3. ✅ Medallion.Shell with ConPTY - **No effect**
4. ❌ Calling Python directly (`python -u -m esptool`) - **Would work but requires Python installation**

### Current Behavior
- User sees "Verifying firmware..." message
- Flash completes successfully after ~60 seconds
- No indication of progress during write phase
- Final success/failure message appears correctly

### Impact
- **Low** - Flashing works perfectly, just no visual feedback
- Users might think the app is frozen during the 60-second flash
- Not a blocker for production use

### Workarounds Considered
1. **Call Python directly** - Requires Python on user's machine (not acceptable)
2. **Rebuild esptool.exe** - Requires maintaining custom esptool build (too much overhead)
3. **Add a spinner/animation** - Doesn't solve the core issue
4. **Show elapsed time** - Could help, but still no actual progress

### Recommendation
**Accept this limitation.** The app works perfectly for its core purpose (flashing ESP32-S3 devices). Real-time progress is a "nice-to-have" feature that would require significant complexity for minimal benefit.

### User Communication
Add a note in the UI: *"Flashing in progress... This may take up to 60 seconds. Please wait."*

This sets proper expectations without requiring code changes.

---

## Summary
✅ **App works perfectly**
✅ **Flashing is reliable**  
✅ **All features functional**
⚠️ **No real-time progress** (known limitation, low impact)

**Status: Ship it!** 🚀
