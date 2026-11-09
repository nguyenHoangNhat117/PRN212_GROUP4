using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.ViewModels;

public class StudentRecordsViewModel : StudentRecordViewModel
{
    private readonly IStudentRecordDialogService dialogService;

    private StudentRecord? selectedRecord;
    private bool includeArchived;
    private bool isBusy;

    private string statusMessage = "Đang tải hồ sơ...";

    public Student Student { get; }

    public ObservableCollection<StudentRecord> Records { get; }
        = new();

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand AddRecordCommand { get; }

    public AsyncRelayCommand EditRecordCommand { get; }

    public AsyncRelayCommand ArchiveRecordCommand { get; }

    public StudentRecord? SelectedRecord
    {
        get => selectedRecord;

        set
        {
            if (SetProperty(ref selectedRecord, value))
            {
                RefreshCommands();
            }
        }
    }

    public bool IncludeArchived
    {
        get => includeArchived;

        set => SetProperty(ref includeArchived, value);
    }

    public bool IsBusy
    {
        get => isBusy;

        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(CanInteract));
                RefreshCommands();
            }
        }
    }

    public bool CanInteract => !IsBusy;

    public string StatusMessage
    {
        get => statusMessage;

        private set => SetProperty(ref statusMessage, value);
    }

    public StudentRecordsViewModel(
        Student student,
        IStudentRecordDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(student);

        Student = student;

        this.dialogService = dialogService
            ?? throw new ArgumentNullException(nameof(dialogService));

        RefreshCommand = new AsyncRelayCommand(
            LoadRecordsAsync,
            HandleCommandError,
            () => !IsBusy);

        AddRecordCommand = new AsyncRelayCommand(
            AddRecordAsync,
            HandleCommandError,
            () => !IsBusy);

        EditRecordCommand = new AsyncRelayCommand(
            EditRecordAsync,
            HandleCommandError,
            CanModifySelectedRecord);

        ArchiveRecordCommand = new AsyncRelayCommand(
            ArchiveSelectedRecordAsync,
            HandleCommandError,
            CanModifySelectedRecord);
    }

    private bool CanModifySelectedRecord()
    {
        return !IsBusy
            && SelectedRecord != null
            && SelectedRecord.StudentID == Student.StudentID
            && !SelectedRecord.IsArchived;
    }

    private void RefreshCommands()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        AddRecordCommand.RaiseCanExecuteChanged();
        EditRecordCommand.RaiseCanExecuteChanged();
        ArchiveRecordCommand.RaiseCanExecuteChanged();
    }

    private void HandleCommandError(Exception ex)
    {
        StatusMessage =
            "Thao tác chưa hoàn tất. "
            + "Nhấn Tải lại để kiểm tra dữ liệu mới nhất.";

        dialogService.ShowWarning(ex.Message);
    }

    public async Task LoadRecordsAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Đang tải hồ sơ...";

        try
        {
            await ReloadRecordsCoreAsync(
                SelectedRecord?.RecordID);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadRecordsCoreAsync(int? selectedRecordID)
    {
        // Chỉ thay danh sách khi truy vấn đã thành công.
        var records = await GetAsync(
            Student.StudentID,
            IncludeArchived);

        SelectedRecord = null;
        Records.Clear();

        foreach (var record in records)
        {
            Records.Add(record);
        }

        SelectedRecord = Records.FirstOrDefault(
            record => record.RecordID == selectedRecordID);

        StatusMessage = Records.Count == 0
            ? "Không có hồ sơ phù hợp với bộ lọc."
            : $"Đã tải {Records.Count} hồ sơ.";
    }

    private async Task AddRecordAsync()
    {
        IsBusy = true;

        try
        {
            int? recordID = dialogService.ShowAddRecord(Student);

            if (!recordID.HasValue)
            {
                return;
            }

            await ReloadAfterSaveAsync(
                recordID.Value,
                "Thêm hồ sơ học vụ thành công!");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EditRecordAsync()
    {
        var record = SelectedRecord;

        if (record == null || record.IsArchived)
        {
            return;
        }

        IsBusy = true;

        try
        {
            bool saved = dialogService.ShowEditRecord(
                Student,
                record);

            if (!saved)
            {
                return;
            }

            await ReloadAfterSaveAsync(
                record.RecordID,
                "Cập nhật hồ sơ học vụ thành công!");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ArchiveSelectedRecordAsync()
    {
        var record = SelectedRecord;

        if (record == null || record.IsArchived)
        {
            return;
        }

        IsBusy = true;

        try
        {
            if (!dialogService.ConfirmArchive(record))
            {
                return;
            }

            StatusMessage = "Đang lưu trữ hồ sơ...";

            await ArchiveAsync(
                Student.StudentID,
                record.RecordID,
                record.RowVer);

            record.IsArchived = true;
            SelectedRecord = null;

            await ReloadAfterSaveAsync(
                record.RecordID,
                "Lưu trữ hồ sơ thành công!\n\n"
                + "Tích Include archived records để xem lại.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadAfterSaveAsync(
        int recordID,
        string successMessage)
    {
        StatusMessage = "Đang tải hồ sơ...";

        try
        {
            await ReloadRecordsCoreAsync(recordID);
        }
        catch (Exception ex)
        {
            StatusMessage =
                "Dữ liệu đã lưu nhưng tải lại thất bại. "
                + "Nhấn Tải lại để cập nhật.";

            dialogService.ShowWarning(
                "Thao tác lưu đã thành công, "
                + "nhưng không thể tải lại danh sách.\n"
                + "Không cần thực hiện lưu lại.\n\n"
                + ex.Message);

            return;
        }

        dialogService.ShowInformation(successMessage);
    }
}