using System.Globalization;
using System.Text.Json;
using GodotHub.Core.Internal;
using GodotHub.Core.Models;
using YamlDotNet.Serialization;

namespace GodotHub.Core.Services;

public sealed class GodotReleaseProvider : IGodotReleaseProvider
{
    public static readonly Uri DefaultVersionsUri = new("https://godotengine.org/versions.json");
    public static readonly Uri DefaultDownloadConfigsUri = new("https://raw.githubusercontent.com/godotengine/godot-website/master/_data/download_configs.yml");

    private readonly HttpClient _httpClient;
    private readonly Uri _versionsUri;
    private readonly Uri _downloadConfigsUri;
    private readonly SemaphoreSlim _metadataLock = new(1, 1);

    private MetadataCache? _metadataCache;

    public GodotReleaseProvider(HttpClient httpClient)
        : this(httpClient, DefaultVersionsUri, DefaultDownloadConfigsUri)
    {
    }

    public GodotReleaseProvider(HttpClient httpClient, Uri versionsUri, Uri downloadConfigsUri)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _versionsUri = versionsUri ?? throw new ArgumentNullException(nameof(versionsUri));
        _downloadConfigsUri = downloadConfigsUri ?? throw new ArgumentNullException(nameof(downloadConfigsUri));
    }

    public async Task<IReadOnlyList<GodotVersion>> GetVersionsAsync(CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        return metadata.Versions;
    }

    public async Task<IReadOnlyList<GodotRelease>> GetReleasesAsync(CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        return metadata.Releases;
    }

    public async Task<IReadOnlyList<GodotDownloadArtifact>> GetDownloadsAsync(
        GodotRelease release,
        GodotBuildKind buildKind,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(release);

        var metadata = await GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        if (!GodotReleaseKey.TryParse($"{release.Version}-{release.Name}", out var releaseKey))
        {
            return [];
        }

        var config = metadata.DownloadConfig.Resolve(releaseKey);
        if (config is null)
        {
            return [];
        }

        if (buildKind == GodotBuildKind.DotNet)
        {
            config = config.Mono;
            if (config is null)
            {
                return [];
            }
        }

        return CreateArtifacts(release, buildKind, config);
    }

    public Uri CreateDownloadUrl(string version, string releaseName, string platform, string slug)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Version is required.", nameof(version));
        }

        if (string.IsNullOrWhiteSpace(releaseName))
        {
            throw new ArgumentException("Release name is required.", nameof(releaseName));
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new ArgumentException("Platform is required.", nameof(platform));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug is required.", nameof(slug));
        }

        var query = string.Join(
            '&',
            $"version={Uri.EscapeDataString(version)}",
            $"flavor={Uri.EscapeDataString(releaseName)}",
            $"slug={Uri.EscapeDataString(slug)}",
            $"platform={Uri.EscapeDataString(platform)}");

        return new UriBuilder("https", "downloads.godotengine.org")
        {
            Query = query
        }.Uri;
    }

    private async Task<MetadataCache> GetMetadataAsync(CancellationToken cancellationToken)
    {
        if (_metadataCache is { } cached)
        {
            return cached;
        }

        await _metadataLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_metadataCache is { } lockedCached)
            {
                return lockedCached;
            }

            var versionsJson = await FetchStringAsync(_versionsUri, cancellationToken).ConfigureAwait(false);
            var downloadConfigYaml = await FetchStringAsync(_downloadConfigsUri, cancellationToken).ConfigureAwait(false);

            try
            {
                var versions = ParseVersions(versionsJson);
                var releases = versions.SelectMany(version => version.Releases).ToArray();
                var downloadConfig = ParseDownloadConfig(downloadConfigYaml);

                _metadataCache = new MetadataCache(versions, releases, downloadConfig);
                return _metadataCache;
            }
            catch (JsonException exception)
            {
                throw new GodotReleaseProviderException("Failed to parse Godot versions metadata.", exception);
            }
            catch (YamlDotNet.Core.YamlException exception)
            {
                throw new GodotReleaseProviderException("Failed to parse Godot download configuration metadata.", exception);
            }
        }
        finally
        {
            _metadataLock.Release();
        }
    }

    private async Task<string> FetchStringAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw new GodotReleaseProviderException($"Failed to fetch Godot metadata from '{uri}'.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GodotReleaseProviderException($"Timed out while fetching Godot metadata from '{uri}'.", exception);
        }
    }

    private static IReadOnlyList<GodotVersion> ParseVersions(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var versionElements = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray(),
            JsonValueKind.Object when TryGetProperty(root, "versions", out var versionsElement) && versionsElement.ValueKind == JsonValueKind.Array
                => versionsElement.EnumerateArray(),
            _ => throw new JsonException("Expected versions metadata to be an array or an object with a versions array.")
        };

        return versionElements
            .Select(ParseVersion)
            .Where(version => version is not null)
            .Select(version => version!)
            .ToArray();
    }

    private static GodotVersion? ParseVersion(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var versionName = GetString(element, "name", "version");
        if (string.IsNullOrWhiteSpace(versionName))
        {
            return null;
        }

        var releases = new List<GodotRelease>();
        if (TryGetProperty(element, "releases", out var releasesElement) && releasesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var releaseElement in releasesElement.EnumerateArray())
            {
                var release = ParseRelease(versionName, releaseElement);
                if (release is not null)
                {
                    releases.Add(release);
                }
            }
        }

        var topLevelReleaseName = GetString(element, "release_name", "releaseName", "flavor", "flavour");
        if (releases.Count == 0 && !string.IsNullOrWhiteSpace(topLevelReleaseName))
        {
            releases.Add(CreateRelease(versionName, topLevelReleaseName, element));
        }

        return new GodotVersion(versionName, releases);
    }

    private static GodotRelease? ParseRelease(string versionName, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var stringReleaseName = element.GetString();
            return string.IsNullOrWhiteSpace(stringReleaseName)
                ? null
                : CreateRelease(versionName, stringReleaseName, element);
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var releaseName = GetString(element, "name", "release_name", "releaseName", "flavor", "flavour");
        return string.IsNullOrWhiteSpace(releaseName)
            ? null
            : CreateRelease(versionName, releaseName, element);
    }

    private static GodotRelease CreateRelease(string versionName, string releaseName, JsonElement element)
    {
        var (channel, channelNumber) = GodotReleaseNameParser.Parse(releaseName);
        return new GodotRelease(
            versionName,
            releaseName,
            channel,
            channelNumber,
            TryParseDate(GetString(element, "date", "release_date", "releaseDate")),
            TryParseUri(GetString(element, "notes", "release_notes", "releaseNotes", "release_notes_url", "releaseNotesUrl", "url")));
    }

    private static DateOnly? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] formats = ["d MMMM yyyy", "dd MMMM yyyy", "d MMM yyyy", "dd MMM yyyy", "yyyy-MM-dd"];
        return DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact)
            || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out exact)
            ? exact
            : null;
    }

    private static Uri? TryParseUri(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement property)
    {
        foreach (var jsonProperty in element.EnumerateObject())
        {
            if (string.Equals(jsonProperty.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                property = jsonProperty.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private GodotDownloadConfigIndex ParseDownloadConfig(string yaml)
    {
        var deserializer = new DeserializerBuilder().Build();
        var root = deserializer.Deserialize<Dictionary<object, object?>>(yaml);
        if (root is null)
        {
            throw new GodotReleaseProviderException("Godot download configuration was empty.");
        }

        var defaults = new Dictionary<int, DownloadConfig>();
        if (TryGetMap(root, "defaults", out var defaultsMap))
        {
            foreach (var (key, value) in defaultsMap)
            {
                if (TryParseIntKey(key, out var majorVersion) && value is Dictionary<object, object?> configMap)
                {
                    defaults[majorVersion] = ParseConfig(configMap);
                }
            }
        }

        var overrides = new List<DownloadConfigOverride>();
        if (TryGetList(root, "overrides", out var overrideItems))
        {
            foreach (var overrideItem in overrideItems.OfType<Dictionary<object, object?>>())
            {
                var parsedOverride = ParseOverride(overrideItem);
                if (parsedOverride is not null)
                {
                    overrides.Add(parsedOverride);
                }
            }
        }

        return new GodotDownloadConfigIndex(defaults, overrides);
    }

    private static DownloadConfigOverride? ParseOverride(Dictionary<object, object?> map)
    {
        if (!TryGetScalar(map, "version", out var versionText) || !int.TryParse(versionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version))
        {
            return null;
        }

        if (!TryGetList(map, "range", out var range) || range.Count < 2)
        {
            return null;
        }

        if (!GodotReleaseKey.TryParse(Convert.ToString(range[0], CultureInfo.InvariantCulture) ?? string.Empty, out var from)
            || !GodotReleaseKey.TryParse(Convert.ToString(range[1], CultureInfo.InvariantCulture) ?? string.Empty, out var to))
        {
            return null;
        }

        var hasConfigValue = TryGetValue(map, "config", out var rawConfig);
        var configMap = rawConfig as Dictionary<object, object?> ?? [];
        var hasConfig = rawConfig is Dictionary<object, object?>;
        var clearsDefaults = hasConfigValue && (!hasConfig || configMap.Count == 0);
        var config = hasConfig ? ParseConfig(configMap) : new DownloadConfig();

        return new DownloadConfigOverride(version, from, to, config, clearsDefaults);
    }

    private static DownloadConfig ParseConfig(Dictionary<object, object?> map)
    {
        var config = new DownloadConfig
        {
            Templates = GetScalar(map, "templates"),
            Editor = GetStringMap(map, "editor"),
            Extras = GetStringMap(map, "extras"),
            Mono = TryGetMap(map, "mono", out var monoMap) ? ParseConfig(monoMap) : null
        };

        return config;
    }

    private IReadOnlyList<GodotDownloadArtifact> CreateArtifacts(GodotRelease release, GodotBuildKind buildKind, DownloadConfig config)
    {
        var artifacts = new List<GodotDownloadArtifact>();

        foreach (var (platform, slug) in config.Editor)
        {
            artifacts.Add(new GodotDownloadArtifact(
                release.Version,
                release.Name,
                platform,
                slug,
                buildKind,
                GodotArtifactKind.Editor,
                CreateDownloadUrl(release.Version, release.Name, platform, slug)));
        }

        if (!string.IsNullOrWhiteSpace(config.Templates))
        {
            artifacts.Add(new GodotDownloadArtifact(
                release.Version,
                release.Name,
                "templates",
                config.Templates,
                buildKind,
                GodotArtifactKind.ExportTemplates,
                CreateDownloadUrl(release.Version, release.Name, "templates", config.Templates)));
        }

        foreach (var (platform, slug) in config.Extras)
        {
            artifacts.Add(new GodotDownloadArtifact(
                release.Version,
                release.Name,
                platform,
                slug,
                buildKind,
                GodotArtifactKind.Extra,
                CreateDownloadUrl(release.Version, release.Name, platform, slug)));
        }

        return artifacts;
    }

    private static Dictionary<string, string> GetStringMap(Dictionary<object, object?> map, string key)
    {
        if (!TryGetMap(map, key, out var child))
        {
            return [];
        }

        return child
            .Select(pair => new
            {
                Key = Convert.ToString(pair.Key, CultureInfo.InvariantCulture),
                Value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture)
            })
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key!, pair => pair.Value!, StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetScalar(Dictionary<object, object?> map, string key)
    {
        return TryGetScalar(map, key, out var value) ? value : null;
    }

    private static bool TryGetScalar(Dictionary<object, object?> map, string key, out string? value)
    {
        if (TryGetValue(map, key, out var rawValue))
        {
            value = Convert.ToString(rawValue, CultureInfo.InvariantCulture);
            return !string.IsNullOrWhiteSpace(value);
        }

        value = null;
        return false;
    }

    private static bool TryGetMap(Dictionary<object, object?> map, string key, out Dictionary<object, object?> child)
    {
        if (TryGetValue(map, key, out var value) && value is Dictionary<object, object?> dictionary)
        {
            child = dictionary;
            return true;
        }

        child = [];
        return false;
    }

    private static bool TryGetList(Dictionary<object, object?> map, string key, out List<object?> list)
    {
        if (TryGetValue(map, key, out var value) && value is List<object?> values)
        {
            list = values;
            return true;
        }

        list = [];
        return false;
    }

    private static bool TryGetValue(Dictionary<object, object?> map, string key, out object? value)
    {
        foreach (var pair in map)
        {
            if (string.Equals(Convert.ToString(pair.Key, CultureInfo.InvariantCulture), key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryParseIntKey(object key, out int value)
    {
        return int.TryParse(Convert.ToString(key, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private sealed record MetadataCache(
        IReadOnlyList<GodotVersion> Versions,
        IReadOnlyList<GodotRelease> Releases,
        GodotDownloadConfigIndex DownloadConfig);

    private sealed class GodotDownloadConfigIndex(
        IReadOnlyDictionary<int, DownloadConfig> defaults,
        IReadOnlyList<DownloadConfigOverride> overrides)
    {
        public DownloadConfig? Resolve(GodotReleaseKey releaseKey)
        {
            var majorVersion = releaseKey.VersionParts[0];
            defaults.TryGetValue(majorVersion, out var defaultConfig);

            var matchedOverride = overrides.FirstOrDefault(item => item.Matches(majorVersion, releaseKey));
            if (matchedOverride is null)
            {
                return defaultConfig;
            }

            if (matchedOverride.ClearsDefaults)
            {
                return matchedOverride.Config;
            }

            return defaultConfig is null
                ? matchedOverride.Config
                : defaultConfig.Merge(matchedOverride.Config);
        }
    }

    private sealed record DownloadConfigOverride(
        int Version,
        GodotReleaseKey From,
        GodotReleaseKey To,
        DownloadConfig Config,
        bool ClearsDefaults)
    {
        public bool Matches(int majorVersion, GodotReleaseKey releaseKey)
        {
            return Version == majorVersion && releaseKey.CompareTo(From) >= 0 && releaseKey.CompareTo(To) <= 0;
        }
    }

    private sealed class DownloadConfig
    {
        public string? Templates { get; init; }

        public IReadOnlyDictionary<string, string> Editor { get; init; } = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, string> Extras { get; init; } = new Dictionary<string, string>();

        public DownloadConfig? Mono { get; init; }

        public DownloadConfig Merge(DownloadConfig overlay)
        {
            return new DownloadConfig
            {
                Templates = overlay.Templates ?? Templates,
                Editor = Merge(Editor, overlay.Editor),
                Extras = Merge(Extras, overlay.Extras),
                Mono = Mono is null
                    ? overlay.Mono
                    : overlay.Mono is null
                        ? Mono
                        : Mono.Merge(overlay.Mono)
            };
        }

        private static IReadOnlyDictionary<string, string> Merge(
            IReadOnlyDictionary<string, string> current,
            IReadOnlyDictionary<string, string> overlay)
        {
            var merged = new Dictionary<string, string>(current, StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in overlay)
            {
                merged[key] = value;
            }

            return merged;
        }
    }
}
