using GodotHub.Core.Models;

namespace GodotHub.Core.Contracts;

/// <summary>
/// Defines methods for retrieving information about available Godot versions, releases,
/// and download artifacts, as well as constructing download URLs.
/// </summary>
public interface IGodotReleaseProvider
{
    /// <summary>
    /// Retrieves a list of all available Godot versions asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list of <see cref="GodotVersion"/> objects representing the available Godot versions.
    /// </returns>
    Task<IReadOnlyList<GodotVersion>> GetVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of all available Godot releases asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list of <see cref="GodotRelease"/> objects representing the available Godot releases.
    /// </returns>
    Task<IReadOnlyList<GodotRelease>> GetReleasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of downloadable artifacts for the specified Godot release and build kind asynchronously.
    /// </summary>
    /// <param name="release">
    /// The Godot release for which to retrieve downloadable artifacts.
    /// </param>
    /// <param name="buildKind">
    /// The build kind to filter the downloadable artifacts, such as Standard or DotNet.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list of <see cref="GodotDownloadArtifact"/> objects representing the downloadable artifacts for the specified release and build kind.
    /// </returns>
    Task<IReadOnlyList<GodotDownloadArtifact>> GetDownloadsAsync(
        GodotRelease release,
        GodotBuildKind buildKind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a download URL for a specific Godot release artifact.
    /// </summary>
    /// <param name="version">
    /// The version of the Godot release (e.g., "4.0.0").
    /// </param>
    /// <param name="releaseName">
    /// The name of the release flavor (e.g., "stable", "mono").
    /// </param>
    /// <param name="platform">
    /// The target platform for the artifact (e.g., "windows", "linux").
    /// </param>
    /// <param name="slug">
    /// The unique slug identifier for the artifact.
    /// </param>
    /// <returns>
    /// A <see cref="Uri"/> representing the fully formed download URL for the specified release artifact.
    /// </returns>
    Uri CreateDownloadUrl(string version, string releaseName, string platform, string slug);
}
