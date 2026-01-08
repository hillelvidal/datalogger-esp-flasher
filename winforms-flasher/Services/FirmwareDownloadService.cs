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
        private string _downloadDirectory;

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgressChanged;

        public FirmwareDownloadService(ILogger logger, string downloadDirectory)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _downloadDirectory = downloadDirectory;
            
            // Ensure download directory exists
            Directory.CreateDirectory(_downloadDirectory);
        }
        
        public void SetDownloadDirectory(string directory)
        {
            _downloadDirectory = directory;
            Directory.CreateDirectory(_downloadDirectory);
        }

        public async Task<string> DownloadFirmwareAsync(FirmwareVersion firmware)
        {
            var version = firmware.Version.TrimStart('v');
            var versionFolder = Path.Combine(_downloadDirectory, $"firmware-{version}");
            Directory.CreateDirectory(versionFolder);
            
            // Extract filename from URL or use default naming
            var firmwareFileName = GetFileNameFromUrl(firmware.FirmwareUrl) ?? $"scanin-datalogger-{version}.bin";
            var firmwarePath = Path.Combine(versionFolder, firmwareFileName);
            
            if (IsFirmwareDownloaded(firmware))
            {
                _logger.LogInformation($"Firmware {firmware.Version} already downloaded");
                return firmwarePath;
            }

            _logger.LogInformation($"Downloading firmware {firmware.Version}");

            try
            {
                await DownloadFileAsync(firmware.FirmwareUrl, firmwarePath, firmware.FileSize);
                
                // Download shared bootloader and partitions to _common folder
                await EnsureSharedFilesDownloadedAsync(firmware);
                
                // Download build-info.txt if available
                if (firmware.Files != null && firmware.Files.ContainsKey("build-info"))
                {
                    var buildInfoPath = Path.Combine(versionFolder, "build-info.txt");
                    await DownloadFileAsync(firmware.Files["build-info"], buildInfoPath, 0);
                }

                _logger.LogInformation($"Firmware {firmware.Version} downloaded successfully to {versionFolder}");
                return firmwarePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download firmware {firmware.Version}");
                
                if (Directory.Exists(versionFolder))
                {
                    Directory.Delete(versionFolder, true);
                }
                
                throw;
            }
        }
        
        private string? GetFileNameFromUrl(string url)
        {
            try
            {
                // For Google Drive URLs, we can't extract filename easily
                // Return null to use default naming
                if (url.Contains("drive.google.com"))
                    return null;
                    
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrEmpty(fileName) ? null : fileName;
            }
            catch
            {
                return null;
            }
        }
        
        private async Task EnsureSharedFilesDownloadedAsync(FirmwareVersion firmware)
        {
            var commonFolder = Path.Combine(_downloadDirectory, "_common");
            Directory.CreateDirectory(commonFolder);
            
            var bootloaderPath = Path.Combine(commonFolder, "bootloader.bin");
            var partitionsPath = Path.Combine(commonFolder, "partitions.bin");
            
            if (!File.Exists(bootloaderPath) && !string.IsNullOrEmpty(firmware.BootloaderUrl))
            {
                _logger.LogInformation("Downloading shared bootloader.bin");
                await DownloadFileAsync(firmware.BootloaderUrl, bootloaderPath, 0);
            }
            
            if (!File.Exists(partitionsPath) && !string.IsNullOrEmpty(firmware.PartitionsUrl))
            {
                _logger.LogInformation("Downloading shared partitions.bin");
                await DownloadFileAsync(firmware.PartitionsUrl, partitionsPath, 0);
            }
        }
        
        private async Task DownloadFileAsync(string url, string localPath, long expectedSize)
        {
            // Handle Google Drive URLs which may require following redirects
            var finalUrl = url;
            if (url.Contains("drive.google.com"))
            {
                finalUrl = await GetGoogleDriveDirectDownloadUrlAsync(url);
            }

            using var response = await _httpClient.GetAsync(finalUrl, HttpCompletionOption.ResponseHeadersRead);
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

        private async Task<string> GetGoogleDriveDirectDownloadUrlAsync(string url)
        {
            try
            {
                // For small files, Google Drive uses direct download
                // For large files, it shows a virus scan warning page
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                var content = await response.Content.ReadAsStringAsync();

                // Check if we got a virus scan warning page
                if (content.Contains("virus-scan-warning") || content.Contains("download-anyway"))
                {
                    // Extract the confirm download URL
                    var match = System.Text.RegularExpressions.Regex.Match(content, @"href=""(/uc\?export=download[^""]+)""");
                    if (match.Success)
                    {
                        var confirmUrl = "https://drive.google.com" + match.Groups[1].Value.Replace("&amp;", "&");
                        _logger.LogInformation("Following Google Drive confirmation link");
                        return confirmUrl;
                    }
                }

                return url;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process Google Drive URL, using original");
                return url;
            }
        }

        public bool IsFirmwareDownloaded(FirmwareVersion firmware)
        {
            var version = firmware.Version.TrimStart('v');
            var versionFolder = Path.Combine(_downloadDirectory, $"firmware-{version}");
            
            if (!Directory.Exists(versionFolder))
                return false;
            
            // Check if any .bin file exists (excluding bootloader/partitions)
            var binFiles = Directory.GetFiles(versionFolder, "*.bin")
                .Where(f => 
                {
                    var fileName = Path.GetFileName(f);
                    return !fileName.Equals("bootloader.bin", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.Equals("partitions.bin", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            
            return binFiles.Count > 0;
        }
        
        public string GetLocalFirmwarePath(FirmwareVersion firmware)
        {
            var version = firmware.Version.TrimStart('v');
            var versionFolder = Path.Combine(_downloadDirectory, $"firmware-{version}");
            
            if (!Directory.Exists(versionFolder))
                return Path.Combine(versionFolder, $"scanin-datalogger-{version}.bin");
            
            // Find any .bin file (excluding bootloader/partitions)
            var binFiles = Directory.GetFiles(versionFolder, "*.bin")
                .Where(f => 
                {
                    var fileName = Path.GetFileName(f);
                    return !fileName.Equals("bootloader.bin", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.Equals("partitions.bin", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            
            return binFiles.Count > 0 ? binFiles[0] : Path.Combine(versionFolder, $"scanin-datalogger-{version}.bin");
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
