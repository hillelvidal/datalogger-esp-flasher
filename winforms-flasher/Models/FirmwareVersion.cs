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

        [FirestoreProperty("storageUrl")]
        public string StorageUrl { get; set; } = string.Empty;

        [FirestoreProperty("releaseDate")]
        public DateTime ReleaseDate { get; set; }

        [FirestoreProperty("fileSize")]
        public long FileSize { get; set; }

        [FirestoreProperty("checksum")]
        public string Checksum { get; set; } = string.Empty;

        [FirestoreProperty("isLatest")]
        public bool IsLatest { get; set; }

        public string DisplayText => $"v{Version} - {Description} ({ReleaseDate:yyyy-MM-dd})";
        
        public string LocalFileName => $"firmware_v{Version}.bin";
    }
}
