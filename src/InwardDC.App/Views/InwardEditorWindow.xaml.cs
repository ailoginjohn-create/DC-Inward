using System.Windows;
using InwardDC.App.ViewModels;

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
}
