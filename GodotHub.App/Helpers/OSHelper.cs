using System.Runtime.InteropServices;

namespace GodotHub.App.Helpers;

public static class OsHelper
{
    public static string GetOsName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return RuntimeInformation.OSArchitecture == Architecture.X64 ? "win64" : "win32";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";

        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macos" : "unknown";
    }
}