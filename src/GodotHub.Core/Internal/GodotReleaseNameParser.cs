using System.Globalization;
using GodotHub.Core.Models;

namespace GodotHub.Core.Internal;

internal static class GodotReleaseNameParser
{
    public static (GodotReleaseChannel Channel, int? Number) Parse(string releaseName)
    {
        if (string.Equals(releaseName, "stable", StringComparison.OrdinalIgnoreCase))
        {
            return (GodotReleaseChannel.Stable, null);
        }

        var prefixLength = releaseName.TakeWhile(char.IsLetter).Count();
        if (prefixLength == 0)
        {
            return (GodotReleaseChannel.Unknown, null);
        }

        var prefix = releaseName[..prefixLength].ToLowerInvariant();
        var numberText = releaseName[prefixLength..];
        var number = int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : (int?)null;

        var channel = prefix switch
        {
            "dev" => GodotReleaseChannel.Dev,
            "alpha" => GodotReleaseChannel.Alpha,
            "beta" => GodotReleaseChannel.Beta,
            "rc" => GodotReleaseChannel.ReleaseCandidate,
            _ => GodotReleaseChannel.Unknown
        };

        return (channel, number);
    }
}
