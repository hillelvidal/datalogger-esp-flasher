"""
Command Line Interface Module

Provides rich CLI interface for ESP32 firmware flasher with rich progress indicators - Hebrew RTL
"""

import click
import time
from rich.console import Console
from rich.progress import Progress, SpinnerColumn, TextColumn, BarColumn, TaskProgressColumn
from rich.panel import Panel
from rich.table import Table
from rich.text import Text
from pathlib import Path
from typing import Optional, List

from ..device_detector import DeviceDetector, ESP32Device
from ..firmware_handler import FirmwareHandler, FirmwareInfo
from ..flash_controller import FlashController, FlashResult
from ..utils.logger import setup_logging

# Hebrew RTL console with proper encoding
console = Console(force_terminal=True, width=120)
logger = setup_logging()


class CLI:
    """Command Line Interface for ESP32 Flasher"""
    
    def __init__(self):
        self.console = console
        self.device_detector = DeviceDetector()
        self.firmware_handler = FirmwareHandler()
        self.flash_controller = FlashController()
        
    def print_banner(self):
        """Print application banner"""
        self.console.print("\n[bold blue]ESP32 Firmware Flasher[/] [dim]v1.0[/]\n")
    
    def scan_for_devices(self) -> List[ESP32Device]:
        """
        Scan for ESP32 devices with progress indication
        
        Returns:
            List of detected ESP32 devices
        """
        with Progress(
            SpinnerColumn(),
            TextColumn("[progress.description]{task.description}"),
            console=self.console
        ) as progress:
            task = progress.add_task("🔍 Scanning for ESP32 devices...", total=None)
            
            # Simulate some scanning time for better UX
            time.sleep(0.5)
            devices = self.device_detector.detect_esp32_devices()
            
            progress.update(task, description="✅ Device scan completed")
            time.sleep(0.3)
        
        return devices
    
    def display_devices(self, devices: List[ESP32Device]):
        """Display detected devices in a formatted table"""
        if not devices:
            self.console.print("❌ [bold red]No ESP32 devices detected[/]")
            self.console.print("\n[yellow]Troubleshooting tips:[/]")
            self.console.print("• Check USB cable connection")
            self.console.print("• Ensure device is powered on")
            self.console.print("• Install USB-to-UART drivers (CP210x, CH340, etc.)")
            self.console.print("• Try a different USB port")
            return
        
        self.console.print(f"\n✅ [bold green]Found {len(devices)} ESP32 device(s):[/]\n")
        
        table = Table(show_header=True, header_style="bold magenta")
        table.add_column("Port", style="cyan")
        table.add_column("Chip Type", style="green")
        table.add_column("Description", style="white")
        table.add_column("VID:PID", style="yellow")
        
        for device in devices:
            vid_pid = f"{device.vid:04X}:{device.pid:04X}" if device.vid and device.pid else "N/A"
            table.add_row(
                device.port,
                device.chip_type,
                device.description[:50] + "..." if len(device.description) > 50 else device.description,
                vid_pid
            )
        
        self.console.print(table)
    
    def prepare_firmware(self, firmware_path: Optional[str] = None) -> Optional[FirmwareInfo]:
        """
        Prepare firmware for flashing with progress indication
        
        Args:
            firmware_path: Optional specific firmware path
            
        Returns:
            FirmwareInfo if successful, None otherwise
        """
        with Progress(
            SpinnerColumn(),
            TextColumn("[progress.description]{task.description}"),
            console=self.console
        ) as progress:
            task = progress.add_task("📁 Preparing firmware...", total=None)
            
            if firmware_path:
                firmware_info = self.firmware_handler.validate_firmware(firmware_path)
            else:
                firmware_info = self.firmware_handler.get_default_firmware()
            
            if not firmware_info or not firmware_info.valid:
                progress.update(task, description="❌ Firmware preparation failed")
                time.sleep(0.5)
                return None
            
            progress.update(task, description="✅ Firmware prepared successfully")
            time.sleep(0.3)
        
        # Display firmware info
        self.display_firmware_info(firmware_info)
        
        # Show that this is the latest firmware if multiple files exist
        firmware_files = self.firmware_handler.find_firmware_files()
        valid_files = [f for f in firmware_files if f.valid]
        if len(valid_files) > 1:
            self.console.print(f"ℹ️  [blue]Selected latest firmware from {len(valid_files)} available files[/]")
        
        return firmware_info
    
    def display_firmware_info(self, firmware_info: FirmwareInfo):
        """Display firmware information without panel frame"""
        self.console.print(f"📄 [bold]File:[/] {firmware_info.path}")
        self.console.print(f"📏 [bold]Size:[/] {self.firmware_handler.format_file_size(firmware_info.size)}")
        
        if firmware_info.modified_date:
            self.console.print(f"📅 [bold]Date:[/] {firmware_info.modified_date.strftime('%Y-%m-%d %H:%M:%S')}")
        
        self.console.print(f"🔒 [bold]Checksum:[/] {firmware_info.checksum[:16]}...")
        
        if firmware_info.version:
            self.console.print(f"🏷️  [bold]Version:[/] {firmware_info.version}")
        
        # Note: FirmwareInfo doesn't have chip_type, only FlashResult does
        
        self.console.print()
    
    def flash_firmware_with_progress(
        self,
        device: ESP32Device,
        firmware_info: FirmwareInfo,
        baud_rate: int = 460800,
        erase_all: bool = False
    ) -> FlashResult:
        """
        Flash firmware with simple progress indication focused only on flash progress
        
        Args:
            device: Target ESP32 device
            firmware_info: Firmware to flash
            baud_rate: Flash baud rate
            erase_all: Whether to erase entire flash
            
        Returns:
            FlashResult with operation status
        """
        # Set up progress callback - only show actual flash progress
        progress_obj = None
        task_id = None
        
        def progress_callback(percent: float, message: str):
            nonlocal progress_obj, task_id
            if progress_obj and task_id is not None:
                # Handle connection attempts with dots instead of new lines
                if "Connecting" in message:
                    # Add dots to show connection attempts on same line
                    dots = "." * min(int(percent / 10), 6)  # Max 6 dots
                    display_message = f"Connecting{dots}"
                else:
                    display_message = message
                
                # Update progress for all flash operations to show real-time progress
                progress_obj.update(task_id, completed=percent, description=f"⚡ {display_message}")
        
        self.flash_controller.set_progress_callback(progress_callback)
        
        # Start flashing with progress bar
        with Progress(
            SpinnerColumn(),
            TextColumn("[progress.description]{task.description}"),
            BarColumn(),
            TaskProgressColumn(),
            console=self.console,
            transient=True  # Don't leave progress lines in terminal history
        ) as progress:
            progress_obj = progress
            task_id = progress.add_task("⚡ Flashing firmware...", total=100)
            
            result = self.flash_controller.flash_firmware(
                port=device.port,
                firmware_path=firmware_info.path,
                baud_rate=baud_rate,
                erase_all=erase_all,
                verify=True,
                bootloader_path=firmware_info.bootloader_path,
                partitions_path=firmware_info.partitions_path
            )
        
        return result
    
    def display_flash_result(self, result: FlashResult):
        """Display flash operation result"""
        if result.success:
            self.console.print("✅ [bold green]Flash completed successfully![/]")
            self.console.print(f"⏱️  Duration: {result.duration:.1f} seconds")
            
            if result.chip_type:
                self.console.print(f"💾 Chip: {result.chip_type}")
            
            self.console.print("\n🎉 [bold green]SUCCESS: Firmware flashed successfully![/]")
            self.console.print("✅ [green]Your ESP32 device is ready to use![/]")
            
        else:
            error_text = Text()
            error_text.append("❌ ", style="bold red")
            error_text.append("Flash operation failed", style="bold red")
            error_text.append(f"\n💬 {result.message}")
            error_text.append(f"\n⏱️  Duration: {result.duration:.1f} seconds")
            
            panel = Panel(error_text, title="Error", border_style="red")
            self.console.print(panel)
            
            # Show troubleshooting tips with retry option
            self.console.print("\n[yellow]Troubleshooting tips:[/]")
            if "Failed to connect" in result.message or "No serial data received" in result.message:
                self.console.print("• [bold red]Put ESP32 in download mode manually:[/]")
                self.console.print("  1. Hold down BOOT button")
                self.console.print("  2. Press and release RESET button")
                self.console.print("  3. Release BOOT button")
                self.console.print("  4. Try flashing again immediately")
                self.console.print("• Check USB cable (try a different one)")
                self.console.print("• Try a lower baud rate: --baud 115200")
                
                # Offer retry option
                if click.confirm("\nWould you like to try flashing again?", default=True):
                    return True  # Signal to retry
            else:
                self.console.print("• Check device connection and try again")
                self.console.print("• Ensure device is in download mode")
                self.console.print("• Try a lower baud rate (--baud 115200)")
                self.console.print("• Use --erase-all to perform full chip erase")
            
            return False  # No retry
    
    def confirm_flash_operation(self, device: ESP32Device, firmware_info: FirmwareInfo) -> bool:
        """
        Ask user to put ESP32 in flash mode and confirm operation
        
        Args:
            device: Target device
            firmware_info: Firmware to flash
            
        Returns:
            True if user confirms, False otherwise
        """
        self.console.print("\n[bold yellow]⚠️  ESP32 Flash Mode Required[/]")
        self.console.print("\n[bold red]IMPORTANT: Put your ESP32 in flash mode before proceeding:[/]")
        self.console.print("  1. [bold]Hold down the BOOT button[/]")
        self.console.print("  2. [bold]Press and release the RESET button[/]")
        self.console.print("  3. [bold]Release the BOOT button[/]")
        self.console.print("  4. Your ESP32 is now in flash mode")
        
        self.console.print(f"\n📋 [bold]Flash Details:[/]")
        self.console.print(f"   Device: [cyan]{device.port}[/] ({device.chip_type})")
        self.console.print(f"   Firmware: [green]{firmware_info.path}[/]")
        self.console.print(f"   Size: [yellow]{self.firmware_handler.format_file_size(firmware_info.size)}[/]")
        
        return click.confirm("\nESP32 is in flash mode and ready to proceed?", default=True)
    
    def list_all_ports(self):
        """List all available serial ports for debugging"""
        ports = self.device_detector.list_all_ports()
        
        if not ports:
            self.console.print("No serial ports found")
            return
        
        self.console.print(f"\n[bold]All Serial Ports ({len(ports)} found):[/]\n")
        
        table = Table(show_header=True, header_style="bold magenta")
        table.add_column("Port", style="cyan")
        table.add_column("Description", style="white")
        table.add_column("VID", style="yellow")
        table.add_column("PID", style="yellow")
        table.add_column("Serial", style="green")
        
        for port in ports:
            table.add_row(
                port['port'],
                port['description'][:40] + "..." if len(port['description']) > 40 else port['description'],
                port['vid'],
                port['pid'],
                port['serial'][:20] + "..." if len(port['serial']) > 20 else port['serial']
            )
        
        self.console.print(table)
    
    def error(self, message: str):
        """Display error message"""
        self.console.print(f"❌ [bold red]Error:[/] {message}")
    
    def warning(self, message: str):
        """Display warning message"""
        self.console.print(f"⚠️  [bold yellow]Warning:[/] {message}")
    
    def info(self, message: str):
        """Display info message"""
        self.console.print(f"ℹ️  [bold blue]Info:[/] {message}")
    
    def success(self, message: str):
        """Display success message"""
        self.console.print(f"✅ [bold green]Success:[/] {message}")
