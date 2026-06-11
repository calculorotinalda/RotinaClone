using System;
using System.IO;
using System.Text;
using System.Windows.Input;
using RotinaClone.Domain.Interfaces;
using RotinaClone.App.Helpers;
using RotinaClone.Infrastructure.Services;
using RotinaClone.Infrastructure.Data;
using RotinaClone.Application.Cloning;
using RotinaClone.Application.Services;

namespace RotinaClone.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // ViewModels
        public DashboardViewModel DashboardVM { get; }
        public CloneWizardViewModel CloneWizardVM { get; }
        public BackupManagerViewModel BackupManagerVM { get; }
        public WinPeCreatorViewModel WinPeCreatorVM { get; }
        public SettingsViewModel SettingsVM { get; }
        public LogViewModel LogVM { get; }

        private ViewModelBase _currentViewModel;
        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ICommand NavigateCommand { get; }

        public MainViewModel()
        {
            // Core Services Initialization
            IDiskService diskService = new WindowsDiskService();
            ISettingsRepository settingsRepo = new SettingsRepository();
            ICloningEngine cloningEngine = new CloningEngine();
            IBackupService backupService = new BackupService();

            // Ensure log file exists at startup
            EnsureLogFile();

            // Error reporter lambda to update status message and log
            Action<string> reportError = msg =>
            {
                StatusMessage = msg;
                AppendLog($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}");
            };

            // ViewModel Instantiation with error reporting
            DashboardVM = new DashboardViewModel(diskService);
            CloneWizardVM = new CloneWizardViewModel(diskService, cloningEngine, reportError);
            BackupManagerVM = new BackupManagerViewModel(settingsRepo, backupService, reportError);
            WinPeCreatorVM = new WinPeCreatorViewModel(diskService, reportError);
            SettingsVM = new SettingsViewModel(settingsRepo);
            LogVM = new LogViewModel();

            // Set default landing page
            _currentViewModel = DashboardVM;

            NavigateCommand = new RelayCommand<string>(Navigate);
        }

        private void EnsureLogFile()
        {
            try
            {
                // Primary log location (local app data)
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RotinaClone", "logs");
                Directory.CreateDirectory(folder);
                var logFile = Path.Combine(folder, "log.txt");
                if (!File.Exists(logFile))
                {
                    using (var fs = new FileStream(logFile, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(fs, Encoding.UTF8))
                    {
                        writer.WriteLine($"[INFO] Application started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                // Secondary log location for easy development access (project root)
                var devLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                if (!File.Exists(devLogPath))
                {
                    using (var fs = new FileStream(devLogPath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(fs, Encoding.UTF8))
                    {
                        writer.WriteLine($"[INFO] Development log created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    }
                }
            }
            catch
            {
                // Silently ignore any failures
            }
        }

        private void AppendLog(string message)
        {
            try
            {
                // Primary log (local app data)
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RotinaClone", "logs");
                Directory.CreateDirectory(folder);
                var logFile = Path.Combine(folder, "log.txt");
                using (var fs = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.WriteLine(message);
                }

                // Secondary development log (project root)
                var devLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
                using (var fs = new FileStream(devLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.WriteLine(message);
                }
            }
            catch
            {
                // Swallow logging errors
            }
        }

        private void Navigate(string destination)
        {
            switch (destination)
            {
                case "Dashboard":
                    CurrentViewModel = DashboardVM;
                    _ = DashboardVM.LoadDataAsync();
                    break;
                case "Clone":
                    CurrentViewModel = CloneWizardVM;
                    break;
                case "Backup":
                    CurrentViewModel = BackupManagerVM;
                    break;
                case "WinPE":
                    CurrentViewModel = WinPeCreatorVM;
                    break;
                case "Settings":
                    CurrentViewModel = SettingsVM;
                    break;
                case "Logs":
                    CurrentViewModel = LogVM;
                    break;
            }
        }
    }
}
