using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using GodotHub.App.Helpers;
using GodotHub.App.Views;
using NLog;
using ReactiveUI;

namespace GodotHub.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private static readonly ILogger _Logger = LoggingHelper.CreateLogger("Main");
    
    public ObservableCollection<InstanceViewModel> Instances { get; } = [];
    
    public ReactiveCommand<Window, Unit> AddInstanceCommand { get; }
    public ReactiveCommand<Window, Unit> OpenSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewInstancesFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewLauncherRootFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCacheCommand { get; }
    public ReactiveCommand<Unit, Unit> ReportBugCommand { get; }
    public ReactiveCommand<Window, Unit> OpenAboutCommand { get; }
    
    public MainWindowViewModel()
    {
        _Logger.Debug("Initializing");
        AddInstanceCommand = ReactiveCommand.CreateFromTask<Window>(ExecuteAddInstance);
        OpenSettingsCommand = ReactiveCommand.CreateFromTask<Window>(ExecuteOpenSettings);
        ViewInstancesFolderCommand = ReactiveCommand.Create(ExecuteViewInstancesFolder);
        ViewLauncherRootFolderCommand = ReactiveCommand.Create(ExecuteViewLauncherRootFolder);
        ClearCacheCommand = ReactiveCommand.Create(ExecuteClearCache);
        ReportBugCommand = ReactiveCommand.Create(ExecuteReportBug);
        OpenAboutCommand = ReactiveCommand.Create<Window>(ExecuteOpenAbout);
    }

    public async void InitializeAsync()
    {
        await LoadInstances();
    }

    private async Task ExecuteAddInstance(Window window)
    {
        _Logger.Debug("Adding new instance");
        var selector = new CreateInstanceViewModel();
        selector.InitializeAsync();
        var result = await new CreateInstanceWindow(selector).ShowDialog<bool>(window);
        if (result)
        {
            if (selector.SelectedRelease is { } release)
            {
                _Logger.Debug("Creating new instance - {0}.", release.Name);
                var instance = new InstanceViewModel(release, selector.Name, selector.Group, selector.IconPath, selector.IsMono);
                Instances.Add(instance);
                var path = DirectoryManager.EnsureInstanceDirectory(instance.Name);
                instance.InstanceDirectory = path;
                
                var osName = OsHelper.GetOsName();
                var asset = instance.Release.Assets.FirstOrDefault(x => x.Name != null && x.Name.Contains(osName) && (instance.IsMono ? x.Name.Contains("mono") : !x.Name.Contains("mono")));
                instance.Asset = asset;
        
                if (asset == null || string.IsNullOrEmpty(asset.DownloadUrl))
                {
                    _Logger.Error("No asset found for {0} on {1} for {2} runtime.", instance.Release.Name, osName, instance.IsMono ? "Mono" : "Default");
                    return;
                }
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonIncludeConverter<InstanceViewModel>());
                
                var json = JsonSerializer.Serialize(instance, options);
                await File.WriteAllTextAsync(Path.Combine(path, "instance.json"), json);
                _Logger.Debug("Instance created successfully - {0}.", release.Name);
            }
        }
    }

    private async Task ExecuteOpenSettings(Window window)
    {
        _Logger.Debug("Opening settings window");
        var settings = new SettingsViewModel();
        var settingsWindow = new SettingsWindow(settings);
        var result = await settingsWindow.ShowDialog<bool>(window);
        if (result) 
            settings.SaveSettings();
    }
    
    private void ExecuteClearCache()
    {
        _Logger.Debug("Attempt to clearing cache");
        var cachePath = DirectoryManager.GetInstancesCacheDirectory();
        var cacheFiles = Directory.GetFiles(cachePath);
        if (cacheFiles.Length == 0)
        {
            _Logger.Debug("No cache files found");
            return;
        }

        foreach (var cacheFile in cacheFiles) 
            File.Delete(cacheFile);
        
        _Logger.Debug("Cache cleared for {0} files", cacheFiles.Length);
    }
    
    private void ExecuteReportBug()
    {
        _Logger.Debug("Opening bug report page");
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "https://github.com/DarkPro1337/GodotHub/issues/new"
        });
    }
    
    private void ExecuteOpenAbout(Window window)
    {
        _Logger.Debug("Opening about window");
    }
    
    private async Task LoadInstances()
    {
        _Logger.Debug("Loading instances");
        var instancesPath = DirectoryManager.GetInstancesDirectory();
        var instanceFiles = Directory.GetFiles(instancesPath, "instance.json", SearchOption.AllDirectories);
        if (instanceFiles.Length == 0)
        {
            _Logger.Debug("No instances found");
            return;
        }
        
        foreach (var instanceFile in instanceFiles)
        {
            _Logger.Trace("Loading instance - {0}", instanceFile);
            var instance = JsonSerializer.Deserialize<InstanceViewModel>(await File.ReadAllTextAsync(instanceFile));
            if (instance is null) 
                continue;
            
            Instances.Add(instance);
            _Logger.Debug("Loaded instance - {0}", instance.Name);
        }
    }
    
    private void ExecuteViewInstancesFolder() => DirectoryManager.OpenFolderInExplorer(DirectoryManager.GetInstancesDirectory());

    private void ExecuteViewLauncherRootFolder() => DirectoryManager.OpenFolderInExplorer(AppDomain.CurrentDomain.BaseDirectory);
}