using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RotinaClone.Application.Cloning;
using RotinaClone.Application.Services;
using RotinaClone.Domain.Models;
using RotinaClone.Infrastructure.Data;
using RotinaClone.Infrastructure.Services;

namespace RotinaClone.CLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=================================================");
            Console.WriteLine("   ROTINA CLONE ENTERPRISE EDITION - CLI TOOL    ");
            Console.WriteLine("=================================================");
            Console.ResetColor();

            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                PrintHelp();
                return;
            }

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "list":
                        await ListDisksAsync();
                        break;
                    case "clone":
                        await ExecuteCloneAsync(args);
                        break;
                    case "run-job":
                        await ExecuteBackupJobAsync(args);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[ERROR] Comando desconhecido: {command}");
                        Console.ResetColor();
                        PrintHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[CRITICAL] Falha na execução: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("\nUso:");
            Console.WriteLine("  rotinaclone-cli.exe <comando> [opções]");
            Console.WriteLine("\nComandos:");
            Console.WriteLine("  list                               Lista os discos rígidos físicos detetados.");
            Console.WriteLine("  clone [opções]                      Inicia uma operação de clonagem de disco.");
            Console.WriteLine("  run-job <id>                       Executa um agendamento de backup pelo ID.");
            Console.WriteLine("\nOpções para 'clone':");
            Console.WriteLine("  --source <index>                   Index do disco de origem.");
            Console.WriteLine("  --target <index>                   Index do disco de destino.");
            Console.WriteLine("  --intelligent                      Ativa a cópia inteligente (padrão).");
            Console.WriteLine("  --sector                           Ativa a cópia setor a setor.");
            Console.WriteLine("  --execute                          Desativa a simulação (gravação real!).");
        }

        private static async Task ListDisksAsync()
        {
            var diskService = new WindowsDiskService();
            Console.WriteLine("\nProcurando discos físicos...");
            var disks = await diskService.GetDisksAsync();

            foreach (var disk in disks)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Disk {disk.Index}: {disk.Model} ({disk.TotalSizeGB:F1} GB) [{disk.InterfaceType}] - {disk.PartitionStyle}");
                Console.ResetColor();

                foreach (var part in disk.Partitions)
                {
                    string letter = string.IsNullOrEmpty(part.DriveLetter) ? "(Sem Letra)" : part.DriveLetter;
                    Console.WriteLine($"  └─ Partição {part.Index}: {letter} {part.FileSystem} - {part.TotalSizeGB:F1} GB");
                }
            }
        }

        private static async Task ExecuteCloneAsync(string[] args)
        {
            int source = GetArgValueInt(args, "--source", -1);
            int target = GetArgValueInt(args, "--target", -1);
            bool sector = args.Contains("--sector");
            bool execute = args.Contains("--execute");

            if (source == -1 || target == -1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Parâmetros --source e --target são obrigatórios.");
                Console.ResetColor();
                return;
            }

            var options = new CloneOptions
            {
                SourceDiskIndex = source,
                DestinationDiskIndex = target,
                IsSectorBySector = sector,
                IsIntelligent = !sector,
                IsSimulation = !execute
            };

            Console.WriteLine($"\n[INFO] Iniciando clonagem (Simulação: {options.IsSimulation})...");
            var engine = new CloningEngine();
            
            using (var cts = new CancellationTokenSource())
            {
                await engine.StartCloneAsync(options, (session) =>
                {
                    Console.Write($"\rProgresso: {session.PercentComplete}% | Velocidade: {session.CurrentSpeedMB:F1} MB/s | Operação: {session.CurrentOperation}              ");
                }, cts.Token);
            }
            Console.WriteLine("\n[SUCCESS] Clonagem concluída.");
        }

        private static async Task ExecuteBackupJobAsync(string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int jobId))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] ID do trabalho de backup não especificado ou inválido.");
                Console.ResetColor();
                return;
            }

            var settingsRepo = new SettingsRepository();
            var backupService = new BackupService();

            var jobs = await settingsRepo.GetBackupJobsAsync();
            var job = jobs.FirstOrDefault(j => j.Id == jobId);

            if (job == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Trabalho de backup com ID {jobId} não encontrado.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"\n[INFO] Iniciando Backup: {job.Name} ({job.Type})...");
            using (var cts = new CancellationTokenSource())
            {
                await backupService.RunBackupAsync(job, (session) =>
                {
                    Console.Write($"\rProgresso: {session.PercentComplete}% | Estado: {session.Status} | {session.CurrentOperation}              ");
                }, cts.Token);
            }
            Console.WriteLine("\n[SUCCESS] Trabalho concluído.");
        }

        private static int GetArgValueInt(string[] args, string flag, int defaultValue)
        {
            int index = Array.IndexOf(args, flag);
            if (index != -1 && index + 1 < args.Length && int.TryParse(args[index + 1], out int val))
            {
                return val;
            }
            return defaultValue;
        }
    }
}
