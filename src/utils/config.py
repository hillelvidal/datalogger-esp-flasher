"""
Configuration Management Module

Handles application configuration and settings.
"""

import os
import json
from pathlib import Path
from typing import Dict, Any, Optional
from dataclasses import dataclass, asdict


@dataclass
class FlashConfig:
    """Flash operation configuration"""
    default_baud_rate: int = 460800
    default_flash_address: int = 0x1000
    verify_after_flash: bool = True
    erase_before_flash: bool = False
    connection_timeout: int = 10
    flash_timeout: int = 300


@dataclass
class UIConfig:
    """User interface configuration"""
    show_progress_bars: bool = True
    verbose_output: bool = False
    auto_confirm: bool = False
    color_output: bool = True


@dataclass
class AppConfig:
    """Main application configuration"""
    flash: FlashConfig
    ui: UIConfig
    firmware_directory: Optional[str] = None
    last_used_port: Optional[str] = None
    
    def __init__(self):
        self.flash = FlashConfig()
        self.ui = UIConfig()
        self.firmware_directory = None
        self.last_used_port = None


class ConfigManager:
    """Manages application configuration"""
    
    def __init__(self, config_dir: Optional[str] = None):
        """
        Initialize configuration manager
        
        Args:
            config_dir: Custom configuration directory
        """
        self.config_dir = Path(config_dir) if config_dir else self._get_default_config_dir()
        self.config_file = self.config_dir / 'esp32_flasher_config.json'
        self.config = AppConfig()
        
        # Ensure config directory exists
        self.config_dir.mkdir(parents=True, exist_ok=True)
        
        # Load existing configuration
        self.load_config()
    
    def _get_default_config_dir(self) -> Path:
        """Get default configuration directory based on OS"""
        if os.name == 'nt':  # Windows
            config_dir = Path(os.environ.get('APPDATA', '')) / 'ESP32Flasher'
        elif os.name == 'posix':  # Unix-like (Linux, macOS)
            if 'darwin' in os.uname().sysname.lower():  # macOS
                config_dir = Path.home() / 'Library' / 'Application Support' / 'ESP32Flasher'
            else:  # Linux
                config_dir = Path.home() / '.config' / 'esp32-flasher'
        else:
            # Fallback to current directory
            config_dir = Path.cwd() / '.esp32-flasher'
        
        return config_dir
    
    def load_config(self) -> bool:
        """
        Load configuration from file
        
        Returns:
            True if config loaded successfully, False otherwise
        """
        try:
            if self.config_file.exists():
                with open(self.config_file, 'r') as f:
                    config_data = json.load(f)
                
                # Update flash config
                if 'flash' in config_data:
                    flash_data = config_data['flash']
                    self.config.flash = FlashConfig(**flash_data)
                
                # Update UI config
                if 'ui' in config_data:
                    ui_data = config_data['ui']
                    self.config.ui = UIConfig(**ui_data)
                
                # Update other settings
                self.config.firmware_directory = config_data.get('firmware_directory')
                self.config.last_used_port = config_data.get('last_used_port')
                
                return True
        except Exception:
            # If loading fails, use defaults
            pass
        
        return False
    
    def save_config(self) -> bool:
        """
        Save configuration to file
        
        Returns:
            True if config saved successfully, False otherwise
        """
        try:
            config_data = {
                'flash': asdict(self.config.flash),
                'ui': asdict(self.config.ui),
                'firmware_directory': self.config.firmware_directory,
                'last_used_port': self.config.last_used_port
            }
            
            with open(self.config_file, 'w') as f:
                json.dump(config_data, f, indent=2)
            
            return True
        except Exception:
            return False
    
    def get_flash_config(self) -> FlashConfig:
        """Get flash configuration"""
        return self.config.flash
    
    def get_ui_config(self) -> UIConfig:
        """Get UI configuration"""
        return self.config.ui
    
    def set_last_used_port(self, port: str):
        """Set last used port and save config"""
        self.config.last_used_port = port
        self.save_config()
    
    def get_last_used_port(self) -> Optional[str]:
        """Get last used port"""
        return self.config.last_used_port
    
    def set_firmware_directory(self, directory: str):
        """Set firmware directory and save config"""
        self.config.firmware_directory = directory
        self.save_config()
    
    def get_firmware_directory(self) -> Optional[str]:
        """Get firmware directory"""
        return self.config.firmware_directory
    
    def update_flash_config(self, **kwargs):
        """Update flash configuration parameters"""
        for key, value in kwargs.items():
            if hasattr(self.config.flash, key):
                setattr(self.config.flash, key, value)
        self.save_config()
    
    def update_ui_config(self, **kwargs):
        """Update UI configuration parameters"""
        for key, value in kwargs.items():
            if hasattr(self.config.ui, key):
                setattr(self.config.ui, key, value)
        self.save_config()
    
    def reset_to_defaults(self):
        """Reset configuration to defaults"""
        self.config = AppConfig()
        self.save_config()
    
    def get_config_file_path(self) -> str:
        """Get path to configuration file"""
        return str(self.config_file)
