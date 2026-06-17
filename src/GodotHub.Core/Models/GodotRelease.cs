namespace GodotHub.Core.Models;

public sealed record GodotRelease(
    string Version,
    string Name,
    GodotReleaseChannel Channel,
    int? ChannelNumber,
    DateOnly? ReleaseDate,
    Uri? ReleaseNotesUrl)
{
    public bool IsStable => Channel == GodotReleaseChannel.Stable;
    public bool IsPreview => !IsStable;

    public string PrettyName => ToString();

    public override string ToString() => $"{Version}-{Name}";
}
