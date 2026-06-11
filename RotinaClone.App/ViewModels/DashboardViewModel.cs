using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using RotinaClone.Domain.Interfaces;
using RotinaClone.Domain.Models;
using RotinaClone.App.Helpers;
using RotinaClone.Infrastructure.Services;

namespace RotinaClone.App.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IDiskService _diskService;
        private readonly WmiMonitorService _wmiMonitor;
        
        private ObservableCollection<DiskInfo> _disks = new ObservableCollection<DiskInfo>();
        private string _osName = "Windows 11 Enterprise";
        private string _bootType = "UEFI Secure Boot";
        private int _totalDisksCount;
        private string _systemDiskModel = "Unknown";
        private double _availableSpaceGB;
        private double _totalSpaceGB;
        private bool _isBusy;

        public ObservableCollection<DiskInfo> Disks
        {
            get => _disks;
            set => SetProperty(ref _disks, value);
        }

        public string OsName
        {
            get => _osName;
            set => SetProperty(ref _osName, value);
        }

        public string BootType
        {
            get => _bootType;
            set => SetProperty(ref _bootType, value);
        }

        public int TotalDisksCount
        {
            get => _totalDisksCount;
            set => SetProperty(ref _totalDisksCount, value);
        }

        public string SystemDiskModel
        {
            get => _systemDiskModel;
            set => SetProperty(ref _systemDiskModel, value);
        }

        public double AvailableSpaceGB
        {
            get => _availableSpaceGB;
            set => SetProperty(ref _availableSpaceGB, value);
        }

        public double TotalSpaceGB
        {
            get => _totalSpaceGB;
            set => SetProperty(ref _totalSpaceGB, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand RefreshCommand { get; }

        public DashboardViewModel(IDiskService diskService)
        {
            _diskService = diskService;
            _wmiMonitor = new WmiMonitorService();
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            
            // Initial asynchronous load
            Task.Run(async () => await LoadDataAsync());
        }

        public async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                OsName = _diskService.GetOperatingSystemName();
                BootType = _diskService.IsUefiBoot() ? "UEFI Mode (Secure Boot)" : "Legacy BIOS Mode";

                var rawDisks = await _diskService.GetDisksAsync();
                
                // Fetch dynamic SMART values and temperatures
                foreach (var d in rawDisks)
                {
                    d.Temperature = _wmiMonitor.GetDiskTemperature(d.Index);
                    d.HealthStatus = _wmiMonitor.GetSmartHealth(d.Index);
                    d.LifeRemainingPercent = _wmiMonitor.GetDiskLifeRemaining(d.Index);
                    
                    // Check BitLocker state for each partition
                    foreach (var p in d.Partitions)
                    {
                        p.IsBitLockerEnabled = _wmiMonitor.IsBitLockerEnabled(p.DriveLetter);
                    }
                    d.IsBitLockerEnabled = d.Partitions.Any(p => p.IsBitLockerEnabled);
                }

                // Update properties on UI thread
                App.Current?.Dispatcher?.Invoke(() =>
                {
                    Disks.Clear();
                    double totalFree = 0;
                    double totalSize = 0;

                    foreach (var disk in rawDisks)
                    {
                        Disks.Add(disk);
                        
                        if (disk.IsSystemDisk)
                        {
                            SystemDiskModel = disk.Model;
                        }

                        foreach (var part in disk.Partitions)
                        {
                            totalFree += part.FreeSizeGB;
                            totalSize += part.TotalSizeGB;
                        }
                    }

                    TotalDisksCount = Disks.Count;
                    AvailableSpaceGB = totalFree;
                    TotalSpaceGB = totalSize;
                });
            }
            catch (Exception)
            {
                // Catch any UI dispatcher locks or WMI errors
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
