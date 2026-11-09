using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

public interface IAcademicGpaProvider
{
    Task ApplyAsync(
        StudentManagementContext db,
        List<(AcademicReportRow Row, int StudentId)> rows,
        HashSet<int> allowedStudents);
}

public sealed class SqlViewGpaProvider : IAcademicGpaProvider
{
    public async Task ApplyAsync(
        StudentManagementContext db,
        List<(AcademicReportRow Row, int StudentId)> rows,
        HashSet<int> allowedStudents)
    {
        var ids = rows
            .Select(item => item.StudentId)
            .Where(allowedStudents.Contains)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return;
        }

        var cumulative = await db.VStudentGpas
            .AsNoTracking()
            .Where(item => ids.Contains(item.StudentId))
            .ToDictionaryAsync(
                item => item.StudentId,
                item => item.CumulativeGpa);

        var semesters = await db.VStudentSemesterGpas
            .AsNoTracking()
            .Where(item => ids.Contains(item.StudentId))
            .ToListAsync();

        var terms = semesters.ToDictionary(
            item => (item.StudentId, item.SemesterCode),
            item => item.SemesterGpa);

        foreach (var (row, studentId) in rows.Where(
            item => allowedStudents.Contains(item.StudentId)))
        {
            row.CumulativeGpa =
                cumulative.GetValueOrDefault(studentId);

            row.SemesterGpa =
                terms.GetValueOrDefault((studentId, row.Semester));
        }
    }
}