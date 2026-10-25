using System.Windows;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Views;

namespace PRN212_GROUP4.Services
{
    public class StudentRecordDialogService
        : IStudentRecordDialogService
    {
        private readonly Window owner;

        public StudentRecordDialogService(Window owner)
        {
            this.owner = owner;
        }

        public int? ShowAddRecord(Student student)
        {
            var window = new AddStudentRecordWindow(student)
            {
                Owner = owner
            };

            return window.ShowDialog() == true
                ? window.CreatedRecordID
                : null;
        }

        public bool ShowEditRecord(
            Student student,
            StudentRecord record)
        {
            var window = new EditStudentRecordWindow(
                student,
                record)
            {
                Owner = owner
            };

            return window.ShowDialog() == true;
        }

        public bool ConfirmArchive(StudentRecord record)
        {
            var result = MessageBox.Show(
                owner,
                "Bạn có muốn lưu trữ hồ sơ sau không?\n\n"
                + record.RecordTitle
                + "\n\nHồ sơ vẫn được giữ trong database "
                + "và không còn cho phép chỉnh sửa.",
                "Xác nhận lưu trữ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            return result == MessageBoxResult.Yes;
        }

        public void ShowInformation(string message)
        {
            MessageBox.Show(
                owner,
                message,
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(
                owner,
                message,
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}