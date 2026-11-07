using System.Collections.ObjectModel;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.ViewModels;

public sealed class EnrollmentViewModel : ViewModelBase
{
    private readonly CourseEnrollmentService service = new();
    private bool busy;
    private string message = "";

    public ObservableCollection<Lookup> Students { get; } = new();
    public ObservableCollection<Lookup> RegistrationStudents { get; } = new();
    public ObservableCollection<Lookup> Courses { get; } = new();
    public ObservableCollection<Lookup> Semesters { get; } = new();
    public ObservableCollection<Lookup> RegistrationSemesters { get; } = new();
    public ObservableCollection<Lookup> Classes { get; } = new();
    public ObservableCollection<EnrollmentRow> Rows { get; } = new();

    public int StudentId { get; set; }
    public int SemesterId { get; set; }
    public int ClassId { get; set; }

    public int RegistrationStudentId { get; set; }
    public int CourseId { get; set; }
    public int RegistrationSemesterId { get; set; }

    public string Status { get; set; } = "ALL";

    public string[] Statuses { get; } =
    {
        "ALL", "REGISTERED", "DROPPED", "COMPLETED"
    };

    public bool CanRegister =>
        AppSession.DisplayAccess?.Can("F08") == true;

    public bool CanInteract => !busy;

    public string Message
    {
        get => message;
        private set => SetProperty(ref message, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand RegisterCommand { get; }

    public EnrollmentViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(
            RefreshAsync, Error, () => !busy);

        RegisterCommand = new AsyncRelayCommand(
            RegisterAsync, Error, () => !busy && CanRegister);
    }

    private void Busy(bool value)
    {
        busy = value;
        OnPropertyChanged(nameof(CanInteract));
        RefreshCommand.RaiseCanExecuteChanged();
        RegisterCommand.RaiseCanExecuteChanged();
    }

    private void Error(Exception ex)
    {
        Rows.Clear();
        Message = ex.Message;
    }

    public async Task InitializeAsync()
    {
        Busy(true);

        try
        {
            var lookups = await service.LookupsAsync();

            Fill(Students, lookups.Students, true);
            Fill(Courses, lookups.Courses, false);
            Fill(Semesters, lookups.Semesters, true);
            Fill(Classes, lookups.Classes, true);
            Fill(RegistrationSemesters, lookups.Semesters, false);

            if (CanRegister)
            {
                Fill(
                    RegistrationStudents,
                    await service.RegistrationStudentsAsync(),
                    false);
            }

            if (RegistrationStudents.Count == 1)
            {
                RegistrationStudentId = RegistrationStudents[0].Id;
                OnPropertyChanged(nameof(RegistrationStudentId));
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            Error(ex);
        }
        finally
        {
            Busy(false);
        }
    }

    private static void Fill(
        ObservableCollection<Lookup> target,
        List<Lookup> values,
        bool all)
    {
        target.Clear();

        if (all)
        {
            target.Add(new Lookup(0, "Tất cả"));
        }

        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    private async Task LoadAsync()
    {
        Rows.Clear();

        var rows = await service.ListAsync(
            StudentId, SemesterId, ClassId, Status);

        foreach (var row in rows)
        {
            Rows.Add(row);
        }

        Message = Rows.Count == 0
            ? "Không có đăng ký phù hợp."
            : $"{Rows.Count} đăng ký.";
    }

    private async Task RefreshAsync()
    {
        Busy(true);

        try
        {
            await LoadAsync();
        }
        finally
        {
            Busy(false);
        }
    }

    private async Task RegisterAsync()
    {
        Busy(true);

        try
        {
            int id = await service.RegisterAsync(
                RegistrationStudentId,
                CourseId,
                RegistrationSemesterId);

            try
            {
                await LoadAsync();

                Message =
                    $"Đăng ký #{id} đã lưu. Đang chờ xếp lớp. "
                    + "Bộ lọc hiện tại có thể ẩn đăng ký vừa tạo.";
            }
            catch (Exception ex)
            {
                Rows.Clear();

                Message =
                    $"Đăng ký #{id} đã lưu; tải lại thất bại: "
                    + ex.Message
                    + ". Không đăng ký lại.";
            }
        }
        finally
        {
            Busy(false);
        }
    }
}