using System;
using System.IO;

namespace RotinaClone.App.Helpers
{
    public static class LogHelper
    {
        private static readonly string PrimaryLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RotinaClone", "logs", "log.txt");
        private static readonly string SecondaryLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

        static LogHelper()
        {
            EnsureLogFile();
        }

        private static void EnsureLogFile()
        {
            try
            {
                var primaryFolder = Path.GetDirectoryName(PrimaryLogPath);
                if (!Directory.Exists(primaryFolder))
                {
                    Directory.CreateDirectory(primaryFolder);
                }

                if (!File.Exists(PrimaryLogPath))
                {
                    File.WriteAllText(PrimaryLogPath, string.Empty);
                }

                var secondaryFolder = Path.GetDirectoryName(SecondaryLogPath);
                if (!Directory.Exists(secondaryFolder))
                {
                    Directory.CreateDirectory(secondaryFolder);
                }

                if (!File.Exists(SecondaryLogPath))
                {
                    File.WriteAllText(SecondaryLogPath, string.Empty);
                }
            }
            catch
            {
                // Silently ignore any errors while ensuring log files.
            }
        }

        public static void AppendLog(string message)
        {
            var timestamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            try
            {
                File.AppendAllText(PrimaryLogPath, timestamped);
            }
            catch
            {
                // ignore primary log failures
            }
            try
            {
                File.AppendAllText(SecondaryLogPath, timestamped);
            }
            catch
            {
                // ignore secondary log failures
            }
        }
    }
}
