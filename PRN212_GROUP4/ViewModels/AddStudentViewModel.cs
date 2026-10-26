using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.ViewModels
{
    public class AddStudentViewModel : ViewModelBase
    {
        private readonly DatabaseService databaseService;

        private bool isBusy;
        private bool departmentsLoaded;
        private bool isSaved;

        private string statusMessage = "Loading departments...";

        public Student NewStudent { get; }

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

        public int? CreatedStudentID { get; private set; }

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

        public AddStudentViewModel()
        {
            databaseService = new DatabaseService();

            NewStudent = new Student
            {
                StudentStatus = "ACTIVE",
                EnrollmentDate = DateTime.Today
            };

            Dates = new StudentDateFields(NewStudent);

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
                // Kiểm tra nội dung ngày đang nhập trước khi lưu.
                Dates.ValidateAndApply(NewStudent);

                CreatedStudentID =
                    await databaseService.AddStudentAsync(NewStudent);

                isSaved = true;
                StatusMessage = "Student saved.";
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
                : "Không thể thêm sinh viên.\n" + ex.Message;
        }
    }
}