# GodotHub

GodotHub is a cross-platform desktop application designed to simplify the management of Godot Engine installations. It allows developers to easily download, organize, and switch between different versions and builds of the Godot Engine.

## Features

- **Version Management**: Browse and download various Godot versions, including Stable, Release Candidate, Beta, Alpha, and Dev builds.
- **Multiple Builds**: Support for both Standard and Mono (C#) versions of the engine.
- **Instance Management**: Create and manage multiple engine instances with custom names and icons.
- **Cross-Platform**: Built with .NET and Avalonia UI, providing a consistent experience across Windows, macOS, and Linux.

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later.

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/GodotHub.git
   cd GodotHub
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

### Running the Application

To start the desktop application, navigate to the desktop project directory and run:

```bash
cd src/GodotHub.Desktop
dotnet run
```

## Tech Stack

- **Framework**: .NET 10
- **UI Framework**: [Avalonia UI](https://avaloniaui.net/)
- **Architecture**: MVVM (Model-View-ViewModel) using [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- **Logging**: NLog
