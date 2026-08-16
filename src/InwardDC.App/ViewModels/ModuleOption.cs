using CommunityToolkit.Mvvm.ComponentModel;

namespace InwardDC.App.ViewModels;

/// <summary>A checkable module row in the user editor's module-access section.</summary>
public partial class ModuleOption : ObservableObject
{
    public ModuleOption(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public string Key { get; }
    public string Label { get; }

    [ObservableProperty]
    private bool _isChecked;
}
