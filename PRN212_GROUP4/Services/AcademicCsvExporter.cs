using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services;

public static class AcademicCsvExporter
{
    public static async Task<string?> ExportAsync(
        IReadOnlyList<AcademicReportRow> rows)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = "Academic_Report_"
                + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                + ".csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        var text = new StringBuilder(
            "StudentCode,FullName,Department,Status,Semester,Class,"
            + "Course,Credits,Score,Letter,SemesterGPA,CumulativeGPA\r\n");

        foreach (var row in rows)
        {
            string?[] values =
            {
                row.StudentCode,
                row.FullName,
                row.Department,
                row.Status,
                row.Semester,
                row.Class,
                row.Course,
                row.Credits?.ToString(CultureInfo.InvariantCulture),
                row.Score?.ToString(CultureInfo.InvariantCulture),
                row.Letter,
                row.SemesterGpa?.ToString(CultureInfo.InvariantCulture),
                row.CumulativeGpa?.ToString(CultureInfo.InvariantCulture)
            };

            text.AppendLine(string.Join(",", values.Select(CsvText.Cell)));
        }

        await File.WriteAllTextAsync(
            dialog.FileName,
            text.ToString(),
            new UTF8Encoding(true));

        return dialog.FileName;
    }
}
