using System.Collections.ObjectModel;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.ViewModels;

public sealed partial class ReportsViewModel : ViewModelBase
{
    private bool busy;
    private string message = "";

    // Remember the criteria used to generate the displayed report.
    // Export uses these criteria, even if the user changes the filters.
    private ReportCriteria? generatedCriteria;

    // These collections provide data for the filters and report table.
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

    // Disable the filter controls while an operation is running.
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

        // Export is available only when the report has data.
        ExportCsvCommand = new AsyncRelayCommand(
            ExportAsync, Error, () => !busy && Rows.Count > 0);
    }

    private void Error(Exception ex)
    {
        // Clear old data so it is not mistaken for a successful result.
        Rows.Clear();
        generatedCriteria = null;
        Message = ex.Message;
        ExportCsvCommand.RaiseCanExecuteChanged();
    }

    private void Busy(bool value)
    {
        busy = value;
        OnPropertyChanged(nameof(CanInteract));

        // Refresh button states when the busy state changes.
        GenerateCommand.RaiseCanExecuteChanged();
        ExportCsvCommand.RaiseCanExecuteChanged();
    }

    public async Task InitializeAsync()
    {
        Busy(true);

        try
        {
            // Load filter options within the user's F16 access scope.
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

        // Generate the default report after loading the filters.
        await GenerateCommand.ExecuteAsync();
    }

    private static void Fill(
        ObservableCollection<Lookup> target,
        List<Lookup> values)
    {
        target.Clear();

        // ID 0 means that this filter does not restrict the result.
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

            // Take a snapshot of the current filter values.
            var criteria = new ReportCriteria(
                Type, StudentId, SemesterId, ClassId);

            // Call the business logic in ReportsViewModel.Business.cs.
            foreach (var row in await GenerateAsync(criteria))
            {
                Rows.Add(row);
            }

            // Save the criteria only after generation succeeds.
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
            // Query again using the last generated criteria.
            // This also checks the user's current access permissions.
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

            // The exporter opens the save dialog and writes the CSV file.
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