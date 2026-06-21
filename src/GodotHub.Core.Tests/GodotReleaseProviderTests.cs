using System.Net;
using GodotHub.Core.Models;
using GodotHub.Core.Services;
using Xunit;

namespace GodotHub.Core.Tests;

public sealed class GodotReleaseProviderTests
{
    private static readonly Uri _versionsUri = new("https://example.test/versions.json");
    private static readonly Uri _configUri = new("https://example.test/download_configs.yml");

    [Fact]
    public async Task GetVersionsAsync_ParsesVersionsJson()
    {
        var provider = CreateProvider();

        var versions = await provider.GetVersionsAsync();

        Assert.Equal(["4.7", "3.0"], versions.Select(version => version.Name));
        var beta = Assert.Single(versions[0].Releases, release => release.Name == "beta5");
        Assert.Equal(GodotReleaseChannel.Beta, beta.Channel);
        Assert.Equal(5, beta.ChannelNumber);
        Assert.Equal(new DateOnly(2026, 5, 20), beta.ReleaseDate);
        Assert.Equal(new Uri("https://godotengine.org/article/beta5"), beta.ReleaseNotesUrl);
    }

    [Fact]
    public async Task GetReleasesAsync_FlattensReleases()
    {
        var provider = CreateProvider();

        var releases = await provider.GetReleasesAsync();

        Assert.Equal(["beta5", "stable", "alpha1", "stable"], releases.Select(release => release.Name));
    }

    [Fact]
    public void CreateDownloadUrl_GeneratesStandardDownloadUrl()
    {
        var provider = CreateProvider();

        var url = provider.CreateDownloadUrl("4.6.3", "stable", "windows.64", "win64.exe.zip");

        Assert.Equal("https://downloads.godotengine.org/?version=4.6.3&flavor=stable&slug=win64.exe.zip&platform=windows.64", url.ToString());
    }

    [Fact]
    public void CreateDownloadUrl_GeneratesDotNetDownloadUrl()
    {
        var provider = CreateProvider();

        var url = provider.CreateDownloadUrl("4.6.3", "stable", "windows.64", "mono_win64.zip");

        Assert.Equal("https://downloads.godotengine.org/?version=4.6.3&flavor=stable&slug=mono_win64.zip&platform=windows.64", url.ToString());
    }

    [Fact]
    public async Task GetDownloadsAsync_AppliesDefaultGodot4Config()
    {
        var provider = CreateProvider();
        var release = new GodotRelease("4.7", "stable", GodotReleaseChannel.Stable, null, null, null);

        var downloads = await provider.GetDownloadsAsync(release, GodotBuildKind.Standard);

        var windows = Assert.Single(downloads, download => download.Platform == "windows.64");
        Assert.Equal("win64.exe.zip", windows.Slug);
        Assert.Equal(GodotArtifactKind.Editor, windows.ArtifactKind);
        Assert.Contains(downloads, download => download.ArtifactKind == GodotArtifactKind.ExportTemplates && download.Platform == "templates");
        Assert.Contains(downloads, download => download.ArtifactKind == GodotArtifactKind.Extra && download.Platform == "aar_library");
    }

    [Fact]
    public async Task GetDownloadsAsync_UsesMonoConfigForDotNetBuilds()
    {
        var provider = CreateProvider();
        var release = new GodotRelease("4.7", "beta5", GodotReleaseChannel.Beta, 5, null, null);

        var downloads = await provider.GetDownloadsAsync(release, GodotBuildKind.DotNet);

        var windows = Assert.Single(downloads, download => download.Platform == "windows.64");
        Assert.Equal("mono_win64.zip", windows.Slug);
        Assert.Equal(GodotBuildKind.DotNet, windows.BuildKind);
        Assert.Contains("flavor=beta5", windows.Url.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDownloadsAsync_AppliesHistoricalOverrideRange()
    {
        var provider = CreateProvider();
        var release = new GodotRelease("3.0.6", "stable", GodotReleaseChannel.Stable, null, null, null);

        var downloads = await provider.GetDownloadsAsync(release, GodotBuildKind.Standard);

        var macos = Assert.Single(downloads, download => download.Platform == "macos.universal");
        Assert.Equal("osx.fat.zip", macos.Slug);
    }

    [Fact]
    public async Task GetDownloadsAsync_ReturnsEmptyArtifactsWhenOverrideClearsConfig()
    {
        var provider = CreateProvider();
        var release = new GodotRelease("3.0", "alpha1", GodotReleaseChannel.Alpha, 1, null, null);

        var downloads = await provider.GetDownloadsAsync(release, GodotBuildKind.Standard);

        Assert.Empty(downloads);
    }

    [Fact]
    public async Task GetDownloadsAsync_ReturnsEmptyArtifactsWhenNoConfigExists()
    {
        var provider = CreateProvider();
        var release = new GodotRelease("5.0", "stable", GodotReleaseChannel.Stable, null, null, null);

        var downloads = await provider.GetDownloadsAsync(release, GodotBuildKind.Standard);

        Assert.Empty(downloads);
    }

    [Fact]
    public async Task GetVersionsAsync_ReusesCachedMetadata()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Responses[_versionsUri] = VersionsJson;
        handler.Responses[_configUri] = DownloadConfigYaml;
        var provider = new GodotReleaseProvider(new HttpClient(handler), _versionsUri, _configUri);

        await provider.GetVersionsAsync();
        await provider.GetReleasesAsync();
        await provider.GetDownloadsAsync(new GodotRelease("4.7", "stable", GodotReleaseChannel.Stable, null, null, null), GodotBuildKind.Standard);

        Assert.Equal(1, handler.RequestCounts[_versionsUri]);
        Assert.Equal(1, handler.RequestCounts[_configUri]);
    }

    private static GodotReleaseProvider CreateProvider()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Responses[_versionsUri] = VersionsJson;
        handler.Responses[_configUri] = DownloadConfigYaml;
        return new GodotReleaseProvider(new HttpClient(handler), _versionsUri, _configUri);
    }

    private const string VersionsJson = """
        [
          {
            "name": "4.7",
            "flavor": "stable",
            "date": "21 May 2026",
            "releases": [
              { "name": "beta5", "date": "20 May 2026", "notes": "https://godotengine.org/article/beta5" },
              { "name": "stable", "date": "bad date", "notes": "https://godotengine.org/article/stable" }
            ]
          },
          {
            "name": "3.0",
            "releases": [
              { "name": "alpha1", "date": "1 January 2018" },
              { "name": "stable", "date": "29 January 2018" }
            ]
          }
        ]
        """;

    private const string DownloadConfigYaml = """
        defaults:
          4:
            templates: export_templates.tpz
            editor:
              linux.64: linux.x86_64.zip
              macos.universal: macos.universal.zip
              windows.64: win64.exe.zip
            mono:
              templates: mono_export_templates.tpz
              editor:
                linux.64: mono_linux_x86_64.zip
                macos.universal: mono_macos.universal.zip
                windows.64: mono_win64.zip
            extras:
              aar_library: template_release.aar
          3:
            templates: export_templates.tpz
            editor:
              linux.64: x11.64.zip
              macos.universal: osx.universal.zip
              windows.64: win64.exe.zip
        overrides:
          - version: 3
            range:
              - "3.0-alpha1"
              - "3.0-rc3"
            config:
          - version: 3
            range:
              - "3.0-stable"
              - "3.0.6-stable"
            config:
              editor:
                macos.universal: osx.fat.zip
        """;

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public Dictionary<Uri, string> Responses { get; } = [];

        public Dictionary<Uri, int> RequestCounts { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri ?? throw new InvalidOperationException("Request URI is missing.");
            RequestCounts[uri] = RequestCounts.GetValueOrDefault(uri) + 1;

            return Responses.TryGetValue(uri, out var response)
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(response)
                })
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
