using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

/// <summary>
/// F15 View Academic Progress. Chủ chức năng: Nguyễn Tấn Lực (CE190710).
/// Dùng chung dữ liệu và quy tắc với F13–F14; chỉ hiển thị điểm đã công bố.
/// Lecturer chỉ thấy các lớp mình phụ trách và KHÔNG nhận GPA tổng hợp
/// (GPA có thể chứa kết quả của lớp khác - cùng quy ước với F16).
/// </summary>
public sealed class AcademicProgressService
{
    public async Task<List<Lookup>> StudentsAsync()
    {
        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, "F15");

        return await GradeAccess.Students(db, access, "F15")
            .OrderBy(student => student.StudentCode)
            .Select(student => new Lookup(
                student.StudentId,
                student.StudentCode + " - " + student.FullName))
            .ToListAsync();
    }

    public async Task<ProgressSummary> GetAsync(int studentId)
    {
        if (studentId <= 0)
        {
            throw new ArgumentException("Chọn sinh viên.");
        }

        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, "F15");

        await GradeAccess.EnsureStudentAsync(
            db, access, studentId, "F15");

        var student = await db.Students
            .AsNoTracking()
            .Where(item => item.StudentId == studentId)
            .Select(item => new { item.StudentCode, item.FullName })
            .SingleAsync();

        var data = await GradeAccess.Enrollments(db, access, "F15")
            .Where(enrollment => enrollment.StudentId == studentId)
            .OrderBy(enrollment => enrollment.Class.Semester.SemesterCode)
            .ThenBy(enrollment => enrollment.Class.Course.CourseCode)
            .Select(enrollment => new
            {
                Semester = enrollment.Class.Semester.SemesterCode,
                Course = enrollment.Class.Course.CourseCode
                    + " - " + enrollment.Class.Course.CourseName,
                Class = enrollment.Class.ClassCode,
                Credits = (int)enrollment.Class.Course.Credits,
                enrollment.EnrollmentStatus,
                Score = enrollment.Grade != null
                    && enrollment.Grade.IsPublished
                        ? (decimal?)enrollment.Grade.FinalScore
                        : null
            })
            .ToListAsync();

        var scale = await db.GradeScales
            .AsNoTracking()
            .Where(band => band.IsActive)
            .ToListAsync();

        var courses = new List<ProgressCourseRow>();
        int registered = 0, inProgress = 0, withResult = 0, passed = 0;

        foreach (var item in data)
        {
            var band = GradePolicy.Band(item.Score, scale);
            bool hasResult =
                item.EnrollmentStatus == "COMPLETED"
                && item.Score.HasValue;

            registered += item.Credits;

            if (item.EnrollmentStatus == "ENROLLED")
            {
                inProgress += item.Credits;
            }

            if (hasResult)
            {
                withResult += item.Credits;

                if (GradePolicy.IsPassed(band?.GradePoint))
                {
                    passed += item.Credits;
                }
            }

            courses.Add(new ProgressCourseRow
            {
                Semester = item.Semester,
                Course = item.Course,
                Class = item.Class,
                Credits = item.Credits,
                Score = item.Score,
                Letter = band?.GradeLetter ?? "",
                Status = hasResult ? "Hoàn thành"
                    : item.EnrollmentStatus == "COMPLETED"
                        ? "Chờ công bố điểm"
                    : item.EnrollmentStatus == "ENROLLED"
                        ? "Đang học"
                    : item.EnrollmentStatus
            });
        }

        // GPA tổng hợp chỉ cho chính sinh viên hoặc Staff trong phạm vi khoa.
        bool aggregateAllowed =
            access.CanStaff("F15")
            || (access.CanStudent("F15")
                && access.StudentId == studentId);

        decimal? gpa = null;

        if (aggregateAllowed)
        {
            gpa = await db.VStudentGpas
                .AsNoTracking()
                .Where(item => item.StudentId == studentId)
                .Select(item => item.CumulativeGpa)
                .FirstOrDefaultAsync();
        }

        return new ProgressSummary
        {
            StudentCode = student.StudentCode,
            StudentName = student.FullName,
            ShowGpa = aggregateAllowed,
            CumulativeGpa = gpa,
            CreditsRegistered = registered,
            CreditsInProgress = inProgress,
            CreditsWithResult = withResult,
            CreditsPassed = passed,
            Courses = courses
        };
    }
}

