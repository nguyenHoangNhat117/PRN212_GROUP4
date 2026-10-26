using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.ViewModels
{
    public class EditStudentViewModel : ViewModelBase
    {
        private readonly DatabaseService databaseService;

        private bool isBusy;
        private bool isSaved;
        private bool departmentsLoaded;

        private string statusMessage = "Loading departments...";

        public Student EditedStudent { get; }

        public StudentDateFields Dates { get; }

        public ObservableCollection<Department> Departments { get; }
            = new();

        public string[] StudentStatuses { get; } =
        {
            "ACTIVE",
            "INACTIVE",
            "GRADUATED",
            "SUSPENDED"
        };

        public AsyncRelayCommand SaveCommand { get; }

        public event EventHandler? SaveSucceeded;

        public bool IsBusy
        {
            get => isBusy;

            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    RefreshState();
                }
            }
        }

        public bool IsFormEnabled =>
            !IsBusy && departmentsLoaded && !isSaved;

        public bool CanClose => !IsBusy;

        public string StatusMessage
        {
            get => statusMessage;

            private set => SetProperty(ref statusMessage, value);
        }

        public EditStudentViewModel(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            databaseService = new DatabaseService();

            // Sửa trên bản sao, Cancel không thay đổi dữ liệu gốc.
            EditedStudent = new Student
            {
                StudentID = student.StudentID,
                StudentCode = student.StudentCode,
                UserID = student.UserID,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Email = student.Email,
                Phone = student.Phone,
                DepartmentID = student.DepartmentID,
                DepartmentName = student.DepartmentName,
                EnrollmentDate = student.EnrollmentDate,
                StudentStatus = student.StudentStatus,
                CreatedAt = student.CreatedAt,

                RowVer = student.RowVer == null
                    ? Array.Empty<byte>()
                    : (byte[])student.RowVer.Clone()
            };

            Dates = new StudentDateFields(EditedStudent);

            SaveCommand = new AsyncRelayCommand(
                SaveAsync,
                HandleSaveError,
                () => !IsBusy && departmentsLoaded && !isSaved);
        }

        private void RefreshState()
        {
            OnPropertyChanged(nameof(IsFormEnabled));
            OnPropertyChanged(nameof(CanClose));

            SaveCommand.RaiseCanExecuteChanged();
        }

        public async Task LoadDepartmentsAsync()
        {
            if (IsBusy || departmentsLoaded || isSaved)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = "Loading departments...";

            try
            {
                var departments =
                    await databaseService.GetDepartmentsAsync();

                Departments.Clear();

                foreach (var department in departments)
                {
                    Departments.Add(department);
                }

                // Nếu khoa hiện tại đã ngừng hoạt động,
                // vẫn hiển thị để người dùng biết khoa đang lưu.
                if (EditedStudent.DepartmentID.HasValue
                    && !Departments.Any(department =>
                        department.DepartmentID
                        == EditedStudent.DepartmentID.Value))
                {
                    Departments.Add(new Department
                    {
                        DepartmentID =
                            EditedStudent.DepartmentID.Value,

                        DepartmentName =
                            EditedStudent.DepartmentName
                            ?? "Khoa hiện tại",

                        IsActive = false
                    });
                }

                departmentsLoaded = true;
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage =
                    "Không thể tải danh sách khoa. "
                    + "Hãy đóng form và mở lại.\n"
                    + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "Saving...";

            try
            {
                Dates.ValidateAndApply(EditedStudent);

                await databaseService.UpdateStudentAsync(
                    EditedStudent);

                isSaved = true;
                StatusMessage = "Changes saved.";
            }
            finally
            {
                IsBusy = false;
            }

            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }

        private void HandleSaveError(Exception ex)
        {
            StatusMessage = isSaved
                ? "Dữ liệu đã được lưu. Hãy đóng form.\n" + ex.Message
                : "Không thể cập nhật sinh viên.\n" + ex.Message;
        }
    }
}