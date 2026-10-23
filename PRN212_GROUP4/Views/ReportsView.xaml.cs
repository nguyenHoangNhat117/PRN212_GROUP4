using Microsoft.Win32;
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
using System.IO;

namespace PRN212_GROUP4.Views
{
    public partial class ReportsView : Page
    {
        private readonly ReportsViewModel viewModel;

        private bool isLoading;
        private bool isExporting;

        public ReportsView()
        {
            InitializeComponent();

            viewModel = new ReportsViewModel();
            DataContext = viewModel;

            Loaded += ReportsView_Loaded;
        }

        private async void ReportsView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= ReportsView_Loaded;

            await GenerateReportAsync();
        }

        private async void GenerateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await GenerateReportAsync();
        }

        private async Task GenerateReportAsync()
        {
            if (isLoading || isExporting)
            {
                return;
            }

            isLoading = true;
            FilterPanel.IsEnabled = false;

            ReportStatus.Text = "Loading report...";

            try
            {
                await viewModel.GenerateReportAsync();

                ReportStatus.Text =
                    viewModel.ReportStudents.Count == 0
                        ? "No students match the selected filters."
                        : "Report generated at "
                          + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            }
            catch (Exception ex)
            {
                ReportStatus.Text =
                    "Report failed. Any previous results are unchanged.";

                MessageBox.Show(
                    "Không thể tạo báo cáo sinh viên.\n\n"
                    + ex.Message,
                    "Lỗi báo cáo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                isLoading = false;
                FilterPanel.IsEnabled = true;
            }
        }

        private async void ExportCsvButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isLoading || isExporting)
            {
                return;
            }

            if (viewModel.ReportStudents.Count == 0)
            {
                MessageBox.Show(
                    "Danh sách báo cáo đang trống. "
                    + "Bạn hãy chọn lại bộ lọc và nhấn Generate.",
                    "Chưa có dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Lưu báo cáo sinh viên",
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = "Student_Report_"
                           + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                           + ".csv"
            };

            var owner = Window.GetWindow(this);

            bool? result = owner != null
                ? saveDialog.ShowDialog(owner)
                : saveDialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            isExporting = true;
            FilterPanel.IsEnabled = false;

            ReportStatus.Text = "Exporting report...";

            try
            {
                // Lấy đúng dữ liệu đang hiển thị trong bảng.
                var rows = viewModel.ReportStudents
                    .Select(student => new[]
                    {
                        student.StudentCode,
                        student.FullName,
                        student.DepartmentName ?? "Unassigned",
                        student.Email ?? string.Empty,
                        student.StudentStatus
                    })
                    .ToList();

                var csv = new StringBuilder();

                csv.AppendLine(
                    "StudentCode,FullName,DepartmentName,Email,StudentStatus");

                foreach (var row in rows)
                {
                    csv.AppendLine(
                        string.Join(",", row.Select(ToCsvCell)));
                }

                // UTF-8 có BOM giúp Excel đọc được tiếng Việt.
                await File.WriteAllTextAsync(
                    saveDialog.FileName,
                    csv.ToString(),
                    new UTF8Encoding(true));

                ReportStatus.Text =
                    $"Exported {rows.Count} students at "
                    + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                MessageBox.Show(
                    $"Đã xuất {rows.Count} sinh viên thành công.\n\n"
                    + "File được lưu tại:\n"
                    + saveDialog.FileName,
                    "Xuất báo cáo thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ReportStatus.Text =
                    "Export failed. Please try again.";

                MessageBox.Show(
                    "Không thể xuất file CSV.\n\n"
                    + ex.Message,
                    "Lỗi xuất báo cáo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                isExporting = false;
                FilterPanel.IsEnabled = true;
            }
        }

        private static string ToCsvCell(string? value)
        {
            string text = value ?? string.Empty;

            // Giữ dữ liệu dạng văn bản khi mở bằng Excel.
            string trimmed = text.TrimStart();

            if (trimmed.StartsWith("=")
                || trimmed.StartsWith("+")
                || trimmed.StartsWith("-")
                || trimmed.StartsWith("@")
                || text.StartsWith("\t")
                || text.StartsWith("\r")
                || text.StartsWith("\n"))
            {
                text = "'" + text;
            }

            // Xử lý dấu phẩy, dấu ngoặc kép và xuống dòng.
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }
}
