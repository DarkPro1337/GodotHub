using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using DynamicData;
using GodotHub.App.Helpers;
using GodotHub.App.Views;
using GodotHub.Lib;
using NLog;
using ReactiveUI;

namespace GodotHub.App.ViewModels;

public class CreateInstanceViewModel : ViewModelBase
{
    private static readonly ILogger _Logger = LoggingHelper.CreateLogger("InstanceCreator");
    
    private bool _isLoadingReleases;
    private bool _isError;
    private bool _isMono;
    private string _errorMessage = "Something went wrong.";
    private string _nameWatermark = "Name";
    private string _name = "";
    private string _group = "";
    private string _iconPath = DirectoryManager.GetDefaultIconPath();
    
    private ReleaseType _selectedReleaseTypes = ReleaseType.Stable;
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
    
    public bool IsMono
    {
        get => _isMono;
        set => this.RaiseAndSetIfChanged(ref _isMono, value);
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }
    
    public string NameWatermark
    {
        get => _nameWatermark;
        set => this.RaiseAndSetIfChanged(ref _nameWatermark, value);
    }
    
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }
    
    public string Group
    {
        get => _group;
        set => this.RaiseAndSetIfChanged(ref _group, value);
    }
    
    public string IconPath
    {
        get => _iconPath;
        set => this.RaiseAndSetIfChanged(ref _iconPath, value);
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
    
    public ReleaseType SelectedReleaseTypes
    {
        get => _selectedReleaseTypes;
        set => this.RaiseAndSetIfChanged(ref _selectedReleaseTypes, value);
    }

    public bool CanBeSaved => SelectedRelease != null && !IsLoadingReleases;

    public ObservableCollection<GodotRelease> Releases { get; } = [];
    public ObservableCollection<GodotRelease> FilteredReleases { get; } = [];
    
    public ObservableCollection<FilterItem> Filters { get; } =
    [
        new FilterItem { Type = ReleaseType.Stable, Name = "Stable", IsChecked = true },
        new FilterItem { Type = ReleaseType.ReleaseCandidate, Name = "Release Candidate" },
        new FilterItem { Type = ReleaseType.Beta, Name = "Beta" },
        new FilterItem { Type = ReleaseType.Alpha, Name = "Alpha" },
        new FilterItem { Type = ReleaseType.Dev, Name = "Dev" }
    ];
    
    public ReactiveCommand<Window, Unit> SaveCommand { get; }
    public ReactiveCommand<Window, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshReleasesCommand { get; }
    public ReactiveCommand<Window, Unit> OpenIconDialogCommand { get; } 

    public CreateInstanceViewModel()
    {
        SaveCommand = ReactiveCommand.Create<Window>(ExecuteSave);
        CancelCommand = ReactiveCommand.Create<Window>(ExecuteCancel);
        RefreshReleasesCommand = ReactiveCommand.CreateFromTask(LoadReleases);
        OpenIconDialogCommand = ReactiveCommand.CreateFromTask<Window>(ExecuteOpenIconDialog);
        
        foreach (var filter in Filters)
            filter.WhenAnyValue(f => f.IsChecked).Subscribe(_ => FilterReleases());
        
        FilterReleases();
    }

    public async void InitializeAsync() => await LoadReleases();

    private void ExecuteSave(Window window) => window.Close(true);

    private void ExecuteCancel(Window window) => window.Close();

    private async Task LoadReleases()
    {
        try
        {
            _Logger.Trace("Fetching releases...");
            IsError = false;
            IsLoadingReleases = true;
            var releases = await GodotApi.GetGitHubReleasesAsync();
            Releases.Clear();
            Releases.AddRange(releases);
            FilterReleases();
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException req && req.Message.Contains("rate limit"))
            {
                IsError = true;
                ErrorMessage = "Rate limit exceeded! Please try again later or provide a GitHub token in the settings.";
                _Logger.Error(ex, "Rate limit exceeded while fetching releases");
            }
            else
            {
                IsError = true;
                ErrorMessage = "Something went wrong while fetching the releases. Please try again later.";
                _Logger.Error(ex, "Error while fetching releases");
            }
        }
        finally
        {
            IsLoadingReleases = false;
        }
    }
    
    private void FilterReleases()
    {
        FilteredReleases.Clear();
        var selectedTypes = Filters.Where(f => f.IsChecked).Aggregate(ReleaseType.None, (current, filter) => current | filter.Type);
        
        foreach (var release in Releases)
        {
            if ((selectedTypes & release.Type) != 0) 
                FilteredReleases.Add(release);
        }
    }
    
    private async Task ExecuteOpenIconDialog(Window window)
    {
        var iconPicker = new PickIconDialogViewModel();
        iconPicker.Initialize();
        var iconPickerWindow = new PickIconDialogWindow(iconPicker);
        var result = await iconPickerWindow.ShowDialog<bool>(window);
        if (result) 
            IconPath = iconPicker.SelectedIcon?.Path ?? "";
    }
}

public class FilterItem : ReactiveObject
{
    private bool _isChecked;

    public ReleaseType Type { get; set; }
    public string Name { get; set; }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}