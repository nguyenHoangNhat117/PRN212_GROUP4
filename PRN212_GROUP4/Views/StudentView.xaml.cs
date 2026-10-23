using PRN212_GROUP4.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PRN212_GROUP4.Views
{
    public partial class StudentView : Page
    {
        public StudentView()
        {
            InitializeComponent();
        }

        public StudentView(StudentViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }

        private async void AddStudentButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not StudentViewModel viewModel)
            {
                return;
            }

            AddStudentWindow window = new AddStudentWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() != true)
            {
                return;
            }

            await ReloadAfterSaveAsync(
                viewModel,
                window.CreatedStudentID,
                "Thêm sinh viên thành công!");
        }

        private async void EditStudentButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not StudentViewModel viewModel)
            {
                return;
            }

            if (viewModel.SelectedStudent == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn sinh viên cần sửa.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            int studentID = viewModel.SelectedStudent.StudentID;

            EditStudentWindow window =
                new EditStudentWindow(viewModel.SelectedStudent)
                {
                    Owner = Window.GetWindow(this)
                };

            if (window.ShowDialog() != true)
            {
                return;
            }

            await ReloadAfterSaveAsync(
                viewModel,
                studentID,
                "Cập nhật hồ sơ thành công!");
        }

        private async Task ReloadAfterSaveAsync(
            StudentViewModel viewModel,
            int? studentID,
            string successMessage)
        {
            try
            {
                await viewModel.LoadStudentsAsync();

                viewModel.SelectedStudent =
                    viewModel.Students.FirstOrDefault(
                        student => student.StudentID == studentID);

                MessageBox.Show(
                    successMessage,
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Dữ liệu đã được lưu vào database, "
                    + "nhưng chưa tải lại được danh sách.\n\n"
                    + "Hãy mở lại ứng dụng để tải dữ liệu mới.\n\n"
                    + ex.Message,
                    "Lỗi tải lại danh sách",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void RecordsButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (DataContext is not StudentViewModel viewModel)
            {
                return;
            }

            if (viewModel.SelectedStudent == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn sinh viên cần xem hồ sơ học vụ.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            StudentRecordsWindow window =
                new StudentRecordsWindow(viewModel.SelectedStudent)
                {
                    Owner = Window.GetWindow(this)
                };

            window.ShowDialog();
        }
    }
}