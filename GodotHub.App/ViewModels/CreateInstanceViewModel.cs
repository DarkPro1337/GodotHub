using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using DynamicData;
using GodotHub.Lib;
using ReactiveUI;

namespace GodotHub.App.ViewModels;

public class CreateInstanceViewModel : ViewModelBase
{
    private bool _isLoadingReleases;
    private bool _isError;
    private string _errorMessage = "Something went wrong.";
    
    private GodotRelease? _selectedRelease;
    
    public bool IsLoadingReleases
    {
        get => _isLoadingReleases;
        set
        {
            this.RaiseAndSetIfChanged(ref _isLoadingReleases, value);
            this.RaisePropertyChanged(nameof(CanBeSaved));
        }
    }

    public bool IsError
    {
        get => _isError;
        set => this.RaiseAndSetIfChanged(ref _isError, value);
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public GodotRelease? SelectedRelease
    {
        get => _selectedRelease;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedRelease, value);
            this.RaisePropertyChanged(nameof(CanBeSaved));
        }
    }

    public bool CanBeSaved => SelectedRelease != null && !IsLoadingReleases;

    public ObservableCollection<GodotRelease> Releases { get; } = [];
    
    public ReactiveCommand<Window, Unit> SaveCommand { get; }
    public ReactiveCommand<Window, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshReleasesCommand { get; }

    public CreateInstanceViewModel()
    {
        SaveCommand = ReactiveCommand.Create<Window>(ExecuteSave);
        CancelCommand = ReactiveCommand.Create<Window>(ExecuteCancel);
        RefreshReleasesCommand = ReactiveCommand.CreateFromTask(LoadReleases);
    }
    
    public async void InitializeAsync()
    {
        await LoadReleases();
    }

    private void ExecuteSave(Window window) => window.Close(true);

    private void ExecuteCancel(Window window) => window.Close();

    public async Task LoadReleases()
    {
        try
        {
            IsError = false;
            IsLoadingReleases = true;
            var releases = await GodotApi.GetGitHubReleasesAsync();
            Releases.Clear();
            Releases.AddRange(releases);
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException req && req.Message.Contains("rate limit"))
            {
                IsError = true;
                ErrorMessage = "Rate limit exceeded! Please try again later or provide a GitHub token in the settings.";
            }
            else
            {
                IsError = true;
                ErrorMessage = "Something went wrong while fetching the releases. Please try again later.";
            }
        }
        finally
        {
            IsLoadingReleases = false;
        }
    }
}