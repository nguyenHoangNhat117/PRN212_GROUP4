using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services
{
    public class StudentReportService : IStudentReportService
    {
        public async Task<string?> ExportCsvAsync(
            IReadOnlyList<Student> students)
        {
            if (students.Count == 0)
            {
                throw new InvalidOperationException(
                    "Danh sách báo cáo đang trống.");
            }

            var dialog = new SaveFileDialog
            {
                Title = "Lưu báo cáo sinh viên",
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = "Student_Report_"
                    + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                    + ".csv"
            };

            var owner = Application.Current?.MainWindow;

            bool? result = owner != null
                ? dialog.ShowDialog(owner)
                : dialog.ShowDialog();

            if (result != true)
            {
                return null;
            }

            var csv = new StringBuilder();

            csv.AppendLine(
                "StudentCode,FullName,DepartmentName,Email,StudentStatus");

            foreach (var student in students)
            {
                string[] values =
                {
                    student.StudentCode,
                    student.FullName,
                    student.DepartmentName ?? "Unassigned",
                    student.Email ?? string.Empty,
                    student.StudentStatus
                };

                csv.AppendLine(
                    string.Join(",", values.Select(ToCsvCell)));
            }

            // UTF-8 có BOM để Excel đọc được tiếng Việt.
            await File.WriteAllTextAsync(
                dialog.FileName,
                csv.ToString(),
                new UTF8Encoding(true));

            return dialog.FileName;
        }

        private static string ToCsvCell(string? value)
        {
            string text = value ?? string.Empty;

            string trimmed = text.TrimStart();

            // Tránh để Excel diễn giải dữ liệu thành công thức.
            if (trimmed.StartsWith("=")
                || trimmed.StartsWith("+")
                || trimmed.StartsWith("-")
                || trimmed.StartsWith("@")
                || text.StartsWith("\t")
                || text.StartsWith("\r")
                || text.StartsWith("\n"))
            {
                text = "'" + text;
            }

            return "\"" + text.Replace("\"", "\"\"") + "\"";
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