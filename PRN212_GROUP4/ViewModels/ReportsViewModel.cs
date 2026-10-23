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
    public class ReportsViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService databaseService;

        private int? selectedDepartmentID = 0;

        public ObservableCollection<Department> Departments { get; }

        public ObservableCollection<Student> ReportStudents { get; }

        public string[] StatusOptions { get; } =
        {
            "ALL",
            "ACTIVE",
            "INACTIVE",
            "GRADUATED",
            "SUSPENDED"
        };

        public string SelectedStatus { get; set; } = "ALL";

        public int? SelectedDepartmentID
        {
            get => selectedDepartmentID;

            set
            {
                if (selectedDepartmentID == value)
                {
                    return;
                }

                selectedDepartmentID = value;

                OnPropertyChanged();
            }
        }

        public ReportsViewModel()
        {
            databaseService = new DatabaseService();

            Departments = new ObservableCollection<Department>();

            ReportStudents = new ObservableCollection<Student>();

            Departments.Add(new Department
            {
                DepartmentID = 0,
                DepartmentName = "All departments"
            });
        }

        public async Task GenerateReportAsync()
        {
            int departmentID = SelectedDepartmentID ?? 0;
            string status = SelectedStatus;

            // Đọc dữ liệu mới nhất mỗi lần tạo báo cáo.
            var students = await databaseService.GetStudentsAsync();

            // Danh sách khoa đang có sinh viên trong dữ liệu báo cáo.
            var departments = students
                .Where(student => student.DepartmentID.HasValue)
                .GroupBy(student => student.DepartmentID!.Value)
                .Select(group => new Department
                {
                    DepartmentID = group.Key,

                    DepartmentName =
                        group.First().DepartmentName
                        ?? $"Department #{group.Key}"
                })
                .OrderBy(department => department.DepartmentName)
                .ToList();

            Departments.Clear();

            Departments.Add(new Department
            {
                DepartmentID = 0,
                DepartmentName = "All departments"
            });

            foreach (Department department in departments)
            {
                Departments.Add(department);
            }

            // Nếu khoa không còn trong dữ liệu, trở lại Tất cả.
            if (!Departments.Any(
                department => department.DepartmentID == departmentID))
            {
                departmentID = 0;
            }

            SelectedDepartmentID = departmentID;

            var results = students.Where(student =>
                (departmentID == 0
                    || student.DepartmentID == departmentID)
                &&
                (status == "ALL"
                    || student.StudentStatus == status));

            ReportStudents.Clear();

            foreach (Student student in results)
            {
                ReportStudents.Add(student);
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
