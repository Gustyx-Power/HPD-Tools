# FiveM Police Broadcast Overlay

A Windows desktop application that provides a transparent overlay for FiveM players to send broadcast messages via keyboard shortcuts.

## Project Structure

```
FiveMPoliceOverlay/
├── Models/              # Data models and configuration classes
├── Services/            # Business logic services
├── Views/               # WPF UI windows and controls
├── Infrastructure/      # Low-level utilities and Windows API wrappers
├── App.xaml            # WPF application definition
└── App.xaml.cs         # Application entry point
```

## Technology Stack

- **.NET 6.0** - Target framework
- **WPF** - UI framework for overlay and settings windows
- **System.Text.Json** - Configuration serialization
- **Windows Forms** - System tray integration (NotifyIcon)

## Build Configuration

- **Platform**: x64
- **Output Type**: WinExe (Windows Application)
- **Self-Contained**: Yes (includes .NET 6.0 runtime)

## Development Status

This project is currently under development. Core infrastructure has been set up in Task 1.

## Requirements

- Windows 10/11 (x64)
- .NET 6.0 SDK (for development)
- Visual Studio 2022 or JetBrains Rider (recommended)

## Building

```bash
dotnet build FiveMPoliceOverlay.sln -c Release
```

## Running

```bash
dotnet run --project FiveMPoliceOverlay/FiveMPoliceOverlay.csproj
```

## License

See LICENSE file in the root directory.
