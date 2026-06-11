using System.Threading.Tasks;
using System.Windows.Input;
using RotinaClone.Domain.Interfaces;
using RotinaClone.App.Helpers;

namespace RotinaClone.App.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsRepository _settingsRepo;
        
        private bool _isDarkMode = true;
        private bool _isSimulationMode = true;
        private string _smtpServer = "smtp.rotinaclone.com";
        private string _smtpEmail = "alerts@rotinaclone.com";
        private bool _isEmailEnabled = false;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    ThemeManager.SwitchTheme(value);
                    Task.Run(() => _settingsRepo.SaveSettingAsync("Theme", value ? "Dark" : "Light"));
                }
            }
        }

        public bool IsSimulationMode
        {
            get => _isSimulationMode;
            set
            {
                if (SetProperty(ref _isSimulationMode, value))
                {
                    Task.Run(() => _settingsRepo.SaveSettingAsync("SimulationMode", value.ToString()));
                }
            }
        }

        public string SmtpServer
        {
            get => _smtpServer;
            set
            {
                if (SetProperty(ref _smtpServer, value))
                {
                    Task.Run(() => _settingsRepo.SaveSettingAsync("SmtpServer", value));
                }
            }
        }

        public string SmtpEmail
        {
            get => _smtpEmail;
            set
            {
                if (SetProperty(ref _smtpEmail, value))
                {
                    Task.Run(() => _settingsRepo.SaveSettingAsync("SmtpEmail", value));
                }
            }
        }

        public bool IsEmailEnabled
        {
            get => _isEmailEnabled;
            set
            {
                if (SetProperty(ref _isEmailEnabled, value))
                {
                    Task.Run(() => _settingsRepo.SaveSettingAsync("EmailEnabled", value.ToString()));
                }
            }
        }

        public SettingsViewModel(ISettingsRepository settingsRepo)
        {
            _settingsRepo = settingsRepo;
            
            // Load settings asynchronously
            Task.Run(async () =>
            {
                string theme = await _settingsRepo.GetSettingAsync("Theme", "Dark");
                string sim = await _settingsRepo.GetSettingAsync("SimulationMode", "True");
                string smtp = await _settingsRepo.GetSettingAsync("SmtpServer", "smtp.rotinaclone.com");
                string email = await _settingsRepo.GetSettingAsync("SmtpEmail", "alerts@rotinaclone.com");
                string emailEnabled = await _settingsRepo.GetSettingAsync("EmailEnabled", "False");

                App.Current?.Dispatcher?.Invoke(() =>
                {
                    // Set fields directly to avoid raising duplicate db save events
                    _isDarkMode = theme == "Dark";
                    _isSimulationMode = bool.Parse(sim);
                    _smtpServer = smtp;
                    _smtpEmail = email;
                    _isEmailEnabled = bool.Parse(emailEnabled);

                    OnPropertyChanged(nameof(IsDarkMode));
                    OnPropertyChanged(nameof(IsSimulationMode));
                    OnPropertyChanged(nameof(SmtpServer));
                    OnPropertyChanged(nameof(SmtpEmail));
                    OnPropertyChanged(nameof(IsEmailEnabled));

                    // Make sure visual UI theme matches loaded settings
                    ThemeManager.SwitchTheme(_isDarkMode);
                });
            });
        }
    }
}
