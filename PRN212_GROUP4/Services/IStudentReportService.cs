using System.Collections.Generic;
using System.Threading.Tasks;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services
{
    public interface IStudentReportService
    {
        Task<string?> ExportCsvAsync(
            IReadOnlyList<Student> students);

        void ShowInformation(string message);

        void ShowWarning(string message);
    }
}