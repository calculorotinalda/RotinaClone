# Initialize Solution and Projects for Rotina Clone

Write-Host "Creating Solution..."
dotnet new sln -n RotinaClone

Write-Host "Creating projects..."
dotnet new classlib -n RotinaClone.Domain -f net8.0
dotnet new classlib -n RotinaClone.Infrastructure -f net8.0
dotnet new classlib -n RotinaClone.Application -f net8.0
dotnet new wpf -n RotinaClone.App -f net8.0-windows
dotnet new worker -n RotinaClone.Service -f net8.0
dotnet new console -n RotinaClone.CLI -f net8.0
dotnet new xunit -n RotinaClone.Tests -f net8.0

Write-Host "Adding projects to solution..."
dotnet sln RotinaClone.sln add RotinaClone.Domain
dotnet sln RotinaClone.sln add RotinaClone.Infrastructure
dotnet sln RotinaClone.sln add RotinaClone.Application
dotnet sln RotinaClone.sln add RotinaClone.App
dotnet sln RotinaClone.sln add RotinaClone.Service
dotnet sln RotinaClone.sln add RotinaClone.CLI
dotnet sln RotinaClone.sln add RotinaClone.Tests

Write-Host "Adding project references..."
# Infrastructure references Domain
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj reference RotinaClone.Domain/RotinaClone.Domain.csproj

# Application references Domain
dotnet add RotinaClone.Application/RotinaClone.Application.csproj reference RotinaClone.Domain/RotinaClone.Domain.csproj

# App references Application and Infrastructure
dotnet add RotinaClone.App/RotinaClone.App.csproj reference RotinaClone.Application/RotinaClone.Application.csproj
dotnet add RotinaClone.App/RotinaClone.App.csproj reference RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj

# Service references Application and Infrastructure
dotnet add RotinaClone.Service/RotinaClone.Service.csproj reference RotinaClone.Application/RotinaClone.Application.csproj
dotnet add RotinaClone.Service/RotinaClone.Service.csproj reference RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj

# CLI references Application and Infrastructure
dotnet add RotinaClone.CLI/RotinaClone.CLI.csproj reference RotinaClone.Application/RotinaClone.Application.csproj
dotnet add RotinaClone.CLI/RotinaClone.CLI.csproj reference RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj

# Tests references Domain, Application and Infrastructure
dotnet add RotinaClone.Tests/RotinaClone.Tests.csproj reference RotinaClone.Domain/RotinaClone.Domain.csproj
dotnet add RotinaClone.Tests/RotinaClone.Tests.csproj reference RotinaClone.Application/RotinaClone.Application.csproj
dotnet add RotinaClone.Tests/RotinaClone.Tests.csproj reference RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj

Write-Host "Adding NuGet Packages..."
# Domain packages (none needed initially, maybe just WMI or System.Management if we put models there, but we'll put them in Infra)

# Infrastructure packages
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package Serilog
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package Serilog.Sinks.File
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package Serilog.Sinks.Console
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package System.Management

# App packages
dotnet add RotinaClone.App/RotinaClone.App.csproj package CommunityToolkit.Mvvm
dotnet add RotinaClone.App/RotinaClone.App.csproj package Microsoft.Extensions.DependencyInjection
dotnet add RotinaClone.App/RotinaClone.App.csproj package Serilog.Sinks.File

# Service packages
dotnet add RotinaClone.Service/RotinaClone.Service.csproj package Microsoft.Extensions.Hosting.WindowsServices

# CLI packages (none needed initially, maybe command line parser but System.CommandLine or plain args is fine)

Write-Host "Initialization complete!"
