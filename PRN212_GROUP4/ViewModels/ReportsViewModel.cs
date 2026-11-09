using System.Collections.ObjectModel;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.ViewModels;

public sealed partial class ReportsViewModel : ViewModelBase
{
    private bool busy;
    private string message = "";
    private ReportCriteria? generatedCriteria;

    public ObservableCollection<Lookup> Students { get; } = new();
    public ObservableCollection<Lookup> Semesters { get; } = new();
    public ObservableCollection<Lookup> Classes { get; } = new();
    public ObservableCollection<AcademicReportRow> Rows { get; } = new();

    public string[] Types { get; } =
    {
        "Sinh viên", "Lớp", "Kết quả học tập"
    };

    public string Type { get; set; } = "Sinh viên";
    public int StudentId { get; set; }
    public int SemesterId { get; set; }
    public int ClassId { get; set; }

    public bool CanInteract => !busy;

    public string Message
    {
        get => message;
        private set => SetProperty(ref message, value);
    }

    public AsyncRelayCommand GenerateCommand { get; private set; } = null!;
    public AsyncRelayCommand ExportCsvCommand { get; private set; } = null!;

    public ReportsViewModel() : this(null, null) { }

    partial void InitializeCommands()
    {
        GenerateCommand = new AsyncRelayCommand(
            GenerateAsync, Error, () => !busy);

        ExportCsvCommand = new AsyncRelayCommand(
            ExportAsync, Error, () => !busy && Rows.Count > 0);
    }

    private void Error(Exception ex)
    {
        Rows.Clear();
        generatedCriteria = null;
        Message = ex.Message;
        ExportCsvCommand.RaiseCanExecuteChanged();
    }

    private void Busy(bool value)
    {
        busy = value;
        OnPropertyChanged(nameof(CanInteract));
        GenerateCommand.RaiseCanExecuteChanged();
        ExportCsvCommand.RaiseCanExecuteChanged();
    }

    public async Task InitializeAsync()
    {
        Busy(true);

        try
        {
            var lookups = await new EnrollmentViewModel()
                .LookupsAsync("F16");

            Fill(Students, lookups.Students);
            Fill(Semesters, lookups.Semesters);
            Fill(Classes, lookups.Classes);
        }
        catch (Exception ex)
        {
            Error(ex);
        }
        finally
        {
            Busy(false);
        }

        await GenerateCommand.ExecuteAsync();
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

    private async Task GenerateAsync()
    {
        Busy(true);

        try
        {
            Rows.Clear();
            generatedCriteria = null;

            var criteria = new ReportCriteria(
                Type, StudentId, SemesterId, ClassId);

            foreach (var row in await GenerateAsync(criteria))
            {
                Rows.Add(row);
            }

            generatedCriteria = criteria;

            Message = Rows.Count == 0
                ? "Không có dữ liệu phù hợp."
                : $"{Type}: {Rows.Count} dòng. "
                  + "Ô trống = chưa có kết quả hoặc ngoài quyền xem GPA.";
        }
        finally
        {
            Busy(false);
        }
    }

    private async Task ExportAsync()
    {
        if (generatedCriteria == null)
        {
            return;
        }

        Busy(true);

        try
        {
            var data = await GenerateAsync(generatedCriteria);

            Rows.Clear();

            foreach (var row in data)
            {
                Rows.Add(row);
            }

            if (data.Count == 0)
            {
                Message = "Không còn dữ liệu được phép xuất.";
                return;
            }

            string? file = await AcademicCsvExporter.ExportAsync(data);

            Message = file == null
                ? "Đã hủy xuất file."
                : $"Đã xuất {data.Count} dòng: {file}";
        }
        finally
        {
            Busy(false);
        }
    }
}