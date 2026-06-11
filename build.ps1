# Build Script for Rotina Clone Enterprise Edition
# Restores, compiles, runs tests, publishes single-file portable builds, compiles Inno Setup installer, and writes execution logs to log.txt

$ErrorActionPreference = "Stop"
$logPath = "log.txt"
$startTime = [System.Diagnostics.Stopwatch]::StartNew()

function Log-Message {
    param (
        [string]$Message,
        [string]$Type = "INFO"
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $formattedMessage = "[$timestamp] [$Type] $Message"
    Write-Host $formattedMessage
    $formattedMessage | Out-File -FilePath $logPath -Append -Encoding utf8
}

# Initialize Log File
Clear-Content -Path $logPath -ErrorAction SilentlyContinue
Log-Message "Iniciando compilação do Rotina Clone Enterprise Edition..."

try {
    # Step 1: Restore Dependencies
    Log-Message "Passo 1/6: Restaurando dependências do NuGet..."
    $restoreOutput = dotnet restore RotinaClone.sln -r win-x64 2>&1
    Log-Message "dotnet restore output: $restoreOutput"
    if ($LASTEXITCODE -ne 0) { Log-Message "ERROR during restore: $restoreOutput" "ERROR"; exit 1 }
    Log-Message "Dependências restauradas com sucesso."

    # Step 2: Compile Solution
    Log-Message "Passo 2/6: Compilando solução em modo Release..."
    $buildOutput = dotnet build RotinaClone.sln -c Release --no-restore 2>&1
    Log-Message "dotnet build output: $buildOutput"
    if ($LASTEXITCODE -ne 0) { Log-Message "ERROR during build: $buildOutput" "ERROR"; exit 1 }
    Log-Message "Compilação da solução concluída com sucesso."

    # Step 3: Run Unit Tests
    Log-Message "Passo 3/6: Executando testes unitários..."
    $testOutput = dotnet test RotinaClone.Tests/RotinaClone.Tests.csproj -c Release --no-build 2>&1
    Log-Message "dotnet test output: $testOutput"
    if ($LASTEXITCODE -ne 0) { Log-Message "ERROR during tests: $testOutput" "ERROR"; exit 1 }
    Log-Message "Testes unitários executados e aprovados."

    # Step 4: Publish Portable Executables (Single-File / Self-Contained)
    Log-Message "Passo 4/6: Publicando aplicações portáteis (Single-File)..."
    
    # Define a unique publish folder to avoid file lock issues
    $publishFolder = "publish_portable_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    
    # Ensure any previous temporary folder is removed (silently ignore errors)
    if (Test-Path $publishFolder) {
        Remove-Item -Recurse -Force $publishFolder -ErrorAction SilentlyContinue
    }

    # WPF Application Publish
    Log-Message "A publicar WPF UI..."
    dotnet publish RotinaClone.App/RotinaClone.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o $publishFolder 2>&1 | ForEach-Object { Log-Message "publish app output: $_" }
    if ($LASTEXITCODE -ne 0) { Log-Message "ERROR during app publish" "ERROR"; exit 1 }
    
    # CLI Tool Publish
    Log-Message "A publicar CLI..."
    dotnet publish RotinaClone.CLI/RotinaClone.CLI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishFolder 2>&1 | ForEach-Object { Log-Message "publish CLI output: $_" }
    if ($LASTEXITCODE -ne 0) { Log-Message "ERROR during CLI publish" "ERROR"; exit 1 }
    
    # Windows Service Publish
    Log-Message "A publicar Windows Service..."
    dotnet publish RotinaClone.Service/RotinaClone.Service.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishFolder 2>&1 | ForEach-Object { Log-Message "publish Service output: $_" }
    if ($LASTEXITCODE -ne 0) { Log-Message "ERROR during Service publish" "ERROR"; exit 1 }

    # Copy icons into publish folder
    New-Item -ItemType Directory -Force -Path "$publishFolder\icons" | Out-Null
    Copy-Item "icons\icon.ico" -Destination "$publishFolder\icons"
    Copy-Item "icons\icon.png" -Destination "$publishFolder\icons"
    
    Log-Message "Publicação portátil concluída na pasta .\$publishFolder"

    # Step 5: Compile Inno Setup Installer
    Log-Message "Passo 5/6: Compilando instalador profissional (Inno Setup)..."
    
    $isccPaths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
        "C:\Program Files\Inno Setup 5\ISCC.exe"
    )

    $isccPath = $null
    foreach ($path in $isccPaths) {
        if (Test-Path $path) {
            $isccPath = $path
            break
        }
    }

    if ($isccPath -ne $null) {
        Log-Message "Compilador Inno Setup detetado em: $isccPath"
        & $isccPath setup.iss | Out-Null
        Log-Message "Instalador compilado com sucesso na pasta .\publish_release"
    } else {
        Log-Message "Compilador Inno Setup (ISCC.exe) não foi localizado. O instalador setup.iss não pôde ser gerado." "WARNING"
        Log-Message "Nota: Pode instalar o Inno Setup ou executar o script num computador com o Inno Setup instalado." "INFO"
    }

    # Step 6: Log Execution Summary
    $startTime.Stop()
    $duration = $startTime.Elapsed
    Log-Message "Passo 6/6: Concluído!"
    Log-Message "-------------------------------------------------"
    Log-Message "ESTADO FINAL DA BUILD: SUCESSO"
    Log-Message "Tempo de Compilação: $($duration.Minutes)m $($duration.Seconds)s"
    Log-Message "-------------------------------------------------"

} catch {
    $startTime.Stop()
    $duration = $startTime.Elapsed
    Log-Message "ERRO OCORRIDO DURANTE A COMPILAÇÃO: $_" "ERROR"
    Log-Message "-------------------------------------------------"
    Log-Message "ESTADO FINAL DA BUILD: FALHOU"
    Log-Message "Tempo decorrido: $($duration.Minutes)m $($duration.Seconds)s"
    Log-Message "-------------------------------------------------"
    exit 1
}
