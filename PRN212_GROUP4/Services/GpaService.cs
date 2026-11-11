using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

/// <summary>
/// F14 Calculate GPA. Chủ chức năng: Nguyễn Tấn Lực (CE190710).
///
/// Không có công thức GPA thứ hai: kết quả lấy từ các view vStudentGPA,
/// vStudentSemesterGPA, vStudentCourseResults (cùng nguồn với
/// SqlViewGpaProvider mà F16 đang dùng), nên báo cáo và màn GPA luôn khớp.
/// Chính sách quy đổi nằm ở bảng GradeScale (cấu hình DEMO, chờ nhóm chốt).
/// Không có điểm hợp lệ → GPA = null, giao diện hiển thị "Chưa có dữ liệu".
/// </summary>
public sealed class GpaService
{
    public async Task<List<Lookup>> StudentsAsync()
    {
        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, "F14");

        return await GradeAccess.Students(db, access, "F14")
            .OrderBy(student => student.StudentCode)
            .Select(student => new Lookup(
                student.StudentId,
                student.StudentCode + " - " + student.FullName))
            .ToListAsync();
    }

    public async Task<GpaSummary> GetAsync(int studentId)
    {
        if (studentId <= 0)
        {
            throw new ArgumentException("Chọn sinh viên.");
        }

        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, "F14");

        await GradeAccess.EnsureStudentAsync(
            db, access, studentId, "F14");

        var student = await db.Students
            .AsNoTracking()
            .Where(item => item.StudentId == studentId)
            .Select(item => new { item.StudentCode, item.FullName })
            .SingleAsync();

        var cumulative = await db.VStudentGpas
            .AsNoTracking()
            .Where(item => item.StudentId == studentId)
            .FirstOrDefaultAsync();

        var terms = await db.VStudentSemesterGpas
            .AsNoTracking()
            .Where(item => item.StudentId == studentId)
            .OrderBy(item => item.SemesterCode)
            .ToListAsync();

        var results = await db.VStudentCourseResults
            .AsNoTracking()
            .Where(item => item.StudentId == studentId)
            .OrderBy(item => item.SemesterCode)
            .ThenBy(item => item.CourseCode)
            .ToListAsync();

        var scale = await db.GradeScales
            .AsNoTracking()
            .Where(band => band.IsActive)
            .ToListAsync();

        return new GpaSummary
        {
            StudentCode = student.StudentCode,
            StudentName = student.FullName,
            CumulativeGpa = cumulative?.CumulativeGpa,
            CountedCredits = cumulative?.CountedCredits ?? 0,

            // View hiện tính mọi lần học của một môn; nhóm chưa chốt học lại.
            HasRepeatedCourses = results
                .GroupBy(item => item.CourseCode)
                .Any(group => group.Count() > 1),

            ScaleText = GradePolicy.Describe(scale),

            Semesters = terms
                .Select(item => new SemesterGpaRow
                {
                    Semester = item.SemesterCode,
                    Gpa = item.SemesterGpa,
                    Credits = item.CountedCredits ?? 0
                })
                .ToList(),

            Courses = results
                .Select(item => new GpaCourseRow
                {
                    Semester = item.SemesterCode,
                    Course = item.CourseCode + " - " + item.CourseName,
                    Class = item.ClassCode,
                    Credits = item.Credits,
                    Score = item.FinalScore,
                    Letter = item.GradeLetter ?? "",
                    Point = item.GradePoint
                })
                .ToList()
        };
    }
}

