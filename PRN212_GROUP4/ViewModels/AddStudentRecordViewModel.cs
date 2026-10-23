using PRN212_GROUP4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.ViewModels
{
    public class AddStudentRecordViewModel
    {
        private readonly DatabaseService databaseService;

        public Student Student { get; }

        public StudentRecord NewRecord { get; }

        public DateTime? RecordDate { get; set; } = DateTime.Today;

        public AddStudentRecordViewModel(
            Student student,
            int? createdByUserID = null)
        {
            databaseService = new DatabaseService();

            Student = student;

            NewRecord = new StudentRecord
            {
                StudentID = student.StudentID,
                CreatedByUserID = createdByUserID,
                IsArchived = false
            };
        }

        public async Task<int> SaveAsync()
        {
            if (!RecordDate.HasValue)
            {
                throw new ArgumentException(
                    "Vui lòng chọn ngày ghi nhận hồ sơ.");
            }

            NewRecord.RecordDate = RecordDate.Value.Date;

            return await databaseService.AddStudentRecordAsync(
                NewRecord);
        }
    }
}
