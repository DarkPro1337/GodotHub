using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace GodotHub.App.Helpers;

public static class LoggingHelper
{
    static LoggingHelper()
    {
        var uniqueIdentifier = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var config = new LoggingConfiguration();

        var tempPath = Path.Combine(Path.GetTempPath(), "GodotHubLogs");
        Directory.CreateDirectory(tempPath);

        var logfile = new FileTarget("logfile")
        {
            FileName = Path.Combine(tempPath, $"logfile-{uniqueIdentifier}.log"),
            Layout = "[${longdate}]  ${level:uppercase=true}  ${logger}  ${message} ${exception:format=tostring}",
            ArchiveEvery = FileArchivePeriod.Day,
            ArchiveNumbering = ArchiveNumberingMode.Rolling,
            MaxArchiveFiles = 5,
            ConcurrentWrites = true,
            KeepFileOpen = false
        };

        var consoleTarget = new ConsoleTarget("logconsole")
        {
            Layout = "[${longdate}]  ${level:uppercase=true}  ${logger}  ${message} ${exception:format=tostring}"
        };

        config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);

        LogManager.Configuration = config;
    }

    public static ILogger CreateLogger(string name) => LogManager.GetLogger(name);
}