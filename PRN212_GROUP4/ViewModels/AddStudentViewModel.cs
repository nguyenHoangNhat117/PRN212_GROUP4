using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.ViewModels;

public class AddStudentViewModel : StudentProfileViewModel
{
    private bool isBusy;
    private bool departmentsLoaded;
    private bool isSaved;

    private string statusMessage =
        "Đang tải khoa và ngành học...";

    public Student NewStudent { get; }

    public StudentDateFields Dates { get; }

    public ObservableCollection<Department> Departments { get; }
        = new();

    public ObservableCollection<Lookup> Majors { get; }
        = new();

    public string[] Genders { get; } =
    {
        "Nam", "Nữ", "Khác"
    };

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
        NewStudent = new Student
        {
            StudentStatus = "ACTIVE",
            EnrollmentDate = DateTime.Today,
            Cohort = $"K{DateTime.Today.Year}"
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
        StatusMessage = "Đang tải khoa và ngành học...";

        try
        {
            var departments = await DepartmentsAsync();

            if (departments.Count != 1)
            {
                throw new InvalidOperationException(
                    "Không xác định được khoa đang phụ trách. "
                    + "Kiểm tra tài khoản nhân viên học vụ.");
            }

            var department = departments[0];

            var majors = await MajorsAsync(
                department.DepartmentID);

            if (majors.Count == 0)
            {
                throw new InvalidOperationException(
                    "Khoa chưa có ngành học đang hoạt động.");
            }

            Departments.Clear();

            foreach (var item in departments)
            {
                Departments.Add(item);
            }

            Majors.Clear();

            foreach (var item in majors)
            {
                Majors.Add(item);
            }

            NewStudent.DepartmentID = department.DepartmentID;
            NewStudent.DepartmentName = department.DepartmentName;
            NewStudent.MajorID = null;

            OnPropertyChanged(nameof(NewStudent));

            departmentsLoaded = true;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            departmentsLoaded = false;

            StatusMessage =
                "Không thể tải khoa/ngành học. "
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
        StatusMessage = "Đang lưu hồ sơ...";

        try
        {
            Dates.ValidateAndApply(NewStudent);

            CreatedStudentID = await AddAsync(NewStudent);

            isSaved = true;
            StatusMessage = "Đã lưu hồ sơ sinh viên.";
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
            ? "Hồ sơ đã được lưu. Hãy đóng form.\n" + ex.Message
            : "Không thể thêm sinh viên.\n" + ex.Message;
    }
}