using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.ViewModels;

public class StudentViewModel : StudentProfileViewModel
{
    private readonly IStudentDialogService dialogService;
    private Student? selectedStudent;
    private bool isBusy;
    private string searchText = string.Empty;
    private string appliedSearchText = string.Empty;

    public ObservableCollection<Student> Students { get; } = new();
    public ListCollectionView DashboardStudents { get; }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand AddStudentCommand { get; }
    public AsyncRelayCommand EditStudentCommand { get; }
    public RelayCommand ViewRecordsCommand { get; }
    public RelayCommand SearchCommand { get; }

    public bool CanManageStudents =>
        AppSession.DisplayAccess?.CanStaff("F05") == true;

    public string ProfileHeading =>
        CanManageStudents ? "Quản lý sinh viên" : "Hồ sơ của tôi";

    public string ProfileDescription =>
        CanManageStudents
            ? "Tra cứu và cập nhật hồ sơ sinh viên trong khoa phụ trách."
            : "Thông tin cá nhân của bạn. "
              + "Sinh viên được cập nhật email và số điện thoại.";

    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value ?? string.Empty))
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    SearchStudents();
                }
            }
        }
    }

    public string DashboardResultMessage
    {
        get
        {
            if (DashboardStudents.Count == 0
                && !string.IsNullOrWhiteSpace(appliedSearchText))
            {
                return "Không tìm thấy sinh viên phù hợp.";
            }

            return $"Hiển thị {DashboardStudents.Count}"
                + $" / {Students.Count} sinh viên.";
        }
    }

    public Student? SelectedStudent
    {
        get => selectedStudent;
        set
        {
            if (SetProperty(ref selectedStudent, value))
            {
                RefreshCommands();
            }
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RefreshCommands();
            }
        }
    }

    public StudentViewModel() : this(new StudentDialogService()) { }

    public StudentViewModel(IStudentDialogService dialogService)
    {
        this.dialogService = dialogService
            ?? throw new ArgumentNullException(nameof(dialogService));

        DashboardStudents = new ListCollectionView(Students)
        {
            Filter = MatchesSearch
        };

        RefreshCommand = new AsyncRelayCommand(
            LoadStudentsAsync,
            HandleCommandError,
            () => !IsBusy);

        AddStudentCommand = new AsyncRelayCommand(
            AddStudentAsync,
            HandleCommandError,
            () => !IsBusy
                && AppSession.DisplayAccess?.Can("F05") == true);

        EditStudentCommand = new AsyncRelayCommand(
            EditStudentAsync,
            HandleCommandError,
            () => !IsBusy && SelectedStudent != null);

        ViewRecordsCommand = new RelayCommand(
            ViewRecords,
            () => !IsBusy
                && SelectedStudent != null
                && AppSession.DisplayAccess?.Can("F05") == true);

        SearchCommand = new RelayCommand(SearchStudents, () => !IsBusy);
    }

    private void RefreshCommands()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        AddStudentCommand.RaiseCanExecuteChanged();
        EditStudentCommand.RaiseCanExecuteChanged();
        ViewRecordsCommand.RaiseCanExecuteChanged();
        SearchCommand.RaiseCanExecuteChanged();
    }

    private void SearchStudents()
    {
        appliedSearchText = NormalizeText(SearchText);
        DashboardStudents.Refresh();
        OnPropertyChanged(nameof(DashboardResultMessage));
    }

    private bool MatchesSearch(object item)
    {
        if (item is not Student student)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(appliedSearchText))
        {
            return true;
        }

        string code = NormalizeText(student.StudentCode);
        string name = NormalizeText(student.FullName);

        return code.Contains(appliedSearchText)
            || name.Contains(appliedSearchText);
    }

    private static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string normalized = text.Trim()
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var result = new StringBuilder();

        foreach (char character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character)
                != UnicodeCategory.NonSpacingMark)
            {
                result.Append(character == 'đ' ? 'd' : character);
            }
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
    }

    private void HandleCommandError(Exception ex)
    {
        dialogService.ShowWarning(
            "Không thể thực hiện thao tác.\n\n" + ex.Message);
    }

    public async Task LoadStudentsAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await ReloadStudentsCoreAsync(SelectedStudent?.StudentID);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadStudentsCoreAsync(int? selectedStudentID)
    {
        var students = await GetAsync();

        SelectedStudent = null;
        Students.Clear();

        foreach (var student in students)
        {
            Students.Add(student);
        }

        SelectedStudent = Students.FirstOrDefault(
            student => student.StudentID == selectedStudentID)
            ?? Students.FirstOrDefault();

        DashboardStudents.Refresh();
        OnPropertyChanged(nameof(DashboardResultMessage));
    }

    private async Task AddStudentAsync()
    {
        IsBusy = true;

        try
        {
            int? studentID = dialogService.ShowAddStudent();

            if (!studentID.HasValue)
            {
                return;
            }

            SearchText = string.Empty;
            SearchStudents();

            await ReloadAfterSaveAsync(
                studentID.Value,
                "Thêm sinh viên thành công!");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EditStudentAsync()
    {
        var student = SelectedStudent;

        if (student == null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            if (!dialogService.ShowEditStudent(student))
            {
                return;
            }

            await ReloadAfterSaveAsync(
                student.StudentID,
                "Cập nhật hồ sơ thành công!");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadAfterSaveAsync(
        int studentID,
        string successMessage)
    {
        try
        {
            await ReloadStudentsCoreAsync(studentID);
        }
        catch (Exception ex)
        {
            dialogService.ShowWarning(
                "Dữ liệu đã được lưu, nhưng không thể "
                + "tải lại danh sách.\n"
                + "Không cần thực hiện lưu lại.\n\n"
                + ex.Message);

            return;
        }

        dialogService.ShowInformation(successMessage);
    }

    private void ViewRecords()
    {
        var student = SelectedStudent;

        if (student == null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            dialogService.ShowStudentRecords(student);
        }
        catch (Exception ex)
        {
            HandleCommandError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }
}