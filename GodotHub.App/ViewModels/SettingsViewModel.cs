using System.Collections.Generic;
using System.Collections.ObjectModel;
using GodotHub.App.Helpers;
using NLog;
using ReactiveUI;

namespace GodotHub.App.ViewModels;

public class Category : ReactiveObject
{
    public string Title { get; set; }
    public string Icon { get; set; }
    public ObservableCollection<SettingsItem> SettingsItems { get; set; }
}

public abstract class SettingsItem : ReactiveObject
{
    public string Label { get; set; }
}

public class CheckBoxSettingsItem : SettingsItem
{
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}

public class TextBoxSettingsItem : SettingsItem
{
    private string _text;
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }
}

public class ComboBoxSettingsItem : SettingsItem
{
    private string _selectedOption;
    public List<string> Options { get; set; }
    public string SelectedOption
    {
        get => _selectedOption;
        set => this.RaiseAndSetIfChanged(ref _selectedOption, value);
    }
}

public class IntegerSettingsItem : SettingsItem
{
    private int _value;
    public int Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }
    public string Unit { get; set; }
}


public class SettingsViewModel : ViewModelBase
{
    private static readonly ILogger _Logger = LoggingHelper.CreateLogger("Settings");
    
    private Category? _selectedCategory;
    public ObservableCollection<Category> Categories { get; }
    public ObservableCollection<SettingsItem>? SettingsItems { get; }

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    public SettingsViewModel()
    {
        Categories = new ObservableCollection<Category>
        {
            new() { Title = "General", Icon = "fa-folder", SettingsItems = new ObservableCollection<SettingsItem>
            {
                new CheckBoxSettingsItem { Label = "Enable Feature", IsChecked = true },
                new TextBoxSettingsItem { Label = "File Path", Text = "C:\\example\\path" },
                new ComboBoxSettingsItem { Label = "Options", Options = new List<string> { "Option1", "Option2" }, SelectedOption = "Option1" },
                new IntegerSettingsItem { Label = "Memory Limit", Value = 1024, Unit = "MiB" }
            }},
            new() { Title = "Advanced", Icon = "fa-cogs", SettingsItems = new ObservableCollection<SettingsItem>
            {
                
            }}
        };
    }

    public void SaveSettings()
    {
        // TODO: Write settings to JSON file
    }
}