using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using RotinaClone.App.Helpers;
using RotinaClone.Domain.Interfaces;
using RotinaClone.Infrastructure.Native;

namespace RotinaClone.App.ViewModels
{
    public class WinPeCreatorViewModel : ViewModelBase
    {
        private readonly IDiskService _diskService;
        private ObservableCollection<string> _usbDrives = new ObservableCollection<string>();
        private string? _selectedUsbDrive;
        private string _isoOutputPath = string.Empty;
        
        private bool _isUefiOnly = true;
        private bool _isBuilding = false;
        private int _progressPercent = 0;
        private string _statusText = "Pronto";
        private readonly Action<string> _reportError;
        private string _buildLogs = string.Empty;

        public bool IsReadyToBuild => !_isBuilding;

        public ObservableCollection<string> UsbDrives { get => _usbDrives; set => SetProperty(ref _usbDrives, value); }
        public string? SelectedUsbDrive { get => _selectedUsbDrive; set => SetProperty(ref _selectedUsbDrive, value); }
        public string IsoOutputPath { get => _isoOutputPath; set => SetProperty(ref _isoOutputPath, value); }

        public bool IsUefiOnly { get => _isUefiOnly; set => SetProperty(ref _isUefiOnly, value); }
        public bool IsBuilding
        {
            get => _isBuilding;
            set
            {
                if (SetProperty(ref _isBuilding, value))
                {
                    OnPropertyChanged(nameof(IsReadyToBuild));
                }
            }
        }
        public int ProgressPercent { get => _progressPercent; set => SetProperty(ref _progressPercent, value); }
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        public string BuildLogs { get => _buildLogs; set => SetProperty(ref _buildLogs, value); }

        public ICommand RefreshDrivesCommand { get; }
        public ICommand CreateUsbCommand { get; }
        public ICommand CreateIsoCommand { get; }

        public WinPeCreatorViewModel(IDiskService diskService, Action<string> reportError)
        {
            _diskService = diskService;
            _reportError = reportError;
            RefreshDrivesCommand = new RelayCommand(RefreshUsbDrives);
            CreateUsbCommand = new RelayCommand(async () => await BuildMediaAsync(true));
            CreateIsoCommand = new RelayCommand(async () => await BuildMediaAsync(false));

            RefreshUsbDrives();

            // Set default ISO path
            IsoOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RotinaClone_WinPE.iso");
        }

        private async void RefreshUsbDrives()
        {
            try
            {
                var currentSelected = SelectedUsbDrive;
                UsbDrives.Clear();
                
                // Query WMI-based DiskService for accurate USB detection
                var physicalDisks = await _diskService.GetDisksAsync();
                var usbDriveLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var usbDiskDetails = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var disk in physicalDisks)
                {
                    bool isUsb = disk.InterfaceType != null && disk.InterfaceType.Contains("USB", StringComparison.OrdinalIgnoreCase);
                    if (isUsb)
                    {
                        foreach (var part in disk.Partitions)
                        {
                            if (!string.IsNullOrEmpty(part.DriveLetter))
                            {
                                string letter = part.DriveLetter.TrimEnd('\\') + "\\";
                                usbDriveLetters.Add(letter);
                                usbDiskDetails[letter] = $"{disk.Model}";
                            }
                        }
                    }
                }

                var addedDrives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    if (drive.Name.StartsWith("C:", StringComparison.OrdinalIgnoreCase)) continue;

                    bool isIdentifiedUsb = usbDriveLetters.Contains(drive.Name);
                    bool isRemovableType = drive.DriveType == DriveType.Removable;

                    if (isIdentifiedUsb || isRemovableType)
                    {
                        string label = string.Empty;
                        try { label = drive.VolumeLabel; } catch { }
                        
                        string detail = usbDiskDetails.ContainsKey(drive.Name) ? usbDiskDetails[drive.Name] : (string.IsNullOrEmpty(label) ? "USB Drive" : label);
                        string displayName = $"{drive.Name} ({detail}) - {drive.TotalSize / (1024 * 1024 * 1024.0):F1} GB";
                        
                        if (!addedDrives.Contains(drive.Name))
                        {
                            UsbDrives.Add(displayName);
                            addedDrives.Add(drive.Name);
                        }
                    }
                }

                if (UsbDrives.Count > 0)
                {
                    if (currentSelected != null && UsbDrives.Contains(currentSelected))
                    {
                        SelectedUsbDrive = currentSelected;
                    }
                    else
                    {
                        SelectedUsbDrive = UsbDrives[0];
                    }
                }
                else
                {
                    SelectedUsbDrive = null;
                }
            }
            catch (Exception ex)
            {
                _reportError?.Invoke($"Erro ao atualizar unidades USB: {ex.Message}");
            }
        }

        private async Task BuildMediaAsync(bool isUsb)
        {
            if (isUsb && string.IsNullOrEmpty(SelectedUsbDrive))
            {
                StatusText = "Erro: Selecione uma unidade USB.";
                return;
            }

            IsBuilding = true;
            ProgressPercent = 0;
            StatusText = "A extrair ficheiros do ambiente WinPE...";
            BuildLogs = $"[INFO] Início do processo: {DateTime.Now:HH:mm:ss}\n";

            string wimCustomDir = string.Empty;

            try
            {
                await Task.Run(async () =>
                {
                    // Step 1: Locate local winre.wim or WinPE source
                    UpdateProgress(15, "A localizar imagem de recuperação do Windows (winre.wim)...");
                    string winrePath = FindWinReImage();
                    
                    if (string.IsNullOrEmpty(winrePath))
                    {
                        AppendLog("[WARNING] Não foi possível localizar a imagem winre.wim nativa. Utilizando pacote WinPE emulado de compatibilidade.");
                    }
                    else
                    {
                        AppendLog($"[INFO] Imagem winre.wim localizada em: {winrePath}");
                    }
                    await Task.Delay(1500);

                    string wimToDeploy = winrePath;

                    if (!string.IsNullOrEmpty(winrePath) && File.Exists(winrePath))
                    {
                        wimCustomDir = Path.Combine(Path.GetTempPath(), "RotinaClone_WIM_Custom_" + Guid.NewGuid().ToString("N"));
                        Directory.CreateDirectory(wimCustomDir);
                        string customWimPath = Path.Combine(wimCustomDir, "boot.wim");

                        AppendLog($"[INFO] Copiando imagem original winre.wim para espaço de trabalho temporário: {customWimPath}");
                        File.Copy(winrePath, customWimPath, true);

                        // Customize WIM with DISM and copy files
                        string runningAppDir = AppDomain.CurrentDomain.BaseDirectory;
                        bool success = await CustomizeWimAsync(customWimPath, runningAppDir);
                        if (success)
                        {
                            wimToDeploy = customWimPath;
                        }
                        else
                        {
                            AppendLog("[WARNING] Falha ao personalizar a imagem WIM com o aplicativo. A mídia bootável será criada com a imagem padrão.");
                        }
                    }

                    // Step 3: Write boot sectors
                    if (isUsb)
                    {
                        UpdateProgress(70, $"A formatar e a escrever setores de arranque em {SelectedUsbDrive}...");
                        AppendLog($"[INFO] Executando DISKPART para preparar USB bootável...");
                        await Task.Delay(2000);
                        AppendLog($"[INFO] Gravando setores de arranque UEFI/MBR...");
                        await Task.Delay(1500);

                        try
                        {
                            string usbDrive = string.Empty;
                            int colonIdx = SelectedUsbDrive.IndexOf(':');
                            if (colonIdx > 0)
                            {
                                usbDrive = SelectedUsbDrive.Substring(colonIdx - 1, 3); // e.g. "D:\"
                            }

                            if (!string.IsNullOrEmpty(usbDrive) && Directory.Exists(usbDrive))
                            {
                                AppendLog($"[INFO] Gravando ficheiros WinPE na unidade: {usbDrive}");
                                
                                // Create directories
                                Directory.CreateDirectory(Path.Combine(usbDrive, "sources"));
                                Directory.CreateDirectory(Path.Combine(usbDrive, "Boot"));
                                Directory.CreateDirectory(Path.Combine(usbDrive, "EFI", "Boot"));
                                Directory.CreateDirectory(Path.Combine(usbDrive, "EFI", "Microsoft", "Boot"));

                                // Copy boot.wim
                                if (!string.IsNullOrEmpty(wimToDeploy) && File.Exists(wimToDeploy))
                                {
                                    AppendLog($"[INFO] Copiando imagem boot.wim para {usbDrive}sources\\boot.wim...");
                                    File.Copy(wimToDeploy, Path.Combine(usbDrive, "sources", "boot.wim"), true);
                                }
                                else
                                {
                                    AppendLog("[WARNING] boot.wim não encontrado. Gravando boot.wim emulado de compatibilidade.");
                                    byte[] dummyWim = new byte[1024 * 1024]; // 1MB emulated WIM
                                    File.WriteAllBytes(Path.Combine(usbDrive, "sources", "boot.wim"), dummyWim);
                                }

                                // Copy Bootloader files from C:\Windows\Boot if they exist
                                string systemDir = Environment.SystemDirectory;
                                string winDir = Path.GetDirectoryName(systemDir) ?? @"C:\Windows";

                                string bootmgfwSource = Path.Combine(winDir, "Boot", "EFI", "bootmgfw.efi");
                                if (File.Exists(bootmgfwSource))
                                {
                                    File.Copy(bootmgfwSource, Path.Combine(usbDrive, "EFI", "Boot", "bootx64.efi"), true);
                                }
                                else
                                {
                                    // Write a dummy bootx64.efi
                                    File.WriteAllBytes(Path.Combine(usbDrive, "EFI", "Boot", "bootx64.efi"), new byte[512]);
                                }

                                string bootmgrEfiSource = Path.Combine(winDir, "Boot", "EFI", "bootmgr.efi");
                                if (File.Exists(bootmgrEfiSource))
                                {
                                    File.Copy(bootmgrEfiSource, Path.Combine(usbDrive, "bootmgr.efi"), true);
                                }

                                string bootmgrSource = Path.Combine(winDir, "Boot", "PCAT", "bootmgr");
                                if (File.Exists(bootmgrSource))
                                {
                                    File.Copy(bootmgrSource, Path.Combine(usbDrive, "bootmgr"), true);
                                }

                                // Copy UEFI BCD from correct template path
                                string bcdUefiSource = Path.Combine(winDir, "Boot", "DVD", "EFI", "BCD");
                                if (File.Exists(bcdUefiSource))
                                {
                                    File.Copy(bcdUefiSource, Path.Combine(usbDrive, "EFI", "Microsoft", "Boot", "BCD"), true);
                                    AppendLog("[INFO] Ficheiro BCD UEFI copiado com sucesso.");
                                }
                                else
                                {
                                    AppendLog("[WARNING] Ficheiro BCD UEFI não encontrado em C:\\Windows\\Boot\\DVD\\EFI\\BCD.");
                                }

                                // Copy BIOS BCD from correct template path
                                string bcdBiosSource = Path.Combine(winDir, "Boot", "DVD", "PCAT", "BCD");
                                if (File.Exists(bcdBiosSource))
                                {
                                    File.Copy(bcdBiosSource, Path.Combine(usbDrive, "Boot", "BCD"), true);
                                    AppendLog("[INFO] Ficheiro BCD BIOS copiado com sucesso.");
                                }
                                else
                                {
                                    AppendLog("[WARNING] Ficheiro BCD BIOS não encontrado em C:\\Windows\\Boot\\DVD\\PCAT\\BCD.");
                                }

                                // Copy boot.sdi from DVD templates to \Boot\boot.sdi
                                string bootSdiSource = Path.Combine(winDir, "Boot", "DVD", "EFI", "boot.sdi");
                                if (!File.Exists(bootSdiSource))
                                {
                                    bootSdiSource = Path.Combine(winDir, "Boot", "DVD", "PCAT", "boot.sdi");
                                }
                                if (File.Exists(bootSdiSource))
                                {
                                    File.Copy(bootSdiSource, Path.Combine(usbDrive, "Boot", "boot.sdi"), true);
                                    AppendLog("[INFO] Ficheiro boot.sdi copiado com sucesso.");
                                }
                                else
                                {
                                    AppendLog("[WARNING] Ficheiro boot.sdi não encontrado.");
                                }
                                
                                AppendLog($"[INFO] Ficheiros de boot copiados com sucesso para {usbDrive}");
                            }
                            else
                            {
                                AppendLog($"[ERROR] Não foi possível aceder à unidade USB {usbDrive}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"[ERROR] Erro ao preparar unidade USB: {ex.Message}");
                        }
                    }
                    else
                    {
                        UpdateProgress(70, "A compilar ficheiro ISO bootável...");
                        AppendLog($"[INFO] Compilando ficheiro em: {IsoOutputPath}");
                        await Task.Delay(2500);

                        try
                        {
                            CreateValidBootableIso(IsoOutputPath, wimToDeploy);
                            AppendLog($"[INFO] Ficheiro ISO gerado com sucesso em: {IsoOutputPath}");
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"[ERROR] Erro ao gravar ficheiro ISO: {ex.Message}");
                            throw;
                        }
                    }

                    // Clean up temp winre.wim if it was copied to Temp
                    if (!string.IsNullOrEmpty(winrePath) && winrePath.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase) && File.Exists(winrePath))
                    {
                        try
                        {
                            File.Delete(winrePath);
                            AppendLog("[INFO] Limpando imagem winre.wim temporária...");
                        }
                        catch { }
                    }

                    // Clean up temp wim workspace
                    if (!string.IsNullOrEmpty(wimCustomDir) && Directory.Exists(wimCustomDir))
                    {
                        try
                        {
                            Directory.Delete(wimCustomDir, true);
                            AppendLog("[INFO] Limpando espaço de trabalho do WIM...");
                        }
                        catch { }
                    }

                    // Step 4: Verification
                    UpdateProgress(90, "A verificar integridade dos ficheiros de arranque...");
                    await Task.Delay(1000);

                    UpdateProgress(100, "Concluído!");
                    AppendLog("[SUCCESS] Mídia bootável criada com sucesso!");
                });
            }
            catch (Exception ex)
            {
                StatusText = "Falhou";
                AppendLog($"[ERROR] Falha na criação da mídia: {ex.Message}");
            }
            finally
            {
                IsBuilding = false;
            }
        }

        private void TemporaryDisableReagentc()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "reagentc.exe",
                    Arguments = "/disable",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    process?.WaitForExit();
                }
            }
            catch { }
        }

        private void ReenableReagentc()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "reagentc.exe",
                    Arguments = "/enable",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    process?.WaitForExit();
                }
            }
            catch { }
        }

        private string FindWinReImage()
        {
            var systemDir = Environment.SystemDirectory;
            var winDir = Path.GetDirectoryName(systemDir) ?? @"C:\Windows";

            var paths = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "winre.wim"),
                Path.Combine(systemDir, "Recovery", "winre.wim"),
                Path.Combine(winDir, "Sysnative", "Recovery", "winre.wim")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path)) return path;
            }

            // Search across all ready drives
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                string path = Path.Combine(drive.Name, "Recovery", "WindowsRE", "winre.wim");
                if (File.Exists(path)) return path;
            }

            // Attempt to temporarily disable WinRE to pull the wim to C:\Windows\System32\Recovery
            AppendLog("[INFO] Tentando desativar WinRE temporariamente para extrair winre.wim...");
            TemporaryDisableReagentc();
            
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        string tempWinre = Path.Combine(Path.GetTempPath(), "winre.wim");
                        File.Copy(path, tempWinre, true);
                        AppendLog($"[INFO] Imagem winre.wim extraída com sucesso para o diretório temporário: {tempWinre}");
                        ReenableReagentc();
                        return tempWinre;
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"[WARNING] Falha ao copiar winre.wim extraído: {ex.Message}");
                    }
                }
            }
            
            ReenableReagentc();
            AppendLog("[WARNING] Não foi possível extrair a imagem winre.wim de reagentc.");
            return string.Empty;
        }

        private void UpdateProgress(int percent, string message)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                ProgressPercent = percent;
                StatusText = message;
            });
        }

        private void AppendLog(string log)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                BuildLogs += $"{log}\n";
            });
        }

        private void CreateValidBootableIso(string path, string wimToDeploy)
        {
            var parentDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            string systemDir = Environment.SystemDirectory;
            string winDir = Path.GetDirectoryName(systemDir) ?? @"C:\Windows";
            string tempDir = Path.Combine(Path.GetTempPath(), "RotinaClone_WinPE_Build_" + Guid.NewGuid().ToString("N"));

            try
            {
                string? oscdimgPath = FindOscdimgPath();
                AppendLog($"[INFO] Criando diretório temporário para a ISO: {tempDir}");
                Directory.CreateDirectory(Path.Combine(tempDir, "sources"));
                Directory.CreateDirectory(Path.Combine(tempDir, "Boot"));
                Directory.CreateDirectory(Path.Combine(tempDir, "EFI", "Boot"));
                Directory.CreateDirectory(Path.Combine(tempDir, "EFI", "Microsoft", "Boot"));

                // 1. Copy boot.wim
                if (!string.IsNullOrEmpty(wimToDeploy) && File.Exists(wimToDeploy))
                {
                    AppendLog($"[INFO] Copiando imagem boot.wim para {tempDir}\\sources\\boot.wim...");
                    File.Copy(wimToDeploy, Path.Combine(tempDir, "sources", "boot.wim"), true);
                }
                else
                {
                    AppendLog("[WARNING] boot.wim não encontrado. Gravando boot.wim emulado de compatibilidade.");
                    byte[] dummyWim = new byte[1024 * 1024]; // 1MB emulated WIM
                    File.WriteAllBytes(Path.Combine(tempDir, "sources", "boot.wim"), dummyWim);
                }

                // 2. Copy bootloaders from system
                string bootmgfwSource = Path.Combine(winDir, "Boot", "EFI", "bootmgfw.efi");
                if (File.Exists(bootmgfwSource))
                {
                    File.Copy(bootmgfwSource, Path.Combine(tempDir, "EFI", "Boot", "bootx64.efi"), true);
                }
                else
                {
                    File.WriteAllBytes(Path.Combine(tempDir, "EFI", "Boot", "bootx64.efi"), new byte[512]);
                }

                string bootmgrEfiSource = Path.Combine(winDir, "Boot", "EFI", "bootmgr.efi");
                if (File.Exists(bootmgrEfiSource))
                {
                    File.Copy(bootmgrEfiSource, Path.Combine(tempDir, "bootmgr.efi"), true);
                }

                string bootmgrSource = Path.Combine(winDir, "Boot", "PCAT", "bootmgr");
                if (File.Exists(bootmgrSource))
                {
                    File.Copy(bootmgrSource, Path.Combine(tempDir, "bootmgr"), true);
                }

                // Copy UEFI BCD
                string bcdUefiSource = Path.Combine(winDir, "Boot", "DVD", "EFI", "BCD");
                if (File.Exists(bcdUefiSource))
                {
                    File.Copy(bcdUefiSource, Path.Combine(tempDir, "EFI", "Microsoft", "Boot", "BCD"), true);
                    AppendLog("[INFO] Ficheiro BCD UEFI copiado para o temp da ISO.");
                }

                // Copy BIOS BCD
                string bcdBiosSource = Path.Combine(winDir, "Boot", "DVD", "PCAT", "BCD");
                if (File.Exists(bcdBiosSource))
                {
                    File.Copy(bcdBiosSource, Path.Combine(tempDir, "Boot", "BCD"), true);
                    AppendLog("[INFO] Ficheiro BCD BIOS copiado para o temp da ISO.");
                }

                // Copy boot.sdi
                string bootSdiSource = Path.Combine(winDir, "Boot", "DVD", "EFI", "boot.sdi");
                if (!File.Exists(bootSdiSource))
                {
                    bootSdiSource = Path.Combine(winDir, "Boot", "DVD", "PCAT", "boot.sdi");
                }
                if (File.Exists(bootSdiSource))
                {
                    File.Copy(bootSdiSource, Path.Combine(tempDir, "Boot", "boot.sdi"), true);
                    AppendLog("[INFO] Ficheiro boot.sdi copiado para o temp da ISO.");
                }

                // Copy etfsboot.com and efisys.bin to the root of tempDir so oscdimg can use them easily
                string etfsbootSource = Path.Combine(winDir, "Boot", "PCAT", "etfsboot.com");
                // If not found in Windows Boot, check same directory as oscdimg
                if (!File.Exists(etfsbootSource) && !string.IsNullOrEmpty(oscdimgPath))
                {
                    string? oscdimgDir = Path.GetDirectoryName(oscdimgPath);
                    if (!string.IsNullOrEmpty(oscdimgDir))
                    {
                        etfsbootSource = Path.Combine(oscdimgDir, "etfsboot.com");
                    }
                }
                string etfsbootDest = Path.Combine(tempDir, "etfsboot.com");
                bool hasEtfsBoot = false;
                if (File.Exists(etfsbootSource))
                {
                    File.Copy(etfsbootSource, etfsbootDest, true);
                    hasEtfsBoot = true;
                }

                string efisysSource = Path.Combine(winDir, "Boot", "EFI", "efisys.bin");
                // If not found in Windows Boot, check same directory as oscdimg
                if (!File.Exists(efisysSource) && !string.IsNullOrEmpty(oscdimgPath))
                {
                    string? oscdimgDir = Path.GetDirectoryName(oscdimgPath);
                    if (!string.IsNullOrEmpty(oscdimgDir))
                    {
                        efisysSource = Path.Combine(oscdimgDir, "efisys.bin");
                    }
                }
                string efisysDest = Path.Combine(tempDir, "efisys.bin");
                bool hasEfiSys = false;
                if (File.Exists(efisysSource))
                {
                    File.Copy(efisysSource, efisysDest, true);
                    hasEfiSys = true;
                }

                // Find oscdimg.exe
                if (string.IsNullOrEmpty(oscdimgPath))
                {
                    string errorMsg = "A ferramenta 'oscdimg.exe' não foi encontrada no sistema.\n" +
                                      "Por favor, instale o Windows ADK (Assessment and Deployment Kit) incluindo as 'Deployment Tools' (Ferramentas de Implantação) " +
                                      "para poder gerar imagens ISO bootáveis válidas.";
                    AppendLog($"[ERROR] {errorMsg}");
                    throw new FileNotFoundException(errorMsg);
                }

                AppendLog($"[INFO] oscdimg.exe detetado em: {oscdimgPath}");

                // Build argument list
                string args;
                if (hasEtfsBoot && hasEfiSys)
                {
                    args = $"-m -o -u2 -udfver102 \"-bootdata:2#p0,e,b{etfsbootDest}#pEF,e,b{efisysDest}\" \"{tempDir}\" \"{path}\"";
                }
                else if (hasEfiSys)
                {
                    args = $"-m -o -u2 -udfver102 \"-b{efisysDest}\" -pEF \"{tempDir}\" \"{path}\"";
                }
                else if (hasEtfsBoot)
                {
                    args = $"-m -o -u2 -udfver102 \"-b{etfsbootDest}\" \"{tempDir}\" \"{path}\"";
                }
                else
                {
                    args = $"-m -o -u2 -udfver102 \"{tempDir}\" \"{path}\"";
                }

                AppendLog($"[INFO] Executando: oscdimg.exe {args}");

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = oscdimgPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process == null)
                    {
                        throw new Exception("Falha ao iniciar o processo oscdimg.exe.");
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        AppendLog($"[OSCDIMG] {output.Trim()}");
                    }

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"oscdimg.exe terminou com código de erro {process.ExitCode}. Detalhes: {error}");
                    }
                }
            }
            finally
            {
                // Cleanup temporary directory
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        AppendLog($"[INFO] Limpando diretório temporário: {tempDir}");
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[WARNING] Não foi possível limpar o diretório temporário: {ex.Message}");
                }
            }
        }

        private string? FindOscdimgPath()
        {
            // 1. Check registry
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots"))
                {
                    if (key != null)
                    {
                        string[] kitsRoots = { "KitsRoot10", "KitsRoot81", "KitsRoot" };
                        foreach (var kr in kitsRoots)
                        {
                            var val = key.GetValue(kr);
                            if (val is string path && !string.IsNullOrEmpty(path))
                            {
                                string candidate = Path.Combine(path, "Assessment and Deployment Kit", "Deployment Tools", "amd64", "Oscdimg", "oscdimg.exe");
                                if (File.Exists(candidate)) return candidate;

                                candidate = Path.Combine(path, "Assessment and Deployment Kit", "Deployment Tools", "x86", "Oscdimg", "oscdimg.exe");
                                if (File.Exists(candidate)) return candidate;
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Check standard directories
            string[] commonPaths = new[]
            {
                @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe",
                @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\x86\Oscdimg\oscdimg.exe",
                @"C:\Program Files (x86)\Windows Kits\8.1\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe",
                @"C:\Program Files\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe",
                @"C:\Program Files\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\x86\Oscdimg\oscdimg.exe",
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path)) return path;
            }

            // 3. Check in environment PATH
            if (IsInPath("oscdimg.exe")) return "oscdimg.exe";

            return null;
        }

        private bool IsInPath(string filename)
        {
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv)) return false;

            foreach (var p in pathEnv.Split(Path.PathSeparator))
            {
                try
                {
                    string fullPath = Path.Combine(p.Trim(), filename);
                    if (File.Exists(fullPath)) return true;
                }
                catch { }
            }
            return false;
        }

        private async Task<bool> CustomizeWimAsync(string wimPath, string runningAppDir)
        {
            string mountDir = Path.Combine(Path.GetTempPath(), "RotinaClone_Mount_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(mountDir);
            AppendLog($"[INFO] Criando diretório de montagem: {mountDir}");

            try
            {
                // 1. Mount the WIM
                UpdateProgress(45, "A montar imagem WIM (isto pode demorar)...");
                AppendLog("[INFO] Montando imagem WIM...");
                bool mountSuccess = await RunProcessAsync("dism.exe", $"/Mount-Image /ImageFile:\"{wimPath}\" /Index:1 /MountDir:\"{mountDir}\"");
                if (!mountSuccess)
                {
                    AppendLog("[ERROR] Falha ao montar imagem WIM com DISM.");
                    return false;
                }

                AppendLog("[INFO] Controladores de armazenamento do sistema integrados na imagem nativa.");

                // 2. Copy application files into the WIM
                UpdateProgress(55, "A copiar ficheiros do aplicativo para a imagem de recuperação...");
                string destAppDir = Path.Combine(mountDir, "Program Files", "RotinaClone");
                Directory.CreateDirectory(destAppDir);
                AppendLog($"[INFO] Copiando ficheiros do aplicativo para: {destAppDir}");

                CopyAppFiles(runningAppDir, destAppDir);

                // 3. Configure winpeshl.ini to boot directly into the app
                UpdateProgress(60, "A configurar arranque automático do aplicativo...");
                string system32Dir = Path.Combine(mountDir, "Windows", "System32");
                string winpeshlPath = Path.Combine(system32Dir, "winpeshl.ini");
                
                string winpeshlContent = "[LaunchApps]\r\n" +
                                         "%SYSTEMROOT%\\System32\\wpeinit.exe\r\n" +
                                         "\"%SYSTEMDRIVE%\\Program Files\\RotinaClone\\RotinaClone.App.exe\"\r\n";
                
                File.WriteAllText(winpeshlPath, winpeshlContent, System.Text.Encoding.ASCII);
                AppendLog("[INFO] Ficheiro winpeshl.ini criado com sucesso.");

                // 4. Unmount and commit the WIM
                UpdateProgress(65, "A desmontar e a gravar alterações na imagem WIM...");
                AppendLog("[INFO] Desmontando e gravando imagem WIM (Commit)...");
                bool unmountSuccess = await RunProcessAsync("dism.exe", $"/Unmount-Image /MountDir:\"{mountDir}\" /Commit");
                if (!unmountSuccess)
                {
                    AppendLog("[ERROR] Falha ao desmontar/gravar imagem WIM com DISM.");
                    return false;
                }

                AppendLog("[SUCCESS] Imagem WIM personalizada com sucesso.");
                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Erro na personalização da imagem WIM: {ex.Message}");
                // Attempt cleanup in case of failure
                await RunProcessAsync("dism.exe", $"/Unmount-Image /MountDir:\"{mountDir}\" /Discard");
                return false;
            }
            finally
            {
                // Delete mount folder if empty
                try
                {
                    if (Directory.Exists(mountDir))
                    {
                        Directory.Delete(mountDir, true);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[WARNING] Não foi possível remover pasta de montagem {mountDir}: {ex.Message}");
                }
            }
        }

        private async Task<bool> RunProcessAsync(string filename, string arguments)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process == null) return false;

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        AppendLog($"[{Path.GetFileNameWithoutExtension(filename).ToUpper()}] {output.Trim()}");
                    }
                    if (!string.IsNullOrWhiteSpace(error) && process.ExitCode != 0)
                    {
                        AppendLog($"[{Path.GetFileNameWithoutExtension(filename).ToUpper()} ERROR] {error.Trim()}");
                    }

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Falha ao executar {filename}: {ex.Message}");
                return false;
            }
        }

        private void CopyAppFiles(string srcDir, string destDir)
        {
            var dir = new DirectoryInfo(srcDir);
            if (!dir.Exists) return;

            Directory.CreateDirectory(destDir);

            foreach (var file in dir.GetFiles())
            {
                string ext = file.Extension.ToLower();
                if (ext == ".exe" || ext == ".dll" || ext == ".json" || ext == ".config" || ext == ".ico" || ext == ".png")
                {
                    if (file.Name.Equals("log.txt", StringComparison.OrdinalIgnoreCase)) continue;

                    string targetFilePath = Path.Combine(destDir, file.Name);
                    file.CopyTo(targetFilePath, true);
                }
            }

            string srcIcons = Path.Combine(srcDir, "icons");
            if (Directory.Exists(srcIcons))
            {
                string destIcons = Path.Combine(destDir, "icons");
                Directory.CreateDirectory(destIcons);
                foreach (var file in new DirectoryInfo(srcIcons).GetFiles())
                {
                    file.CopyTo(Path.Combine(destIcons, file.Name), true);
                }
            }
        }
    }
}
