using System.Windows;
using System.Windows.Controls;
using TelegramChannelDownloader.Desktop.ViewModels;

namespace TelegramChannelDownloader.Desktop.Views;

/// <summary>
/// Interaction logic for AuthenticationView.xaml
/// </summary>
public partial class AuthenticationView : UserControl
{
    public AuthenticationView()
    {
        InitializeComponent();
    }

    private void TwoFactorPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AuthenticationViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.TwoFactorCode = passwordBox.Password;
        }
    }
}