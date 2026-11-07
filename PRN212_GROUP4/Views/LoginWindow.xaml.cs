using System.Windows;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.Views;

public partial class LoginWindow : Window
{
    private bool busy;

    public LoginWindow()
    {
        InitializeComponent();
        Closing += (_, args) => args.Cancel = busy;
    }

    private async void Login_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (busy)
        {
            return;
        }

        busy = true;
        LoginButton.IsEnabled = false;
        Message.Text = "Đang đăng nhập...";

        bool success = false;
        var service = new AuthenticationService();

        try
        {
            var outcome = await service.LoginAsync(
                Username.Text,
                Password.Password);

            Password.Clear();

            if (outcome == LoginOutcome.PasswordChangeRequired)
            {
                var dialog = new ChangePasswordWindow(service)
                {
                    Owner = this
                };

                success = dialog.ShowDialog() == true;

                if (!success)
                {
                    Message.Text =
                        "Bạn cần đổi mật khẩu để hoàn tất đăng nhập.";
                }
            }
            else
            {
                success = true;
            }
        }
        catch (Exception ex)
        {
            AppSession.Clear();
            Message.Text = ex.Message;
        }
        finally
        {
            service.CancelPendingChange();
            Password.Clear();

            busy = false;
            LoginButton.IsEnabled = true;
        }

        if (success)
        {
            DialogResult = true;
        }
    }
}