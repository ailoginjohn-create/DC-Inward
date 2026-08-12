using System.Windows;
using InwardDC.App.ViewModels;

namespace InwardDC.App.Views;

public partial class PurposeEditorWindow : Window
{
    public PurposeEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is PurposeEditorViewModel vm)
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
