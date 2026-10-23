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
    public class StudentViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService databaseService;

        private Student? selectedStudent;

        public ObservableCollection<Student> Students { get; }

        public Student? SelectedStudent
        {
            get => selectedStudent;

            set
            {
                if (object.ReferenceEquals(selectedStudent, value))
                {
                    return;
                }

                selectedStudent = value;

                OnPropertyChanged();
            }
        }

        public StudentViewModel()
        {
            databaseService = new DatabaseService();

            Students = new ObservableCollection<Student>();
        }

        public async Task LoadStudentsAsync()
        {
            // Chỉ thay danh sách sau khi đọc database thành công.
            var students = await databaseService.GetStudentsAsync();

            SelectedStudent = null;

            Students.Clear();

            foreach (Student student in students)
            {
                Students.Add(student);
            }
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