using PRN212_GROUP4.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.ViewModels
{
    public class StudentViewModel
    {
        private readonly DatabaseService databaseService;

        public ObservableCollection<Student> Students { get; }

        public StudentViewModel()
        {
            databaseService = new DatabaseService();

            Students = new ObservableCollection<Student>();
        }

        public async Task LoadStudentsAsync()
        {
            // Đọc dữ liệu trước, chỉ thay danh sách khi đọc thành công.
            var students = await databaseService.GetStudentsAsync();

            Students.Clear();

            foreach (Student student in students)
            {
                Students.Add(student);
            }
        }
    }
}
