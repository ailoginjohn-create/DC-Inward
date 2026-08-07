using System.Windows;
using InwardDC.App.ViewModels;

namespace InwardDC.App.Views;

public partial class ItemEditorWindow : Window
{
    public ItemEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ItemEditorViewModel vm)
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
