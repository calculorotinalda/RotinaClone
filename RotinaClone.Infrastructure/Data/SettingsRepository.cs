using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RotinaClone.Domain.Interfaces;
using RotinaClone.Domain.Models;

namespace RotinaClone.Infrastructure.Data
{
    public class SettingsRepository : ISettingsRepository
    {
        public SettingsRepository()
        {
            using (var context = new ConfigDbContext())
            {
                context.Database.EnsureCreated();
            }
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue)
        {
            using (var context = new ConfigDbContext())
            {
                var item = await context.Settings.FirstOrDefaultAsync(s => s.Key == key);
                return item != null ? item.Value : defaultValue;
            }
        }

        public async Task SaveSettingAsync(string key, string value)
        {
            using (var context = new ConfigDbContext())
            {
                var item = await context.Settings.FirstOrDefaultAsync(s => s.Key == key);
                if (item != null)
                {
                    item.Value = value;
                }
                else
                {
                    context.Settings.Add(new SettingItem { Key = key, Value = value });
                }
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<BackupJob>> GetBackupJobsAsync()
        {
            using (var context = new ConfigDbContext())
            {
                return await context.BackupJobs.ToListAsync();
            }
        }

        public async Task SaveBackupJobAsync(BackupJob job)
        {
            using (var context = new ConfigDbContext())
            {
                if (job.Id == 0)
                {
                    context.BackupJobs.Add(job);
                }
                else
                {
                    context.Entry(job).State = EntityState.Modified;
                }
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteBackupJobAsync(int id)
        {
            using (var context = new ConfigDbContext())
            {
                var job = await context.BackupJobs.FindAsync(id);
                if (job != null)
                {
                    context.BackupJobs.Remove(job);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
