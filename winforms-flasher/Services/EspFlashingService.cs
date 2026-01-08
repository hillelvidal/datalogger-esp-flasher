using Microsoft.Extensions.Logging;
using ESPFlasher.Models;
using System.Diagnostics;
using System.Text;

namespace ESPFlasher.Services
{
    public class EspFlashingService
    {
        private readonly ILogger _logger;
        private readonly string _esptoolPath;

        public event EventHandler<FlashProgressEventArgs>? FlashProgressChanged;
        public event EventHandler<string>? FlashStatusChanged;

        public EspFlashingService(ILogger logger)
        {
            _logger = logger;
            _esptoolPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "esptool.exe");
        }

        public async Task<bool> FlashFirmwareAsync(string firmwarePath, EspDevice device, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Starting firmware flash to {device.PortName}");
                FlashStatusChanged?.Invoke(this, "Preparing to flash firmware...");

                if (!File.Exists(firmwarePath))
                {
                    throw new FileNotFoundException($"Firmware file not found: {firmwarePath}");
                }

                if (!File.Exists(_esptoolPath))
                {
                    throw new FileNotFoundException($"esptool.exe not found: {_esptoolPath}");
                }

                // Step 1: Detect chip type
                FlashStatusChanged?.Invoke(this, "Detecting ESP chip type...");
                var chipType = await DetectChipTypeAsync(device.PortName, cancellationToken);
                _logger.LogInformation($"Detected chip type: {chipType}");

                // Step 2: Erase flash
                FlashStatusChanged?.Invoke(this, "Erasing flash memory...");
                FlashProgressChanged?.Invoke(this, new FlashProgressEventArgs(10, "Erasing flash..."));
                
                if (!await EraseFlashAsync(device.PortName, cancellationToken))
                {
                    throw new InvalidOperationException("Flash erase failed");
                }

                // Step 3: Flash firmware
                FlashStatusChanged?.Invoke(this, "Writing firmware to device...");
                FlashProgressChanged?.Invoke(this, new FlashProgressEventArgs(20, "Writing firmware..."));
                
                if (!await WriteFirmwareAsync(firmwarePath, device.PortName, cancellationToken))
                {
                    throw new InvalidOperationException("Firmware write failed");
                }

                // Step 4: Verify firmware
                FlashStatusChanged?.Invoke(this, "Verifying firmware...");
                FlashProgressChanged?.Invoke(this, new FlashProgressEventArgs(90, "Verifying firmware..."));
                
                if (!await VerifyFirmwareAsync(firmwarePath, device.PortName, cancellationToken))
                {
                    _logger.LogWarning("Firmware verification failed, but flash may still be successful");
                }

                FlashProgressChanged?.Invoke(this, new FlashProgressEventArgs(100, "Flash completed successfully!"));
                FlashStatusChanged?.Invoke(this, "Firmware flashed successfully!");
                
                _logger.LogInformation($"Firmware flash completed successfully on {device.PortName}");
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Firmware flash operation was cancelled");
                FlashStatusChanged?.Invoke(this, "Flash operation cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Firmware flash failed on {device.PortName}");
                FlashStatusChanged?.Invoke(this, $"Flash failed: {ex.Message}");
                return false;
            }
        }

        private async Task<string> DetectChipTypeAsync(string portName, CancellationToken cancellationToken)
        {
            var args = $"--port {portName} chip_id";
            var result = await RunEsptoolAsync(args, cancellationToken);
            
            if (result.Contains("ESP32-S3"))
                return "ESP32-S3";
            else if (result.Contains("ESP32-S2"))
                return "ESP32-S2";
            else if (result.Contains("ESP32-C3"))
                return "ESP32-C3";
            else if (result.Contains("ESP32"))
                return "ESP32";
            else
                return "Unknown";
        }

        private async Task<bool> EraseFlashAsync(string portName, CancellationToken cancellationToken)
        {
            var args = $"--port {portName} --chip esp32s3 --before default_reset --after hard_reset erase_flash";
            var result = await RunEsptoolAsync(args, cancellationToken);
            return result.Contains("erased successfully") || result.Contains("Chip erase completed");
        }

        private async Task<bool> WriteFirmwareAsync(string firmwarePath, string portName, CancellationToken cancellationToken)
        {
            // Look for bootloader and partitions in _common folder (parent of firmware folder)
            var firmwareDir = Path.GetDirectoryName(firmwarePath);
            var parentDir = Path.GetDirectoryName(firmwareDir);
            var commonDir = Path.Combine(parentDir ?? "", "_common");
            
            var bootloaderPath = Path.Combine(commonDir, "bootloader.bin");
            var partitionsPath = Path.Combine(commonDir, "partitions.bin");
            
            bool hasBootloader = File.Exists(bootloaderPath);
            bool hasPartitions = File.Exists(partitionsPath);
            
            _logger.LogInformation($"Looking in: {commonDir}");
            _logger.LogInformation($"Bootloader found: {hasBootloader} at {bootloaderPath}");
            _logger.LogInformation($"Partitions found: {hasPartitions} at {partitionsPath}");
            
            // Build the flash command with all available files
            var args = $"--port {portName} --chip esp32s3 --baud 460800 --before default_reset --after hard_reset write_flash --flash_mode dio --flash_freq 80m --flash_size detect";
            
            if (hasBootloader)
            {
                args += $" 0x0 \"{bootloaderPath}\"";
                _logger.LogInformation("Including bootloader at 0x0");
            }
            
            if (hasPartitions)
            {
                args += $" 0x8000 \"{partitionsPath}\"";
                _logger.LogInformation("Including partitions at 0x8000");
            }
            
            // Always include the main firmware at 0x10000
            args += $" 0x10000 \"{firmwarePath}\"";
            _logger.LogInformation("Including firmware at 0x10000");
            
            var result = await RunEsptoolAsync(args, cancellationToken, true);
            return result.Contains("Hash of data verified") || result.Contains("Leaving");
        }

        private async Task<bool> VerifyFirmwareAsync(string firmwarePath, string portName, CancellationToken cancellationToken)
        {
            try
            {
                // Verify the app region at 0x10000 to match write-flash
                var args = $"--port {portName} verify-flash 0x10000 \"{firmwarePath}\"";
                var result = await RunEsptoolAsync(args, cancellationToken);
                return result.Contains("Verify successful");
            }
            catch
            {
                // Verification is optional, don't fail the entire process
                return true;
            }
        }

        private async Task<string> RunEsptoolAsync(string args, CancellationToken cancellationToken, bool trackProgress = false)
        {
            _logger.LogInformation($"Running esptool: {_esptoolPath} {args}");
            
            // Write command to debug file
            var debugFile = Path.Combine(Path.GetTempPath(), "esptool_debug.txt");
            try
            {
                File.AppendAllText(debugFile, $"\n\n=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
                File.AppendAllText(debugFile, $"esptool.exe path: {_esptoolPath}\n");
                File.AppendAllText(debugFile, $"esptool.exe exists: {File.Exists(_esptoolPath)}\n");
                File.AppendAllText(debugFile, $"Working directory: {AppDomain.CurrentDomain.BaseDirectory}\n");
                File.AppendAllText(debugFile, $"Command: {_esptoolPath} {args}\n\n");
            }
            catch { }
            
            // Verify esptool.exe exists
            if (!File.Exists(_esptoolPath))
            {
                var errorMsg = $"esptool.exe not found at: {_esptoolPath}\n\n";
                errorMsg += $"Working directory: {AppDomain.CurrentDomain.BaseDirectory}\n";
                errorMsg += $"Files in directory:\n";
                try
                {
                    var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.exe");
                    errorMsg += string.Join("\n", files.Select(f => $"  - {Path.GetFileName(f)}"));
                }
                catch { }
                throw new FileNotFoundException(errorMsg);
            }
            
            // Test if esptool.exe can run at all
            try
            {
                var testProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _esptoolPath,
                        Arguments = "--help",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                testProcess.Start();
                var testOutput = testProcess.StandardOutput.ReadToEnd();
                var testError = testProcess.StandardError.ReadToEnd();
                testProcess.WaitForExit();
                
                File.AppendAllText(debugFile, $"Test run (--help):\n");
                File.AppendAllText(debugFile, $"Exit code: {testProcess.ExitCode}\n");
                File.AppendAllText(debugFile, $"Output: {testOutput}\n");
                File.AppendAllText(debugFile, $"Error: {testError}\n\n");
                
                if (testProcess.ExitCode != 0 && string.IsNullOrWhiteSpace(testOutput) && string.IsNullOrWhiteSpace(testError))
                {
                    throw new InvalidOperationException($"esptool.exe fails to run (exit code {testProcess.ExitCode}) with no output. This may indicate:\n" +
                        "1. Missing Visual C++ Runtime - Install from Microsoft\n" +
                        "2. Antivirus blocking execution\n" +
                        "3. Corrupted esptool.exe file");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(debugFile, $"Test run failed: {ex.Message}\n\n");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _esptoolPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var output = new StringBuilder();
            var error = new StringBuilder();

            using var process = new Process { StartInfo = startInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                    _logger.LogDebug($"esptool output: {e.Data}");
                    
                    if (trackProgress)
                    {
                        ParseProgressFromOutput(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(e.Data);
                    _logger.LogDebug($"esptool error: {e.Data}");
                    
                    if (trackProgress)
                    {
                        ParseProgressFromOutput(e.Data);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill();
                }
                catch { }
                throw;
            }

            var stdOut = output.ToString();
            var stdErr = error.ToString();
            var fullOutput = stdOut + stdErr;
            
            // Write output to debug file (reuse debugFile from above)
            try
            {
                File.AppendAllText(debugFile, $"Exit Code: {process.ExitCode}\n");
                File.AppendAllText(debugFile, $"STDOUT:\n{stdOut}\n");
                File.AppendAllText(debugFile, $"STDERR:\n{stdErr}\n");
            }
            catch { }
            
            if (process.ExitCode != 0)
            {
                _logger.LogError($"esptool failed with exit code {process.ExitCode}");
                _logger.LogError($"STDOUT: {stdOut}");
                _logger.LogError($"STDERR: {stdErr}");
                
                var errorMsg = $"esptool.exe failed (exit code {process.ExitCode})\n\n";
                
                if (!string.IsNullOrWhiteSpace(stdErr))
                {
                    errorMsg += $"Error Output:\n{stdErr}\n\n";
                }
                
                if (!string.IsNullOrWhiteSpace(stdOut))
                {
                    errorMsg += $"Standard Output:\n{stdOut}";
                }
                
                if (string.IsNullOrWhiteSpace(stdErr) && string.IsNullOrWhiteSpace(stdOut))
                {
                    errorMsg += "No output captured from esptool.exe";
                }
                
                errorMsg += $"\n\nDebug log: {debugFile}";
                
                throw new InvalidOperationException(errorMsg);
            }

            return fullOutput;
        }

        private void ParseProgressFromOutput(string output)
        {
            try
            {
                // Parse progress from esptool output
                // Example: "Writing at 0x00008000... (50 %)"
                var match = System.Text.RegularExpressions.Regex.Match(output, @"\((\d+)\s*%\)");
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var percentage))
                    {
                        // Adjust percentage to account for the overall process (20-90% for writing)
                        var adjustedPercentage = 20 + (percentage * 70 / 100);
                        FlashProgressChanged?.Invoke(this, new FlashProgressEventArgs(adjustedPercentage, $"Writing firmware... {percentage}%"));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse progress from esptool output");
            }
        }

        public async Task<bool> TestEsptoolAsync()
        {
            try
            {
                if (!File.Exists(_esptoolPath))
                {
                    _logger.LogError($"esptool.exe not found at {_esptoolPath}");
                    return false;
                }

                var result = await RunEsptoolAsync("version", CancellationToken.None);
                _logger.LogInformation($"esptool version check successful: {result}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "esptool test failed");
                return false;
            }
        }
    }

    public class FlashProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; }
        public string StatusMessage { get; }

        public FlashProgressEventArgs(int progressPercentage, string statusMessage)
        {
            ProgressPercentage = progressPercentage;
            StatusMessage = statusMessage;
        }
    }
}
