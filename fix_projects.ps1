# Fix Rotina Clone Project Structure and Packages

Write-Host "Creating RotinaClone.App WPF project..."
dotnet new wpf -n RotinaClone.App -f net8.0

Write-Host "Adding RotinaClone.App to solution..."
dotnet sln RotinaClone.sln add RotinaClone.App

Write-Host "Adding references to RotinaClone.App..."
dotnet add RotinaClone.App/RotinaClone.App.csproj reference RotinaClone.Application/RotinaClone.Application.csproj
dotnet add RotinaClone.App/RotinaClone.App.csproj reference RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj

Write-Host "Fixing packages in RotinaClone.Infrastructure..."
# Remove incompatible package refs if they exist
dotnet remove RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet remove RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet remove RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package System.Management

# Add .NET 8 compatible packages
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite -v 8.0.12
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design -v 8.0.12
dotnet add RotinaClone.Infrastructure/RotinaClone.Infrastructure.csproj package System.Management -v 8.0.0

Write-Host "Fixing packages in RotinaClone.Service..."
dotnet remove RotinaClone.Service/RotinaClone.Service.csproj package Microsoft.Extensions.Hosting.WindowsServices
dotnet add RotinaClone.Service/RotinaClone.Service.csproj package Microsoft.Extensions.Hosting.WindowsServices -v 8.0.1

Write-Host "Adding packages to RotinaClone.App..."
dotnet add RotinaClone.App/RotinaClone.App.csproj package CommunityToolkit.Mvvm -v 8.2.2
dotnet add RotinaClone.App/RotinaClone.App.csproj package Microsoft.Extensions.DependencyInjection -v 8.0.1
dotnet add RotinaClone.App/RotinaClone.App.csproj package Serilog.Sinks.File -v 6.1.0

Write-Host "Restoring solution..."
dotnet restore RotinaClone.sln

Write-Host "Fixing complete!"
