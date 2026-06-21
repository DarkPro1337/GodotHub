using System;
using Avalonia.Controls;
using GodotHub.Desktop.Helpers;
using GodotHub.Desktop.ViewModels;
using NLog;

namespace GodotHub.Desktop.Views;

public partial class CreateInstanceWindow : Window
{
    private static readonly ILogger _logger = LoggingHelper.CreateLogger<MainWindow>();

    public CreateInstanceWindow()
    {
        InitializeComponent();
    }

    public CreateInstanceWindow(CreateInstanceViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    protected override async void OnOpened(EventArgs e)
    {
        try
        {
            base.OnOpened(e);

            if (DataContext is not CreateInstanceViewModel viewModel)
                return;

            await viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize create instance window");
        }
    }
}