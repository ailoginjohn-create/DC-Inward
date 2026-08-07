using System.Windows;
using InwardDC.App.ViewModels;

namespace InwardDC.App.Views;

public partial class CustomerEditorWindow : Window
{
    public CustomerEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is CustomerEditorViewModel vm)
            {
                vm.RequestClose += saved =>
                {
                    DialogResult = saved;
                    Close();
                };
            }
        };
    }
}
