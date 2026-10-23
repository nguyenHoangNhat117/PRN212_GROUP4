using PRN212_GROUP4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.ViewModels
{
    public class EditStudentRecordViewModel
    {
        private readonly DatabaseService databaseService;

        private readonly StudentRecord originalRecord;

        public Student Student { get; }

        public StudentRecord EditedRecord { get; }

        public DateTime? RecordDate { get; set; }

        public EditStudentRecordViewModel(
            Student student,
            StudentRecord record)
        {
            if (record.StudentID != student.StudentID)
            {
                throw new ArgumentException(
                    "Hồ sơ không thuộc sinh viên đang chọn.");
            }

            databaseService = new DatabaseService();

            Student = student;

            // Hai bản sao riêng:
            // một để đối chiếu, một để nhập thay đổi.
            originalRecord = CopyRecord(record);
            EditedRecord = CopyRecord(record);

            RecordDate = record.RecordDate;
        }

        public async Task SaveAsync()
        {
            if (!RecordDate.HasValue)
            {
                throw new ArgumentException(
                    "Vui lòng chọn ngày ghi nhận hồ sơ.");
            }

            EditedRecord.RecordDate = RecordDate.Value.Date;

            await databaseService.UpdateStudentRecordAsync(
                EditedRecord,
                originalRecord);
        }

        private static StudentRecord CopyRecord(StudentRecord source)
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
