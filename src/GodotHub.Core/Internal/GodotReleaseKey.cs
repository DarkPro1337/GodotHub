using System.Globalization;
using GodotHub.Core.Models;

namespace GodotHub.Core.Internal;

internal sealed record GodotReleaseKey(IReadOnlyList<int> VersionParts, GodotReleaseChannel Channel, int? ChannelNumber)
    : IComparable<GodotReleaseKey>
{
    public static bool TryParse(string value, out GodotReleaseKey key)
    {
        key = new GodotReleaseKey([], GodotReleaseChannel.Unknown, null);

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('-', 2, StringSplitOptions.TrimEntries);
        var versionText = parts[0];
        var releaseName = parts.Length == 2 ? parts[1] : "stable";

        var versionParts = new List<int>();
        foreach (var part in versionText.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var versionPart))
            {
                return false;
            }

            versionParts.Add(versionPart);
        }

        if (versionParts.Count == 0)
        {
            return false;
        }

        var (channel, channelNumber) = GodotReleaseNameParser.Parse(releaseName);
        key = new GodotReleaseKey(versionParts, channel, channelNumber);
        return true;
    }

    public static GodotReleaseKey FromRelease(string version, string releaseName)
    {
        if (!TryParse($"{version}-{releaseName}", out var key))
        {
            throw new ArgumentException($"Invalid Godot release identifier '{version}-{releaseName}'.");
        }

        return key;
    }

    public int CompareTo(GodotReleaseKey? other)
    {
        if (other is null)
        {
            return 1;
        }

        var maxLength = Math.Max(VersionParts.Count, other.VersionParts.Count);
        for (var i = 0; i < maxLength; i++)
        {
            var left = i < VersionParts.Count ? VersionParts[i] : 0;
            var right = i < other.VersionParts.Count ? other.VersionParts[i] : 0;
            var versionComparison = left.CompareTo(right);
            if (versionComparison != 0)
            {
                return versionComparison;
            }
        }

        var channelComparison = GetChannelOrder(Channel).CompareTo(GetChannelOrder(other.Channel));
        if (channelComparison != 0)
        {
            return channelComparison;
        }

        return (ChannelNumber ?? 0).CompareTo(other.ChannelNumber ?? 0);
    }

    private static int GetChannelOrder(GodotReleaseChannel channel)
    {
        return channel switch
        {
            GodotReleaseChannel.Dev => 0,
            GodotReleaseChannel.Alpha => 1,
            GodotReleaseChannel.Beta => 2,
            GodotReleaseChannel.ReleaseCandidate => 3,
            GodotReleaseChannel.Stable => 4,
            _ => -1
        };
    }
}
