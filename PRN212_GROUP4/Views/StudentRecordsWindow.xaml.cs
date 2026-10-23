using PRN212_GROUP4.Models;
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
using System.ComponentModel;

namespace PRN212_GROUP4.Views
{
    public partial class StudentRecordsWindow : Window
    {
        private readonly StudentRecordsViewModel viewModel;

        private bool isLoading;
        private bool isClosed;
        private bool isArchiving;

        public StudentRecordsWindow(Student student)
        {
            InitializeComponent();

            viewModel = new StudentRecordsViewModel(student);

            DataContext = viewModel;

            Loaded += StudentRecordsWindow_Loaded;

            Closed += (_, _) => isClosed = true;
        }

        private async void StudentRecordsWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= StudentRecordsWindow_Loaded;

            await ReloadRecordsAsync();
        }

        private async void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await ReloadRecordsAsync();
        }

        private async void IncludeArchived_Click(
            object sender,
            RoutedEventArgs e)
        {
            await ReloadRecordsAsync();
        }

        private async void AddRecordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isLoading || isClosed)
            {
                return;
            }

            AddStudentRecordWindow window =
                new AddStudentRecordWindow(viewModel.Student)
                {
                    Owner = this
                };

            if (window.ShowDialog() != true)
            {
                return;
            }

            bool loaded = await ReloadRecordsAsync(
                window.CreatedRecordID);

            if (isClosed)
            {
                return;
            }

            if (loaded)
            {
                MessageBox.Show(
                    this,
                    "Thêm hồ sơ học vụ thành công!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                LoadStatus.Text =
                    "Record saved, but refresh failed. "
                    + "Click Refresh; do not add the record again.";
            }
        }

        private async void EditRecordButton_Click(
        object sender,
        RoutedEventArgs e)
        {
            if (isLoading || isClosed)
            {
                return;
            }

            StudentRecord? selectedRecord = viewModel.SelectedRecord;

            if (selectedRecord == null)
            {
                MessageBox.Show(
                    this,
                    "Vui lòng chọn hồ sơ cần sửa.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (selectedRecord.IsArchived)
            {
                MessageBox.Show(
                    this,
                    "Hồ sơ đã lưu trữ chỉ được xem, không được chỉnh sửa.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            int recordID = selectedRecord.RecordID;

            EditStudentRecordWindow window =
                new EditStudentRecordWindow(
                    viewModel.Student,
                    selectedRecord)
                {
                    Owner = this
                };

            if (window.ShowDialog() != true)
            {
                return;
            }

            bool loaded = await ReloadRecordsAsync(recordID);

            if (isClosed)
            {
                return;
            }

            if (loaded)
            {
                MessageBox.Show(
                    this,
                    "Cập nhật hồ sơ học vụ thành công!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                LoadStatus.Text =
                    "Changes saved, but refresh failed. Click Refresh.";
            }
        }

        private async Task<bool> ReloadRecordsAsync(
            int? selectRecordID = null)
        {
            if (isLoading || isClosed)
            {
                return false;
            }

            isLoading = true;

            ToolbarPanel.IsEnabled = false;
            LoadStatus.Text = "Loading records...";

            // Giữ bản ghi đang chọn nếu vẫn còn trong danh sách.
            int? previousRecordID =
                selectRecordID ?? viewModel.SelectedRecord?.RecordID;

            try
            {
                await viewModel.LoadRecordsAsync();

                if (isClosed)
                {
                    return false;
                }

                viewModel.SelectedRecord =
                    viewModel.Records.FirstOrDefault(
                        record => record.RecordID == previousRecordID);

                LoadStatus.Text = viewModel.Records.Count == 0
                    ? "No records found for the current filter."
                    : $"{viewModel.Records.Count} record(s) loaded.";

                return true;
            }
            catch (Exception ex)
            {
                if (isClosed)
                {
                    return false;
                }

                LoadStatus.Text =
                    "Refresh failed. Any displayed records may be outdated.";

                MessageBox.Show(
                    this,
                    "Không thể tải hồ sơ học vụ.\n\n" + ex.Message,
                    "Lỗi tải dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
            finally
            {
                isLoading = false;

                if (!isClosed)
                {
                    ToolbarPanel.IsEnabled = true;
                }
            }
        }

        private async void ArchiveRecordButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (isLoading || isClosed || isArchiving)
            {
                return;
            }

            StudentRecord? record = viewModel.SelectedRecord;

            if (record == null)
            {
                MessageBox.Show(
                    this,
                    "Vui lòng chọn hồ sơ cần lưu trữ.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (record.IsArchived)
            {
                MessageBox.Show(
                    this,
                    "Hồ sơ này đã được lưu trữ.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MessageBoxResult confirmation = MessageBox.Show(
                this,
                "Bạn có muốn lưu trữ hồ sơ sau không?\n\n"
                + record.RecordTitle
                + "\n\nHồ sơ sẽ được giữ trong database "
                + "và không còn cho phép chỉnh sửa.",
                "Xác nhận lưu trữ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            isArchiving = true;
            ToolbarPanel.IsEnabled = false;
            LoadStatus.Text = "Archiving record...";

            try
            {
                await viewModel.ArchiveRecordAsync(record);

                bool loaded = await ReloadRecordsAsync(record.RecordID);

                if (isClosed)
                {
                    return;
                }

                if (loaded)
                {
                    MessageBox.Show(
                        this,
                        "Lưu trữ hồ sơ thành công!\n\n"
                        + "Tích Include archived records để xem lại.",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    LoadStatus.Text =
                        "Record archived, but refresh failed. Click Refresh.";
                }
            }
            catch (Exception ex)
            {
                if (isClosed)
                {
                    return;
                }

                LoadStatus.Text =
                    "Archive was not completed. Refresh to check the latest status.";

                MessageBox.Show(
                    this,
                    ex.Message,
                    "Không thể lưu trữ hồ sơ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                isArchiving = false;

                if (!isClosed)
                {
                    ToolbarPanel.IsEnabled = true;
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (isArchiving)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }

    }
}
