using System;
using Avalonia.Controls;
using GodotHub.Desktop.ViewModels;

namespace GodotHub.Desktop.Views;

public partial class CreateInstanceWindow : Window
{
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
            {
                throw new InvalidOperationException(
                    $"{nameof(CreateInstanceWindow)} requires " +
                    $"{nameof(CreateInstanceViewModel)} as its DataContext.");
            }

            await viewModel.InitializeAsync();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }
}