using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GodotHub.App.ViewModels;

namespace GodotHub.App.Views;

public partial class CreateInstanceWindow : Window
{
    public CreateInstanceWindow(CreateInstanceViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}