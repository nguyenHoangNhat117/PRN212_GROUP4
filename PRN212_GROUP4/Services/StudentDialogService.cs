using PRN212_GROUP4.Models;
using PRN212_GROUP4.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PRN212_GROUP4.Services
{
    public class StudentDialogService : IStudentDialogService
    {
        private static void SetOwner(Window window)
        {
            var owner = Application.Current?.MainWindow;

            if (owner != null && owner != window)
            {
                window.Owner = owner;
            }
        }

        public int? ShowAddStudent()
        {
            var window = new AddStudentWindow();

            SetOwner(window);

            return window.ShowDialog() == true
                ? window.CreatedStudentID
                : null;
        }

        public bool ShowEditStudent(Student student)
        {
            var window = new EditStudentWindow(student);

            SetOwner(window);

            return window.ShowDialog() == true;
        }

        public void ShowStudentRecords(Student student)
        {
            var window = new StudentRecordsWindow(student);

            SetOwner(window);

            window.ShowDialog();
        }

        public void ShowInformation(string message)
        {
            MessageBox.Show(
                message,
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(
                message,
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
