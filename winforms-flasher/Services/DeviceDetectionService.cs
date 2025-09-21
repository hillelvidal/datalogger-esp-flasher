using System.IO.Ports;
using System.Management;
using Microsoft.Extensions.Logging;
using ESPFlasher.Models;

namespace ESPFlasher.Services
{
    public class DeviceDetectionService
    {
        private readonly ILogger _logger;
        
        // Common ESP32 USB-to-Serial chip VID/PID combinations
        private readonly Dictionary<(int vid, int pid), string> _knownChips = new()
        {
            { (0x10C4, 0xEA60), "CP210x" },
            { (0x1A86, 0x7523), "CH340" },
            { (0x1A86, 0x55D4), "CH341" },
            { (0x0403, 0x6001), "FTDI" },
            { (0x0403, 0x6010), "FTDI" },
            { (0x239A, 0x80C2), "ESP32-S2" },
            { (0x303A, 0x1001), "ESP32-S3" }
        };

        public DeviceDetectionService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<EspDevice>> DetectEspDevicesAsync()
        {
            return await Task.Run(() =>
            {
                var devices = new List<EspDevice>();
                
                try
                {
                    _logger.LogInformation("Scanning for ESP devices...");
                    
                    var portNames = SerialPort.GetPortNames();
                    _logger.LogInformation($"Found {portNames.Length} serial ports");

                    foreach (var portName in portNames)
                    {
                        try
                        {
                            var device = GetDeviceInfo(portName);
                            if (device != null)
                            {
                                devices.Add(device);
                                _logger.LogInformation($"ESP device detected: {device.DisplayText}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Error checking port {portName}");
                        }
                    }
                    
                    _logger.LogInformation($"Total ESP devices found: {devices.Count}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during device detection");
                }
                
                return devices;
            });
        }

        private EspDevice? GetDeviceInfo(string portName)
        {
            try
            {
                // Query WMI for USB device information
                var query = $"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%{portName}%'";
                using var searcher = new ManagementObjectSearcher(query);
                using var collection = searcher.Get();
                
                foreach (ManagementObject obj in collection)
                {
                    var name = obj["Name"]?.ToString() ?? "";
                    var deviceId = obj["DeviceID"]?.ToString() ?? "";
                    
                    if (IsEspDevice(name, deviceId, out var chipType, out var vid, out var pid))
                    {
                        return new EspDevice
                        {
                            PortName = portName,
                            Description = name,
                            ChipType = chipType,
                            VendorId = vid,
                            ProductId = pid,
                            IsConnected = IsPortAccessible(portName)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get device info for {portName}");
            }
            
            return null;
        }

        private bool IsEspDevice(string name, string deviceId, out string chipType, out int vid, out int pid)
        {
            chipType = "Unknown";
            vid = 0;
            pid = 0;
            
            // Extract VID and PID from device ID
            if (ExtractVidPid(deviceId, out vid, out pid))
            {
                if (_knownChips.TryGetValue((vid, pid), out var knownChip))
                {
                    chipType = knownChip;
                    return true;
                }
            }
            
            // Check by name patterns
            var nameLower = name.ToLower();
            if (nameLower.Contains("cp210") || nameLower.Contains("silicon labs"))
            {
                chipType = "CP210x";
                return true;
            }
            
            if (nameLower.Contains("ch340") || nameLower.Contains("ch341"))
            {
                chipType = "CH340/CH341";
                return true;
            }
            
            if (nameLower.Contains("ftdi"))
            {
                chipType = "FTDI";
                return true;
            }
            
            if (nameLower.Contains("esp32") || nameLower.Contains("esp"))
            {
                chipType = "ESP32";
                return true;
            }
            
            return false;
        }

        private bool ExtractVidPid(string deviceId, out int vid, out int pid)
        {
            vid = 0;
            pid = 0;
            
            try
            {
                var vidMatch = System.Text.RegularExpressions.Regex.Match(deviceId, @"VID_([0-9A-F]{4})");
                var pidMatch = System.Text.RegularExpressions.Regex.Match(deviceId, @"PID_([0-9A-F]{4})");
                
                if (vidMatch.Success && pidMatch.Success)
                {
                    vid = Convert.ToInt32(vidMatch.Groups[1].Value, 16);
                    pid = Convert.ToInt32(pidMatch.Groups[1].Value, 16);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to extract VID/PID from {deviceId}");
            }
            
            return false;
        }

        private bool IsPortAccessible(string portName)
        {
            try
            {
                using var port = new SerialPort(portName);
                port.Open();
                port.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TestDeviceConnectionAsync(string portName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var port = new SerialPort(portName, 115200);
                    port.ReadTimeout = 1000;
                    port.WriteTimeout = 1000;
                    
                    port.Open();
                    
                    // Try to communicate with the device
                    port.Write("AT\r\n");
                    Thread.Sleep(100);
                    
                    port.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Device connection test failed for {portName}");
                    return false;
                }
            });
        }
    }
}
