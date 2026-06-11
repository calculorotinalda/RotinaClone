using System;

namespace RotinaClone.Domain.Models
{
    public class CloneSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public string Status { get; set; } = "Pending"; // Pending, Running, Verifying, Completed, Failed, Cancelled
        public int PercentComplete { get; set; }
        public long BytesCopied { get; set; }
        public long TotalBytesToCopy { get; set; }
        public double CurrentSpeedBytesPerSecond { get; set; }
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
        public TimeSpan EstimatedTimeRemaining { get; set; } = TimeSpan.Zero;
        public string CurrentOperation { get; set; } = "Starting...";
        public string LogMessage { get; set; } = string.Empty;

        public double ProgressPercent => TotalBytesToCopy > 0 ? (double)BytesCopied / TotalBytesToCopy * 100 : 0;
        public double CurrentSpeedMB => CurrentSpeedBytesPerSecond / (1024.0 * 1024.0);
    }
}
