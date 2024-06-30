using System;
using System.Diagnostics;
using System.IO;
using NLog;

namespace GodotHub.App.Helpers;

public static class DirectoryManager
{
    private static readonly ILogger _Logger = LoggingHelper.CreateLogger("Main");
    private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly string GodotHubPath = Path.Combine(AppDataPath, nameof(GodotHub));

    public static string GetSafeInstanceName(string instanceName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(Array.ConvertAll(instanceName.ToCharArray(), c => Array.IndexOf(invalidChars, c) >= 0 ? '_' : c));

        return safeName;
    }

    public static string EnsureInstanceDirectory(string instanceName)
    {
        var safeInstanceName = GetSafeInstanceName(instanceName);
        var appSpecificPath = Path.Combine(GodotHubPath, "Instances", safeInstanceName);
        
        if (!Directory.Exists(appSpecificPath)) 
            Directory.CreateDirectory(appSpecificPath);

        return appSpecificPath;
    }
    
    public static string GetInstancesDirectory()
    {
        if (!Directory.Exists(Path.Combine(GodotHubPath, "Instances"))) 
            Directory.CreateDirectory(Path.Combine(GodotHubPath, "Instances"));
        
        return Path.Combine(GodotHubPath, "Instances");
    }
    
    public static string GetIconsDirectory()
    {
        if (!Directory.Exists(Path.Combine(GodotHubPath, "Icons"))) 
            Directory.CreateDirectory(Path.Combine(GodotHubPath, "Icons"));
        
        return Path.Combine(GodotHubPath, "Icons");
    }

    public static string GetDefaultIconPath()
    {
        if (Directory.GetFiles(GetIconsDirectory()).Length == 0)
            return string.Empty;
        
        return File.Exists(Path.Combine(GetIconsDirectory(), "default.png"))
            ? Path.Combine(GetIconsDirectory(), "default.png")
            : Directory.GetFiles(GetIconsDirectory())[0];
    }
    
    public static void OpenFolderInExplorer(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        else
        {
            _Logger.Error($"The directory '{folderPath}' does not exist.");
        }
    }
    
    public static string GetFileNameFromUrl(string url)
    {
        var uri = new Uri(url);
        return Path.GetFileName(uri.LocalPath);
    }
}