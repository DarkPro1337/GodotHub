using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace GodotHub.Desktop.Helpers;

public static class LoggingHelper
{
    private const string LogLayout =
        "[${longdate}] ${level:uppercase=true:padding=-5}" +
        "[${logger}] ${message} ${exception:format=tostring}";

    public static string LogsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GodotHub",
        "Logs");

    public static void Initialize()
    {
        Directory.CreateDirectory(LogsDirectory);

        var config = new LoggingConfiguration();

        var fileTarget = new FileTarget("file")
        {
            FileName = Path.Combine(LogsDirectory, "godothub.log"),
            Layout = LogLayout,
            ArchiveOldFileOnStartup = true,
            ArchiveEvery = FileArchivePeriod.Day,
            ArchiveAboveSize = 10 * 1024 * 1024,
            ArchiveSuffixFormat = "_{0:00}",
            MaxArchiveFiles = 10,
            KeepFileOpen = true
        };

        config.AddRule(
            LogLevel.Debug,
            LogLevel.Fatal,
            fileTarget);

#if DEBUG
        var consoleTarget = new ConsoleTarget("console")
        {
            Layout = LogLayout
        };

        config.AddRule(
            LogLevel.Trace,
            LogLevel.Fatal,
            consoleTarget);
#endif

        LogManager.Configuration = config;
    }

    public static ILogger CreateLogger<T>()
    {
        var name = GetLoggerName(typeof(T));
        return LogManager.GetLogger(name);
    }

    private static string GetLoggerName(Type type)
    {
        var name = type.Name;

        name = RemoveSuffix(name, "ViewModel");
        name = RemoveSuffix(name, "Window");

        return name;
    }

    private static string RemoveSuffix(string value, string suffix)
    {
        return value.EndsWith(suffix, StringComparison.Ordinal)
            ? value[..^suffix.Length]
            : value;
    }

    public static void Shutdown()
    {
        LogManager.Shutdown();
    }
}