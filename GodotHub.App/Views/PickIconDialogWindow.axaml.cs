﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GodotHub.App.ViewModels;

namespace GodotHub.App.Views;

public partial class PickIconDialogWindow : Window
{
    public PickIconDialogWindow(PickIconDialogViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}