namespace ESPFlasher.Models
{
    public class FirmwareItem
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public FirmwareSource Source { get; set; }
        public FirmwareStatus Status { get; set; }
        public string LocalPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        
        public int BuildNumber { get; set; }
        public string GitCommit { get; set; } = string.Empty;
        public string GitBranch { get; set; } = string.Empty;
        public string FirmwareSize { get; set; } = string.Empty;
        
        public FirmwareVersion? CloudMetadata { get; set; }
        
        public bool IsComplete { get; set; }
        
        public string DisplayName => string.IsNullOrEmpty(Version) ? Name : Version;
        
        public string StatusIcon => Status switch
        {
            FirmwareStatus.Downloaded => "●",
            FirmwareStatus.CloudOnly => "○",
            FirmwareStatus.Downloading => "⬇",
            FirmwareStatus.Incomplete => "⚠",
            _ => "?"
        };
        
        public string SourceText => Source switch
        {
            FirmwareSource.Local => "Local",
            FirmwareSource.GoogleDrive => "Cloud",
            _ => "Unknown"
        };
        
        public string DateText => Date.ToString("yyyy-MM-dd HH:mm");
        
        public string ListViewText => $"{StatusIcon} {DisplayName}";
        
        public string DetailText => BuildNumber > 0 
            ? $"Build #{BuildNumber} | {FirmwareSize} | {GitCommit}" 
            : FirmwareSize;
    }
    
    public enum FirmwareSource
    {
        Local,
        GoogleDrive
    }
    
    public enum FirmwareStatus
    {
        CloudOnly,
        Downloaded,
        Downloading,
        Incomplete
    }
}
