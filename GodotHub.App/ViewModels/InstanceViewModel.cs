using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GodotHub.App.Helpers;
using GodotHub.Lib;
using NLog;
using ReactiveUI;

namespace GodotHub.App.ViewModels;

public class InstanceViewModel : ViewModelBase
{
    private static readonly ILogger _Logger = LoggingHelper.CreateLogger("Instance");
    
    private readonly HttpClient _httpClient = new();
    
    private GodotRelease _release;
    private GodotReleaseAsset? _asset;
    private string _name;
    private string _group;
    private string _iconPath;
    private bool _isMono;
    private string? _instanceDirectory;
    
    private int _downloadProgress;
    
    public GodotRelease Release
    {
        get => _release;
        set => this.RaiseAndSetIfChanged(ref _release, value);
    }
    
    [JsonInclude]
    public GodotReleaseAsset? Asset
    {
        get => _asset;
        set => this.RaiseAndSetIfChanged(ref _asset, value);
    }
    
    [JsonInclude]
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }
    
    [JsonInclude]
    public string Group
    {
        get => _group;
        set => this.RaiseAndSetIfChanged(ref _group, value);
    }
    
    [JsonInclude]
    public string IconPath
    {
        get => _iconPath;
        set => this.RaiseAndSetIfChanged(ref _iconPath, value);
    }
    
    [JsonInclude]
    public bool IsMono
    {
        get => _isMono;
        set => this.RaiseAndSetIfChanged(ref _isMono, value);
    }

    [JsonInclude]
    public string? InstanceDirectory
    {
        get => _instanceDirectory;
        set => this.RaiseAndSetIfChanged(ref _instanceDirectory, value);
    }
    
    public int DownloadProgress
    {
        get => _downloadProgress;
        set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
    }
    
    public ReactiveCommand<Unit, Unit> LaunchCommand { get; }
    public ReactiveCommand<MainWindowViewModel, Unit> EditCommand { get; }
    public ReactiveCommand<MainWindowViewModel, Unit> DeleteCommand { get; }

    public InstanceViewModel(GodotRelease release, string name, string group, string iconPath, bool isMono)
    {
        _release = release;
        
        if (string.IsNullOrEmpty(name) && release.TagName != null)
            _name = "Unnamed " + release.TagName;
        else
            _name = name;
        
        _group = string.IsNullOrEmpty(group) ? "Default" : group;
        _iconPath = iconPath;
        _isMono = isMono;
        
        LaunchCommand = ReactiveCommand.Create(ExecuteLaunch);
        EditCommand = ReactiveCommand.Create<MainWindowViewModel>(ExecuteEdit);
        DeleteCommand = ReactiveCommand.Create<MainWindowViewModel>(ExecuteDelete);
    }

    public async void ExecuteLaunch()
    {
        InstanceDirectory = DirectoryManager.EnsureInstanceDirectory(_name);
        var existingExecutables = Directory.GetFiles(InstanceDirectory, "*.exe");
        if (existingExecutables.Length > 0)
        {
            _Logger.Debug("Found Godot executable {0}", existingExecutables[0]);
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = existingExecutables[0],
                WorkingDirectory = InstanceDirectory
            });
            return;
        }
        
        if (Asset?.DownloadUrl == null || Asset.Size == 0)
        {
            _Logger.Error("Asset download URL is null or asset size is 0");
            return;
        }
        
        _Logger.Debug("Found asset {0} for {1} on {2} runtime.", Asset.Name, OsHelper.GetOsName(), _isMono ? "Mono" : "Default");
        var fileName = DirectoryManager.GetFileNameFromUrl(Asset.DownloadUrl);
        var downloadPath = Path.Combine(DirectoryManager.GetInstancesCacheDirectory(), fileName);
        _Logger.Debug("Downloading {0} to {1}", fileName, InstanceDirectory);
        if (File.Exists(downloadPath))
        {
            if (new FileInfo(downloadPath).Length == Asset.Size)
            {
                _Logger.Debug("File {0} already exists and has the correct size", fileName);
                ExtractZipFile(downloadPath, InstanceDirectory);
                _Logger.Debug("Extracted {0} to {1}", fileName, InstanceDirectory);
            }
            else
            {
                _Logger.Debug("File {0} already exists but has the wrong size", fileName);
                File.Delete(downloadPath);
                await DownloadAssetAsync(Asset.DownloadUrl, downloadPath, Asset.Size);
                _Logger.Debug("Downloaded {0} to {1}", fileName, InstanceDirectory);
            }
        }

        var godotExecutables = Directory.GetFiles(InstanceDirectory, "*.exe");
        _Logger.Debug("Found Godot executable {0}", godotExecutables[0]);
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = godotExecutables[0],
            WorkingDirectory = InstanceDirectory
        });
        _Logger.Debug("Launched Godot executable {0}", godotExecutables[0]);
    }
    
    private void ExecuteEdit(MainWindowViewModel viewModel)
    {
        _Logger.Debug("Editing instance {0}", _name);
    }

    private void ExecuteDelete(MainWindowViewModel viewModel)
    {
        _Logger.Debug("Deleting instance {0}", _name);
        if (InstanceDirectory == null)
        {
            _Logger.Error("Instance directory is null");
            return;
        }

        Directory.Delete(InstanceDirectory, true);
        viewModel.Instances.Remove(this);
        _Logger.Debug("Deleted instance {0}", _name);
    }
    
    private async Task DownloadAssetAsync(string url, string destinationPath, int assetSize)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var canReportProgress = assetSize > 0;

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 8192, true);
        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer)) != 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;

            if (canReportProgress) 
                DownloadProgress = (int)((totalRead * 100) / assetSize);
        }
    }
    
    private void ExtractZipFile(string zipFilePath, string destinationDirectory)
    {
        using var archive = ZipFile.OpenRead(zipFilePath);
        var rootDirectory = archive.Entries
            .Select(entry => entry.FullName.Split('/')[0])
            .Distinct()
            .SingleOrDefault();

        var rootDirectoryName = Path.GetFileNameWithoutExtension(zipFilePath);

        if (rootDirectory != null && rootDirectory == rootDirectoryName && archive.Entries.Any(e => e.FullName.StartsWith(rootDirectory + "/")))
        {
            foreach (var entry in archive.Entries)
            {
                var relativePath = entry.FullName.Substring(rootDirectory.Length + 1);
                var destinationPath = Path.Combine(destinationDirectory, relativePath);

                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }
        else
        {
            foreach (var entry in archive.Entries)
            {
                var destinationPath = Path.Combine(destinationDirectory, entry.FullName);

                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }
    }
}