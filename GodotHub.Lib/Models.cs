using System.Text.Json.Serialization;

namespace GodotHub.Lib;

public enum ReleaseType
{
    Stable,
    ReleaseCandidate,
    Beta,
    Alpha,
    Dev,
    Unknown
}

public sealed record GodotRelease
(
    [property: JsonPropertyName("tag_name")] string? TagName,
    [property: JsonPropertyName("html_url")] string? HtmlUrl,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("published_at")] DateTime PublishedAt,
    [property: JsonPropertyName("assets")] List<GodotReleaseAsset> Assets
)
{
    public ReleaseType? Type 
    {
        get 
        {
            if (TagName == null)
                return ReleaseType.Unknown;

            if (TagName.Contains("-stable"))
                return ReleaseType.Stable;

            if (TagName.Contains("-rc"))
                return ReleaseType.ReleaseCandidate;

            if (TagName.Contains("-beta"))
                return ReleaseType.Beta;

            if (TagName.Contains("-alpha"))
                return ReleaseType.Alpha;
            
            if (TagName.Contains("-dev"))
                return ReleaseType.Dev;

            return ReleaseType.Unknown;
        }
    }
};

public sealed record GodotReleaseAsset
(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("size")] int Size,
    [property: JsonPropertyName("browser_download_url")] string? DownloadUrl
);