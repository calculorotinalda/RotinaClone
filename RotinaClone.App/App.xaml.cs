using RotinaClone.App.Helpers;
using System;

namespace RotinaClone.App
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
            try
            {
                string systemDir = Environment.SystemDirectory;
                if (!string.IsNullOrEmpty(systemDir) && string.Equals(System.IO.Path.GetPathRoot(systemDir), @"X:\", StringComparison.OrdinalIgnoreCase))
                {
                    System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

                    // Run wpeinit.exe to ensure storage drivers and WMI are initialized in WinPE
                    try
                    {
                        string wpeinitPath = System.IO.Path.Combine(systemDir, "wpeinit.exe");
                        if (System.IO.File.Exists(wpeinitPath))
                        {
                            var psi = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = wpeinitPath,
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            var process = System.Diagnostics.Process.Start(psi);
                            process?.WaitForExit();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.AppendLog($"[WARNING] Falha ao executar wpeinit.exe em segundo plano: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.AppendLog($"[WARNING] Falha ao definir modo de renderização por software ou wpeinit: {ex.Message}");
            }

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogHelper.AppendLog($"[UNHANDLED EXCEPTION] {e.Exception.Message}\n{e.Exception.StackTrace}");
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogHelper.AppendLog($"[UNHANDLED DOMAIN EXCEPTION] {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            LogHelper.AppendLog($"[UNOBSERVED TASK EXCEPTION] {e.Exception.Message}\n{e.Exception.StackTrace}");
            e.SetObserved();
        }
    }
}
