using System.Windows;
using PRN212_GROUP4.Services;
using PRN212_GROUP4.ViewModels;
using PRN212_GROUP4.Views;
using PRN212_GROUP4.Extensions.Views;
using System.Windows;
using PRN212_GROUP4.Services;
using PRN212_GROUP4.ViewModels;
using PRN212_GROUP4.Views;

namespace PRN212_GROUP4;

public partial class MainWindow : Window
{
    private bool loggingOut;

    public MainWindow()
    {
        InitializeComponent();

        var access = AppSession.DisplayAccess
            ?? throw new UnauthorizedAccessException(
                "Vui lòng đăng nhập.");

        Identity.Text = access.DisplayName;

        RoleLabel.Text =
            access.StaffDepartmentId != null ? "NHÂN VIÊN HỌC VỤ" :
            access.LecturerId != null ? "GIẢNG VIÊN" :
            access.StudentId != null ? "SINH VIÊN" :
            "TÀI KHOẢN";

        ScopeLabel.Text =
            access.StaffDepartmentId != null
                ? "Quản lý dữ liệu trong phạm vi khoa được cấp."
                : access.LecturerId != null
                    ? "Thông tin các lớp được phân công giảng dạy."
                    : "Không gian cá nhân · Chỉ hiển thị hồ sơ "
                      + "và đăng ký của bạn.";

        StudentsButton.Visibility = access.Can("F03")
            ? Visibility.Visible
            : Visibility.Collapsed;

        EnrollmentButton.Visibility = access.Can("F09")
            ? Visibility.Visible
            : Visibility.Collapsed;

        GradesButton.Visibility =
            access.Can("F11") || access.Can("F12") || access.Can("F13")
                ? Visibility.Visible
                : Visibility.Collapsed;

        GpaButton.Visibility = access.Can("F14")
            ? Visibility.Visible
            : Visibility.Collapsed;

        ProgressButton.Visibility = access.Can("F15")
            ? Visibility.Visible
            : Visibility.Collapsed;

        ReportsButton.Visibility = access.Can("F16")
            ? Visibility.Visible
            : Visibility.Collapsed;

        Closed += (_, _) =>
        {
            MainFrame.Content = null;
            AppSession.Clear();

            if (!loggingOut)
            {
                Application.Current.Shutdown();
            }
        };

        Loaded += (_, _) =>
        {
            if (access.Can("F03"))
            {
                Students_Click(this, new RoutedEventArgs());
            }
            else if (access.Can("F09"))
            {
                Enrollment_Click(this, new RoutedEventArgs());
            }
            else if (access.Can("F16"))
            {
                Reports_Click(this, new RoutedEventArgs());
            }
            else if (access.Can("F11") || access.Can("F12")
                || access.Can("F13"))
            {
                Grades_Click(this, new RoutedEventArgs());
            }
            else if (access.Can("F14"))
            {
                Gpa_Click(this, new RoutedEventArgs());
            }
            else if (access.Can("F15"))
            {
                Progress_Click(this, new RoutedEventArgs());
            }
            else
            {
                PageHeading.Text = "Tài khoản";
                ScopeLabel.Text =
                    "Tài khoản chưa được cấp chức năng trong module này.";
            }
        };
    }

    private void Navigate(
        string title,
        System.Windows.Controls.Button active)
    {
        PageHeading.Text = title;

        foreach (var button in new[]
        {
            StudentsButton,
            EnrollmentButton,
            GradesButton,
            GpaButton,
            ProgressButton,
            ReportsButton
        })
        {
            button.Tag = button == active ? "active" : "";
        }
    }

    private async void Students_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate("Hồ sơ sinh viên", StudentsButton);

        var vm = new StudentViewModel();
        MainFrame.Content = new StudentView(vm);

        try
        {
            await vm.LoadStudentsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Không tải được hồ sơ");
        }
    }

    private void Enrollment_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate("Đăng ký môn học", EnrollmentButton);
        MainFrame.Content = new EnrollmentView();
    }

    private void Grades_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate("Điểm số", GradesButton);
        MainFrame.Content = new GradesView();
    }

    private void Gpa_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate("GPA", GpaButton);
        MainFrame.Content = new GpaView();
    }

    private void Progress_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate("Tiến độ học tập", ProgressButton);
        MainFrame.Content = new ProgressView();
    }

    private void Reports_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate("Báo cáo học tập", ReportsButton);
        MainFrame.Content = new ReportsView();
    }

    private void Logout_Click(
        object sender,
        RoutedEventArgs e)
    {
        loggingOut = true;
        Close();

        ((App)Application.Current).ShowLogin();
    }

    private void ChangePassword_Click(
    object sender,
    RoutedEventArgs e)
    {
        var dialog = new ChangePasswordWindow
        {
            Owner = this
        };

        dialog.ShowDialog();
    }
    private void AdditionalModules_Click(
    object sender,
    RoutedEventArgs e)
    {
        var window = new ExtendedModulesWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        window.ShowDialog();
    }
}