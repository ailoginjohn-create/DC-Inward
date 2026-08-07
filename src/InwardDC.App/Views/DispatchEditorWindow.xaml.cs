using System.Windows;
using InwardDC.App.ViewModels;

namespace InwardDC.App.Views;

public partial class DispatchEditorWindow : Window
{
    public DispatchEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is DispatchEditorViewModel vm)
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
