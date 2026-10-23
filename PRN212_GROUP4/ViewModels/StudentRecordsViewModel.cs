using PRN212_GROUP4.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.ViewModels
{
    public class StudentRecordsViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService databaseService;

        private StudentRecord? selectedRecord;

        public Student Student { get; }

        public ObservableCollection<StudentRecord> Records { get; }

        public bool IncludeArchived { get; set; }

        public StudentRecord? SelectedRecord
        {
            get => selectedRecord;

            set
            {
                if (object.ReferenceEquals(selectedRecord, value))
                {
                    return;
                }

                selectedRecord = value;

                OnPropertyChanged();
            }
        }

        public StudentRecordsViewModel(Student student)
        {
            databaseService = new DatabaseService();

            Student = student;

            Records = new ObservableCollection<StudentRecord>();
        }

        public async Task LoadRecordsAsync()
        {
            var records =
                await databaseService.GetStudentRecordsAsync(
                    Student.StudentID,
                    IncludeArchived);

            SelectedRecord = null;

            Records.Clear();

            foreach (StudentRecord record in records)
            {
                Records.Add(record);
            }
        }

        public async Task ArchiveRecordAsync(StudentRecord record)
        {
            if (record == null || record.StudentID != Student.StudentID)
            {
                throw new System.ArgumentException(
                    "Hồ sơ không thuộc sinh viên đang chọn.");
            }

            if (record.IsArchived)
            {
                throw new System.InvalidOperationException(
                    "Hồ sơ này đã được lưu trữ.");
            }

            await databaseService.ArchiveStudentRecordAsync(
                Student.StudentID,
                record.RecordID);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
