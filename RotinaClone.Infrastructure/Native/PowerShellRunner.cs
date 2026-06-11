using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace RotinaClone.Infrastructure.Native
{
    public static class PowerShellRunner
    {
        public static async Task<(int ExitCode, string Output, string Error)> RunCommandAsync(string command, string args = "")
        {
            var tcs = new TaskCompletionSource<(int ExitCode, string Output, string Error)>();
            
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Exited += (s, e) =>
            {
                tcs.SetResult((process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString()));
                process.Dispose();
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                tcs.SetResult((-1, string.Empty, ex.Message));
            }

            return await tcs.Task;
        }

        public static async Task<(int ExitCode, string Output, string Error)> RunPowerShellScriptAsync(string scriptContent)
        {
            // Encode the script content in Base64 to avoid quote-escaping issues
            byte[] bytes = Encoding.Unicode.GetBytes(scriptContent);
            string base64Script = Convert.ToBase64String(bytes);
            return await RunCommandAsync("powershell.exe", $"-NoProfile -NonInteractive -EncodedCommand {base64Script}");
        }
    }
}
