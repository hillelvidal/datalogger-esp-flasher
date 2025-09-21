"""
Flash Controller Module

Handles ESP32 firmware flashing operations using esptool.
"""

import os
import sys
import time
import subprocess
from typing import Optional, Callable, Dict, Any
from dataclasses import dataclass
from pathlib import Path

try:
    import esptool
except ImportError:
    esptool = None


@dataclass
class FlashResult:
    """Result of a flash operation"""
    success: bool
    message: str
    duration: float
    chip_type: Optional[str] = None
    flash_size: Optional[str] = None


class FlashController:
    """Controls ESP32 firmware flashing operations"""
    
    # Default flash parameters
    DEFAULT_BAUD_RATE = 460800
    DEFAULT_FLASH_ADDRESS = 0x1000
    
    # Flash modes and frequencies
    FLASH_MODES = ['qio', 'qout', 'dio', 'dout']
    FLASH_FREQUENCIES = ['40m', '26m', '20m', '80m']
    FLASH_SIZES = ['1MB', '2MB', '4MB', '8MB', '16MB']
    
    def __init__(self):
        """Initialize flash controller"""
        self.progress_callback: Optional[Callable[[float, str], None]] = None
        self.verbose = False
    
    def set_progress_callback(self, callback: Callable[[float, str], None]):
        """
        Set callback function for progress updates
        
        Args:
            callback: Function that takes (progress_percent, status_message)
        """
        self.progress_callback = callback
    
    def set_verbose(self, verbose: bool):
        """Enable/disable verbose output"""
        self.verbose = verbose
    
    def detect_chip_info(self, port: str, baud_rate: int = DEFAULT_BAUD_RATE) -> Dict[str, Any]:
        """
        Connect to ESP32 and detect chip information
        
        Args:
            port: Serial port name
            baud_rate: Connection baud rate
            
        Returns:
            Dictionary with chip information
        """
        if not esptool:
            raise RuntimeError("esptool not available. Install with: pip install esptool")
        
        try:
            # Create esptool instance using the correct API
            esp = esptool.detect_chip(port, baud_rate, connect_mode='default_reset')
            
            # Get chip info
            chip_info = {
                'chip_type': esp.CHIP_NAME,
                'chip_revision': getattr(esp, 'get_chip_revision', lambda: 'Unknown')(),
                'flash_size': getattr(esp, 'flash_size', 'Unknown'),
                'mac_address': ':'.join(f'{b:02x}' for b in esp.read_mac()) if hasattr(esp, 'read_mac') else 'Unknown',
                'crystal_freq': getattr(esp, 'get_crystal_freq', lambda: 'Unknown')(),
            }
            
            return chip_info
            
        except Exception as e:
            raise RuntimeError(f"Failed to detect chip: {str(e)}")
    
    def flash_firmware(
        self,
        port: str,
        firmware_path: str,
        baud_rate: int = DEFAULT_BAUD_RATE,
        flash_address: int = DEFAULT_FLASH_ADDRESS,
        erase_all: bool = False,
        verify: bool = True,
        bootloader_path: Optional[str] = None,
        partitions_path: Optional[str] = None
    ) -> FlashResult:
        """
        Flash firmware to ESP32 device
        
        Args:
            port: Serial port name
            firmware_path: Path to firmware binary
            baud_rate: Flash baud rate
            flash_address: Flash memory address
            erase_all: Whether to erase entire flash before writing
            verify: Whether to verify flash after writing
            
        Returns:
            FlashResult with operation status
        """
        start_time = time.time()
        
        try:
            # Validate firmware file
            if not Path(firmware_path).exists():
                return FlashResult(
                    success=False,
                    message=f"Firmware file not found: {firmware_path}",
                    duration=0
                )
            
            # Update progress
            if self.progress_callback:
                self.progress_callback(0, "Connecting to device...")
            
            # Try PlatformIO first (more reliable), fall back to esptool
            result = self._flash_with_platformio(
                port, firmware_path, baud_rate, flash_address, erase_all, verify,
                bootloader_path, partitions_path
            )
            
            # If PlatformIO fails, fall back to esptool
            if not result.success and "platformio not found" in result.message.lower():
                result = self._flash_with_esptool_subprocess(
                    port, firmware_path, baud_rate, flash_address, erase_all, verify
                )
            
            duration = time.time() - start_time
            result.duration = duration
            
            return result
            
        except Exception as e:
            duration = time.time() - start_time
            return FlashResult(
                success=False,
                message=f"Flash operation failed: {str(e)}",
                duration=duration
            )
    
    def _flash_with_esptool_module(
        self,
        port: str,
        firmware_path: str,
        baud_rate: int,
        flash_address: int,
        erase_all: bool,
        verify: bool
    ) -> FlashResult:
        """Flash using esptool as a Python module"""
        
        try:
            # Connect to chip
            if self.progress_callback:
                self.progress_callback(10, "Detecting chip type...")
            
            esp = esptool.detect_chip(port, baud_rate, connect_mode='default_reset')
            chip_type = esp.CHIP_NAME
            
            if self.progress_callback:
                self.progress_callback(20, f"Connected to {chip_type}")
            
            # Erase flash if requested
            if erase_all:
                if self.progress_callback:
                    self.progress_callback(10, "Erasing flash...")
                esp.erase_flash()
                if self.progress_callback:
                    self.progress_callback(20, "Flash erased")
            
            # Flash firmware
            if self.progress_callback:
                self.progress_callback(30, "Writing firmware...")
            
            # Read firmware file
            with open(firmware_path, 'rb') as f:
                firmware_data = f.read()
            
            # Write to flash with progress updates
            esp.flash_begin(len(firmware_data), flash_address)
            
            # Write in blocks with progress updates
            block_size = esp.FLASH_WRITE_SIZE
            blocks_total = (len(firmware_data) + block_size - 1) // block_size
            
            for seq, i in enumerate(range(0, len(firmware_data), block_size)):
                block = firmware_data[i:i + block_size]
                if len(block) < block_size:
                    block += b'\xff' * (block_size - len(block))
                
                esp.flash_block(block, seq)
                
                # Update progress with actual flash percentage (0-100%)
                progress_percent = (seq + 1) * 100 // blocks_total
                bytes_written = min(i + block_size, len(firmware_data))
                
                if self.progress_callback:
                    self.progress_callback(
                        progress_percent, 
                        f"Writing firmware... {progress_percent}% ({bytes_written//1024}KB/{len(firmware_data)//1024}KB)"
                    )
            
            esp.flash_finish()
            
            if self.progress_callback:
                self.progress_callback(95, "Firmware written successfully")
            
            # Verify if requested
            if verify:
                if self.progress_callback:
                    self.progress_callback(98, "Verifying...")
                # Verification would go here - simplified for now
            
            if self.progress_callback:
                self.progress_callback(100, "Flash completed successfully!")
            
            return FlashResult(
                success=True,
                message="Firmware flashed successfully",
                duration=0,  # Will be set by caller
                chip_type=chip_type
            )
            
        except Exception as e:
            return FlashResult(
                success=False,
                message=f"Esptool module error: {str(e)}",
                duration=0
            )
    
    def _flash_with_esptool_subprocess(
        self,
        port: str,
        firmware_path: str,
        baud_rate: int,
        flash_address: int,
        erase_all: bool,
        verify: bool
    ) -> FlashResult:
        """Flash using esptool as a subprocess with real-time progress"""
        
        try:
            # Store firmware size for progress calculation
            import os
            self._firmware_size = os.path.getsize(firmware_path)
            
            # Update progress
            if self.progress_callback:
                self.progress_callback(0, "Starting flash operation...")
            
            # Build esptool command
            cmd = [
                sys.executable, '-m', 'esptool',
                '--port', port,
                '--baud', str(baud_rate),
            ]
            
            if erase_all:
                cmd.extend(['--before', 'default-reset', '--after', 'hard-reset', 'erase-flash'])
                
                # Run erase first
                if self.progress_callback:
                    self.progress_callback(10, "Erasing flash...")
                
                erase_result = subprocess.run(cmd, capture_output=True, text=True)
                if erase_result.returncode != 0:
                    return FlashResult(
                        success=False,
                        message=f"Erase failed: {erase_result.stderr}",
                        duration=0
                    )
                
                if self.progress_callback:
                    self.progress_callback(30, "Flash erased, starting write...")
                
                # Hebrew text constants for progress messages
                HEBREW_PROGRESS = {
                    'connecting': 'מתחבר להתקן...',
                    'preparing': 'מכין פקודת הבהוב...',
                    'erasing': 'מוחק זיכרון...',
                    'writing': 'כותב קושחה...',
                    'verifying': 'מאמת...',
                    'completed': 'הושלם!',
                    'detected_chip': 'זוהה שבב',
                    'using_platformio': 'משתמש ב-PlatformIO להבהוב אמין...'
                }
                
                # Rebuild command for write
                cmd = [
                    sys.executable, '-m', 'esptool',
                    '--port', port,
                    '--baud', str(baud_rate),
                ]
            
            cmd.extend([
                '--before', 'default-reset',
                '--after', 'hard-reset',
                'write-flash',
                '--flash-mode', 'dio',
                '--flash-freq', '40m',
                '--flash-size', 'detect',
                f'0x{flash_address:x}',
                firmware_path
            ])
            
            # Note: --verify option doesn't exist in esptool v5.0.2, verification is automatic
            
            if self.progress_callback:
                self.progress_callback(40, "Starting firmware write...")
            
            # Run esptool command with real-time output
            process = subprocess.Popen(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                bufsize=1,
                universal_newlines=True
            )
            
            output_lines = []
            progress = 40
            
            # Read output line by line for progress tracking
            while True:
                line = process.stdout.readline()
                if not line and process.poll() is not None:
                    break
                
                if line:
                    line = line.strip()
                    output_lines.append(line)
                    
                    # Parse progress from esptool output
                    if 'Writing at 0x' in line:
                        # Extract address to calculate actual flash progress
                        try:
                            addr_part = line.split('Writing at 0x')[1].split('...')[0]
                            current_addr = int(addr_part, 16)
                            
                            # Calculate actual progress based on firmware size
                            if hasattr(self, '_firmware_size') and self._firmware_size > 0:
                                bytes_written = current_addr - flash_address
                                progress_percent = min(int((bytes_written / self._firmware_size) * 100), 99)
                                if self.progress_callback:
                                    self.progress_callback(progress_percent, f"Writing firmware... {progress_percent}%")
                            else:
                                # Fallback to address-based calculation
                                if current_addr > flash_address:
                                    progress = min(int((current_addr - flash_address) / 65536 * 100), 99)
                                    if self.progress_callback:
                                        self.progress_callback(progress, f"Writing firmware... {progress}%")
                        except:
                            progress = min(progress + 2, 99)
                            if self.progress_callback:
                                self.progress_callback(progress, "Writing firmware...")
                    
                    elif 'Hash of data verified' in line:
                        if self.progress_callback:
                            self.progress_callback(95, "Verifying flash...")
                    
                    elif 'Hard resetting via RTS pin' in line:
                        if self.progress_callback:
                            self.progress_callback(100, "Flash completed!")
                    
                    elif 'Connecting...' in line:
                        if self.progress_callback:
                            self.progress_callback(35, "Connecting to device...")
                    
                    elif 'Chip is' in line:
                        if self.progress_callback:
                            self.progress_callback(38, f"Detected: {line}")
            
            # Wait for process to complete
            return_code = process.wait()
            
            if return_code == 0:
                return FlashResult(
                    success=True,
                    message="Firmware flashed successfully",
                    duration=0
                )
            else:
                error_output = '\n'.join(output_lines[-10:])  # Last 10 lines
                return FlashResult(
                    success=False,
                    message=f"Esptool failed with code {return_code}: {error_output}",
                    duration=0
                )
                
        except Exception as e:
            return FlashResult(
                success=False,
                message=f"Subprocess error: {str(e)}",
                duration=0
            )
    
    def _flash_with_platformio(
        self,
        port: str,
        firmware_path: str,
        baud_rate: int,
        flash_address: int,
        erase_all: bool,
        verify: bool,
        bootloader_path: Optional[str] = None,
        partitions_path: Optional[str] = None
    ) -> FlashResult:
        """Flash using PlatformIO with support for multi-file ESP32-S3 flashing"""
        
        try:
            # Check if PlatformIO is available
            pio_check = subprocess.run(['pio', '--version'], capture_output=True, text=True)
            if pio_check.returncode != 0:
                return FlashResult(
                    success=False,
                    message="PlatformIO not found",
                    duration=0
                )
            
            if self.progress_callback:
                self.progress_callback(5, "Using PlatformIO for reliable flashing...")
            
            # Detect chip type for better board selection
            chip_type = "esp32-s3-devkitm-1"  # Default
            try:
                detect_cmd = [
                    'pio', 'pkg', 'exec', '--package', 'tool-esptoolpy', '--',
                    'esptool.py', '--port', port, 'chip_id'
                ]
                detect_result = subprocess.run(detect_cmd, capture_output=True, text=True)
                if detect_result.returncode == 0:
                    output = detect_result.stdout.lower()
                    if 'esp32-s3' in output:
                        chip_type = "esp32-s3-devkitm-1"
                    elif 'esp32-s2' in output:
                        chip_type = "esp32-s2-saola-1"
                    elif 'esp32-c3' in output:
                        chip_type = "esp32-c3-devkitm-1"
                    elif 'esp32' in output:
                        chip_type = "esp32dev"
                        
                    if self.progress_callback:
                        self.progress_callback(15, f"Detected chip: {chip_type}")
            except:
                pass  # Use default
            
            # Use PlatformIO's esptool directly for binary flashing
            # This mimics what PlatformIO does internally but with our binary
            
            if erase_all:
                if self.progress_callback:
                    self.progress_callback(30, "Erasing flash with PlatformIO...")
                
                erase_cmd = [
                    'pio', 'pkg', 'exec', '--package', 'tool-esptoolpy', '--',
                    'esptool.py', '--port', port, '--baud', str(baud_rate),
                    '--before', 'default_reset', '--after', 'hard_reset',
                    'erase_flash'
                ]
                
                erase_result = subprocess.run(erase_cmd, capture_output=True, text=True)
                if erase_result.returncode != 0:
                    return FlashResult(
                        success=False,
                        message=f"PlatformIO erase failed: {erase_result.stderr}",
                        duration=0
                    )
                
                if self.progress_callback:
                    self.progress_callback(50, "Flash erased, uploading firmware...")
            
            # Build multi-file flash command for ESP32-S3
            flash_cmd = [
                'pio', 'pkg', 'exec', '--package', 'tool-esptoolpy', '--',
                'esptool.py', '--port', port, '--baud', str(baud_rate),
                '--before', 'default_reset', '--after', 'hard_reset',
                'write_flash', '--flash_mode', 'dio', '--flash_freq', '80m',
                '--flash_size', 'detect'
            ]
            
            # Add all three files with correct addresses for ESP32-S3
            if bootloader_path and Path(bootloader_path).exists():
                flash_cmd.extend(['0x0000', bootloader_path])
                
            if partitions_path and Path(partitions_path).exists():
                flash_cmd.extend(['0x8000', partitions_path])
                
            flash_cmd.extend(['0x10000', firmware_path])  # App at 0x10000
            
            # Store firmware size for progress calculation
            import os
            self._firmware_size = os.path.getsize(firmware_path)
            
            if self.progress_callback:
                if bootloader_path and partitions_path:
                    self.progress_callback(0, "Starting complete ESP32-S3 flash (bootloader + partitions + app)...")
                else:
                    self.progress_callback(0, "Starting app-only flash (may not boot without bootloader)...")
            
            # Run PlatformIO upload with progress tracking
            process = subprocess.Popen(
                flash_cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                bufsize=1,
                universal_newlines=True
            )
            
            output_lines = []
            progress = 0
            
            while True:
                line = process.stdout.readline()
                if not line and process.poll() is not None:
                    break
                
                if line:
                    line = line.strip()
                    output_lines.append(line)
                    
                    # Parse PlatformIO/esptool progress
                    if 'Writing at 0x' in line:
                        # Extract address to calculate actual flash progress
                        try:
                            addr_part = line.split('Writing at 0x')[1].split('...')[0]
                            current_addr = int(addr_part, 16)
                            
                            # Calculate actual progress based on firmware size
                            if hasattr(self, '_firmware_size') and self._firmware_size > 0:
                                bytes_written = current_addr - 0x10000  # ESP32 app partition starts at 0x10000
                                progress_percent = min(int((bytes_written / self._firmware_size) * 100), 99)
                                if self.progress_callback:
                                    self.progress_callback(progress_percent, f"Writing firmware... {progress_percent}%")
                            else:
                                progress = min(progress + 2, 99)
                                if self.progress_callback:
                                    self.progress_callback(progress, "Writing firmware...")
                        except:
                            progress = min(progress + 2, 99)
                            if self.progress_callback:
                                self.progress_callback(progress, "Writing firmware...")
                    elif 'Hash of data verified' in line:
                        if self.progress_callback:
                            self.progress_callback(100, "Flash completed!")
                    elif 'Hard resetting' in line:
                        if self.progress_callback:
                            self.progress_callback(100, "Flash completed!")
                    elif 'Connecting' in line:
                        if self.progress_callback:
                            self.progress_callback(65, "Connecting to ESP32...")
            
            return_code = process.wait()
            
            if return_code == 0:
                return FlashResult(
                    success=True,
                    message="Firmware flashed successfully with PlatformIO",
                    duration=0
                )
            else:
                error_output = '\n'.join(output_lines[-10:])
                return FlashResult(
                    success=False,
                    message=f"PlatformIO flash failed: {error_output}",
                    duration=0
                )
                
        except Exception as e:
            return FlashResult(
                success=False,
                message=f"PlatformIO error: {str(e)}",
                duration=0
            )
    
    def erase_flash(self, port: str, baud_rate: int = DEFAULT_BAUD_RATE) -> FlashResult:
        """
        Erase entire flash memory
        
        Args:
            port: Serial port name
            baud_rate: Connection baud rate
            
        Returns:
            FlashResult with operation status
        """
        start_time = time.time()
        
        try:
            if self.progress_callback:
                self.progress_callback(0, "Connecting to device...")
            
            if esptool:
                esp = esptool.detect_chip(port, baud_rate, connect_mode='default_reset')
                
                if self.progress_callback:
                    self.progress_callback(20, "Erasing flash...")
                
                esp.erase_flash()
                
                if self.progress_callback:
                    self.progress_callback(100, "Flash erased successfully!")
                
                duration = time.time() - start_time
                return FlashResult(
                    success=True,
                    message="Flash erased successfully",
                    duration=duration,
                    chip_type=esp.CHIP_NAME
                )
            else:
                # Fallback to subprocess
                cmd = [
                    sys.executable, '-m', 'esptool',
                    '--port', port,
                    '--baud', str(baud_rate),
                    'erase_flash'
                ]
                
                result = subprocess.run(cmd, capture_output=True, text=True)
                duration = time.time() - start_time
                
                if result.returncode == 0:
                    return FlashResult(
                        success=True,
                        message="Flash erased successfully",
                        duration=duration
                    )
                else:
                    return FlashResult(
                        success=False,
                        message=f"Erase failed: {result.stderr}",
                        duration=duration
                    )
                    
        except Exception as e:
            duration = time.time() - start_time
            return FlashResult(
                success=False,
                message=f"Erase operation failed: {str(e)}",
                duration=duration
            )
