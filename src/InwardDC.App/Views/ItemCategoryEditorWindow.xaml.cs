using System.Windows;
using InwardDC.App.ViewModels;

namespace InwardDC.App.Views;

public partial class ItemCategoryEditorWindow : Window
{
    public ItemCategoryEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ItemCategoryEditorViewModel vm)
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
