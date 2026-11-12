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
        // Let the user choose where to save the CSV file.
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true,

            // Ask before replacing an existing file.
            OverwritePrompt = true,

            FileName = "Academic_Report_"
                + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                + ".csv"
        };

        // Returning null tells the ViewModel that export was canceled.
        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        // Convert report rows to CSV and write the selected file.
        // UTF-8 with BOM helps Excel recognize Vietnamese text.
        await File.WriteAllTextAsync(
            dialog.FileName,
            AcademicCsvFormatter.Format(rows),
            new UTF8Encoding(true));

        // Return the saved path for the success message.
        return dialog.FileName;
    }
}