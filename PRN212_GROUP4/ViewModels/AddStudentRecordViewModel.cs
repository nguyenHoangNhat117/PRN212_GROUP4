using System;
using System.Threading.Tasks;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.ViewModels
{
    public class AddStudentRecordViewModel : ViewModelBase
    {
        private readonly DatabaseService databaseService;

        private bool isBusy;
        private bool isSaved;

        private string statusMessage = string.Empty;
        private DateTime? recordDate = DateTime.Today;

        public Student Student { get; }

        public StudentRecord NewRecord { get; }

        public AsyncRelayCommand SaveCommand { get; }

        public int? CreatedRecordID { get; private set; }

        public event EventHandler? SaveSucceeded;

        public DateTime? RecordDate
        {
            get => recordDate;

            set => SetProperty(ref recordDate, value);
        }

        public bool IsBusy
        {
            get => isBusy;

            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    RefreshState();
                }
            }
        }

        public bool IsFormEnabled => !IsBusy && !isSaved;

        public bool CanClose => !IsBusy;

        public string StatusMessage
        {
            get => statusMessage;

            private set => SetProperty(ref statusMessage, value);
        }

        public AddStudentRecordViewModel(
            Student student,
            int? createdByUserID = null)
        {
            ArgumentNullException.ThrowIfNull(student);

            databaseService = new DatabaseService();

            Student = student;

            NewRecord = new StudentRecord
            {
                StudentID = student.StudentID,
                CreatedByUserID = createdByUserID,
                IsArchived = false
            };

            SaveCommand = new AsyncRelayCommand(
                SaveAsync,
                HandleSaveError,
                () => !IsBusy && !isSaved);
        }

        private void RefreshState()
        {
            OnPropertyChanged(nameof(IsFormEnabled));
            OnPropertyChanged(nameof(CanClose));

            SaveCommand.RaiseCanExecuteChanged();
        }

        private async Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "Saving...";

            try
            {
                if (!RecordDate.HasValue)
                {
                    throw new ArgumentException(
                        "Vui lòng chọn ngày ghi nhận hồ sơ.");
                }

                NewRecord.RecordDate = RecordDate.Value.Date;

                CreatedRecordID =
                    await databaseService.AddStudentRecordAsync(
                        NewRecord);

                isSaved = true;

                StatusMessage = "Record saved.";
            }
            finally
            {
                IsBusy = false;
            }

            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }

        private void HandleSaveError(Exception ex)
        {
            StatusMessage = isSaved
                ? "Hồ sơ đã được lưu. Hãy đóng form.\n" + ex.Message
                : "Không thể thêm hồ sơ.\n" + ex.Message;
        }
    }
}