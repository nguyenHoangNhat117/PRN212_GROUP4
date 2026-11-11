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

/// <summary>F15 View Academic Progress.</summary>
public sealed class ProgressViewModel : BusyViewModelBase
{
    private readonly AcademicProgressService service = new();

    private int studentId;
    private string gpaText = "—";
    private string registeredText = "";
    private string inProgressText = "";
    private string withResultText = "";
    private string passedText = "";

    public ObservableCollection<Lookup> Students { get; } = new();
    public ObservableCollection<ProgressCourseRow> Courses { get; } = new();

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

    public string GpaText
    {
        get => gpaText;
        private set => SetProperty(ref gpaText, value);
    }

    public string RegisteredText
    {
        get => registeredText;
        private set => SetProperty(ref registeredText, value);
    }

    public string InProgressText
    {
        get => inProgressText;
        private set => SetProperty(ref inProgressText, value);
    }

    public string WithResultText
    {
        get => withResultText;
        private set => SetProperty(ref withResultText, value);
    }

    public string PassedText
    {
        get => passedText;
        private set => SetProperty(ref passedText, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }

    public ProgressViewModel()
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
        Courses.Clear();
        GpaText = "—";
        RegisteredText = "";
        InProgressText = "";
        WithResultText = "";
        PassedText = "";
    }

    private async Task ShowAsync()
    {
        Clear();

        if (StudentId == 0)
        {
            Message = "Chọn sinh viên để xem tiến độ học tập.";
            return;
        }

        var summary = await service.GetAsync(StudentId);

        GpaText = summary.GpaText;
        RegisteredText = summary.CreditsRegistered.ToString();
        InProgressText = summary.CreditsInProgress.ToString();
        WithResultText = summary.CreditsWithResult.ToString();
        PassedText = summary.CreditsPassed.ToString();

        foreach (var item in summary.Courses)
        {
            Courses.Add(item);
        }

        Message = Courses.Count == 0
            ? $"{summary.StudentCode} chưa có môn nào được xếp lớp."
            : $"Tiến độ của {summary.StudentCode} - {summary.StudentName}. "
              + "Chỉ tính điểm đã công bố; \"tín chỉ đạt\" là quy tắc tạm "
              + "(chưa phải mức F).";
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

