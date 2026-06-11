using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using RotinaClone.Domain.Interfaces;
using RotinaClone.Domain.Models;

namespace RotinaClone.Infrastructure.Services
{
    public class WindowsDiskService : IDiskService
    {
        public Task<List<DiskInfo>> GetDisksAsync()
        {
            return Task.Run(() =>
            {
                var disks = new List<DiskInfo>();
                var diskMap = new Dictionary<int, DiskInfo>();

                // 1. Query Win32_DiskDrive first (always succeeds and returns all physical drives including USB)
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject drive in collection)
                        {
                            try
                            {
                                int index = Convert.ToInt32(drive["Index"]);
                                string model = drive["Model"]?.ToString()?.Trim() ?? "Unknown Disk";
                                string serial = drive["SerialNumber"]?.ToString()?.Trim() ?? string.Empty;
                                long size = Convert.ToInt64(drive["Size"]);
                                string type = drive["InterfaceType"]?.ToString() ?? "SATA";

                                var disk = new DiskInfo
                                {
                                    Index = index,
                                    Model = model,
                                    SerialNumber = serial,
                                    InterfaceType = type.Contains("USB", StringComparison.OrdinalIgnoreCase) ? "USB" : "HDD/SSD",
                                    TotalSize = size,
                                    HealthStatus = "Healthy"
                                };

                                disks.Add(disk);
                                diskMap[index] = disk;
                            }
                            catch
                            {
                                // Skip individual failed drive parse
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore Win32_DiskDrive query failures
                }

                // 2. Query MSFT_PhysicalDisk to enrich with MediaType (SSD vs HDD), BusType, and HealthStatus
                try
                {
                    using (var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage", "SELECT * FROM MSFT_PhysicalDisk"))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject drive in collection)
                        {
                            try
                            {
                                int index = Convert.ToInt32(drive["DeviceId"]);
                                if (diskMap.TryGetValue(index, out var disk))
                                {
                                    ushort mediaType = Convert.ToUInt16(drive["MediaType"]); // 3 = HDD, 4 = SSD, 0 = Unspecified
                                    ushort busType = Convert.ToUInt16(drive["BusType"]); // 17 = NVMe, 8 = SATA SSD, 7 = USB, etc.
                                    ushort health = Convert.ToUInt16(drive["HealthStatus"]); // 0 = Healthy, 1 = Warning, 2 = Critical

                                    // Enrich InterfaceType
                                    if (busType == 17) disk.InterfaceType = "NVMe";
                                    else if (busType == 7) disk.InterfaceType = "USB";
                                    else if (mediaType == 4) disk.InterfaceType = "SSD";
                                    else if (mediaType == 3) disk.InterfaceType = "HDD";

                                    // Enrich HealthStatus
                                    string healthStr = "Healthy";
                                    if (health == 1) healthStr = "Warning";
                                    else if (health == 2) healthStr = "Critical";
                                    disk.HealthStatus = healthStr;
                                }
                            }
                            catch
                            {
                                // Skip enrichment for this item
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore MSFT_PhysicalDisk query failures (e.g., on Windows 7)
                }

                // Query Partitions and Drive Letters
                foreach (var disk in disks)
                {
                    PopulatePartitions(disk);
                }

                // Sort by Index
                disks.Sort((a, b) => a.Index.CompareTo(b.Index));
                return disks;
            });
        }

        private void PopulatePartitions(DiskInfo disk)
        {
            try
            {
                // Query partition layout style (GPT/MBR)
                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_DiskDrive WHERE Index = {disk.Index}"))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject drive in collection)
                    {
                        string partStyle = drive["Partitions"] != null ? "MBR" : "GPT"; // basic heuristic
                        // WMI Win32_DiskDrive doesn't directly show GPT/MBR, but we can check Signature or PartitionStyle
                        var signature = drive["Signature"];
                        disk.PartitionStyle = signature != null && Convert.ToInt64(signature) != 0 ? "MBR" : "GPT";
                    }
                }

                // Get partitions on this disk
                string query = $"SELECT * FROM Win32_DiskPartition WHERE DiskIndex = {disk.Index}";
                using (var searcher = new ManagementObjectSearcher(query))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject part in collection)
                    {
                        try
                        {
                            int index = Convert.ToInt32(part["Index"]);
                            string name = part["Name"]?.ToString() ?? $"Partition {index}";
                            long size = Convert.ToInt64(part["Size"]);
                            long offset = Convert.ToInt64(part["StartingOffset"]);
                            bool bootable = Convert.ToBoolean(part["BootPartition"]);
                            bool primary = Convert.ToBoolean(part["PrimaryPartition"]);

                            string driveLetter = string.Empty;
                            string fileSystem = "RAW";
                            long freeSpace = 0;
                            long usedSpace = size;

                            // Associate Partition with Logical Disk to get Drive Letter and Filesystem info
                            string assocQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{part["DeviceID"]}'}} WHERE AssocClass = Win32_LogicalDiskToPartition";
                            using (var assocSearcher = new ManagementObjectSearcher(assocQuery))
                            using (var logicalDisks = assocSearcher.Get())
                            {
                                foreach (ManagementObject ld in logicalDisks)
                                {
                                    driveLetter = ld["DeviceID"]?.ToString() ?? string.Empty; // e.g. C:
                                    fileSystem = ld["FileSystem"]?.ToString() ?? "RAW";
                                    freeSpace = Convert.ToInt64(ld["FreeSpace"]);
                                    usedSpace = size - freeSpace;

                                    if (driveLetter.Equals("C:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        disk.IsSystemDisk = true;
                                    }
                                }
                            }

                            var partition = new PartitionInfo
                            {
                                Index = index,
                                Name = name,
                                DriveLetter = driveLetter,
                                FileSystem = fileSystem,
                                TotalSize = size,
                                UsedSize = usedSpace,
                                FreeSize = freeSpace,
                                IsSystem = bootable || driveLetter.Equals("C:", StringComparison.OrdinalIgnoreCase),
                                StartOffset = offset,
                                PartitionType = primary ? "Primary" : "Extended"
                            };

                            disk.Partitions.Add(partition);
                        }
                        catch
                        {
                            // Ignore
                        }
                    }
                }

                // Sort partitions by offset
                disk.Partitions.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            }
            catch
            {
                // Ignore
            }
        }

        public Task<DiskInfo?> GetDiskDetailsAsync(int index)
        {
            return Task.Run(async () =>
            {
                var disks = await GetDisksAsync();
                return disks.Find(d => d.Index == index);
            });
        }

        public Task RefreshDisksAsync()
        {
            return Task.CompletedTask;
        }

        public bool IsUefiBoot()
        {
            try
            {
                // Reading HKLM\System\CurrentControlSet\Control\PEFirmwareType
                // 1 = BIOS, 2 = UEFI
                var value = Registry.GetValue(@"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control", "PEFirmwareType", null);
                if (value != null)
                {
                    int type = Convert.ToInt32(value);
                    return type == 2;
                }
            }
            catch
            {
                // Fallback
            }

            return false;
        }

        public string GetOperatingSystemName()
        {
            try
            {
                var reg = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "Windows");
                var displayVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion", "");
                return $"{reg} {displayVersion}".Trim();
            }
            catch
            {
                return Environment.OSVersion.ToString();
            }
        }
    }
}
