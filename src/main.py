#!/usr/bin/env python3
"""
ESP32 Firmware Flasher - Main Entry Point

A user-friendly tool for flashing firmware to ESP32-based datalogger devices.
"""

import click
import sys
from pathlib import Path
from typing import Optional

from .ui.cli import CLI
from .device_detector import DeviceDetector
from .firmware_handler import FirmwareHandler
from .flash_controller import FlashController


@click.group()
@click.version_option(version="1.0.0", prog_name="ESP32 Firmware Flasher")
@click.pass_context
def main(ctx):
    """ESP32 Firmware Flasher - Flash firmware to ESP32 devices with ease."""
    ctx.ensure_object(dict)
    ctx.obj['cli'] = CLI()


@main.command()
@click.option('--port', '-p', help='Specific serial port (e.g., COM3, /dev/ttyUSB0)')
@click.option('--firmware', '-f', help='Path to firmware file')
@click.option('--baud', '-b', default=460800, help='Baud rate for flashing (default: 460800)')
@click.option('--erase-all', is_flag=True, help='Erase entire flash before writing')
@click.option('--no-verify', is_flag=True, help='Skip verification after flashing')
@click.option('--yes', '-y', is_flag=True, help='Skip confirmation prompt')
@click.pass_context
def flash(ctx, port, firmware, baud, erase_all, no_verify, yes):
    """Flash firmware to ESP32 device."""
    cli = ctx.obj['cli']
    
    # Print banner
    cli.print_banner()
    
    try:
        # Step 1: Device Detection
        cli.console.print("\n[bold blue][1/4] 🔍 Device Detection[/]")
        
        if port:
            # Use specified port
            device = DeviceDetector.get_device_by_port(port)
            if not device:
                cli.error(f"No ESP32 device found on port {port}")
                # Show available devices for reference
                devices = cli.scan_for_devices()
                if devices:
                    cli.display_devices(devices)
                sys.exit(1)
            devices = [device]
        else:
            # Auto-detect devices
            devices = cli.scan_for_devices()
            if not devices:
                cli.display_devices(devices)
                sys.exit(1)
            elif len(devices) > 1:
                cli.display_devices(devices)
                cli.error("Multiple devices found. Please specify --port")
                sys.exit(1)
            device = devices[0]
        
        cli.display_devices([device])
        
        # Step 2: Firmware Preparation
        cli.console.print("\n[bold blue][2/4] 📁 Firmware Preparation[/]")
        
        firmware_info = cli.prepare_firmware(firmware)
        if not firmware_info:
            if firmware:
                cli.error(f"Invalid firmware file: {firmware}")
            else:
                cli.error("No valid firmware found. Place firmware in ./firmware/ directory")
                # Try to create sample firmware for testing
                cli.info("Creating sample firmware for testing...")
                sample_path = FirmwareHandler().create_sample_firmware()
                cli.info(f"Sample firmware created: {sample_path}")
            sys.exit(1)
        
        # Step 3: Confirmation
        if not yes:
            if not cli.confirm_flash_operation(device, firmware_info):
                cli.info("Flash operation cancelled by user")
                sys.exit(0)
        
        # Step 4: Flash Operation
        cli.console.print("\n[bold blue][3/4] ⚡ Flashing Firmware[/]")
        
        result = cli.flash_firmware_with_progress(
            device=device,
            firmware_info=firmware_info,
            baud_rate=baud,
            erase_all=erase_all
        )
        
        # Step 5: Results
        cli.console.print("\n[bold blue][4/4] 📊 Results[/]")
        cli.display_flash_result(result)
        
        sys.exit(0 if result.success else 1)
        
    except KeyboardInterrupt:
        cli.warning("Operation cancelled by user")
        sys.exit(1)
    except Exception as e:
        cli.error(f"Unexpected error: {str(e)}")
        sys.exit(1)


@main.command('list-devices')
@click.option('--all', is_flag=True, help='Show all serial ports, not just ESP32 devices')
@click.pass_context
def list_devices(ctx, all):
    """List available ESP32 devices."""
    cli = ctx.obj['cli']
    
    if all:
        cli.list_all_ports()
    else:
        devices = cli.scan_for_devices()
        cli.display_devices(devices)


@main.command()
@click.argument('firmware_path')
@click.pass_context
def verify(ctx, firmware_path):
    """Verify firmware file without flashing."""
    cli = ctx.obj['cli']
    
    firmware_handler = FirmwareHandler()
    firmware_info = firmware_handler.validate_firmware(firmware_path)
    
    if firmware_info.valid:
        cli.success("Firmware file is valid")
        cli.display_firmware_info(firmware_info)
    else:
        cli.error("Firmware file is invalid or corrupted")
        cli.console.print(f"File: {firmware_path}")
        cli.console.print(f"Size: {firmware_info.size} bytes")
        sys.exit(1)


@main.command()
@click.option('--port', '-p', help='Specific serial port')
@click.option('--baud', '-b', default=460800, help='Baud rate (default: 460800)')
@click.pass_context
def erase(ctx, port, baud):
    """Erase ESP32 flash memory."""
    cli = ctx.obj['cli']
    
    # Device detection
    if port:
        device = DeviceDetector.get_device_by_port(port)
        if not device:
            cli.error(f"No ESP32 device found on port {port}")
            sys.exit(1)
    else:
        devices = cli.scan_for_devices()
        if not devices:
            cli.display_devices(devices)
            sys.exit(1)
        elif len(devices) > 1:
            cli.display_devices(devices)
            cli.error("Multiple devices found. Please specify --port")
            sys.exit(1)
        device = devices[0]
    
    # Confirmation
    cli.warning(f"This will erase ALL data on {device.port} ({device.chip_type})")
    if not click.confirm("Are you sure you want to continue?"):
        cli.info("Erase operation cancelled")
        sys.exit(0)
    
    # Erase operation
    flash_controller = FlashController()
    
    def progress_callback(percent: float, message: str):
        # Simple progress indication for erase
        pass
    
    flash_controller.set_progress_callback(progress_callback)
    
    cli.console.print("🔥 Erasing flash memory...")
    result = flash_controller.erase_flash(device.port, baud)
    
    if result.success:
        cli.success(f"Flash erased successfully in {result.duration:.1f}s")
    else:
        cli.error(f"Erase failed: {result.message}")
        sys.exit(1)


@main.command()
@click.option('--port', '-p', help='Specific serial port')
@click.option('--baud', '-b', default=115200, help='Baud rate (default: 115200)')
@click.pass_context
def info(ctx, port, baud):
    """Get information about connected ESP32 device."""
    cli = ctx.obj['cli']
    
    # Device detection
    if port:
        device = DeviceDetector.get_device_by_port(port)
        if not device:
            cli.error(f"No ESP32 device found on port {port}")
            sys.exit(1)
    else:
        devices = cli.scan_for_devices()
        if not devices:
            cli.display_devices(devices)
            sys.exit(1)
        elif len(devices) > 1:
            cli.display_devices(devices)
            cli.error("Multiple devices found. Please specify --port")
            sys.exit(1)
        device = devices[0]
    
    # Get chip information
    flash_controller = FlashController()
    
    try:
        cli.console.print(f"🔍 Connecting to {device.port}...")
        chip_info = flash_controller.detect_chip_info(device.port, baud)
        
        cli.console.print("\n[bold green]Device Information:[/]")
        cli.console.print(f"Port: [cyan]{device.port}[/]")
        cli.console.print(f"Chip Type: [green]{chip_info['chip_type']}[/]")
        cli.console.print(f"Chip Revision: [yellow]{chip_info['chip_revision']}[/]")
        cli.console.print(f"Flash Size: [blue]{chip_info['flash_size']}[/]")
        cli.console.print(f"MAC Address: [magenta]{chip_info['mac_address']}[/]")
        cli.console.print(f"Crystal Frequency: [white]{chip_info['crystal_freq']}[/]")
        
    except Exception as e:
        cli.error(f"Failed to get device info: {str(e)}")
        sys.exit(1)


if __name__ == '__main__':
    main()
