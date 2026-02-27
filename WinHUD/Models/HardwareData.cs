namespace WinHUD.Models
{
    public class HardwareData
    {
        public float CpuUsagePercent { get; set; }
        public float GpuUsagePercent { get; set; }
        public float RamAvailableMb { get; set; }

        public float DiskReadBytesPerSec { get; set; }
        public float DiskWriteBytesPerSec { get; set; }

        public long NetDownloadBytesPerSec { get; set; }
        public long NetUploadBytesPerSec { get; set; }

        // Store individual disk loads as Key-Value pairs (e.g., "C:" -> 45.0)
        public Dictionary<string, float> DiskLoads { get; set; } = [];
    }
}
