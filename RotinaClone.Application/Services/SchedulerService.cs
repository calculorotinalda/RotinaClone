using System;
using System.Threading.Tasks;
using RotinaClone.Domain.Models;
using RotinaClone.Infrastructure.Native;

namespace RotinaClone.Application.Services
{
    public class SchedulerService
    {
        public async Task ScheduleJobAsync(BackupJob job)
        {
            if (job.ScheduleType.Equals("Manual", StringComparison.OrdinalIgnoreCase))
            {
                await UnscheduleJobAsync(job.Id);
                return;
            }

            try
            {
                // Formulate target arguments to launch the CLI executable for this job
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rotinaclone-cli.exe");
                string taskName = $"RotinaClone_Backup_{job.Id}";
                string taskRun = $"\\\"{exePath}\\\" run-job {job.Id}";

                string frequency = "Daily";
                if (job.ScheduleType.Equals("Weekly", StringComparison.OrdinalIgnoreCase)) frequency = "Weekly";
                else if (job.ScheduleType.Equals("Monthly", StringComparison.OrdinalIgnoreCase)) frequency = "Monthly";

                // PowerShell command to register a scheduled task
                string script = $@"
$action = New-ScheduledTaskAction -Execute '{exePath}' -Argument 'run-job {job.Id}'
$trigger = New-ScheduledTaskTrigger -{frequency} -At '{job.ScheduleTime}'
Register-ScheduledTask -TaskName '{taskName}' -Action $action -Trigger $trigger -Description 'Rotina Clone Backup: {job.Name}' -Force
";
                var result = await PowerShellRunner.RunPowerShellScriptAsync(script);
                if (result.ExitCode != 0)
                {
                    // Fallback using schtasks.exe
                    string schtasksFreq = frequency.ToUpper();
                    string cmd = $"schtasks /create /tn \"{taskName}\" /tr \"{exePath} run-job {job.Id}\" /sc {schtasksFreq} /st {job.ScheduleTime} /f";
                    await PowerShellRunner.RunCommandAsync("cmd.exe", $"/c {cmd}");
                }
            }
            catch
            {
                // Ignore or log
            }
        }

        public async Task UnscheduleJobAsync(int jobId)
        {
            try
            {
                string taskName = $"RotinaClone_Backup_{jobId}";
                string script = $"Unregister-ScheduledTask -TaskName '{taskName}' -Confirm:$false";
                var result = await PowerShellRunner.RunPowerShellScriptAsync(script);
                if (result.ExitCode != 0)
                {
                    await PowerShellRunner.RunCommandAsync("cmd.exe", $"/c schtasks /delete /tn \"{taskName}\" /f");
                }
            }
            catch
            {
                // Ignore
            }
        }
    }
}
