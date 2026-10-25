using System;
using System.Threading.Tasks;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.ViewModels
{
    public class EditStudentRecordViewModel : ViewModelBase
    {
        private readonly DatabaseService databaseService;
        private readonly StudentRecord originalRecord;

        private bool isBusy;
        private bool isSaved;

        private string statusMessage = string.Empty;
        private DateTime? recordDate;

        public Student Student { get; }

        public StudentRecord EditedRecord { get; }

        public AsyncRelayCommand SaveCommand { get; }

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

        public EditStudentRecordViewModel(
            Student student,
            StudentRecord record)
        {
            ArgumentNullException.ThrowIfNull(student);
            ArgumentNullException.ThrowIfNull(record);

            if (record.StudentID != student.StudentID)
            {
                throw new ArgumentException(
                    "Hồ sơ không thuộc sinh viên đang chọn.");
            }

            if (record.IsArchived)
            {
                throw new InvalidOperationException(
                    "Không thể chỉnh sửa hồ sơ đã lưu trữ.");
            }

            databaseService = new DatabaseService();

            Student = student;

            // Một bản để đối chiếu khi lưu.
            originalRecord = CopyRecord(record);

            // Một bản để nhập thay đổi.
            EditedRecord = CopyRecord(record);

            RecordDate = record.RecordDate;

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

                EditedRecord.RecordDate = RecordDate.Value.Date;

                // Giữ nguyên kiểm tra dữ liệu gốc trong DatabaseService.
                await databaseService.UpdateStudentRecordAsync(
                    EditedRecord,
                    originalRecord);

                isSaved = true;

                StatusMessage = "Changes saved.";
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
                : "Không thể cập nhật hồ sơ.\n" + ex.Message;
        }

        private static StudentRecord CopyRecord(
            StudentRecord source)
        {
            return new StudentRecord
            {
                RecordID = source.RecordID,
                StudentID = source.StudentID,
                RecordType = source.RecordType,
                RecordTitle = source.RecordTitle,
                RecordContent = source.RecordContent,
                RecordDate = source.RecordDate,
                IsArchived = source.IsArchived,
                CreatedByUserID = source.CreatedByUserID,
                CreatedAt = source.CreatedAt
            };
        }
    }
}