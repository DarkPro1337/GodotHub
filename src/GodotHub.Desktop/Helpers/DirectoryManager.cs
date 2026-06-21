using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;

namespace GodotHub.Desktop.Helpers;

public static class DirectoryManager
{
    private const string AppName = "GodotHub";

    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    private static readonly string _rootDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

    private static readonly string _instancesDirectory = Path.Combine(_rootDirectory, "Instances");
    private static readonly string _cacheDirectory = Path.Combine(_rootDirectory, "Cache");
    private static readonly string _iconsDirectory = Path.Combine(_rootDirectory, "Icons");

    public static string GetInstancesDirectory() => EnsureDirectory(_instancesDirectory);
    public static string GetInstancesCacheDirectory() => EnsureDirectory(_cacheDirectory);
    public static string GetIconsDirectory() => EnsureDirectory(_iconsDirectory);

    public static string EnsureInstanceDirectory(string instanceName)
    {
        var safeName = GetSafeInstanceName(instanceName);
        var path = Path.Combine(GetInstancesDirectory(), safeName);

        return EnsureDirectory(path);
    }

    public static string GetSafeInstanceName(string instanceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);

        var invalidChars = Path.GetInvalidFileNameChars();

        var safeName = new string(
            instanceName
                .Trim()
                .Select(c => invalidChars.Contains(c) ||
                             char.IsControl(c)
                    ? '_'
                    : c)
                .ToArray())
            .TrimEnd('.', ' ');

        if (string.IsNullOrWhiteSpace(safeName) || safeName is "." or "..")
            throw new ArgumentException("Instance name cannot be converted to a valid directory name.", nameof(instanceName));

        return safeName;
    }

    public static bool OpenDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            _logger.Warn("Directory {DirectoryPath} does not exist", directoryPath);
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directoryPath,
                UseShellExecute = true
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to open directory {DirectoryPath}", directoryPath);
            return false;
        }
    }

    public static string GetFileNameFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("The value must be a valid absolute URL.", nameof(url));

        var fileName = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("The URL does not contain a file name.", nameof(url));

        return fileName;
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}