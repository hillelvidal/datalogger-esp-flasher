using Microsoft.Extensions.Logging;
using ESPFlasher.Models;
using System.Security.Cryptography;
using System.Text;

namespace ESPFlasher.Services
{
    public class FirmwareDownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _downloadDirectory;

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgressChanged;

        public FirmwareDownloadService(ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ESPFlasher", "Firmware");
            
            // Ensure download directory exists
            Directory.CreateDirectory(_downloadDirectory);
        }

        public async Task<string> DownloadFirmwareAsync(FirmwareVersion firmware)
        {
            // Create version-specific folder
            var versionFolder = Path.Combine(_downloadDirectory, firmware.LocalFolderName);
            Directory.CreateDirectory(versionFolder);
            
            var firmwarePath = Path.Combine(versionFolder, "firmware.bin");
            
            // Check if already downloaded
            if (IsFirmwareDownloaded(firmware))
            {
                _logger.LogInformation($"Firmware {firmware.Version} already downloaded");
                return firmwarePath;
            }

            _logger.LogInformation($"Downloading firmware {firmware.Version} (all files)");

            try
            {
                // Download firmware.bin (required)
                await DownloadFileAsync(firmware.FirmwareUrl, firmwarePath, firmware.FileSize);
                
                // Download bootloader.bin (if available)
                if (!string.IsNullOrEmpty(firmware.BootloaderUrl))
                {
                    var bootloaderPath = Path.Combine(versionFolder, "bootloader.bin");
                    await DownloadFileAsync(firmware.BootloaderUrl, bootloaderPath, 0);
                    _logger.LogInformation("Bootloader downloaded");
                }
                else
                {
                    _logger.LogWarning("No bootloader URL provided");
                }
                
                // Download partitions.bin (if available)
                if (!string.IsNullOrEmpty(firmware.PartitionsUrl))
                {
                    var partitionsPath = Path.Combine(versionFolder, "partitions.bin");
                    await DownloadFileAsync(firmware.PartitionsUrl, partitionsPath, 0);
                    _logger.LogInformation("Partitions downloaded");
                }
                else
                {
                    _logger.LogWarning("No partitions URL provided");
                }

                _logger.LogInformation($"Firmware {firmware.Version} downloaded successfully to {versionFolder}");
                return firmwarePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download firmware {firmware.Version}");
                
                // Clean up partial download
                if (Directory.Exists(versionFolder))
                {
                    Directory.Delete(versionFolder, true);
                }
                
                throw;
            }
        }
        
        private async Task DownloadFileAsync(string url, string localPath, long expectedSize)
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? expectedSize;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;

                var progressPercentage = totalBytes > 0 ? (int)((downloadedBytes * 100) / totalBytes) : 0;
                DownloadProgressChanged?.Invoke(this, new DownloadProgressEventArgs(progressPercentage, downloadedBytes, totalBytes));
            }

            _logger.LogInformation($"Downloaded: {Path.GetFileName(localPath)} ({downloadedBytes} bytes)");
        }

        public bool IsFirmwareDownloaded(FirmwareVersion firmware)
        {
            var versionFolder = Path.Combine(_downloadDirectory, firmware.LocalFolderName);
            var firmwarePath = Path.Combine(versionFolder, "firmware.bin");
            return File.Exists(firmwarePath);
        }
        
        public string GetLocalFirmwarePath(FirmwareVersion firmware)
        {
            var versionFolder = Path.Combine(_downloadDirectory, firmware.LocalFolderName);
            return Path.Combine(versionFolder, "firmware.bin");
        }

        public async Task<bool> ValidateFirmwareFileAsync(string filePath, FirmwareVersion firmware)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var fileInfo = new FileInfo(filePath);
                
                // Check file size
                if (firmware.FileSize > 0 && fileInfo.Length != firmware.FileSize)
                {
                    _logger.LogWarning($"File size mismatch: expected {firmware.FileSize}, got {fileInfo.Length}");
                    return false;
                }

                // Check checksum if provided
                if (!string.IsNullOrEmpty(firmware.Checksum))
                {
                    var actualChecksum = await CalculateFileChecksumAsync(filePath);
                    if (!string.Equals(actualChecksum, firmware.Checksum, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning($"Checksum mismatch: expected {firmware.Checksum}, got {actualChecksum}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating firmware file {filePath}");
                return false;
            }
        }

        private async Task<string> CalculateFileChecksumAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await Task.Run(() => sha256.ComputeHash(stream));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public void ClearCache()
        {
            try
            {
                if (Directory.Exists(_downloadDirectory))
                {
                    Directory.Delete(_downloadDirectory, true);
                    Directory.CreateDirectory(_downloadDirectory);
                    _logger.LogInformation("Firmware cache cleared");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear firmware cache");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; }
        public long BytesDownloaded { get; }
        public long TotalBytes { get; }

        public DownloadProgressEventArgs(int progressPercentage, long bytesDownloaded, long totalBytes)
        {
            ProgressPercentage = progressPercentage;
            BytesDownloaded = bytesDownloaded;
            TotalBytes = totalBytes;
        }
    }
}
