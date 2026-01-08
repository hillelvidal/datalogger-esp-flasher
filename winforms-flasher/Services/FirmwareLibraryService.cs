using Microsoft.Extensions.Logging;
using ESPFlasher.Models;

namespace ESPFlasher.Services
{
    public class FirmwareLibraryService
    {
        private readonly ILogger _logger;
        private readonly string _firmwareFolder;
        private const string CommonFolderName = "_common";
        
        public FirmwareLibraryService(ILogger logger, string firmwareFolder)
        {
            _logger = logger;
            _firmwareFolder = firmwareFolder;
            EnsureCommonFolderExists();
        }
        
        private void EnsureCommonFolderExists()
        {
            var commonPath = Path.Combine(_firmwareFolder, CommonFolderName);
            Directory.CreateDirectory(commonPath);
        }
        
        public string GetCommonFolderPath()
        {
            return Path.Combine(_firmwareFolder, CommonFolderName);
        }
        
        public string GetBootloaderPath()
        {
            return Path.Combine(GetCommonFolderPath(), "bootloader.bin");
        }
        
        public string GetPartitionsPath()
        {
            return Path.Combine(GetCommonFolderPath(), "partitions.bin");
        }
        
        public bool HasSharedFiles()
        {
            return File.Exists(GetBootloaderPath()) && File.Exists(GetPartitionsPath());
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
                    
                    if (folderName == CommonFolderName)
                        continue;
                    
                    var firmwarePath = Path.Combine(subfolder, "firmware.bin");
                    
                    if (File.Exists(firmwarePath))
                    {
                        var fileInfo = new FileInfo(firmwarePath);
                        var version = folderName.TrimStart('v');
                        
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
            var folderName = version.StartsWith("v") ? version : $"v{version}";
            return Path.Combine(_firmwareFolder, folderName);
        }
        
        public string GetFirmwareBinPath(string version)
        {
            return Path.Combine(GetFirmwareFolderPath(version), "firmware.bin");
        }
        
        public bool ValidateFirmwareForFlashing(FirmwareItem item)
        {
            if (item.Status != FirmwareStatus.Downloaded)
                return false;
            
            if (!File.Exists(item.LocalPath))
                return false;
            
            if (!HasSharedFiles())
                return false;
            
            return true;
        }
    }
}
