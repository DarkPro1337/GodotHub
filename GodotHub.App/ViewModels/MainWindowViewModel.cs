using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using GodotHub.App.Views;
using ReactiveUI;

namespace GodotHub.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ReactiveCommand<Window, Unit> AddInstanceCommand { get; }
    
    public MainWindowViewModel()
    {
        AddInstanceCommand = ReactiveCommand.CreateFromTask<Window>(ExecuteAddInstance);
    }

    private async Task ExecuteAddInstance(Window window)
    {
        var creator = new CreateInstanceViewModel();
        creator.InitializeAsync();
        var result = await new CreateInstanceWindow(creator).ShowDialog<bool>(window);
        if (result)
        {
            ;
        }
    }
}

