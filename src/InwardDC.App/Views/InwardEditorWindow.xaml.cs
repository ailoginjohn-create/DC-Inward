using System.Windows;
using System.Windows.Controls;
using InwardDC.App.ViewModels;
using InwardDC.Application.DTOs;

namespace InwardDC.App.Views;

public partial class InwardEditorWindow : Window
{
    public InwardEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is InwardEditorViewModel vm)
            {
                vm.RequestClose += saved =>
                {
                    DialogResult = saved;
                    Close();
                };
            }
        };
    }

    private void MasterItemCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.DataContext is not InwardLineRow row)
            return;

        if (combo.SelectedItem is not ItemDto item)
            return;

        row.ItemId = item.Id;
        row.ItemName = item.Name;
        combo.Text = item.Name;
        if (string.IsNullOrWhiteSpace(row.ItemMake))
            row.ItemMake = item.Make;
        if (string.IsNullOrWhiteSpace(row.ItemModel))
            row.ItemModel = item.Model;
        if (string.IsNullOrWhiteSpace(row.HsnCode))
            row.HsnCode = item.HsnCode;
        if (string.IsNullOrWhiteSpace(row.Unit) || row.Unit == "Nos")
            row.Unit = item.Unit;
        row.IsSerialTracked = item.IsSerialTracked;
    }

    private void MasterItemCombo_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not ComboBox combo || combo.DataContext is not InwardLineRow row)
            return;

        var selected = combo.SelectedItem as ItemDto;
        var text = combo.Text?.Trim() ?? string.Empty;

        // A master item is still selected and the text matches it: keep the link.
        if (selected is not null
            && (string.Equals(text, selected.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, selected.DisplayName, StringComparison.OrdinalIgnoreCase)))
            return;

        // User typed free text that is not (or no longer is) a master item selection.
        row.ItemId = null;
        row.ItemName = text;
    }
}
