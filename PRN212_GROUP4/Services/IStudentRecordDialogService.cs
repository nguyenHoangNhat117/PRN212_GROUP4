using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services
{
    public interface IStudentRecordDialogService
    {
        int? ShowAddRecord(Student student);

        bool ShowEditRecord(
            Student student,
            StudentRecord record);

        bool ConfirmArchive(StudentRecord record);

        void ShowInformation(string message);

        void ShowWarning(string message);
    }
}