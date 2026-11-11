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

/// <summary>F11 Record Grades - Lecturer nhập điểm cho lớp mình phụ trách.</summary>
public sealed class GradeEntryViewModel : BusyViewModelBase
{
    private readonly GradeService service = new();

    private int classId;
    private GradeRow? selectedRow;
    private string scoreText = "";
    private bool publish;
    private bool markCompleted;

    public ObservableCollection<Lookup> Classes { get; } = new();
    public ObservableCollection<GradeRow> Rows { get; } = new();

    public int ClassId
    {
        get => classId;
        set
        {
            if (SetProperty(ref classId, value))
            {
                _ = RefreshCommand.ExecuteAsync();
            }
        }
    }

    public GradeRow? SelectedRow
    {
        get => selectedRow;
        set
        {
            if (SetProperty(ref selectedRow, value))
            {
                OnPropertyChanged(nameof(SelectedText));
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedText =>
        SelectedRow == null
            ? "Chọn một sinh viên trong danh sách để nhập điểm."
            : $"Nhập điểm cho {SelectedRow.StudentCode} - "
              + $"{SelectedRow.StudentName} · {SelectedRow.Course}";

    public string ScoreText
    {
        get => scoreText;
        set => SetProperty(ref scoreText, value);
    }

    public bool Publish
    {
        get => publish;
        set => SetProperty(ref publish, value);
    }

    public bool MarkCompleted
    {
        get => markCompleted;
        set => SetProperty(ref markCompleted, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }

    public GradeEntryViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(
            LoadAsync, ShowError, () => !IsBusy);

        SaveCommand = new AsyncRelayCommand(
            SaveAsync, ShowError, () => !IsBusy && SelectedRow != null);
    }

    protected override void OnBusyChanged()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
    }

    public async Task InitializeAsync()
    {
        SetBusy(true);

        try
        {
            var lookups = await service.LookupsAsync("F11");

            Classes.Clear();

            foreach (var item in lookups.Classes)
            {
                Classes.Add(item);
            }

            Message = Classes.Count == 0
                ? "Bạn chưa có lớp nào có sinh viên được xếp."
                : "Chọn lớp để xem sinh viên chưa có điểm.";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task FillRowsAsync()
    {
        Rows.Clear();
        SelectedRow = null;

        if (ClassId == 0)
        {
            Message = "Chọn lớp để xem sinh viên chưa có điểm.";
            return;
        }

        var rows = await service.ListUngradedAsync(ClassId);

        foreach (var row in rows)
        {
            Rows.Add(row);
        }

        Message = Rows.Count == 0
            ? "Lớp này không còn sinh viên chờ nhập điểm."
            : $"{Rows.Count} sinh viên chưa có điểm.";
    }

    private async Task LoadAsync()
    {
        SetBusy(true);

        try
        {
            await FillRowsAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SaveAsync()
    {
        if (SelectedRow == null)
        {
            Message = "Chọn sinh viên cần nhập điểm.";
            return;
        }

        if (!GradePolicy.TryParse(ScoreText, out var score, out var error))
        {
            Message = error;
            return;
        }

        var row = SelectedRow;

        SetBusy(true);

        try
        {
            await service.RecordAsync(
                row.EnrollmentId, score, Publish, MarkCompleted);

            ScoreText = "";

            try
            {
                await FillRowsAsync();

                Message =
                    $"Đã lưu điểm {score:0.00} cho {row.StudentCode}. "
                    + $"Còn {Rows.Count} sinh viên chưa có điểm.";
            }
            catch (Exception ex)
            {
                Rows.Clear();

                Message =
                    $"Đã lưu điểm cho {row.StudentCode}; tải lại danh sách "
                    + "thất bại: " + ex.Message
                    + ". Không nhập lại.";
            }
        }
        finally
        {
            SetBusy(false);
        }
    }
}

