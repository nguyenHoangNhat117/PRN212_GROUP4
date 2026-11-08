using System.Data;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

// Dùng kết quả GPA chung do Lực phụ trách.
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

public sealed class AcademicReportService
{
    private readonly IAcademicGpaProvider gpa;
    private readonly Func<StudentManagementContext> createDb;

    public AcademicReportService(IAcademicGpaProvider? gpa = null,
        Func<StudentManagementContext>? createDb = null)
    {
        this.gpa = gpa ?? new SqlViewGpaProvider();
        this.createDb = createDb ?? (() => new StudentManagementContext());
    }

    public async Task<List<AcademicReportRow>> GenerateAsync(
        ReportCriteria criteria)
    {
        if (!new[] { "Sinh viên", "Lớp", "Kết quả học tập" }
            .Contains(criteria.Type))
        {
            throw new ArgumentException("Loại báo cáo không hợp lệ.");
        }

        using var db = createDb();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F16");

        var students = AccessService
            .Students(db, access, "F16")
            .AsNoTracking()
            .Where(student =>
                criteria.StudentId == 0
                || student.StudentId == criteria.StudentId);

        var registrations = CourseEnrollmentService
            .Scoped(db, access, "F16")
            .Where(registration =>
                (criteria.SemesterId == 0
                    || registration.SemesterId == criteria.SemesterId)
                &&
                (criteria.ClassId == 0
                    || registration.ClassId == criteria.ClassId));

        // Báo cáo danh sách sinh viên.
        if (criteria.Type == "Sinh viên")
        {
            if (criteria.SemesterId != 0 || criteria.ClassId != 0)
            {
                students = students.Where(student =>
                    registrations.Any(registration =>
                        registration.StudentId == student.StudentId));
            }

            var result = await students
                .OrderBy(student => student.StudentCode)
                .Select(student => new AcademicReportRow
                {
                    StudentCode = student.StudentCode,
                    FullName = student.FullName,
                    Department = student.Department == null
                        ? ""
                        : student.Department.DepartmentName,
                    Status = student.StudentStatus
                })
                .ToListAsync();
            await transaction.CommitAsync();
            return result;
        }

        // Báo cáo danh sách lớp.
        if (criteria.Type == "Lớp")
        {
            var result = await (
                from registration in registrations
                    .Where(item => item.ClassId != null)
                join student in students
                    on registration.StudentId equals student.StudentId
                orderby registration.ClassCode, student.StudentCode
                select new AcademicReportRow
                {
                    StudentCode = student.StudentCode,
                    FullName = student.FullName,
                    Class = registration.ClassCode!,
                    Course = registration.CourseCode,
                    Semester = registration.SemesterCode,
                    Status = registration.EnrollmentStatus
                        ?? registration.RegistrationStatus
                })
                .ToListAsync();
            await transaction.CommitAsync();
            return result;
        }

        // Báo cáo kết quả học tập.
        var data = await BuildAcademicQuery(
                db,
                students,
                registrations)
            .ToListAsync();

        // Chuyển thành dòng báo cáo sau khi dữ liệu đã được lấy.
        var rows = data
            .Select(item => (
                Row: ToReportRow(item),
                StudentId: item.StudentId))
            .ToList();

        // GPA tổng hợp có thể chứa kết quả ngoài lớp của Lecturer.
        // Chỉ Staff trong phạm vi khoa được nhận GPA tổng hợp.
        bool canGpa = access.CanStaff("F16");

        var gpaStudents = await students
            .Where(student =>
                canGpa
                && student.DepartmentId == access.StaffDepartmentId)
            .Select(student => student.StudentId)
            .ToListAsync();

        await gpa.ApplyAsync(
            db,
            rows,
            gpaStudents.ToHashSet());

        await transaction.CommitAsync();
        return rows.Select(item => item.Row).ToList();
    }

    internal static IQueryable<AcademicResultData> BuildAcademicQuery(
        StudentManagementContext db,
        IQueryable<PRN212_GROUP4.Models.Entities.Student> students,
        IQueryable<VCourseEnrollmentStatus> registrations)
    {
        // Chỉ lấy các cột cần dùng.
        // FinalScore nullable để LEFT JOIN có thể trả về NULL.
        var resultsQuery = db.VStudentCourseResults
            .AsNoTracking()
            .Select(result => new
            {
                result.StudentId,
                result.ClassCode,
                FinalScore = (decimal?)result.FinalScore,
                result.GradeLetter
            });

        return
            from registration in registrations
                .Where(item => item.EnrollmentId != null)

            join student in students
                on registration.StudentId equals student.StudentId

            join course in db.Courses
                on registration.CourseId equals course.CourseId

            join result in resultsQuery
                on new
                {
                    student.StudentId,
                    registration.ClassCode
                }
                equals new
                {
                    result.StudentId,
                    ClassCode = (string?)result.ClassCode
                }
                into results

            from result in results.DefaultIfEmpty()

            orderby student.StudentCode,
                registration.SemesterCode,
                registration.CourseCode

            select new AcademicResultData
            {
                StudentId = student.StudentId,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                Course = registration.CourseCode,
                Class = registration.ClassCode,
                Semester = registration.SemesterCode,
                Credits = course.Credits,
                Score = result.FinalScore,
                Letter = result.GradeLetter
            };
    }

    internal static AcademicReportRow ToReportRow(
        AcademicResultData item)
    {
        return new AcademicReportRow
        {
            StudentCode = item.StudentCode,
            FullName = item.FullName,
            Course = item.Course,
            Class = item.Class ?? "Chưa xếp lớp",
            Semester = item.Semester,
            Credits = item.Credits,
            Score = item.Score,
            Letter = item.Letter,
            Status = item.Score.HasValue
                ? "Có kết quả"
                : "Chưa có kết quả công bố/hoàn tất"
        };
    }

    internal sealed class AcademicResultData
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Course { get; set; } = "";
        public string? Class { get; set; }
        public string Semester { get; set; } = "";
        public int Credits { get; set; }
        public decimal? Score { get; set; }
        public string? Letter { get; set; }
    }
}