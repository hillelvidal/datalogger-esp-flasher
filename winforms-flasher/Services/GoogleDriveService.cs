using Microsoft.Extensions.Logging;
using ESPFlasher.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Net;

namespace ESPFlasher.Services
{
    public class GoogleDriveService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _folderId;

        public GoogleDriveService(string publicFolderUrl, ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _folderId = ExtractFolderIdFromUrl(publicFolderUrl);
            
            if (string.IsNullOrEmpty(_folderId))
            {
                throw new ArgumentException("Invalid Google Drive folder URL", nameof(publicFolderUrl));
            }
        }

        private string ExtractFolderIdFromUrl(string url)
        {
            var match = Regex.Match(url, @"folders/([a-zA-Z0-9_-]+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        public async Task<List<FirmwareVersion>> ScanFirmwareFoldersAsync()
        {
            try
            {
                _logger.LogInformation($"[GoogleDrive] Scanning folder ID: {_folderId}");
                var firmwareVersions = new List<FirmwareVersion>();

                _logger.LogInformation("[GoogleDrive] Fetching subfolders...");
                var subfolders = await GetSubfoldersByScraping(_folderId);
                _logger.LogInformation($"[GoogleDrive] Found {subfolders.Count} subfolders");
                
                foreach (var folder in subfolders)
                {
                    try
                    {
                        _logger.LogInformation($"[GoogleDrive] Parsing folder: {folder.Name} (ID: {folder.Id})");
                        var firmware = await ParseFirmwareFolderAsync(folder);
                        if (firmware != null)
                        {
                            firmwareVersions.Add(firmware);
                            _logger.LogInformation($"[GoogleDrive] ✓ Added firmware: {firmware.Version}");
                        }
                        else
                        {
                            _logger.LogWarning($"[GoogleDrive] ✗ Folder '{folder.Name}' did not contain valid firmware");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[GoogleDrive] Failed to parse firmware folder: {folder.Name}");
                    }
                }

                firmwareVersions = firmwareVersions
                    .OrderByDescending(f => f.ReleaseDate)
                    .ThenByDescending(f => f.Version)
                    .ToList();

                _logger.LogInformation($"[GoogleDrive] ✓ Scan complete: {firmwareVersions.Count} firmware versions ready");
                return firmwareVersions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GoogleDrive] ✗ CRITICAL ERROR scanning folder {_folderId}");
                throw;
            }
        }

        private async Task<List<DriveFolder>> GetSubfoldersByScraping(string folderId)
        {
            var folders = new List<DriveFolder>();
            
            try
            {
                var url = $"https://drive.google.com/drive/folders/{folderId}";
                _logger.LogInformation($"[GoogleDrive] Fetching URL: {url}");
                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation($"[GoogleDrive] Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"[GoogleDrive] HTTP error: {response.StatusCode} - {response.ReasonPhrase}");
                    return folders;
                }
                
                var html = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"[GoogleDrive] Downloaded HTML: {html.Length} characters");

                // Google Drive embeds JSON data in the page
                // Look for the data structure containing folder/file information
                var dataPattern = @"\[""([\w-]+)"",\s*\[""([\w-]+)""\],\s*\[""([^""]*)""\],\s*""([^""]*)"",\s*""([^""]*)"",\s*""([^""]*)"",";
                var matches = Regex.Matches(html, dataPattern);

                var seenFolders = new HashSet<string>();

                foreach (Match match in matches)
                {
                    var itemId = match.Groups[1].Value;
                    var itemName = WebUtility.HtmlDecode(match.Groups[3].Value);
                    var mimeType = match.Groups[4].Value;

                    if (mimeType == "application/vnd.google-apps.folder" && !string.IsNullOrEmpty(itemName))
                    {
                        if (seenFolders.Add(itemName))
                        {
                            folders.Add(new DriveFolder
                            {
                                Id = itemId,
                                Name = itemName,
                                CreatedTime = DateTime.Now,
                                ModifiedTime = DateTime.Now
                            });
                            _logger.LogInformation($"Found subfolder: {itemName} (ID: {itemId})");
                        }
                    }
                }

                if (folders.Count == 0)
                {
                    _logger.LogWarning("[GoogleDrive] ⚠ No subfolders found. Possible reasons:");
                    _logger.LogWarning("  1. Folder is empty");
                    _logger.LogWarning("  2. Folder is not publicly shared");
                    _logger.LogWarning("  3. Google Drive HTML structure changed (scraping pattern needs update)");
                    _logger.LogWarning($"  4. Check folder manually: https://drive.google.com/drive/folders/{folderId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GoogleDrive] ✗ Exception while scraping folder {folderId}");
            }

            _logger.LogInformation($"[GoogleDrive] Returning {folders.Count} folders");
            return folders;
        }

        private async Task<FirmwareVersion?> ParseFirmwareFolderAsync(DriveFolder folder)
        {
            var files = await GetFilesInFolderAsync(folder.Id);
            
            _logger.LogInformation($"[GoogleDrive] Found {files.Count} files in folder '{folder.Name}'");
            
            var firmwareFile = files.FirstOrDefault(f => f.Name.Equals("firmware.bin", StringComparison.OrdinalIgnoreCase));
            var bootloaderFile = files.FirstOrDefault(f => f.Name.Equals("bootloader.bin", StringComparison.OrdinalIgnoreCase));
            var partitionsFile = files.FirstOrDefault(f => f.Name.Equals("partitions.bin", StringComparison.OrdinalIgnoreCase));

            if (firmwareFile == null)
            {
                _logger.LogWarning($"[GoogleDrive] ✗ No firmware.bin found in folder: {folder.Name}");
                if (files.Count > 0)
                {
                    _logger.LogWarning($"[GoogleDrive]   Files found: {string.Join(", ", files.Select(f => f.Name))}");
                }
                return null;
            }
            
            _logger.LogInformation($"[GoogleDrive] ✓ firmware.bin found (ID: {firmwareFile.Id}, Size: {firmwareFile.Size} bytes)");
            if (bootloaderFile != null) _logger.LogInformation($"[GoogleDrive] ✓ bootloader.bin found");
            if (partitionsFile != null) _logger.LogInformation($"[GoogleDrive] ✓ partitions.bin found");

            var firmware = new FirmwareVersion
            {
                Version = folder.Name,
                Description = $"From Google Drive: {folder.Name}",
                ReleaseDate = folder.ModifiedTime != DateTime.MinValue ? folder.ModifiedTime : folder.CreatedTime,
                Files = new Dictionary<string, string>()
            };

            firmware.Files["firmware"] = GetDirectDownloadUrl(firmwareFile.Id);
            
            if (bootloaderFile != null)
            {
                firmware.Files["bootloader"] = GetDirectDownloadUrl(bootloaderFile.Id);
            }
            
            if (partitionsFile != null)
            {
                firmware.Files["partitions"] = GetDirectDownloadUrl(partitionsFile.Id);
            }

            if (firmwareFile.Size > 0)
            {
                firmware.FileSize = firmwareFile.Size;
            }

            _logger.LogInformation($"[GoogleDrive] ✓ Created firmware version: {firmware.Version} with {firmware.Files.Count} file(s)");
            return firmware;
        }

        private async Task<List<DriveFile>> GetFilesInFolderAsync(string folderId)
        {
            if (string.IsNullOrEmpty(folderId))
            {
                return new List<DriveFile>();
            }

            try
            {
                var url = $"https://drive.google.com/drive/folders/{folderId}";
                var response = await _httpClient.GetAsync(url);
                var html = await response.Content.ReadAsStringAsync();

                var files = new List<DriveFile>();
                var dataPattern = @"\[""([\w-]+)"",\s*\[""([\w-]+)""\],\s*\[""([^""]*)""\],\s*""([^""]*)"",\s*""([^""]*)"",\s*""([^""]*)"",\s*""([^""]*)"",\s*(\d+)";
                var matches = Regex.Matches(html, dataPattern);

                var seenFiles = new HashSet<string>();

                foreach (Match match in matches)
                {
                    var itemId = match.Groups[1].Value;
                    var itemName = WebUtility.HtmlDecode(match.Groups[3].Value);
                    var mimeType = match.Groups[4].Value;
                    var sizeStr = match.Groups[8].Value;

                    if (!mimeType.Contains("folder") && !string.IsNullOrEmpty(itemName))
                    {
                        if (seenFiles.Add(itemName))
                        {
                            files.Add(new DriveFile
                            {
                                Id = itemId,
                                Name = itemName,
                                Size = long.TryParse(sizeStr, out var size) ? size : 0
                            });
                            _logger.LogInformation($"Found file: {itemName} (ID: {itemId}, Size: {sizeStr})");
                        }
                    }
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get files in folder: {folderId}");
                return new List<DriveFile>();
            }
        }

        private string GetDirectDownloadUrl(string fileId)
        {
            return $"https://drive.google.com/uc?export=download&id={fileId}";
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    internal class DriveFolder
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
    }

    internal class DriveFile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}
