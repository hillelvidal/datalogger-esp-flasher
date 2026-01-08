using Microsoft.Extensions.Logging;
using ESPFlasher.Models;

namespace ESPFlasher.Services
{
    public class FirmwareLibraryService
    {
        private readonly ILogger _logger;
        private readonly string _firmwareFolder;
        private readonly BuildInfoParser _buildInfoParser;
        
        public FirmwareLibraryService(ILogger logger, string firmwareFolder)
        {
            _logger = logger;
            _firmwareFolder = firmwareFolder;
            _buildInfoParser = new BuildInfoParser(logger);
            Directory.CreateDirectory(_firmwareFolder);
        }
        
        public List<FirmwareItem> ScanLocalFirmwares()
        {
            var items = new List<FirmwareItem>();
            
            if (!Directory.Exists(_firmwareFolder))
            {
                return items;
            }
            
            try
            {
                var subfolders = Directory.GetDirectories(_firmwareFolder);
                
                foreach (var subfolder in subfolders)
                {
                    var folderName = Path.GetFileName(subfolder);
                    
                    // Skip non-firmware folders
                    if (!folderName.StartsWith("firmware-"))
                        continue;
                    
                    // Extract version from folder name: firmware-20260108.4 -> 20260108.4
                    var version = folderName.Replace("firmware-", "");
                    
                    // Expected binary name: scanin-datalogger-20260108.4.bin
                    var firmwarePath = Path.Combine(subfolder, $"scanin-datalogger-{version}.bin");
                    var buildInfoPath = Path.Combine(subfolder, "build-info.txt");
                    
                    if (File.Exists(firmwarePath))
                    {
                        var fileInfo = new FileInfo(firmwarePath);
                        var item = new FirmwareItem
                        {
                            Name = folderName,
                            Version = version,
                            Source = FirmwareSource.Local,
                            Status = FirmwareStatus.Downloaded,
                            LocalPath = firmwarePath,
                            Size = fileInfo.Length,
                            Date = fileInfo.LastWriteTime,
                            IsComplete = true
                        };
                        
                        // Parse build-info.txt if available
                        if (File.Exists(buildInfoPath))
                        {
                            var buildInfo = _buildInfoParser.ParseBuildInfoFile(buildInfoPath);
                            if (buildInfo != null)
                            {
                                item.BuildNumber = buildInfo.BuildNumber;
                                item.GitCommit = buildInfo.GitCommit;
                                item.GitBranch = buildInfo.GitBranch;
                                item.FirmwareSize = buildInfo.FirmwareSize;
                                if (buildInfo.BuildDate != DateTime.MinValue)
                                {
                                    item.Date = buildInfo.BuildDate;
                                }
                            }
                        }
                        
                        items.Add(item);
                    }
                }
                
                _logger.LogInformation($"Found {items.Count} local firmware(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan local firmwares");
            }
            
            return items;
        }
        
        public List<FirmwareItem> ConvertCloudFirmwaresToItems(List<FirmwareVersion> cloudFirmwares, List<FirmwareItem> localItems)
        {
            var items = new List<FirmwareItem>();
            
            foreach (var cloudFw in cloudFirmwares)
            {
                var version = cloudFw.Version.TrimStart('v');
                
                var existsLocally = localItems.Any(l => 
                    l.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
                
                if (!existsLocally)
                {
                    var item = new FirmwareItem
                    {
                        Name = cloudFw.Version,
                        Version = version,
                        Source = FirmwareSource.GoogleDrive,
                        Status = FirmwareStatus.CloudOnly,
                        LocalPath = string.Empty,
                        Size = cloudFw.FileSize,
                        Date = cloudFw.ReleaseDate,
                        Description = cloudFw.Description,
                        CloudMetadata = cloudFw,
                        IsComplete = false
                    };
                    
                    items.Add(item);
                }
            }
            
            return items;
        }
        
        public List<FirmwareItem> MergeAndSort(List<FirmwareItem> localItems, List<FirmwareItem> cloudItems)
        {
            var allItems = new List<FirmwareItem>();
            allItems.AddRange(localItems);
            allItems.AddRange(cloudItems);
            
            return allItems
                .OrderByDescending(f => f.Status == FirmwareStatus.Downloaded)
                .ThenByDescending(f => f.Date)
                .ThenByDescending(f => f.Version)
                .ToList();
        }
        
        public string GetFirmwareFolderPath(string version)
        {
            var folderName = $"firmware-{version}";
            return Path.Combine(_firmwareFolder, folderName);
        }
        
        public string GetFirmwareBinPath(string version)
        {
            return Path.Combine(GetFirmwareFolderPath(version), $"scanin-datalogger-{version}.bin");
        }
        
        public bool ValidateFirmwareForFlashing(FirmwareItem item)
        {
            if (item.Status != FirmwareStatus.Downloaded)
                return false;
            
            if (!File.Exists(item.LocalPath))
                return false;
            
            return true;
        }
    }
}
