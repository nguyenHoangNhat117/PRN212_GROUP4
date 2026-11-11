using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.ViewModels;

/// <summary>F14 Calculate GPA (xem GPA học kỳ và tích lũy).</summary>
public sealed class GpaViewModel : BusyViewModelBase
{
    private readonly GpaService service = new();

    private int studentId;
    private string cumulativeText = "—";
    private string creditsText = "";
    private string note = "";
    private string scaleText = "";

    public ObservableCollection<Lookup> Students { get; } = new();
    public ObservableCollection<SemesterGpaRow> Semesters { get; } = new();
    public ObservableCollection<GpaCourseRow> Courses { get; } = new();

    public int StudentId
    {
        get => studentId;
        set
        {
            if (SetProperty(ref studentId, value))
            {
                _ = RefreshCommand.ExecuteAsync();
            }
        }
    }

    public string CumulativeText
    {
        get => cumulativeText;
        private set => SetProperty(ref cumulativeText, value);
    }

    public string CreditsText
    {
        get => creditsText;
        private set => SetProperty(ref creditsText, value);
    }

    public string Note
    {
        get => note;
        private set => SetProperty(ref note, value);
    }

    public string ScaleText
    {
        get => scaleText;
        private set => SetProperty(ref scaleText, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }

    public GpaViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(
            LoadAsync, ShowError, () => !IsBusy);
    }

    protected override void OnBusyChanged()
    {
        RefreshCommand.RaiseCanExecuteChanged();
    }

    public async Task InitializeAsync()
    {
        SetBusy(true);

        try
        {
            var students = await service.StudentsAsync();

            Students.Clear();

            foreach (var item in students)
            {
                Students.Add(item);
            }

            // Student chỉ có chính mình: chọn sẵn và hiển thị luôn.
            if (students.Count == 1)
            {
                SetProperty(ref studentId, students[0].Id,
                    nameof(StudentId));
            }

            await ShowAsync();
        }
        catch (Exception ex)
        {
            Clear();
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Clear()
    {
        Semesters.Clear();
        Courses.Clear();
        CumulativeText = "—";
        CreditsText = "";
        Note = "";
        ScaleText = "";
    }

    private async Task ShowAsync()
    {
        Clear();

        if (StudentId == 0)
        {
            Message = "Chọn sinh viên để xem GPA.";
            return;
        }

        var summary = await service.GetAsync(StudentId);

        CumulativeText = summary.CumulativeText;
        CreditsText = $"{summary.CountedCredits} tín chỉ được tính";
        ScaleText = "Thang quy đổi (cấu hình demo, chờ nhóm chốt): "
            + summary.ScaleText;

        Note = summary.HasRepeatedCourses
            ? "Lưu ý: có môn học lại. Hiện mọi lần học đều được tính "
              + "vào GPA vì nhóm chưa chốt quy tắc học lại."
            : "";

        foreach (var item in summary.Semesters)
        {
            Semesters.Add(item);
        }

        foreach (var item in summary.Courses)
        {
            Courses.Add(item);
        }

        Message = summary.HasData
            ? $"GPA của {summary.StudentCode} - {summary.StudentName}."
            : "Chưa có kết quả đã công bố và hoàn tất để tính GPA.";
    }

    private async Task LoadAsync()
    {
        SetBusy(true);

        try
        {
            await ShowAsync();
        }
        catch
        {
            Clear();
            throw;
        }
        finally
        {
            SetBusy(false);
        }
    }
}
