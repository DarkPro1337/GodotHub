using System;
using System.Collections.ObjectModel;
using System.IO;
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
    
    public MainWindowViewModel()
    {
        _Logger.Debug("Initializing");
        AddInstanceCommand = ReactiveCommand.CreateFromTask<Window>(ExecuteAddInstance);
        OpenSettingsCommand = ReactiveCommand.CreateFromTask<Window>(ExecuteOpenSettings);
        ViewInstancesFolderCommand = ReactiveCommand.Create(ExecuteViewInstancesFolder);
        ViewLauncherRootFolderCommand = ReactiveCommand.Create(ExecuteViewLauncherRootFolder);
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
                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonIncludeConverter<InstanceViewModel>());
                
                var json = JsonSerializer.Serialize(instance, options);
                await File.WriteAllTextAsync(Path.Combine(path, "instance.json"), json);
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
        {
            settings.SaveSettings();
        }
    }
    
    private void ExecuteViewInstancesFolder() => DirectoryManager.OpenFolderInExplorer(DirectoryManager.GetInstancesDirectory());

    private void ExecuteViewLauncherRootFolder() => DirectoryManager.OpenFolderInExplorer(AppDomain.CurrentDomain.BaseDirectory);
}