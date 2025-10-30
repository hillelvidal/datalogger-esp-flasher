using Google.Cloud.Firestore;

namespace ESPFlasher.Models
{
    [FirestoreData]
    public class FirmwareVersion
    {
        [FirestoreProperty("version")]
        public string Version { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        // Legacy single file support (backward compatible)
        [FirestoreProperty("storageUrl")]
        public string StorageUrl { get; set; } = string.Empty;

        // New multi-file support
        [FirestoreProperty("files")]
        public Dictionary<string, string>? Files { get; set; }

        [FirestoreProperty("releaseDate")]
        public DateTime ReleaseDate { get; set; }

        [FirestoreProperty("fileSize")]
        public long FileSize { get; set; }

        [FirestoreProperty("checksum")]
        public string Checksum { get; set; } = string.Empty;

        [FirestoreProperty("isLatest")]
        public bool IsLatest { get; set; }

        public string DisplayText => $"v{Version} - {Description} ({ReleaseDate:yyyy-MM-dd})";
        
        // Local folder for this version (contains all 3 files)
        public string LocalFolderName => $"v{Version}";
        
        // Helper properties to get file URLs
        public string FirmwareUrl => Files?.GetValueOrDefault("firmware") ?? StorageUrl;
        public string? BootloaderUrl => Files?.GetValueOrDefault("bootloader");
        public string? PartitionsUrl => Files?.GetValueOrDefault("partitions");
        
        public bool HasAllFiles => !string.IsNullOrEmpty(FirmwareUrl) && 
                                   !string.IsNullOrEmpty(BootloaderUrl) && 
                                   !string.IsNullOrEmpty(PartitionsUrl);
    }
}
