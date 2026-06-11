using System;
using System.IO;
using System.Text;
using System.Windows.Input;
using RotinaClone.App.Helpers;

namespace RotinaClone.App.ViewModels
{
    public class LogViewModel : ViewModelBase
    {
        private string _logContent = string.Empty;

        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearLogsCommand { get; }

        public LogViewModel()
        {
            RefreshCommand = new RelayCommand(LoadLogs);
            ClearLogsCommand = new RelayCommand(ClearLogs);
            
            LoadLogs();
        }

        private void LoadLogs()
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RotinaClone", "logs");
                string logFile = Path.Combine(folder, "log.txt");

                if (File.Exists(logFile))
                {
                    // Open with FileShare.ReadWrite to avoid locking collisions with active Serilog log writers
                    using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        LogContent = reader.ReadToEnd();
                    }
                }
                else
                {
                    LogContent = "Nenhum registo de log encontrado.";
                }
            }
            catch (Exception ex)
            {
                LogContent = $"Erro ao ler logs: {ex.Message}";
            }
        }

        private void ClearLogs()
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RotinaClone", "logs");
                string logFile = Path.Combine(folder, "log.txt");

                if (File.Exists(logFile))
                {
                    File.WriteAllText(logFile, string.Empty);
                }
                
                LogContent = "Registos limpos.";
            }
            catch (Exception ex)
            {
                LogContent = $"Erro ao limpar logs: {ex.Message}";
            }
        }
    }
}
