"""
Logging Utilities Module

Provides logging functionality for the ESP32 Flasher application.
"""

import logging
import sys
from pathlib import Path
from typing import Optional
from datetime import datetime


class ColoredFormatter(logging.Formatter):
    """Custom formatter with color support for console output"""
    
    # ANSI color codes
    COLORS = {
        'DEBUG': '\033[36m',    # Cyan
        'INFO': '\033[32m',     # Green
        'WARNING': '\033[33m',  # Yellow
        'ERROR': '\033[31m',    # Red
        'CRITICAL': '\033[35m', # Magenta
        'RESET': '\033[0m'      # Reset
    }
    
    def format(self, record):
        # Add color to levelname
        if record.levelname in self.COLORS:
            record.levelname = f"{self.COLORS[record.levelname]}{record.levelname}{self.COLORS['RESET']}"
        
        return super().format(record)


class FlasherLogger:
    """Logger for ESP32 Flasher application"""
    
    def __init__(self, name: str = 'esp32_flasher', log_dir: Optional[str] = None):
        """
        Initialize logger
        
        Args:
            name: Logger name
            log_dir: Directory for log files
        """
        self.logger = logging.getLogger(name)
        self.logger.setLevel(logging.DEBUG)
        
        # Prevent duplicate handlers
        if self.logger.handlers:
            return
        
        # Set up log directory
        if log_dir:
            self.log_dir = Path(log_dir)
        else:
            self.log_dir = self._get_default_log_dir()
        
        self.log_dir.mkdir(parents=True, exist_ok=True)
        
        # Set up handlers
        self._setup_console_handler()
        self._setup_file_handler()
    
    def _get_default_log_dir(self) -> Path:
        """Get default log directory"""
        # Use same logic as config for consistency
        import os
        
        if os.name == 'nt':  # Windows
            log_dir = Path(os.environ.get('APPDATA', '')) / 'ESP32Flasher' / 'logs'
        elif os.name == 'posix':  # Unix-like
            if 'darwin' in os.uname().sysname.lower():  # macOS
                log_dir = Path.home() / 'Library' / 'Logs' / 'ESP32Flasher'
            else:  # Linux
                log_dir = Path.home() / '.local' / 'share' / 'esp32-flasher' / 'logs'
        else:
            log_dir = Path.cwd() / 'logs'
        
        return log_dir
    
    def _setup_console_handler(self):
        """Set up console logging handler"""
        console_handler = logging.StreamHandler(sys.stdout)
        console_handler.setLevel(logging.INFO)
        
        # Use colored formatter for console
        console_formatter = ColoredFormatter(
            '%(levelname)s - %(message)s'
        )
        console_handler.setFormatter(console_formatter)
        
        self.logger.addHandler(console_handler)
    
    def _setup_file_handler(self):
        """Set up file logging handler"""
        # Create log filename with timestamp
        timestamp = datetime.now().strftime('%Y%m%d')
        log_file = self.log_dir / f'esp32_flasher_{timestamp}.log'
        
        file_handler = logging.FileHandler(log_file)
        file_handler.setLevel(logging.DEBUG)
        
        # Detailed formatter for file
        file_formatter = logging.Formatter(
            '%(asctime)s - %(name)s - %(levelname)s - %(funcName)s:%(lineno)d - %(message)s'
        )
        file_handler.setFormatter(file_formatter)
        
        self.logger.addHandler(file_handler)
    
    def set_console_level(self, level: str):
        """Set console logging level"""
        level_map = {
            'DEBUG': logging.DEBUG,
            'INFO': logging.INFO,
            'WARNING': logging.WARNING,
            'ERROR': logging.ERROR,
            'CRITICAL': logging.CRITICAL
        }
        
        if level.upper() in level_map:
            for handler in self.logger.handlers:
                if isinstance(handler, logging.StreamHandler) and handler.stream == sys.stdout:
                    handler.setLevel(level_map[level.upper()])
                    break
    
    def debug(self, message: str, *args, **kwargs):
        """Log debug message"""
        self.logger.debug(message, *args, **kwargs)
    
    def info(self, message: str, *args, **kwargs):
        """Log info message"""
        self.logger.info(message, *args, **kwargs)
    
    def warning(self, message: str, *args, **kwargs):
        """Log warning message"""
        self.logger.warning(message, *args, **kwargs)
    
    def error(self, message: str, *args, **kwargs):
        """Log error message"""
        self.logger.error(message, *args, **kwargs)
    
    def critical(self, message: str, *args, **kwargs):
        """Log critical message"""
        self.logger.critical(message, *args, **kwargs)
    
    def exception(self, message: str, *args, **kwargs):
        """Log exception with traceback"""
        self.logger.exception(message, *args, **kwargs)
    
    def log_flash_operation(self, port: str, firmware_path: str, success: bool, duration: float, error: Optional[str] = None):
        """Log flash operation details"""
        status = "SUCCESS" if success else "FAILED"
        message = f"Flash operation {status} - Port: {port}, Firmware: {firmware_path}, Duration: {duration:.1f}s"
        
        if success:
            self.info(message)
        else:
            self.error(f"{message}, Error: {error}")
    
    def log_device_detection(self, devices_found: int, devices: list):
        """Log device detection results"""
        self.info(f"Device scan completed - Found {devices_found} ESP32 device(s)")
        for device in devices:
            self.debug(f"Detected device: {device.port} ({device.chip_type}) - {device.description}")
    
    def log_firmware_validation(self, firmware_path: str, valid: bool, size: int, checksum: str):
        """Log firmware validation results"""
        if valid:
            self.info(f"Firmware validation SUCCESS - {firmware_path} ({size} bytes, checksum: {checksum[:16]}...)")
        else:
            self.warning(f"Firmware validation FAILED - {firmware_path}")
    
    def get_log_file_path(self) -> str:
        """Get current log file path"""
        timestamp = datetime.now().strftime('%Y%m%d')
        log_file = self.log_dir / f'esp32_flasher_{timestamp}.log'
        return str(log_file)


# Global logger instance
_logger_instance = None

def get_logger() -> FlasherLogger:
    """Get global logger instance"""
    global _logger_instance
    if _logger_instance is None:
        _logger_instance = FlasherLogger()
    return _logger_instance

def setup_logging() -> FlasherLogger:
    """Setup logging for the application"""
    return get_logger()
