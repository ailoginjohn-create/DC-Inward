using System.Windows;
using System.Windows.Controls;
using InwardDC.App.ViewModels;

namespace InwardDC.App.Views;

public partial class UserEditorWindow : Window
{
    private UserEditorViewModel? _viewModel;

    public UserEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            _viewModel = DataContext as UserEditorViewModel;
            if (_viewModel is not null)
            {
                _viewModel.RequestClose += saved =>
                {
                    DialogResult = saved;
                    Close();
                };
            }
        };
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (ReferenceEquals(sender, PasswordBox))
            _viewModel.SetPassword(PasswordBox.Password);
        else
            _viewModel.SetConfirmPassword(ConfirmPasswordBox.Password);
    }
}
