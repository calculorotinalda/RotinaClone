using System;

namespace RotinaClone.Domain.Models
{
    public class BackupJob
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Full"; // Full, Incremental, Differential
        public string SourcePath { get; set; } = string.Empty; // e.g. Disk 0, Partition 1, or directory path
        public string DestinationPath { get; set; } = string.Empty; // Target folder or VHD file path
        
        public string ScheduleType { get; set; } = "Manual"; // Daily, Weekly, Monthly, Manual
        public string ScheduleTime { get; set; } = "00:00"; // HH:mm format
        
        public DateTime? LastRun { get; set; }
        public DateTime? NextRun { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
