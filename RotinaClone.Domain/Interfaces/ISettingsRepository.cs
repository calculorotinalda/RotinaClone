using System.Collections.Generic;
using System.Threading.Tasks;
using RotinaClone.Domain.Models;

namespace RotinaClone.Domain.Interfaces
{
    public interface ISettingsRepository
    {
        Task<string> GetSettingAsync(string key, string defaultValue);
        Task SaveSettingAsync(string key, string value);
        
        Task<List<BackupJob>> GetBackupJobsAsync();
        Task SaveBackupJobAsync(BackupJob job);
        Task DeleteBackupJobAsync(int id);
    }
}
