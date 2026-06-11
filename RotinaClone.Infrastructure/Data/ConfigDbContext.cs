using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using RotinaClone.Domain.Models;

namespace RotinaClone.Infrastructure.Data
{
    public class ConfigDbContext : DbContext
    {
        public DbSet<SettingItem> Settings { get; set; } = null!;
        public DbSet<BackupJob> BackupJobs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RotinaClone");
                Directory.CreateDirectory(folder);
                string dbPath = Path.Combine(folder, "config.db");
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SettingItem>().HasKey(s => s.Key);
            modelBuilder.Entity<BackupJob>().HasKey(b => b.Id);
        }
    }

    public class SettingItem
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
