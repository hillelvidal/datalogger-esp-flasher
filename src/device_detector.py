"""
ESP32 Device Detection Module

Handles detection and identification of ESP32 devices connected via USB.
"""

import serial.tools.list_ports
from typing import List, Optional, Dict
from dataclasses import dataclass
import platform
import sys

class ESP32Device:
    """Represents a detected ESP32 device"""
    port: str
    description: str
    chip_type: str
    pid: Optional[int] = None
    serial_number: Optional[str] = None


class DeviceDetector:
    """Detects and manages ESP32 devices connected via USB"""
    
    # Known USB-to-Serial chip VID/PID combinations for ESP32 boards
    ESP32_USB_IDS = [
        # Silicon Labs CP210x series
        (0x10C4, 0xEA60),  # CP2102/CP2109
        (0x10C4, 0xEA70),  # CP2105
        (0x10C4, 0xEA71),  # CP2108
        
        # WCH CH340/CH341 series
        (0x1A86, 0x7523),  # CH340
        (0x1A86, 0x5523),  # CH341
        
        # FTDI chips
        (0x0403, 0x6001),  # FT232R
        (0x0403, 0x6010),  # FT2232C/D/H
        (0x0403, 0x6011),  # FT4232H
        (0x0403, 0x6014),  # FT232H
        (0x0403, 0x6015),  # FT-X series
        
        # Espressif native USB (ESP32-S2/S3/C3)
        (0x303A, 0x1001),  # ESP32-S2
        (0x303A, 0x0002),  # ESP32-S2 (DFU mode)
        (0x303A, 0x0003),  # ESP32-S3
        (0x303A, 0x0004),  # ESP32-C3
    ]
    
    # Common ESP32 board descriptions
    ESP32_DESCRIPTIONS = [
        'Silicon Labs CP210x USB to UART Bridge',
        'USB-SERIAL CH340',
        'USB Serial',
        'ESP32',
        'SLAB_USBtoUART',
        'usbserial',
        'ttyUSB',
        'cu.SLAB_USBtoUART',
        'cu.usbserial',
        'cu.wchusbserial',
    ]
    
    @classmethod
    def detect_esp32_devices(cls) -> List[ESP32Device]:
        """
        Detect all connected ESP32 devices
        
        Returns:
            List of ESP32Device objects for detected devices
        """
        devices = []
        ports = serial.tools.list_ports.comports()
        
        for port in ports:
            # Check if this port matches known ESP32 USB IDs
            device = cls._identify_esp32_device(port)
            if device:
                devices.append(device)
        
        return devices
    
    @classmethod
    def _is_esp32_device(cls, port) -> bool:
        """
        Check if a serial port is likely an ESP32 device
        
        Args:
            port: Serial port info object
            
        Returns:
            True if likely an ESP32 device
        """
        # Check VID/PID combinations
        if port.vid and port.pid:
            if (port.vid, port.pid) in cls.ESP32_USB_IDS:
                return True
        
        # Check description strings
        if port.description:
            description_lower = port.description.lower()
            for esp_desc in cls.ESP32_DESCRIPTIONS:
                if esp_desc.lower() in description_lower:
                    return True
        
        # Check device name patterns (Unix-like systems)
        if port.device:
            device_lower = port.device.lower()
            if any(pattern in device_lower for pattern in ['ttyusb', 'cu.slab', 'cu.usbserial', 'cu.wchusbserial']):
                return True
        
        return False
    
    @classmethod
    def _detect_chip_type(cls, port) -> str:
        """
        Attempt to detect the ESP32 chip type based on USB identifiers
        
        Args:
            port: Serial port info object
            
        Returns:
            Detected chip type or 'ESP32' as default
        """
        if not port.vid or not port.pid:
            return 'ESP32'
        
        # Espressif native USB devices
        if port.vid == 0x303A:
            if port.pid == 0x1001 or port.pid == 0x0002:
                return 'ESP32-S2'
            elif port.pid == 0x0003:
                return 'ESP32-S3'
            elif port.pid == 0x0004:
                return 'ESP32-C3'
        
        # For USB-to-Serial bridges, we can't definitively determine the chip type
        # without connecting and querying the device
        return 'ESP32'
    
    @classmethod
    def get_device_by_port(cls, port_name: str) -> Optional[ESP32Device]:
        """
        Get ESP32 device information for a specific port
        
        Args:
            port_name: Serial port name (e.g., 'COM3', '/dev/ttyUSB0')
            
        Returns:
            ESP32Device if found, None otherwise
        """
        devices = cls.detect_esp32_devices()
        for device in devices:
            if device.port == port_name:
                return device
        return None
    
    @classmethod
    def list_all_ports(cls) -> List[Dict[str, str]]:
        """
        List all available serial ports (for debugging)
        
        Returns:
            List of port information dictionaries
        """
        ports = []
        for port in serial.tools.list_ports.comports():
            ports.append({
                'port': port.device,
                'description': port.description or 'N/A',
                'vid': f'0x{port.vid:04X}' if port.vid else 'N/A',
                'pid': f'0x{port.pid:04X}' if port.pid else 'N/A',
                'serial': port.serial_number or 'N/A'
            })
        return ports
