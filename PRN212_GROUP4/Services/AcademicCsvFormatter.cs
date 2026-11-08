using System.Globalization;
using System.Text;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services;

public static class AcademicCsvFormatter
{
    public static string Format(IReadOnlyList<AcademicReportRow> rows)
    {
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

        return text.ToString();
    }
}
