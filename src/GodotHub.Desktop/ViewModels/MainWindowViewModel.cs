using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GodotHub.Core.Models;
using GodotHub.Core.Services;
using GodotHub.Desktop.Helpers;
using GodotHub.Desktop.Services;
using GodotHub.Desktop.Views;
using NLog;

namespace GodotHub.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly ILogger _logger = LoggingHelper.CreateLogger<MainWindowViewModel>();
    private static readonly HttpClient _httpClient = new();

    private readonly GodotInstanceInstaller _instanceInstaller;

    [ObservableProperty]
    private bool _isInstallingInstance;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<InstanceViewModel> Instances { get; } = [];

    public MainWindowViewModel()
    {
        var releaseProvider = new GodotReleaseProvider(_httpClient);
        _instanceInstaller = new GodotInstanceInstaller(_httpClient, releaseProvider);
    }

    public async Task InitializeAsync()
    {
        _logger.Info("Initializing");
        LoadInstances();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task AddInstance(Window owner)
    {
        _logger.Debug("Adding new instance");
        var createInstanceViewModel = new CreateInstanceViewModel();
        await new CreateInstanceWindow(createInstanceViewModel).ShowDialog(owner);
        if (createInstanceViewModel.SelectedRelease != null)
        {
            _logger.Info(
                "Selected release: {Release} for instance {Name} with Mono: {IsMono}",
                createInstanceViewModel.SelectedRelease,
                createInstanceViewModel.Name,
                createInstanceViewModel.IsMono);

            await InstallInstanceAsync(createInstanceViewModel);
        }
    }

    private async Task InstallInstanceAsync(CreateInstanceViewModel createInstanceViewModel)
    {
        if (createInstanceViewModel.SelectedRelease is not { } release)
            return;

        try
        {
            IsInstallingInstance = true;
            StatusMessage = $"Installing {createInstanceViewModel.Name}...";

            var buildKind = createInstanceViewModel.IsMono ? GodotBuildKind.DotNet : GodotBuildKind.Standard;
            var manifest = await _instanceInstaller.InstallAsync(
                createInstanceViewModel.Name,
                createInstanceViewModel.Group,
                release,
                buildKind);

            LoadInstances();
            StatusMessage = $"Installed {manifest.Name}.";
        }
        catch (Exception exception)
        {
            StatusMessage = "Failed to install instance. Check the logs for details.";
            _logger.Error(exception, "Failed to install instance {Name}", createInstanceViewModel.Name);
        }
        finally
        {
            IsInstallingInstance = false;
        }
    }

    private void LoadInstances()
    {
        Instances.Clear();

        foreach (var instanceDirectory in Directory.EnumerateDirectories(DirectoryManager.GetInstancesDirectory()))
        {
            var manifest = GodotInstanceInstaller.TryLoadManifest(instanceDirectory);
            if (manifest is not null)
                Instances.Add(new InstanceViewModel(manifest));
        }
    }

    [RelayCommand]
    private async Task OpenSettings(Window owner)
    {
        _logger.Info("Opening settings");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void ClearCache()
    {
        _logger.Debug("Attempt to clearing cache");
    }

    [RelayCommand]
    private void ReportBug()
    {
        _logger.Debug("Opening bug report page");
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = "https://github.com/DarkPro1337/GodotHub/issues/new"
        });
    }

    [RelayCommand]
    private async Task OpenAbout(Window owner)
    {
        _logger.Debug("Opening about page");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void ViewInstancesFolder()
    {
        _logger.Debug("Opening instances folder...");
        DirectoryManager.OpenDirectory(DirectoryManager.GetInstancesDirectory());
    }

    [RelayCommand]
    private void ViewLauncherRootFolder()
    {
        _logger.Debug("Opening launcher root folder...");
        if (!DirectoryManager.OpenDirectory(AppDomain.CurrentDomain.BaseDirectory))
            _logger.Error("Failed to open launcher root folder");
    }
}
