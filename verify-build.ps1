# Build Verification Script for FiveM Police Broadcast Overlay
# This script verifies the project structure and attempts to build the solution

Write-Host "=== FiveM Police Broadcast Overlay - Build Verification ===" -ForegroundColor Cyan
Write-Host ""

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 6.0 SDK or later." -ForegroundColor Red
    Write-Host "  Download from: https://dotnet.microsoft.com/download/dotnet/6.0" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Check project structure
Write-Host "Verifying project structure..." -ForegroundColor Yellow

$requiredFiles = @(
    "FiveMPoliceOverlay.sln",
    "FiveMPoliceOverlay/FiveMPoliceOverlay.csproj",
    "FiveMPoliceOverlay/App.xaml",
    "FiveMPoliceOverlay/App.xaml.cs",
    "FiveMPoliceOverlay/Views/MainWindow.xaml",
    "FiveMPoliceOverlay/Views/MainWindow.xaml.cs"
)

$requiredFolders = @(
    "FiveMPoliceOverlay/Models",
    "FiveMPoliceOverlay/Services",
    "FiveMPoliceOverlay/Infrastructure",
    "FiveMPoliceOverlay/Views"
)

$allFilesExist = $true
foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "✓ $file" -ForegroundColor Green
    } else {
        Write-Host "✗ $file (missing)" -ForegroundColor Red
        $allFilesExist = $false
    }
}

foreach ($folder in $requiredFolders) {
    if (Test-Path $folder) {
        Write-Host "✓ $folder/" -ForegroundColor Green
    } else {
        Write-Host "✗ $folder/ (missing)" -ForegroundColor Red
        $allFilesExist = $false
    }
}

if (-not $allFilesExist) {
    Write-Host ""
    Write-Host "✗ Project structure incomplete" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Project structure verified" -ForegroundColor Green
Write-Host ""

# Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore FiveMPoliceOverlay.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ NuGet restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ NuGet packages restored" -ForegroundColor Green
Write-Host ""

# Build Debug configuration
Write-Host "Building Debug configuration..." -ForegroundColor Yellow
dotnet build FiveMPoliceOverlay.sln -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Debug build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Debug build successful" -ForegroundColor Green
Write-Host ""

# Build Release configuration
Write-Host "Building Release configuration..." -ForegroundColor Yellow
dotnet build FiveMPoliceOverlay.sln -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Release build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Release build successful" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "=== Build Verification Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "✓ All checks passed!" -ForegroundColor Green
Write-Host ""
Write-Host "Project is ready for development." -ForegroundColor Cyan
Write-Host "Next step: Implement Task 1.1 (Logging Infrastructure)" -ForegroundColor Yellow
Write-Host ""
Write-Host "To run the application:" -ForegroundColor Cyan
Write-Host "  dotnet run --project FiveMPoliceOverlay/FiveMPoliceOverlay.csproj" -ForegroundColor White
Write-Host ""
