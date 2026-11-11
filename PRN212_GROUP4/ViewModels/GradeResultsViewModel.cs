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

/// <summary>F13 View Grades.</summary>
public sealed class GradeResultsViewModel : BusyViewModelBase
{
    private readonly GradeService service = new();

    public ObservableCollection<Lookup> Students { get; } = new();
    public ObservableCollection<Lookup> Semesters { get; } = new();
    public ObservableCollection<Lookup> Classes { get; } = new();
    public ObservableCollection<GradeRow> Rows { get; } = new();

    public int StudentId { get; set; }
    public int SemesterId { get; set; }
    public int ClassId { get; set; }

    public string[] Publications { get; } =
    {
        "Tất cả", "Đã công bố", "Chưa công bố"
    };

    public string Publication { get; set; } = "Tất cả";

    /// <summary>Chỉ Lecturer mới có điểm chưa công bố để lọc.</summary>
    public bool CanFilterPublication =>
        AppSession.DisplayAccess?.CanLecturer("F13") == true;

    public AsyncRelayCommand RefreshCommand { get; }

    public GradeResultsViewModel()
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
            var lookups = await service.LookupsAsync("F13");

            Fill(Students, lookups.Students);
            Fill(Semesters, lookups.Semesters);
            Fill(Classes, lookups.Classes);

            // Student chỉ có chính mình trong danh sách.
            if (lookups.Students.Count == 1)
            {
                StudentId = lookups.Students[0].Id;
                OnPropertyChanged(nameof(StudentId));
            }

            await FillRowsAsync();
        }
        catch (Exception ex)
        {
            Rows.Clear();
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

    private string PublicationKey => Publication switch
    {
        "Đã công bố" => "PUBLISHED",
        "Chưa công bố" => "DRAFT",
        _ => "ALL"
    };

    private async Task FillRowsAsync()
    {
        Rows.Clear();

        var rows = await service.ListAsync(
            "F13", StudentId, SemesterId, ClassId, PublicationKey);

        foreach (var row in rows)
        {
            Rows.Add(row);
        }

        Message = Rows.Count == 0
            ? "Không có kết quả điểm phù hợp."
            : $"{Rows.Count} kết quả điểm.";
    }

    private async Task LoadAsync()
    {
        SetBusy(true);

        try
        {
            await FillRowsAsync();
        }
        catch
        {
            Rows.Clear();
            throw;
        }
        finally
        {
            SetBusy(false);
        }
    }
}

