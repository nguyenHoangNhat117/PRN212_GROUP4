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

/// <summary>F12 Update Grades - Lecturer sửa điểm đã nhập.</summary>
public sealed class GradeEditViewModel : BusyViewModelBase
{
    private readonly GradeService service = new();

    private GradeRow? selectedRow;
    private string scoreText = "";
    private bool publish;
    private bool markCompleted;

    public ObservableCollection<Lookup> Semesters { get; } = new();
    public ObservableCollection<Lookup> Classes { get; } = new();
    public ObservableCollection<GradeRow> Rows { get; } = new();

    public int SemesterId { get; set; }
    public int ClassId { get; set; }

    public GradeRow? SelectedRow
    {
        get => selectedRow;
        set
        {
            if (!SetProperty(ref selectedRow, value))
            {
                return;
            }

            ScoreText = value?.Score?.ToString("0.00") ?? "";
            Publish = value?.IsPublished ?? false;
            MarkCompleted = false;

            OnPropertyChanged(nameof(SelectedText));
            OnPropertyChanged(nameof(CanMarkCompleted));
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public string SelectedText =>
        SelectedRow == null
            ? "Chọn một bản ghi điểm trong danh sách để sửa."
            : $"Sửa điểm của {SelectedRow.StudentCode} - "
              + $"{SelectedRow.StudentName} · {SelectedRow.Course} "
              + $"(điểm hiện tại {SelectedRow.ScoreText})";

    public bool CanMarkCompleted =>
        SelectedRow?.EnrollmentStatus == "ENROLLED";

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

    public GradeEditViewModel()
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
            var lookups = await service.LookupsAsync("F12");

            Fill(Semesters, lookups.Semesters);
            Fill(Classes, lookups.Classes);

            await FillRowsAsync();
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

    private static void Fill(
        ObservableCollection<Lookup> target,
        List<Lookup> values)
    {
        target.Clear();
        target.Add(new Lookup(0, "Tất cả"));

        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    private async Task FillRowsAsync()
    {
        Rows.Clear();
        SelectedRow = null;

        var rows = await service.ListAsync(
            "F12", 0, SemesterId, ClassId);

        foreach (var row in rows)
        {
            Rows.Add(row);
        }

        Message = Rows.Count == 0
            ? "Chưa có điểm nào để sửa."
            : $"{Rows.Count} bản ghi điểm.";
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
            Message = "Chọn bản ghi điểm cần sửa.";
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
            await service.UpdateAsync(
                row.EnrollmentId,
                row.RowVer,
                score,
                Publish,
                MarkCompleted);

            try
            {
                await FillRowsAsync();

                Message = $"Đã cập nhật điểm của {row.StudentCode}.";
            }
            catch (Exception ex)
            {
                Rows.Clear();

                Message =
                    $"Đã cập nhật điểm của {row.StudentCode}; tải lại "
                    + "danh sách thất bại: " + ex.Message;
            }
        }
        finally
        {
            SetBusy(false);
        }
    }
}

