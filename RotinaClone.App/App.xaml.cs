using RotinaClone.App.Helpers;
using System;

namespace RotinaClone.App
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
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
