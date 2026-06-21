using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GodotHub.Core.Contracts;
using GodotHub.Core.Models;
using GodotHub.Desktop.Helpers;
using GodotHub.Desktop.Models;

namespace GodotHub.Desktop.Services;

public sealed class GodotInstanceInstaller(HttpClient httpClient, IGodotReleaseProvider releaseProvider)
{
    public const string ManifestFileName = "instance.json";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<GodotInstanceManifest> InstallAsync(
        string name,
        string group,
        GodotRelease release,
        GodotBuildKind buildKind,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var artifact = await GetEditorArtifactAsync(release, buildKind, cancellationToken).ConfigureAwait(false);
        var instanceDirectory = DirectoryManager.EnsureInstanceDirectory(name);
        var cacheDirectory = DirectoryManager.GetInstancesCacheDirectory();
        var archivePath = Path.Combine(cacheDirectory, GetArchiveFileName(artifact));

        await DownloadAsync(artifact.Url, archivePath, progress, cancellationToken).ConfigureAwait(false);

        var extractionDirectory = Path.Combine(instanceDirectory, "editor");
        if (Directory.Exists(extractionDirectory))
            Directory.Delete(extractionDirectory, true);

        Directory.CreateDirectory(extractionDirectory);
        await ZipFile.ExtractToDirectoryAsync(archivePath, extractionDirectory, true, cancellationToken);

        var executablePath = FindGodotExecutable(extractionDirectory);
        EnsureExecutable(executablePath);

        var manifest = new GodotInstanceManifest
        {
            Name = name.Trim(),
            Group = group.Trim(),
            Version = release.Version,
            ReleaseName = release.Name,
            BuildKind = buildKind,
            Platform = artifact.Platform,
            DownloadUrl = artifact.Url.ToString(),
            ExecutablePath = Path.GetRelativePath(instanceDirectory, executablePath),
            InstalledAt = DateTimeOffset.UtcNow
        };

        await SaveManifestAsync(instanceDirectory, manifest, cancellationToken).ConfigureAwait(false);
        return manifest;
    }

    public static GodotInstanceManifest? TryLoadManifest(string instanceDirectory)
    {
        var manifestPath = Path.Combine(instanceDirectory, ManifestFileName);
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            var manifest = JsonSerializer.Deserialize<GodotInstanceManifest>(
                File.ReadAllText(manifestPath),
                _jsonOptions);

            return string.IsNullOrWhiteSpace(manifest?.Name) || string.IsNullOrWhiteSpace(manifest.ExecutablePath)
                ? null
                : manifest;
        }
        catch
        {
            return null;
        }
    }

    public static bool Launch(GodotInstanceManifest manifest)
    {
        var instanceDirectory = Path.Combine(DirectoryManager.GetInstancesDirectory(), DirectoryManager.GetSafeInstanceName(manifest.Name));
        var executablePath = Path.GetFullPath(Path.Combine(instanceDirectory, manifest.ExecutablePath));

        if (!File.Exists(executablePath))
            return false;

        Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = false
        });

        return true;
    }

    private async Task<GodotDownloadArtifact> GetEditorArtifactAsync(
        GodotRelease release,
        GodotBuildKind buildKind,
        CancellationToken cancellationToken)
    {
        var downloads = await releaseProvider.GetDownloadsAsync(release, buildKind, cancellationToken).ConfigureAwait(false);
        var platformCandidates = GetCurrentPlatformCandidates();

        foreach (var platform in platformCandidates)
        {
            var artifact = downloads.FirstOrDefault(download =>
                download.ArtifactKind == GodotArtifactKind.Editor &&
                string.Equals(download.Platform, platform, StringComparison.OrdinalIgnoreCase));

            if (artifact is not null)
                return artifact;
        }

        throw new InvalidOperationException(
            $"No {buildKind} Godot editor download is available for this OS and architecture.");
    }

    private static IReadOnlyList<string> GetCurrentPlatformCandidates()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => ["windows.arm64", "windows.64"],
                Architecture.X86 => ["windows.32"],
                _ => ["windows.64"]
            };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return ["macos.universal"];

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => ["linux.arm64", "linux.64"],
                Architecture.X86 => ["linux.32"],
                _ => ["linux.64"]
            };
        }

        throw new PlatformNotSupportedException("GodotHub does not know which Godot editor artifact to use for this OS.");
    }

    private static string GetArchiveFileName(GodotDownloadArtifact artifact)
    {
        var fileName = Path.GetFileName(artifact.Slug);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException($"Godot download artifact '{artifact.Slug}' does not contain a file name.");

        return fileName;
    }

    private async Task DownloadAsync(
        Uri url,
        string destinationPath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        await using var source = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        progress?.Report(1);
    }

    private static string FindGodotExecutable(string rootDirectory)
    {
        var files = Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
            .Where(IsGodotExecutableCandidate)
            .OrderBy(path => path.Count(c => c == Path.DirectorySeparatorChar))
            .ThenBy(path => path.Length)
            .ToList();

        return files.FirstOrDefault()
               ?? throw new FileNotFoundException("The downloaded archive did not contain a Godot executable.");
    }

    private static bool IsGodotExecutableCandidate(string path)
    {
        var fileName = Path.GetFileName(path);
        if (!fileName.Contains("Godot", StringComparison.OrdinalIgnoreCase))
            return false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return path.Contains($"{Path.DirectorySeparatorChar}Contents{Path.DirectorySeparatorChar}MacOS{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

        return !fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
               !fileName.EndsWith(".so", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureExecutable(string executablePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        File.SetUnixFileMode(
            executablePath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }

    private static Task SaveManifestAsync(
        string instanceDirectory,
        GodotInstanceManifest manifest,
        CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(instanceDirectory, ManifestFileName);
        var json = JsonSerializer.Serialize(manifest, _jsonOptions);
        return File.WriteAllTextAsync(manifestPath, json, cancellationToken);
    }
}
