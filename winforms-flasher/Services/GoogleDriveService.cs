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
            var firmwareVersions = new List<FirmwareVersion>();
            var subfolders = await GetSubfoldersByScraping(_folderId);
            
            foreach (var folder in subfolders)
            {
                try
                {
                    var firmware = await ParseFirmwareFolderAsync(folder);
                    if (firmware != null)
                    {
                        firmwareVersions.Add(firmware);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to parse firmware folder: {folder.Name}");
                }
            }

            return firmwareVersions
                .OrderByDescending(f => f.ReleaseDate)
                .ThenByDescending(f => f.Version)
                .ToList();
        }

        private async Task<List<DriveFolder>> GetSubfoldersByScraping(string folderId)
        {
            var folders = new List<DriveFolder>();
            
            try
            {
                var url = $"https://drive.google.com/drive/folders/{folderId}";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    return folders;
                }
                
                var html = await response.Content.ReadAsStringAsync();
                var seenFolders = new HashSet<string>();
                
                // Extract from window['_DRIVE_ivd'] JavaScript variable
                var driveIvdPattern = @"window\['_DRIVE_ivd'\]\s*=\s*'([^']+)'";
                var driveIvdMatch = Regex.Match(html, driveIvdPattern);
                
                if (driveIvdMatch.Success)
                {
                    var escapedJson = driveIvdMatch.Groups[1].Value;
                    
                    // Decode hex escape sequences (\x5b -> [, \x22 -> ", etc.)
                    var unescaped = Regex.Replace(escapedJson, @"\\x([0-9a-fA-F]{2})", 
                        m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
                    
                    // Find folder entries: ["ID",["parent"],"name","application/vnd.google-apps.folder"
                    var folderPattern = @"\[""([\w-]+)"",\[""[^""]*""\],""([^""]*)"",""application\\?/vnd\.google-apps\.folder""";
                    var folderMatches = Regex.Matches(unescaped, folderPattern);
                    
                    foreach (Match match in folderMatches)
                    {
                        var itemId = match.Groups[1].Value;
                        var itemName = match.Groups[2].Value;
                        
                        if (!string.IsNullOrEmpty(itemName) && seenFolders.Add(itemName))
                        {
                            folders.Add(new DriveFolder
                            {
                                Id = itemId,
                                Name = itemName,
                                CreatedTime = DateTime.Now,
                                ModifiedTime = DateTime.Now
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while scraping folder {folderId}");
            }

            return folders;
        }

        private async Task<FirmwareVersion?> ParseFirmwareFolderAsync(DriveFolder folder)
        {
            var files = await GetFilesInFolderAsync(folder.Id);
            
            var firmwareFile = files.FirstOrDefault(f => f.Name.Equals("firmware.bin", StringComparison.OrdinalIgnoreCase));
            var bootloaderFile = files.FirstOrDefault(f => f.Name.Equals("bootloader.bin", StringComparison.OrdinalIgnoreCase));
            var partitionsFile = files.FirstOrDefault(f => f.Name.Equals("partitions.bin", StringComparison.OrdinalIgnoreCase));

            if (firmwareFile == null)
            {
                return null;
            }

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
                var seenFiles = new HashSet<string>();
                
                // Extract from _DRIVE_ivd variable (same as folder scanning)
                var driveIvdPattern = @"window\['_DRIVE_ivd'\]\s*=\s*'([^']+)'";
                var driveIvdMatch = Regex.Match(html, driveIvdPattern);
                
                if (driveIvdMatch.Success)
                {
                    var escapedJson = driveIvdMatch.Groups[1].Value;
                    var unescaped = Regex.Replace(escapedJson, @"\\x([0-9a-fA-F]{2})", 
                        m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
                    
                    // Find file entries (not folders)
                    var filePattern = @"\[""([\w-]+)"",\[""[^""]*""\],""([^""]*)"",""(?!application\\?/vnd\.google-apps\.folder)([^""]*)"",\d+,null,\d+,\d+,\d+,\d+,(\d+)";
                    var matches = Regex.Matches(unescaped, filePattern);

                    foreach (Match match in matches)
                    {
                        var itemId = match.Groups[1].Value;
                        var itemName = match.Groups[2].Value;
                        var sizeStr = match.Groups[4].Value;

                        if (!string.IsNullOrEmpty(itemName) && seenFiles.Add(itemName))
                        {
                            files.Add(new DriveFile
                            {
                                Id = itemId,
                                Name = itemName,
                                Size = long.TryParse(sizeStr, out var size) ? size : 0
                            });
                        }
                    }
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get files in folder: {folderId}");
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
