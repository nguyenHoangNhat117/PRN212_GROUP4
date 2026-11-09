using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.ViewModels;

public class EditStudentViewModel : StudentProfileViewModel
{
    private bool isBusy;
    private bool isSaved;
    private bool ready;

    private string statusMessage = "Đang tải dữ liệu...";

    public Student EditedStudent { get; }

    public StudentDateFields Dates { get; }

    public ObservableCollection<Lookup> Majors { get; } = new();

    public string[] Genders { get; } =
    {
        "Nam", "Nữ", "Khác"
    };

    public string[] StudentStatuses { get; } =
    {
        "ACTIVE", "INACTIVE", "GRADUATED", "SUSPENDED"
    };

    public AsyncRelayCommand SaveCommand { get; }

    public bool CanEditAcademicFields =>
        AppSession.DisplayAccess?.CanStaff("F03") == true;

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
        !IsBusy && ready && !isSaved;

    public bool CanClose => !IsBusy;

    public string StatusMessage
    {
        get => statusMessage;

        private set => SetProperty(ref statusMessage, value);
    }

    public EditStudentViewModel(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);

        EditedStudent = new Student
        {
            StudentID = student.StudentID,
            StudentCode = student.StudentCode,
            UserID = student.UserID,
            FullName = student.FullName,

            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Email = student.Email,
            Phone = student.Phone,
            Address = student.Address,

            DepartmentID = student.DepartmentID,
            DepartmentName = student.DepartmentName,

            MajorID = student.MajorID,
            MajorCode = student.MajorCode,
            MajorName = student.MajorName,

            Cohort = student.Cohort,
            EnrollmentDate = student.EnrollmentDate,
            StudentStatus = student.StudentStatus,

            EmergencyContactName = student.EmergencyContactName,
            EmergencyContactPhone = student.EmergencyContactPhone,

            CreatedAt = student.CreatedAt,

            RowVer = student.RowVer?.ToArray()
                ?? Array.Empty<byte>()
        };

        Dates = new StudentDateFields(EditedStudent);

        SaveCommand = new AsyncRelayCommand(
            SaveAsync,
            HandleSaveError,
            () => !IsBusy && ready && !isSaved);
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(IsFormEnabled));
        OnPropertyChanged(nameof(CanClose));

        SaveCommand.RaiseCanExecuteChanged();
    }

    // Giữ tên để code-behind hiện tại tiếp tục gọi được.
    public async Task LoadDepartmentsAsync()
    {
        if (IsBusy || ready || isSaved)
        {
            return;
        }

        IsBusy = true;

        try
        {
            Majors.Clear();

            if (CanEditAcademicFields)
            {
                if (EditedStudent.DepartmentID is not int departmentId)
                {
                    throw new InvalidOperationException(
                        "Hồ sơ chưa có khoa phụ trách.");
                }

                var majors = await MajorsAsync(departmentId);

                foreach (var major in majors)
                {
                    Majors.Add(major);
                }
            }

            // Vẫn hiển thị ngành hiện tại nếu ngành đã ngừng hoạt động.
            if (EditedStudent.MajorID is int current
                && !Majors.Any(major => major.Id == current))
            {
                Majors.Add(new Lookup(
                    current,
                    EditedStudent.MajorName ?? "Ngành hiện tại"));
            }

            OnPropertyChanged(nameof(EditedStudent));

            ready = true;

            StatusMessage = CanEditAcademicFields
                ? string.Empty
                : "Bạn chỉ được sửa email và số điện thoại.";
        }
        catch (Exception ex)
        {
            ready = false;

            StatusMessage =
                "Không tải được dữ liệu. Đóng form và mở lại.\n"
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
        StatusMessage = "Đang lưu...";

        try
        {
            if (CanEditAcademicFields)
            {
                Dates.ValidateAndApply(EditedStudent);
            }

            await UpdateAsync(EditedStudent);

            isSaved = true;
            StatusMessage = "Đã lưu hồ sơ.";
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
            ? "Dữ liệu đã lưu. Hãy đóng form.\n" + ex.Message
            : "Không thể cập nhật.\n" + ex.Message;
    }
}