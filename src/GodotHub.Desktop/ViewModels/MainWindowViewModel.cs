using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using GodotHub.Core.Services;
using GodotHub.Desktop.Helpers;
using GodotHub.Desktop.Views;
using NLog;

namespace GodotHub.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly ILogger _logger = LoggingHelper.CreateLogger<MainWindowViewModel>();

    public MainWindowViewModel()
    {
    }

    public async Task InitializeAsync()
    {
        _logger.Info("Initializing");
    }

    [RelayCommand]
    private async Task AddInstance(Window owner)
    {
        _logger.Info("Adding new instance");
        var createInstanceViewModel = new CreateInstanceViewModel();
        await new CreateInstanceWindow(createInstanceViewModel).ShowDialog(owner);
        if (createInstanceViewModel.SelectedRelease != null)
        {
            _logger.Info("Selected release: {0}", createInstanceViewModel.SelectedRelease);
        }
    }

    [RelayCommand]
    private async Task OpenSettings(Window owner)
    {
        _logger.Info("Opening settings");
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
        _logger.Info("Opening about page");
    }

    [RelayCommand]
    private void ViewInstancesFolder()
    {
        _logger.Info("Opening instances folder...");
        DirectoryManager.OpenDirectory(DirectoryManager.GetInstancesDirectory());
    }

    [RelayCommand]
    private void ViewLauncherRootFolder()
    {
        _logger.Info("Opening launcher root folder...");
        DirectoryManager.OpenDirectory(AppDomain.CurrentDomain.BaseDirectory);
    }
}