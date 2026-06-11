using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RotinaClone.Domain.Interfaces;
using RotinaClone.Domain.Models;
using RotinaClone.App.Helpers;
using RotinaClone.Application.Services;

namespace RotinaClone.App.ViewModels
{
    public class BackupManagerViewModel : ViewModelBase
    {
        private readonly ISettingsRepository _settingsRepo;
        private readonly IBackupService _backupService;
        private readonly SchedulerService _schedulerService;
        private CancellationTokenSource? _cts;
        private readonly Action<string> _reportError;

        private ObservableCollection<BackupJob> _jobs = new ObservableCollection<BackupJob>();
        private BackupJob? _selectedJob;

        // Form fields for adding new job
        private string _newJobName = string.Empty;
        private string _newJobType = "Full"; // Full, Incremental, Differential
        private string _newSourcePath = string.Empty;
        private string _newDestinationPath = string.Empty;
        private string _newScheduleType = "Manual"; // Daily, Weekly, Monthly, Manual
        private string _newScheduleTime = "12:00";

        // Progress metrics
        private bool _isBackupRunning = false;
        private int _progressPercent = 0;
        private string _currentOpText = string.Empty;
        private string _speedText = "0 MB/s";

        public ObservableCollection<BackupJob> Jobs { get => _jobs; set => SetProperty(ref _jobs, value); }
        public BackupJob? SelectedJob { get => _selectedJob; set => SetProperty(ref _selectedJob, value); }

        public string NewJobName { get => _newJobName; set => SetProperty(ref _newJobName, value); }
        public string NewJobType { get => _newJobType; set => SetProperty(ref _newJobType, value); }
        public string NewSourcePath { get => _newSourcePath; set => SetProperty(ref _newSourcePath, value); }
        public string NewDestinationPath { get => _newDestinationPath; set => SetProperty(ref _newDestinationPath, value); }
        public string NewScheduleType { get => _newScheduleType; set => SetProperty(ref _newScheduleType, value); }
        public string NewScheduleTime { get => _newScheduleTime; set => SetProperty(ref _newScheduleTime, value); }

        public bool IsBackupRunning { get => _isBackupRunning; set => SetProperty(ref _isBackupRunning, value); }
        public int ProgressPercent { get => _progressPercent; set => SetProperty(ref _progressPercent, value); }
        public string CurrentOpText { get => _currentOpText; set => SetProperty(ref _currentOpText, value); }
        public string SpeedText { get => _speedText; set => SetProperty(ref _speedText, value); }

        public ICommand SaveJobCommand { get; }
        public ICommand RunJobCommand { get; }
        public ICommand DeleteJobCommand { get; }
        public ICommand CancelBackupCommand { get; }

        public BackupManagerViewModel(ISettingsRepository settingsRepo, IBackupService backupService, Action<string> reportError)
        {
            _settingsRepo = settingsRepo;
            _backupService = backupService;
            _schedulerService = new SchedulerService();
            _reportError = reportError;

            SaveJobCommand = new RelayCommand(async () => await SaveJobAsync());
            RunJobCommand = new RelayCommand(async () => await RunJobAsync());
            DeleteJobCommand = new RelayCommand(async () => await DeleteJobAsync());
            CancelBackupCommand = new RelayCommand(CancelBackup);

            try
            {
                Task.Run(async () => await LoadJobsAsync());
            }
            catch (Exception ex)
            {
                _reportError?.Invoke($"Erro ao carregar trabalhos de backup: {ex.Message}");
            }
        }

        private async Task LoadJobsAsync()
        {
            var list = await _settingsRepo.GetBackupJobsAsync();
            App.Current?.Dispatcher?.Invoke(() =>
            {
                Jobs.Clear();
                foreach (var j in list) Jobs.Add(j);
            });
        }

        private async Task SaveJobAsync()
        {
            if (string.IsNullOrEmpty(NewJobName) || string.IsNullOrEmpty(NewSourcePath) || string.IsNullOrEmpty(NewDestinationPath))
                return;

            var job = new BackupJob
            {
                Name = NewJobName,
                Type = NewJobType,
                SourcePath = NewSourcePath,
                DestinationPath = NewDestinationPath,
                ScheduleType = NewScheduleType,
                ScheduleTime = NewScheduleTime,
                IsEnabled = true
            };

            await _settingsRepo.SaveBackupJobAsync(job);
            await _schedulerService.ScheduleJobAsync(job);
            await LoadJobsAsync();

            // Clear inputs
            NewJobName = string.Empty;
            NewSourcePath = string.Empty;
            NewDestinationPath = string.Empty;
        }

        private async Task RunJobAsync()
        {
            if (SelectedJob == null || IsBackupRunning) return;

            IsBackupRunning = true;
            ProgressPercent = 0;
            CurrentOpText = "A inicializar cópia de segurança...";
            _cts = new CancellationTokenSource();

            try
            {
                await _backupService.RunBackupAsync(SelectedJob, (session) =>
                {
                    App.Current?.Dispatcher?.Invoke(() =>
                    {
                        ProgressPercent = session.PercentComplete;
                        CurrentOpText = session.CurrentOperation;
                        SpeedText = $"{session.CurrentSpeedMB:F1} MB/s";
                    });
                }, _cts.Token);

                // Update database last run time
                SelectedJob.LastRun = DateTime.Now;
                await _settingsRepo.SaveBackupJobAsync(SelectedJob);
            }
            catch (Exception ex)
            {
                App.Current?.Dispatcher?.Invoke(() => CurrentOpText = $"Erro: {ex.Message}");
            }
            finally
            {
                IsBackupRunning = false;
                await LoadJobsAsync();
            }
        }

        private async Task DeleteJobAsync()
        {
            if (SelectedJob == null) return;
            await _schedulerService.UnscheduleJobAsync(SelectedJob.Id);
            await _settingsRepo.DeleteBackupJobAsync(SelectedJob.Id);
            await LoadJobsAsync();
        }

        private void CancelBackup()
        {
            _cts?.Cancel();
        }
    }
}
