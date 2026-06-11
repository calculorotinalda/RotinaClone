using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RotinaClone.Domain.Interfaces;
using RotinaClone.Domain.Models;
using RotinaClone.App.Helpers;
using RotinaClone.Application.Cloning;

namespace RotinaClone.App.ViewModels
{
    public class CloneWizardViewModel : ViewModelBase
    {
        private readonly IDiskService _diskService;
        private readonly ICloningEngine _cloningEngine;
        private CancellationTokenSource? _cts;
        private readonly Action<string> _reportError;

        private int _currentStep = 1; // 1 = Source Select, 2 = Target Select, 3 = Review Options, 4 = Progress
        private ObservableCollection<DiskInfo> _availableDisks = new ObservableCollection<DiskInfo>();
        
        private DiskInfo? _selectedSourceDisk;
        private DiskInfo? _selectedDestinationDisk;
        
        private bool _isSectorBySector = false;
        private bool _isIntelligent = true;
        private bool _align4K = true;
        private bool _useVss = true;
        private bool _verifyIntegrity = true;
        private bool _isSimulation = true; // Safety default switch

        private string _statusText = "Pronto";
        private int _progressPercent = 0;
        private string _speedText = "0.0 MB/s";
        private string _elapsedTimeText = "00:00:00";
        private string _remainingTimeText = "00:00:00";
        private string _currentOperationText = string.Empty;
        private string _operationLogs = string.Empty;

        private bool _canNavigateNext = false;
        private bool _isCloningRunning = false;
        private bool _isCloningComplete = false;

        public int CurrentStep
        {
            get => _currentStep;
            set { SetProperty(ref _currentStep, value); UpdateStepNavigationState(); }
        }

        public ObservableCollection<DiskInfo> AvailableDisks
        {
            get => _availableDisks;
            set => SetProperty(ref _availableDisks, value);
        }

        public DiskInfo? SelectedSourceDisk
        {
            get => _selectedSourceDisk;
            set { SetProperty(ref _selectedSourceDisk, value); UpdateStepNavigationState(); }
        }

        public DiskInfo? SelectedDestinationDisk
        {
            get => _selectedDestinationDisk;
            set { SetProperty(ref _selectedDestinationDisk, value); UpdateStepNavigationState(); }
        }

        public bool IsSectorBySector
        {
            get => _isSectorBySector;
            set { SetProperty(ref _isSectorBySector, value); if (value) IsIntelligent = false; }
        }

        public bool IsIntelligent
        {
            get => _isIntelligent;
            set { SetProperty(ref _isIntelligent, value); if (value) IsSectorBySector = false; }
        }

        public bool Align4K { get => _align4K; set => SetProperty(ref _align4K, value); }
        public bool UseVss { get => _useVss; set => SetProperty(ref _useVss, value); }
        public bool VerifyIntegrity { get => _verifyIntegrity; set => SetProperty(ref _verifyIntegrity, value); }
        public bool IsSimulation { get => _isSimulation; set => SetProperty(ref _isSimulation, value); }

        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        public int ProgressPercent { get => _progressPercent; set => SetProperty(ref _progressPercent, value); }
        public string SpeedText { get => _speedText; set => SetProperty(ref _speedText, value); }
        public string ElapsedTimeText { get => _elapsedTimeText; set => SetProperty(ref _elapsedTimeText, value); }
        public string RemainingTimeText { get => _remainingTimeText; set => SetProperty(ref _remainingTimeText, value); }
        public string CurrentOperationText { get => _currentOperationText; set => SetProperty(ref _currentOperationText, value); }
        public string OperationLogs
        {
            get => _operationLogs;
            set => SetProperty(ref _operationLogs, value);
        }

        public bool CanNavigateNext { get => _canNavigateNext; set => SetProperty(ref _canNavigateNext, value); }
        public bool IsCloningRunning { get => _isCloningRunning; set => SetProperty(ref _isCloningRunning, value); }
        public bool IsCloningComplete { get => _isCloningComplete; set => SetProperty(ref _isCloningComplete, value); }

        public ICommand NextCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand CancelCommand { get; }

        public Action<double, double>? OnSpeedUpdated { get; set; } // Event hooks for real-time speed chart

        public CloneWizardViewModel(IDiskService diskService, ICloningEngine cloningEngine, Action<string> reportError)
        {
            _diskService = diskService;
            _cloningEngine = cloningEngine;
            _reportError = reportError;

            NextCommand = new RelayCommand(NextStep);
            BackCommand = new RelayCommand(PrevStep);
            StartCommand = new RelayCommand(async () => await StartCloningAsync());
            CancelCommand = new RelayCommand(CancelCloning);

            try
            {
                Task.Run(async () => await LoadDisksAsync());
            }
            catch (Exception ex)
            {
                _reportError?.Invoke($"Erro ao carregar discos: {ex.Message}");
            }
        }

        private async Task LoadDisksAsync()
        {
            var list = await _diskService.GetDisksAsync();
            App.Current?.Dispatcher?.Invoke(() =>
            {
                AvailableDisks.Clear();
                foreach (var d in list) AvailableDisks.Add(d);
            });
        }

        private void NextStep()
        {
            if (CurrentStep < 3)
            {
                CurrentStep++;
            }
        }

        private void PrevStep()
        {
            if (CurrentStep > 1 && !IsCloningRunning)
            {
                CurrentStep--;
            }
        }

        private void UpdateStepNavigationState()
        {
            if (CurrentStep == 1)
            {
                CanNavigateNext = SelectedSourceDisk != null;
            }
            else if (CurrentStep == 2)
            {
                CanNavigateNext = SelectedDestinationDisk != null && SelectedDestinationDisk != SelectedSourceDisk;
            }
            else
            {
                CanNavigateNext = true;
            }
        }

        private async Task StartCloningAsync()
        {
            if (SelectedSourceDisk == null || SelectedDestinationDisk == null) return;

            CurrentStep = 4; // Move to progress screen
            IsCloningRunning = true;
            IsCloningComplete = false;
            OperationLogs = $"[INFO] Sessão iniciada às {DateTime.Now:HH:mm:ss}\n";
            OperationLogs += $"[INFO] Origem: Disk {SelectedSourceDisk.Index} ({SelectedSourceDisk.Model})\n";
            OperationLogs += $"[INFO] Destino: Disk {SelectedDestinationDisk.Index} ({SelectedDestinationDisk.Model})\n";
            OperationLogs += $"[INFO] Modo Simulação: {IsSimulation}\n";

            _cts = new CancellationTokenSource();

            var options = new CloneOptions
            {
                SourceDiskIndex = SelectedSourceDisk.Index,
                DestinationDiskIndex = SelectedDestinationDisk.Index,
                IsSectorBySector = IsSectorBySector,
                IsIntelligent = IsIntelligent,
                Align4K = Align4K,
                UseVss = UseVss,
                VerifyIntegrity = VerifyIntegrity,
                IsSimulation = IsSimulation
            };

            try
            {
                await _cloningEngine.StartCloneAsync(options, (session) =>
                {
                    App.Current?.Dispatcher?.Invoke(() =>
                    {
                        ProgressPercent = session.PercentComplete;
                        StatusText = session.Status;
                        CurrentOperationText = session.CurrentOperation;
                        SpeedText = $"{session.CurrentSpeedMB:F1} MB/s";
                        ElapsedTimeText = session.ElapsedTime.ToString(@"hh\:mm\:ss");
                        RemainingTimeText = session.EstimatedTimeRemaining.ToString(@"hh\:mm\:ss");

                        if (!string.IsNullOrEmpty(session.LogMessage))
                        {
                            OperationLogs += $"[LOG] {session.LogMessage}\n";
                        }

                        // Trigger visual chart update if hooks exist
                        if (session.Status == "Running")
                        {
                            OnSpeedUpdated?.Invoke(session.CurrentSpeedMB, session.CurrentSpeedMB * 0.95);
                        }
                    });
                }, _cts.Token);

                App.Current?.Dispatcher?.Invoke(() =>
                {
                    IsCloningRunning = false;
                    IsCloningComplete = true;
                    OperationLogs += $"[SUCCESS] Operação finalizada com estado: {StatusText}\n";
                });
            }
            catch (OperationCanceledException)
            {
                App.Current?.Dispatcher?.Invoke(() =>
                {
                    StatusText = "Cancelado";
                    CurrentOperationText = "Clonagem abortada pelo utilizador.";
                    IsCloningRunning = false;
                    OperationLogs += "[WARNING] Operação cancelada pelo utilizador.\n";
                });
            }
            catch (Exception ex)
            {
                App.Current?.Dispatcher?.Invoke(() =>
                {
                    StatusText = "Falhou";
                    CurrentOperationText = $"Erro: {ex.Message}";
                    IsCloningRunning = false;
                    OperationLogs += $"[ERROR] Erro na clonagem: {ex.Message}\n";
                });
            }
        }

        private void CancelCloning()
        {
            _cts?.Cancel();
        }
    }
}
