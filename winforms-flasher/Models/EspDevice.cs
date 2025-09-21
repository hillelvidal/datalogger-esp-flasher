namespace ESPFlasher.Models
{
    public class EspDevice
    {
        public string PortName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ChipType { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public bool IsConnected { get; set; }

        public string DisplayText => $"{PortName} - {Description} ({ChipType})";

        public override string ToString() => DisplayText;
    }
}
