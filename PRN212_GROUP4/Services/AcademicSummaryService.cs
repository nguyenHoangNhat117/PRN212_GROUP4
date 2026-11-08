using Microsoft.EntityFrameworkCore;
using System.Data;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

public sealed class AcademicSummaryService
{
    private readonly IAcademicGpaProvider provider;
    private readonly Func<StudentManagementContext> createDb;
    public AcademicSummaryService(IAcademicGpaProvider? provider = null,
        Func<StudentManagementContext>? createDb = null)
    {
        this.provider = provider ?? new SqlViewGpaProvider();
        this.createDb = createDb ?? (() => new StudentManagementContext());
    }

    public async Task<AcademicReportRow> GetAsync(int studentId, string semester)
    {
        using var db = createDb();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var access = await AccessService.RequireAsync(db, "F14");
        await AccessService.EnsureStudentAsync(db, access, studentId, "F14");
        if (!(access.CanStudent("F14") && access.StudentId == studentId)
            && !(access.CanStaff("F14") && access.StaffDepartmentId != null))
            throw new UnauthorizedAccessException("Không được xem GPA tổng hợp của sinh viên.");
        var row = new AcademicReportRow { Semester = semester };
        await provider.ApplyAsync(db, new() { (row, studentId) }, new() { studentId });
        await transaction.CommitAsync();
        return row;
    }
}
