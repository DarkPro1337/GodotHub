namespace GodotHub.Core.Models;

public sealed record GodotVersion(string Name, IReadOnlyList<GodotRelease> Releases);
