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
            var localPath = Path.Combine(_downloadDirectory, firmware.LocalFileName);
            
            // Check if file already exists and is valid
            if (File.Exists(localPath))
            {
                if (await ValidateFirmwareFileAsync(localPath, firmware))
                {
                    _logger.LogInformation($"Firmware {firmware.Version} already exists and is valid");
                    return localPath;
                }
                else
                {
                    _logger.LogWarning($"Existing firmware file is invalid, re-downloading");
                    File.Delete(localPath);
                }
            }

            _logger.LogInformation($"Downloading firmware {firmware.Version} from {firmware.StorageUrl}");

            try
            {
                using var response = await _httpClient.GetAsync(firmware.StorageUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? firmware.FileSize;
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

                _logger.LogInformation($"Download completed: {localPath}");

                // Validate the downloaded file
                if (await ValidateFirmwareFileAsync(localPath, firmware))
                {
                    _logger.LogInformation($"Firmware {firmware.Version} downloaded and validated successfully");
                    return localPath;
                }
                else
                {
                    File.Delete(localPath);
                    throw new InvalidOperationException("Downloaded firmware file failed validation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download firmware {firmware.Version}");
                
                // Clean up partial download
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                }
                
                throw;
            }
        }

        public bool IsFirmwareDownloaded(FirmwareVersion firmware)
        {
            var localPath = Path.Combine(_downloadDirectory, firmware.LocalFileName);
            return File.Exists(localPath);
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

        public string GetLocalFirmwarePath(FirmwareVersion firmware)
        {
            return Path.Combine(_downloadDirectory, firmware.LocalFileName);
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
