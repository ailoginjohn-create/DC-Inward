using System.Windows;
using System.Windows.Controls;
using InwardDC.App.ViewModels;

namespace InwardDC.App.Views;

public partial class VendorsView : UserControl
{
    public VendorsView() => InitializeComponent();

    private void OnDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is VendorsViewModel vm && vm.SelectedItem is not null)
            vm.OpenCommand.Execute(vm.SelectedItem);
    }
}
