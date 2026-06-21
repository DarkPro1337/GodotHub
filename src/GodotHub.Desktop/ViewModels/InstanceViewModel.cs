using CommunityToolkit.Mvvm.Input;
using GodotHub.Core.Models;
using GodotHub.Desktop.Models;
using GodotHub.Desktop.Services;

namespace GodotHub.Desktop.ViewModels;

public partial class InstanceViewModel(GodotInstanceManifest manifest) : ViewModelBase
{
    public GodotInstanceManifest Manifest { get; } = manifest;

    public string Name => Manifest.Name;

    public string Group => Manifest.Group;

    public string Version => $"{Manifest.Version}-{Manifest.ReleaseName}";

    public string BuildKind => Manifest.BuildKind == GodotBuildKind.DotNet ? ".NET" : "Standard";

    [RelayCommand]
    private void Launch()
    {
        GodotInstanceInstaller.Launch(Manifest);
    }
}
