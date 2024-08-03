using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using GodotHub.App.Helpers;
using NLog;
using ReactiveUI;

namespace GodotHub.App.ViewModels;

public class Icon
{
    public string? Name { get; set; }
    public string? Path { get; set; }
}

public class PickIconDialogViewModel : ViewModelBase
{
    private static readonly ILogger _Logger = LoggingHelper.CreateLogger("Icon Picker");
    private const string _supportedExtensions = "*.jpg,*.gif,*.png,*.bmp,*.jpe,*.jpeg,*.ico";
    private Icon? _selectedIcon;
    
    public Icon? SelectedIcon
    {
        get => _selectedIcon;
        set => this.RaiseAndSetIfChanged(ref _selectedIcon, value);
    }
    
    public ObservableCollection<Icon> Icons { get; } = [];
    
    public ReactiveCommand<Window, Unit> SelectIconCommand { get; }
    public ReactiveCommand<Window, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenIconsFolderCommand { get; }

    public PickIconDialogViewModel()
    {
        SelectIconCommand = ReactiveCommand.Create<Window>(ExecuteSelectIcon);
        CancelCommand = ReactiveCommand.Create<Window>(ExecuteCancel);
        OpenIconsFolderCommand = ReactiveCommand.Create(ExecuteOpenIconsFolder);
        
        CollectIcons();
        SelectedIcon = Icons.FirstOrDefault();
    }

    private void ExecuteSelectIcon(Window window) => window.Close(true);

    private void ExecuteCancel(Window window) => window.Close();

    private void ExecuteOpenIconsFolder() => 
        DirectoryManager.OpenFolderInExplorer(DirectoryManager.GetIconsDirectory());

    private IEnumerable<string> GetImageFiles()
    {
        var dir = DirectoryManager.GetIconsDirectory();
        var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
        return files.Where(file => _supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
    }

    private void CollectIcons()
    {
        foreach (var imageFile in GetImageFiles()) 
            Icons.Add(new Icon { Name = Path.GetFileNameWithoutExtension(imageFile), Path = imageFile });
        
        _Logger.Trace("Collected {0} icons", Icons.Count);
    }
}