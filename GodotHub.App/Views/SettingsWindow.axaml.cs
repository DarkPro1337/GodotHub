using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GodotHub.App.ViewModels;

namespace GodotHub.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}