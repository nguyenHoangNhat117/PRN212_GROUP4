using PRN212_GROUP4.Models;
using PRN212_GROUP4.ViewModels;
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

        public MainWindow()
        {
            InitializeComponent();

            studentViewModel = new StudentViewModel();

            DataContext = studentViewModel;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            // Chỉ tải tự động một lần khi cửa sổ mở.
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

        // Giữ lại hàm kiểm tra kết nối hiện có.
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