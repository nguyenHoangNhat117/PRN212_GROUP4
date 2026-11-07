using System.Windows;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly AuthenticationService service;
    private readonly bool required;
    private bool busy;

    public ChangePasswordWindow(
        AuthenticationService? requiredChange = null)
    {
        InitializeComponent();

        required = requiredChange != null;
        service = requiredChange ?? new AuthenticationService();

        if (required)
        {
            // Mật khẩu tạm đã được kiểm tra ở bước Login.
            CurrentPasswordPanel.Visibility = Visibility.Collapsed;

            Description.Text =
                "Tài khoản đã được xác thực. "
                + "Bạn cần đặt mật khẩu mới trước khi vào ứng dụng.";

            CancelButton.Content = "Quay lại đăng nhập";
        }

        Closing += (_, args) => args.Cancel = busy;

        Closed += (_, _) =>
        {
            if (required)
            {
                service.CancelPendingChange();
            }
        };
    }

    private async void Save_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (busy)
        {
            return;
        }

        busy = true;
        FormPanel.IsEnabled = false;
        Message.Text = "Đang lưu...";

        bool saved = false;

        try
        {
            if (required)
            {
                await service.CompleteRequiredChangeAsync(
                    NewPassword.Password,
                    Confirmation.Password);
            }
            else
            {
                await service.ChangeOwnPasswordAsync(
                    CurrentPassword.Password,
                    NewPassword.Password,
                    Confirmation.Password);
            }

            saved = true;
        }
        catch (Exception ex)
        {
            Message.Text = ex.Message;
        }
        finally
        {
            CurrentPassword.Clear();
            NewPassword.Clear();
            Confirmation.Clear();

            busy = false;
            FormPanel.IsEnabled = true;
        }

        if (saved)
        {
            MessageBox.Show(
                this,
                "Đã đổi mật khẩu thành công.",
                "Thành công",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
        }
    }

    private void Cancel_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}