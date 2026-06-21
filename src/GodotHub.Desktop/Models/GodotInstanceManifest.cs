using System;
using GodotHub.Core.Models;

namespace GodotHub.Desktop.Models;

public sealed class GodotInstanceManifest
{
    public string Name { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ReleaseName { get; init; } = string.Empty;
    public GodotBuildKind BuildKind { get; init; }
    public string Platform { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public DateTimeOffset InstalledAt { get; init; }
}
