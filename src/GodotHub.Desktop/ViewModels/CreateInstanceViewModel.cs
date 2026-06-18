using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GodotHub.Core.Contracts;
using GodotHub.Core.Models;
using GodotHub.Core.Services;
using GodotHub.Desktop.Helpers;
using NLog;

namespace GodotHub.Desktop.ViewModels;

public partial class CreateInstanceViewModel : ViewModelBase
{
    private static readonly ILogger _logger = LoggingHelper.CreateLogger<CreateInstanceViewModel>();
    private static readonly HttpClient _httpClient = new();
    private readonly IGodotReleaseProvider _releaseProvider = new GodotReleaseProvider(_httpClient);
    private int _filterRequestId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBeSaved))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isLoadingReleases;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isMono;

    [ObservableProperty]
    private string _errorMessage = "Something went wrong.";

    [ObservableProperty]
    private string _nameWatermark = "Name";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _group = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBeSaved))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private GodotRelease? _selectedRelease;

    public bool CanBeSaved =>
        SelectedRelease is not null &&
        !IsLoadingReleases;

    public ObservableCollection<GodotRelease> Releases { get; } = [];

    public ObservableCollection<GodotRelease> FilteredReleases { get; } = [];

    public ObservableCollection<FilterItem> Filters { get; } =
    [
        new() { Type = GodotReleaseChannel.Stable, Name = "Stable", IsChecked = true },
        new() { Type = GodotReleaseChannel.ReleaseCandidate, Name = "RC" },
        new() { Type = GodotReleaseChannel.Beta, Name = "Beta" },
        new() { Type = GodotReleaseChannel.Alpha, Name = "Alpha" },
        new() { Type = GodotReleaseChannel.Dev, Name = "Dev" }
    ];

    public CreateInstanceViewModel()
    {
        foreach (var filter in Filters)
            filter.PropertyChanged += OnFilterPropertyChanged;

        _ = FilterReleasesAsync();
    }

    public Task InitializeAsync()
    {
        return LoadReleasesAsync();
    }

    partial void OnIsMonoChanged(bool value)
    {
        _ = FilterReleasesAsync();
    }

    private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilterItem.IsChecked))
            _ = FilterReleasesAsync();
    }

    [RelayCommand(CanExecute = nameof(CanBeSaved))]
    private static void Save(Window owner) => owner.Close(true);

    [RelayCommand]
    private static void Cancel(Window owner) => owner.Close();

    [RelayCommand]
    private Task RefreshReleasesAsync() => LoadReleasesAsync();

    private async Task LoadReleasesAsync()
    {
        try
        {
            _logger.Trace("Fetching releases");

            IsError = false;
            IsLoadingReleases = true;

            var releases = await _releaseProvider.GetReleasesAsync();
            _logger.Trace("Fetched {0} releases", releases.Count);

            Releases.Clear();

            foreach (var release in releases)
                Releases.Add(release);

            await FilterReleasesAsync();
        }
        catch (Exception exception)
        {
            IsError = true;
            ErrorMessage = "Something went wrong while fetching the releases. Please try again later.";
            _logger.Error(exception, "Error while fetching releases");
        }
        finally
        {
            IsLoadingReleases = false;
        }
    }

    private async Task FilterReleasesAsync()
    {
        var requestId = ++_filterRequestId;
        var selectedChannels = Filters
            .Where(filter => filter.IsChecked)
            .Select(filter => filter.Type)
            .ToHashSet();

        var buildKind = IsMono ? GodotBuildKind.DotNet : GodotBuildKind.Standard;
        var filteredReleases = new List<GodotRelease>();

        foreach (var release in Releases)
        {
            if (!selectedChannels.Contains(release.Channel))
                continue;

            if (buildKind == GodotBuildKind.DotNet)
            {
                var downloads = await _releaseProvider.GetDownloadsAsync(release, buildKind);
                if (downloads.Count == 0)
                    continue;
            }

            filteredReleases.Add(release);
        }

        if (requestId != _filterRequestId)
            return;

        FilteredReleases.Clear();
        foreach (var release in filteredReleases)
            FilteredReleases.Add(release);
    }
}

public sealed partial class FilterItem : ObservableObject
{
    [ObservableProperty]
    private bool _isChecked;

    public GodotReleaseChannel Type { get; init; }

    public string Name { get; init; } = string.Empty;
}
