using System;
using System.Threading;
using System.Threading.Tasks;
using RotinaClone.Domain.Models;

namespace RotinaClone.Domain.Interfaces
{
    public interface IBackupService
    {
        Task<CloneSession> RunBackupAsync(BackupJob job, Action<CloneSession> progressCallback, CancellationToken cancellationToken);
        Task<string> CreateVssSnapshotAsync(string driveLetter); // returns snapshot path e.g. \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy1
        Task DeleteVssSnapshotAsync(string snapshotId);
        Task ExportToImageAsync(int diskIndex, string targetPath, string format, Action<CloneSession> progressCallback, CancellationToken cancellationToken); // format: VHD, VHDX, VMDK, VDI, IMG
    }
}
