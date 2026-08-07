using System.Windows;
using System.Windows.Controls;
using InwardDC.App.ViewModels;

namespace InwardDC.App.Views;

public partial class ItemCategoriesView : UserControl
{
    public ItemCategoriesView() => InitializeComponent();

    private void OnDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is ItemCategoriesViewModel vm && vm.SelectedItem is not null)
            vm.OpenCommand.Execute(vm.SelectedItem);
    }
}
