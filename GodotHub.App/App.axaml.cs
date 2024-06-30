using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GodotHub.App.Helpers;
using GodotHub.App.ViewModels;
using GodotHub.App.Views;
using NLog;

namespace GodotHub.App;

public partial class App : Application
{
    private static readonly ILogger _Logger = LoggingHelper.CreateLogger("GodotHub");
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _Logger.Info("Application initialized.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}