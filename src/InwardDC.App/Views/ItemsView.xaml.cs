using System.Windows;
using System.Windows.Controls;
using InwardDC.App.ViewModels;

namespace InwardDC.App.Views;

public partial class ItemsView : UserControl
{
    public ItemsView() => InitializeComponent();

    private void OnDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is ItemsViewModel vm && vm.SelectedItem is not null)
            vm.OpenCommand.Execute(vm.SelectedItem);
    }
}
