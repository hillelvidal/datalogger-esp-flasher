"""
Firmware Handler Module

Manages firmware files, validation, and metadata.
"""

import os
import hashlib
from pathlib import Path
from typing import Optional, Dict, Any
from dataclasses import dataclass
from datetime import datetime


@dataclass
class FirmwareInfo:
    """Information about firmware file"""
    path: str
    size: int
    checksum: str
    version: Optional[str] = None
    created_date: Optional[datetime] = None
    bootloader_path: Optional[str] = None
    partitions_path: Optional[str] = None
    is_complete_bundle: bool = False
    valid: bool = True
    modified_date: Optional[datetime] = None


class FirmwareHandler:
    """Handles firmware file operations and validation"""
    
    # Supported firmware file extensions
    SUPPORTED_EXTENSIONS = ['.bin', '.elf']
    
    # Minimum and maximum reasonable firmware sizes (in bytes)
    MIN_FIRMWARE_SIZE = 1024  # 1KB
    MAX_FIRMWARE_SIZE = 16 * 1024 * 1024  # 16MB
    
    def __init__(self, firmware_dir: Optional[str] = None):
        """
        Initialize firmware handler
        
        Args:
            firmware_dir: Directory containing firmware files
        """
        self.firmware_dir = firmware_dir or self._get_default_firmware_dir()
    
    def _get_default_firmware_dir(self) -> str:
        """Get the default firmware directory relative to the project root"""
        # Get the directory containing this script
        current_dir = Path(__file__).parent
        # Go up to project root and into firmware directory
        firmware_dir = current_dir.parent / 'firmware'
        return str(firmware_dir)
    
    def validate_firmware(self, firmware_path: str) -> FirmwareInfo:
        """
        Validate firmware file and extract information
        
        Args:
            firmware_path: Path to firmware file
            
        Returns:
            FirmwareInfo object with validation results
        """
        try:
            path = Path(firmware_path)
            
            # Check if file exists
            if not path.exists():
                return FirmwareInfo(
                    path=firmware_path,
                    size=0,
                    checksum="",
                    valid=False
                )
            
            # Check file extension
            if path.suffix.lower() not in self.SUPPORTED_EXTENSIONS:
                return FirmwareInfo(
                    path=firmware_path,
                    size=0,
                    checksum="",
                    valid=False
                )
            
            # Get file info
            stat = path.stat()
            size = stat.st_size
            modified_date = datetime.fromtimestamp(stat.st_mtime)
            
            # Calculate checksum
            checksum = self.calculate_checksum(firmware_path)
            
            # Try to extract version (basic implementation)
            version = self._extract_version(firmware_path)
            
            # Look for bootloader and partition files
            bootloader_path, partitions_path = self._find_companion_files(firmware_path)
            is_complete_bundle = bootloader_path is not None and partitions_path is not None
            
            return FirmwareInfo(
                path=firmware_path,
                size=size,
                checksum=checksum,
                version=version,
                created_date=modified_date,
                modified_date=modified_date,
                bootloader_path=bootloader_path,
                partitions_path=partitions_path,
                is_complete_bundle=is_complete_bundle,
                valid=True
            )
            
        except Exception as e:
            return FirmwareInfo(
                path=firmware_path,
                size=0,
                checksum="",
                valid=False
            )
    
    def _find_companion_files(self, firmware_path: str) -> tuple[Optional[str], Optional[str]]:
        """
        Find bootloader.bin and partitions.bin files alongside firmware.bin
        
        Args:
            firmware_path: Path to main firmware file
            
        Returns:
            Tuple of (bootloader_path, partitions_path) or (None, None) if not found
        """
        firmware_dir = Path(firmware_path).parent
        
        # Look for bootloader.bin
        bootloader_path = firmware_dir / "bootloader.bin"
        if not bootloader_path.exists():
            bootloader_path = None
        
        # Look for partitions.bin
        partitions_path = firmware_dir / "partitions.bin"
        if not partitions_path.exists():
            partitions_path = None
            
        return (str(bootloader_path) if bootloader_path else None, 
                str(partitions_path) if partitions_path else None)
    
    def _extract_version(self, firmware_path: str) -> Optional[str]:
        """
        Extract version information from firmware file
        
        Args:
            firmware_path: Path to firmware file
            
        Returns:
            Version string if found, None otherwise
        """
        try:
            # Basic version extraction from filename
            filename = Path(firmware_path).name
            if 'v' in filename.lower():
                # Try to extract version pattern like v1.2.3
                import re
                version_match = re.search(r'v?(\d+\.\d+\.\d+)', filename, re.IGNORECASE)
                if version_match:
                    return version_match.group(1)
            return None
        except:
            return None
    
    def calculate_checksum(self, firmware_path: str, algorithm: str = 'sha256') -> str:
        """
        Calculate checksum of firmware file
        
        Args:
            firmware_path: Path to firmware file
            algorithm: Hash algorithm to use
            
        Returns:
            Hexadecimal checksum string
        """
        hash_obj = hashlib.new(algorithm)
        
        try:
            with open(firmware_path, 'rb') as f:
                # Read file in chunks to handle large files
                for chunk in iter(lambda: f.read(4096), b''):
                    hash_obj.update(chunk)
            return hash_obj.hexdigest()
        except Exception:
            return ''
    
    def _extract_version_from_filename(self, filename: str) -> Optional[str]:
        """
        Extract version information from firmware filename
        
        Args:
            filename: Firmware filename
            
        Returns:
            Version string if found, None otherwise
        """
        import re
        
        # Common version patterns
        patterns = [
            r'v?(\d+\.\d+\.\d+)',  # v1.2.3 or 1.2.3
            r'v?(\d+\.\d+)',       # v1.2 or 1.2
            r'_v(\d+)',            # _v1
        ]
        
        for pattern in patterns:
            match = re.search(pattern, filename, re.IGNORECASE)
            if match:
                return match.group(1)
        
        return None
    
    def _detect_chip_type_from_filename(self, filename: str) -> Optional[str]:
        """
        Detect ESP32 chip type from firmware filename
        
        Args:
            filename: Firmware filename
            
        Returns:
            Chip type if detected, None otherwise
        """
        filename_lower = filename.lower()
        
        # Check for specific chip types in filename
        if 'esp32-s3' in filename_lower or 'esp32s3' in filename_lower:
            return 'ESP32-S3'
        elif 'esp32-s2' in filename_lower or 'esp32s2' in filename_lower:
            return 'ESP32-S2'
        elif 'esp32-c3' in filename_lower or 'esp32c3' in filename_lower:
            return 'ESP32-C3'
        elif 'esp32-c6' in filename_lower or 'esp32c6' in filename_lower:
            return 'ESP32-C6'
        elif 'esp32' in filename_lower:
            return 'ESP32'
        
        return None
    
    def find_firmware_files(self) -> list[FirmwareInfo]:
        """
        Find all firmware files in the firmware directory
        
        Returns:
            List of FirmwareInfo objects for found files
        """
        firmware_files = []
        firmware_dir = Path(self.firmware_dir)
        
        if not firmware_dir.exists():
            return firmware_files
        
        # Search for firmware files
        for ext in self.SUPPORTED_EXTENSIONS:
            for firmware_file in firmware_dir.glob(f'*{ext}'):
                if firmware_file.is_file():
                    info = self.validate_firmware(str(firmware_file))
                    firmware_files.append(info)
        
        return firmware_files
    
    def get_default_firmware(self) -> Optional[FirmwareInfo]:
        """
        Get the default firmware file (prioritizes firmware.bin, then latest by modification date)
        
        Returns:
            FirmwareInfo for default firmware, None if not found
        """
        firmware_dir = Path(self.firmware_dir)
        
        if not firmware_dir.exists():
            return None
        
        # Find all valid firmware files
        firmware_files = self.find_firmware_files()
        valid_files = [f for f in firmware_files if f.valid]
        
        if not valid_files:
            return None
        
        # First priority: look for firmware.bin
        for firmware in valid_files:
            if Path(firmware.path).name == 'firmware.bin':
                return firmware
        
        # Fallback: Sort by modification date (newest first)
        valid_files.sort(key=lambda f: f.modified_date or datetime.min, reverse=True)
        
        # Return the latest firmware file
        return valid_files[0]
    
    def format_file_size(self, size_bytes: int) -> str:
        """
        Format file size in human-readable format
        
        Args:
            size_bytes: Size in bytes
            
        Returns:
            Formatted size string
        """
        if size_bytes < 1024:
            return f"{size_bytes} B"
        elif size_bytes < 1024 * 1024:
            return f"{size_bytes / 1024:.1f} KB"
        else:
            return f"{size_bytes / (1024 * 1024):.1f} MB"
    
    def create_sample_firmware(self) -> str:
        """
        Create a sample firmware file for testing purposes
        
        Returns:
            Path to created sample firmware file
        """
        firmware_dir = Path(self.firmware_dir)
        firmware_dir.mkdir(parents=True, exist_ok=True)
        
        sample_path = firmware_dir / 'scanin_firmware_v2.1.3.bin'
        
        # Create a sample binary file (not a real firmware, just for testing)
        sample_data = b'\x00' * 1024 * 100  # 100KB of zeros
        
        with open(sample_path, 'wb') as f:
            f.write(sample_data)
        
        return str(sample_path)
