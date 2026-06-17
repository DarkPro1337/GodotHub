using GodotHub.Core.Models;

namespace GodotHub.Core.Services;

public interface IGodotReleaseProvider
{
    Task<IReadOnlyList<GodotVersion>> GetVersionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GodotRelease>> GetReleasesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GodotDownloadArtifact>> GetDownloadsAsync(
        GodotRelease release,
        GodotBuildKind buildKind,
        CancellationToken cancellationToken = default);

    Uri CreateDownloadUrl(string version, string releaseName, string platform, string slug);
}
