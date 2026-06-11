namespace RotinaClone.Domain.Models
{
    public class PartitionInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DriveLetter { get; set; } = string.Empty; // e.g., C:
        public string FileSystem { get; set; } = "RAW"; // NTFS, FAT32, exFAT, RAW
        public long TotalSize { get; set; }
        public long UsedSize { get; set; }
        public long FreeSize { get; set; }
        public bool IsBitLockerEnabled { get; set; }
        public bool IsSystem { get; set; } // Boot, Active, System
        public long StartOffset { get; set; } // Byte offset on disk
        public string PartitionType { get; set; } = string.Empty; // Primary, EFI, MSR, Recovery
        public bool IsSelected { get; set; } // Helper for UI selection

        public double TotalSizeGB => TotalSize / (1024.0 * 1024.0 * 1024.0);
        public double UsedSizeGB => UsedSize / (1024.0 * 1024.0 * 1024.0);
        public double FreeSizeGB => (TotalSize - UsedSize) / (1024.0 * 1024.0 * 1024.0);
        public double UsedPercent => TotalSize > 0 ? (double)UsedSize / TotalSize * 100 : 0;
    }
}
