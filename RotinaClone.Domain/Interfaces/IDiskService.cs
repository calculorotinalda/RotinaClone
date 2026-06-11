using System.Collections.Generic;
using System.Threading.Tasks;
using RotinaClone.Domain.Models;

namespace RotinaClone.Domain.Interfaces
{
    public interface IDiskService
    {
        Task<List<DiskInfo>> GetDisksAsync();
        Task<DiskInfo?> GetDiskDetailsAsync(int index);
        Task RefreshDisksAsync();
        bool IsUefiBoot();
        string GetOperatingSystemName();
    }
}
