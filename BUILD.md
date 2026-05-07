# Build Instructions

## Prerequisites

### Required Software

1. **.NET 6.0 SDK** (or later)
   - Download from: https://dotnet.microsoft.com/download/dotnet/6.0
   - Choose "SDK x64" for Windows
   - Verify installation: `dotnet --version`

2. **Visual Studio 2022** (recommended) or **JetBrains Rider**
   - Visual Studio Community Edition is free
   - Ensure "Desktop development with .NET" workload is installed

### Optional Tools

- **Git** for version control
- **Windows Terminal** for better command-line experience

## Building the Project

### Using Command Line

```bash
# Restore NuGet packages
dotnet restore FiveMPoliceOverlay.sln

# Build Debug configuration
dotnet build FiveMPoliceOverlay.sln -c Debug

# Build Release configuration
dotnet build FiveMPoliceOverlay.sln -c Release
```

### Using Visual Studio

1. Open `FiveMPoliceOverlay.sln` in Visual Studio
2. Select build configuration (Debug or Release)
3. Press `Ctrl+Shift+B` or go to Build → Build Solution

### Using Rider

1. Open `FiveMPoliceOverlay.sln` in Rider
2. Select build configuration from the toolbar
3. Press `Ctrl+Shift+F9` or go to Build → Build Solution

## Running the Application

### From Command Line

```bash
# Run in Debug mode
dotnet run --project FiveMPoliceOverlay/FiveMPoliceOverlay.csproj

# Run Release build
dotnet run --project FiveMPoliceOverlay/FiveMPoliceOverlay.csproj -c Release
```

### From Visual Studio/Rider

Press `F5` to run with debugging, or `Ctrl+F5` to run without debugging.

## Build Output

- **Debug builds**: `FiveMPoliceOverlay/bin/Debug/net6.0-windows/win-x64/`
- **Release builds**: `FiveMPoliceOverlay/bin/Release/net6.0-windows/win-x64/`

## Publishing for Distribution

```bash
# Publish self-contained executable
dotnet publish FiveMPoliceOverlay/FiveMPoliceOverlay.csproj -c Release -r win-x64 --self-contained true

# Output will be in: FiveMPoliceOverlay/bin/Release/net6.0-windows/win-x64/publish/
```

## Troubleshooting

### "No .NET SDKs were found"

- Install .NET 6.0 SDK from the link above
- Restart your terminal/IDE after installation
- Verify with: `dotnet --version`

### Build Errors

- Ensure all NuGet packages are restored: `dotnet restore`
- Clean the solution: `dotnet clean`
- Rebuild: `dotnet build`

### Missing Windows Forms Reference

The project uses Windows Forms for NotifyIcon (system tray). This is included in .NET 6.0 Windows SDK automatically.

## Project Configuration

- **Target Framework**: net6.0-windows
- **Platform**: x64 only
- **Output Type**: WinExe (Windows Application, no console)
- **Self-Contained**: Yes (includes .NET runtime)

## Next Steps

After successful build, proceed to Task 1.1 to implement logging infrastructure.
