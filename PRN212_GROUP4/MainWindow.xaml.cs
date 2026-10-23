using PRN212_GROUP4.Models;
using PRN212_GROUP4.ViewModels;
using PRN212_GROUP4.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PRN212_GROUP4
{
    public partial class MainWindow : Window
    {
        private readonly StudentViewModel studentViewModel;

        private readonly StudentView studentPage;

        private ReportsView? reportsPage;

        private readonly Brush accentBrush =
            new SolidColorBrush(Color.FromRgb(194, 65, 12));

        private readonly Brush navigationTextBrush =
            new SolidColorBrush(Color.FromRgb(209, 204, 198));

        public MainWindow()
        {
            InitializeComponent();

            studentViewModel = new StudentViewModel();

            DataContext = studentViewModel;

            studentPage = new StudentView(studentViewModel);

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            try
            {
                await studentViewModel.LoadStudentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh sách sinh viên.\n\n"
                    + ex.Message,
                    "Lỗi tải dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainFrame.Visibility = Visibility.Collapsed;

            DashboardContent.Visibility = Visibility.Visible;

            PageTitle.Text = "Dashboard";
            PageSubtitle.Text = "Student Management / Overview";

            UpdateNavigation("dashboard");
        }

        private void StudentsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DashboardContent.Visibility = Visibility.Collapsed;

            MainFrame.Visibility = Visibility.Visible;
            MainFrame.Content = studentPage;

            PageTitle.Text = "Students";
            PageSubtitle.Text = "Student Management / Student Profiles";

            UpdateNavigation("students");
        }

        private void ReportsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            reportsPage ??= new ReportsView();

            DashboardContent.Visibility = Visibility.Collapsed;

            MainFrame.Visibility = Visibility.Visible;
            MainFrame.Content = reportsPage;

            PageTitle.Text = "Reports";
            PageSubtitle.Text = "Student Management / Academic Reports";

            UpdateNavigation("reports");
        }

        private void UpdateNavigation(string activePage)
        {
            SetNavigationButton(
                DashboardButton,
                activePage == "dashboard");

            SetNavigationButton(
                StudentsButton,
                activePage == "students");

            SetNavigationButton(
                ReportsButton,
                activePage == "reports");
        }

        private void SetNavigationButton(
            Button button,
            bool isActive)
        {
            button.Background = isActive
                ? accentBrush
                : Brushes.Transparent;

            button.Foreground = isActive
                ? Brushes.White
                : navigationTextBrush;
        }

        private void BtnTestConnection_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                DatabaseService databaseService =
                    new DatabaseService();

                if (databaseService.TestConnection())
                {
                    MessageBox.Show(
                        "Kết nối SQL Server thành công!",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể kết nối SQL Server.\n\n"
                    + ex.Message,
                    "Lỗi kết nối",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}