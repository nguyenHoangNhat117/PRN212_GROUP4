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

        await File.WriteAllTextAsync(
            dialog.FileName,
            AcademicCsvFormatter.Format(rows),
            new UTF8Encoding(true));

        return dialog.FileName;
    }
}
