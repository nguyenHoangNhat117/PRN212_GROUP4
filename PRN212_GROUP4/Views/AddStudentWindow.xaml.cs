using PRN212_GROUP4.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class AddStudentWindow : Window
    {
        private readonly AddStudentViewModel viewModel;

        private bool isSaving;
        private bool isClosed;

        public int? CreatedStudentID { get; private set; }

        public AddStudentWindow()
        {
            InitializeComponent();

            viewModel = new AddStudentViewModel();

            DataContext = viewModel;

            Loaded += AddStudentWindow_Loaded;

            Closed += (_, _) => isClosed = true;
        }

        private async void AddStudentWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= AddStudentWindow_Loaded;

            try
            {
                await viewModel.LoadDepartmentsAsync();

                if (isClosed)
                {
                    return;
                }

                SaveButton.IsEnabled = true;
                OperationStatus.Text = string.Empty;
            }
            catch (Exception ex)
            {
                if (isClosed)
                {
                    return;
                }

                OperationStatus.Text = "Could not load departments.";

                MessageBox.Show(
                    this,
                    "Không thể tải danh sách khoa.\n\n" + ex.Message,
                    "Lỗi tải dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isSaving)
            {
                return;
            }

            isSaving = true;

            FormPanel.IsEnabled = false;
            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            OperationStatus.Text = "Saving...";

            try
            {
                CreatedStudentID = await viewModel.SaveAsync();

                // Cho phép đóng sau khi lưu thành công.
                isSaving = false;

                DialogResult = true;
            }
            catch (Exception ex)
            {
                OperationStatus.Text = "Student was not saved.";

                MessageBox.Show(
                    this,
                    ex.Message,
                    "Không thể thêm sinh viên",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                isSaving = false;

                FormPanel.IsEnabled = true;
                SaveButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Tránh đóng form khi đang gửi yêu cầu lưu.
            if (isSaving)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }
    }
}