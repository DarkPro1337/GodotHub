namespace GodotHub.Core.Services;

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
