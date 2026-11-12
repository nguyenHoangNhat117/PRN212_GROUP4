using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

// This interface allows the report to use different GPA providers.
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
        // Read GPA only for students present in the report
        // and included in the allowed GPA scope.
        var ids = rows
            .Select(item => item.StudentId)
            .Where(allowedStudents.Contains)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return;
        }

        // The SQL view provides cumulative GPA.
        // This service reads the value; it does not calculate GPA.
        var cumulative = await db.VStudentGpas
            .AsNoTracking()
            .Where(item => ids.Contains(item.StudentId))
            .ToDictionaryAsync(
                item => item.StudentId,
                item => item.CumulativeGpa);

        // Read semester GPA for the same allowed students.
        var semesters = await db.VStudentSemesterGpas
            .AsNoTracking()
            .Where(item => ids.Contains(item.StudentId))
            .ToListAsync();

        // Match semester GPA by both student and semester.
        var terms = semesters.ToDictionary(
            item => (item.StudentId, item.SemesterCode),
            item => item.SemesterGpa);

        // Assign GPA only to rows within the allowed scope.
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