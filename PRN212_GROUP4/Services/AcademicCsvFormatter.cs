using System.Globalization;
using System.Text;
using PRN212_GROUP4.Helpers;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services;

public static class AcademicCsvFormatter
{
    public static string Format(
        IReadOnlyList<AcademicReportRow> rows)
    {
        // The header order must match the value order below.
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

                // Use a consistent number format for CSV output.
                row.Credits?.ToString(CultureInfo.InvariantCulture),
                row.Score?.ToString(CultureInfo.InvariantCulture),

                row.Letter,

                row.SemesterGpa?.ToString(CultureInfo.InvariantCulture),
                row.CumulativeGpa?.ToString(CultureInfo.InvariantCulture)
            };

            // Format each cell before joining values with commas.
            // CsvText.Cell handles cell content for CSV output.
            text.AppendLine(
                string.Join(",", values.Select(CsvText.Cell)));
        }

        // Return CSV text; file writing is handled by the exporter.
        return text.ToString();
    }
}