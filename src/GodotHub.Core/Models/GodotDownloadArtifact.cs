namespace GodotHub.Core.Models;

public sealed record GodotDownloadArtifact(
    string Version,
    string Name,
    string Platform,
    string Slug,
    GodotBuildKind BuildKind,
    GodotArtifactKind ArtifactKind,
    Uri Url);
