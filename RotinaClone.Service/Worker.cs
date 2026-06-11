using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RotinaClone.Application.Services;
using RotinaClone.Infrastructure.Data;

namespace RotinaClone.Service
{
    public class Worker : BackgroundService
    {
        private readonly HttpListener _httpListener;
        private readonly BackupService _backupService;
        private readonly SettingsRepository _settingsRepo;

        public Worker()
        {
            _backupService = new BackupService();
            _settingsRepo = new SettingsRepository();

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:9091/");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Start REST API HTTP Listener
            try
            {
                _httpListener.Start();
                _ = Task.Run(() => ListenToHttpRequests(_httpListener, stoppingToken), stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to start HTTP REST API: {ex.Message}");
            }

            // Scheduling check loop (runs every 60 seconds)
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndRunScheduledBackupsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // Fail-safe catch for database lock exceptions
                    File.AppendAllText("service_error_log.txt", $"{DateTime.Now}: Loop error {ex.Message}\n");
                }
                
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }

            _httpListener.Stop();
        }

        private async Task CheckAndRunScheduledBackupsAsync(CancellationToken stoppingToken)
        {
            var jobs = await _settingsRepo.GetBackupJobsAsync();
            var now = DateTime.Now;

            foreach (var job in jobs)
            {
                if (!job.IsEnabled || job.ScheduleType == "Manual") continue;

                // Simple execution check: parse HH:mm schedule
                if (TimeSpan.TryParse(job.ScheduleTime, out var time))
                {
                    var scheduledDateTime = DateTime.Today.Add(time);
                    
                    // If scheduled time has passed today and last run is not today (or null)
                    bool wasRunToday = job.LastRun.HasValue && job.LastRun.Value.Date == DateTime.Today;
                    
                    if (now >= scheduledDateTime && !wasRunToday)
                    {
                        // Run backup job in background
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _backupService.RunBackupAsync(job, (progress) => { }, stoppingToken);
                                job.LastRun = DateTime.Now;
                                await _settingsRepo.SaveBackupJobAsync(job);
                            }
                            catch (Exception)
                            {
                                // Log
                            }
                        }, stoppingToken);
                    }
                }
            }
        }

        private async Task ListenToHttpRequests(HttpListener listener, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleHttpRequest(context), stoppingToken);
                }
                catch
                {
                    // Ignore
                }
            }
        }

        private async Task HandleHttpRequest(HttpListenerContext context)
        {
            var response = context.Response;
            string responseString = "{\"status\":\"Running\",\"service\":\"Rotina Clone Enterprise Service\",\"version\":\"1.0.0\"}";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            try
            {
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                // Ignore
            }
            finally
            {
                response.OutputStream.Close();
            }
        }
    }
}
