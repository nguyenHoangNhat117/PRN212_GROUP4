using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private readonly DatabaseService databaseService;
        private readonly IStudentReportService reportService;

        private int? selectedDepartmentID = 0;
        private string selectedStatus = "ALL";

        private bool isBusy;
        private string statusMessage = "Loading report...";

        public ObservableCollection<Department> Departments { get; }
            = new();

        public ObservableCollection<Student> ReportStudents { get; }
            = new();

        public string[] StatusOptions { get; } =
        {
            "ALL",
            "ACTIVE",
            "INACTIVE",
            "GRADUATED",
            "SUSPENDED"
        };

        public AsyncRelayCommand GenerateCommand { get; }

        public AsyncRelayCommand ExportCsvCommand { get; }

        public int? SelectedDepartmentID
        {
            get => selectedDepartmentID;

            set => SetProperty(ref selectedDepartmentID, value);
        }

        public string SelectedStatus
        {
            get => selectedStatus;

            set => SetProperty(ref selectedStatus, value);
        }

        public bool IsBusy
        {
            get => isBusy;

            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    OnPropertyChanged(nameof(CanInteract));

                    RefreshCommands();
                }
            }
        }

        public bool CanInteract => !IsBusy;

        public string StatusMessage
        {
            get => statusMessage;

            private set => SetProperty(ref statusMessage, value);
        }

        public ReportsViewModel(
            IStudentReportService reportService)
        {
            this.reportService = reportService
                ?? throw new ArgumentNullException(
                    nameof(reportService));

            databaseService = new DatabaseService();

            Departments.Add(new Department
            {
                DepartmentID = 0,
                DepartmentName = "All departments"
            });

            GenerateCommand = new AsyncRelayCommand(
                GenerateReportAsync,
                HandleGenerateError,
                () => !IsBusy);

            ExportCsvCommand = new AsyncRelayCommand(
                ExportCsvAsync,
                HandleExportError,
                () => !IsBusy && ReportStudents.Count > 0);

            ReportStudents.CollectionChanged += (_, _) =>
                ExportCsvCommand.RaiseCanExecuteChanged();
        }

        private void RefreshCommands()
        {
            GenerateCommand.RaiseCanExecuteChanged();
            ExportCsvCommand.RaiseCanExecuteChanged();
        }

        private async Task GenerateReportAsync()
        {
            int departmentID = SelectedDepartmentID ?? 0;
            string status = SelectedStatus;

            IsBusy = true;
            StatusMessage = "Loading report...";

            try
            {
                var students =
                    await databaseService.GetStudentsAsync();

                // Giữ cách lấy danh sách khoa từ các sinh viên hiện có.
                var departments = students
                    .Where(student => student.DepartmentID.HasValue)
                    .GroupBy(student => student.DepartmentID!.Value)
                    .Select(group => new Department
                    {
                        DepartmentID = group.Key,

                        DepartmentName =
                            group.First().DepartmentName
                            ?? $"Department #{group.Key}"
                    })
                    .OrderBy(department => department.DepartmentName)
                    .ToList();

                if (!departments.Any(
                    department =>
                        department.DepartmentID == departmentID))
                {
                    departmentID = 0;
                }

                // Chuẩn bị kết quả trước khi thay danh sách hiển thị.
                var results = students
                    .Where(student =>
                        (departmentID == 0
                            || student.DepartmentID == departmentID)
                        &&
                        (status == "ALL"
                            || student.StudentStatus == status))
                    .ToList();

                Departments.Clear();

                Departments.Add(new Department
                {
                    DepartmentID = 0,
                    DepartmentName = "All departments"
                });

                foreach (var department in departments)
                {
                    Departments.Add(department);
                }

                SelectedDepartmentID = departmentID;

                ReportStudents.Clear();

                foreach (var student in results)
                {
                    ReportStudents.Add(student);
                }

                StatusMessage = ReportStudents.Count == 0
                    ? "No students match the selected filters."
                    : "Report generated at "
                        + DateTime.Now.ToString(
                            "dd/MM/yyyy HH:mm:ss");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportCsvAsync()
        {
            if (ReportStudents.Count == 0)
            {
                return;
            }

            // Xuất danh sách trong bảng, không truy vấn lại database.
            var students = ReportStudents.ToList();

            string previousStatus = StatusMessage;

            IsBusy = true;
            StatusMessage = "Exporting report...";

            try
            {
                string? filePath =
                    await reportService.ExportCsvAsync(students);

                if (filePath == null)
                {
                    // Người dùng hủy chọn nơi lưu.
                    StatusMessage = previousStatus;

                    return;
                }

                StatusMessage =
                    $"Exported {students.Count} students at "
                    + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                reportService.ShowInformation(
                    $"Đã xuất {students.Count} sinh viên thành công."
                    + "\n\nFile được lưu tại:\n"
                    + filePath);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void HandleGenerateError(Exception ex)
        {
            StatusMessage =
                "Không thể tạo báo cáo. "
                + "Dữ liệu đang hiển thị có thể chưa được cập nhật.";

            reportService.ShowWarning(
                "Không thể tạo báo cáo sinh viên.\n\n"
                + ex.Message);
        }

        private void HandleExportError(Exception ex)
        {
            StatusMessage =
                "Xuất báo cáo thất bại. Vui lòng thử lại.";

            reportService.ShowWarning(
                "Không thể xuất file CSV.\n\n"
                + ex.Message);
        }
    }
}