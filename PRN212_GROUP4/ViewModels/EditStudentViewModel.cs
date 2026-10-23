using PRN212_GROUP4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.ViewModels
{
    public class EditStudentViewModel
    {
        private readonly DatabaseService databaseService;

        public Student EditedStudent { get; }

        public string[] StudentStatuses { get; } =
        {
            "ACTIVE",
            "INACTIVE",
            "GRADUATED",
            "SUSPENDED"
        };

        public EditStudentViewModel(Student student)
        {
            databaseService = new DatabaseService();

            // Sửa trên bản sao.
            // Bấm Cancel sẽ không làm đổi dữ liệu đang hiển thị.
            EditedStudent = new Student
            {
                StudentID = student.StudentID,
                StudentCode = student.StudentCode,
                UserID = student.UserID,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Email = student.Email,
                Phone = student.Phone,
                DepartmentID = student.DepartmentID,
                DepartmentName = student.DepartmentName,
                EnrollmentDate = student.EnrollmentDate,
                StudentStatus = student.StudentStatus,
                CreatedAt = student.CreatedAt,

                RowVer = student.RowVer == null
                    ? Array.Empty<byte>()
                    : (byte[])student.RowVer.Clone()
            };
        }

        public async Task SaveAsync()
        {
            await databaseService.UpdateStudentAsync(EditedStudent);
        }
    }
}