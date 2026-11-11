using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.ViewModels;

/// <summary>
/// Trang "Điểm số": gom F11 (nhập), F12 (sửa), F13 (xem) và chỉ hiện
/// phần mà tài khoản được cấp quyền.
/// </summary>
public sealed class GradeCenterViewModel : ViewModelBase
{
    private string mode = "";

    public GradeEntryViewModel? Entry { get; }
    public GradeEditViewModel? Edit { get; }
    public GradeResultsViewModel? Results { get; }

    public bool CanEntry => Entry != null;
    public bool CanEdit => Edit != null;
    public bool CanResults => Results != null;

    public bool ShowEntry => mode == "Entry";
    public bool ShowEdit => mode == "Edit";
    public bool ShowResults => mode == "Results";

    public string EntryTag => ShowEntry ? "active" : "";
    public string EditTag => ShowEdit ? "active" : "";
    public string ResultsTag => ShowResults ? "active" : "";

    public RelayCommand ShowEntryCommand { get; }
    public RelayCommand ShowEditCommand { get; }
    public RelayCommand ShowResultsCommand { get; }

    public GradeCenterViewModel()
    {
        var access = AppSession.DisplayAccess;

        if (access?.Can("F11") == true) Entry = new GradeEntryViewModel();
        if (access?.Can("F12") == true) Edit = new GradeEditViewModel();
        if (access?.Can("F13") == true) Results = new GradeResultsViewModel();

        ShowEntryCommand = new RelayCommand(() => SetMode("Entry"));
        ShowEditCommand = new RelayCommand(() => SetMode("Edit"));
        ShowResultsCommand = new RelayCommand(() => SetMode("Results"));

        mode =
            CanResults ? "Results" :
            CanEntry ? "Entry" :
            CanEdit ? "Edit" : "";
    }

    public async Task InitializeAsync()
    {
        var tasks = new List<Task>();

        if (Entry != null) tasks.Add(Entry.InitializeAsync());
        if (Edit != null) tasks.Add(Edit.InitializeAsync());
        if (Results != null) tasks.Add(Results.InitializeAsync());

        await Task.WhenAll(tasks);
    }

    private void SetMode(string value)
    {
        if (mode == value)
        {
            return;
        }

        mode = value;

        OnPropertyChanged(nameof(ShowEntry));
        OnPropertyChanged(nameof(ShowEdit));
        OnPropertyChanged(nameof(ShowResults));
        OnPropertyChanged(nameof(EntryTag));
        OnPropertyChanged(nameof(EditTag));
        OnPropertyChanged(nameof(ResultsTag));

        // Làm mới dữ liệu của tab vừa mở để thấy thay đổi ở tab khác.
        switch (value)
        {
            case "Entry":
                _ = Entry?.RefreshCommand.ExecuteAsync();
                break;
            case "Edit":
                _ = Edit?.RefreshCommand.ExecuteAsync();
                break;
            case "Results":
                _ = Results?.RefreshCommand.ExecuteAsync();
                break;
        }
    }
}

