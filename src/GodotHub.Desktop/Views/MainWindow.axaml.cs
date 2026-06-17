using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using GodotHub.Desktop.Helpers;
using GodotHub.Desktop.ViewModels;
using NLog;

namespace GodotHub.Desktop.Views;

public partial class MainWindow : Window
{
    private static readonly ILogger _logger = LoggingHelper.CreateLogger<MainWindow>();

    public ObservableCollection<InstanceViewModel> Instances { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        try
        {
            base.OnOpened(e);

            if (DataContext is not MainWindowViewModel viewModel)
                return;

            await viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize main window");
        }
    }
}