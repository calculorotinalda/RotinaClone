using System.Collections.Generic;

namespace RotinaClone.Domain.Models
{
    public class DiskInfo
    {
        public int Index { get; set; }
        public string Model { get; set; } = "Unknown Disk";
        public string SerialNumber { get; set; } = string.Empty;
        public string InterfaceType { get; set; } = "Unknown"; // NVMe, SSD, HDD, USB
        public long TotalSize { get; set; }
        public string PartitionStyle { get; set; } = "GPT"; // GPT or MBR
        public int Temperature { get; set; } // in °C
        public string HealthStatus { get; set; } = "Healthy"; // Healthy, Warning, Critical
        public int LifeRemainingPercent { get; set; } = 100;
        public bool IsSystemDisk { get; set; }
        public bool IsBitLockerEnabled { get; set; }
        public List<PartitionInfo> Partitions { get; set; } = new List<PartitionInfo>();

        public double TotalSizeGB => TotalSize / (1024.0 * 1024.0 * 1024.0);
    }
}
