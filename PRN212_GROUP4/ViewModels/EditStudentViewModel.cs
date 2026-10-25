using System;
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
        private string statusMessage = string.Empty;

        public Student EditedStudent { get; }

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

        public bool IsFormEnabled => !IsBusy && !isSaved;

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

            // Chỉnh sửa trên bản sao.
            // Cancel không làm thay đổi sinh viên đang hiển thị.
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

            SaveCommand = new AsyncRelayCommand(
                SaveAsync,
                HandleSaveError,
                () => !IsBusy && !isSaved);
        }

        private void RefreshState()
        {
            OnPropertyChanged(nameof(IsFormEnabled));
            OnPropertyChanged(nameof(CanClose));

            SaveCommand.RaiseCanExecuteChanged();
        }

        private async Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "Saving...";

            try
            {
                // Giữ nguyên kiểm tra RowVer trong DatabaseService.
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