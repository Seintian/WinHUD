namespace WinHUD.Models
{
    // This class represents the hardware data that will be displayed in the HUD.
    public class HardwareData
    {
        public float CpuUsagePercent { get; set; }
        public float GpuUsagePercent { get; set; }
        public float RamAvailableMb { get; set; }

        public float DiskReadBytesPerSec { get; set; }
        public float DiskWriteBytesPerSec { get; set; }

        public long NetDownloadBytesPerSec { get; set; }
        public long NetUploadBytesPerSec { get; set; }
    }
}
