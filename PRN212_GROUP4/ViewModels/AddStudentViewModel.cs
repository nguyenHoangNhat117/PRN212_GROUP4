using PRN212_GROUP4.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.ViewModels
{
    public class AddStudentViewModel
    {
        private readonly DatabaseService databaseService;

        public Student NewStudent { get; }

        public ObservableCollection<Department> Departments { get; }

        public string[] StudentStatuses { get; } =
        {
            "ACTIVE",
            "INACTIVE",
            "GRADUATED",
            "SUSPENDED"
        };

        public AddStudentViewModel()
        {
            databaseService = new DatabaseService();

            Departments = new ObservableCollection<Department>();

            NewStudent = new Student
            {
                StudentStatus = "ACTIVE",
                EnrollmentDate = DateTime.Today
            };
        }

        public async Task LoadDepartmentsAsync()
        {
            var departments =
                await databaseService.GetDepartmentsAsync();

            Departments.Clear();

            foreach (Department department in departments)
            {
                Departments.Add(department);
            }
        }

        public async Task<int> SaveAsync()
        {
            return await databaseService.AddStudentAsync(NewStudent);
        }
    }
}
