namespace GodotHub.Core.Services;

/// <summary>
/// Represents an error that occurred while retrieving Godot release information.
/// </summary>
public sealed class GodotReleaseProviderException : Exception
{
    public GodotReleaseProviderException(string message)
        : base(message)
    {
    }

    public GodotReleaseProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
