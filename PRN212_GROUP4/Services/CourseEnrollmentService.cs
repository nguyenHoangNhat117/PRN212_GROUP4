using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

public sealed class CourseEnrollmentService
{
    public async Task<int> RegisterAsync(
        int studentId,
        int courseId,
        int semesterId)
    {
        if (studentId <= 0 || courseId <= 0 || semesterId <= 0)
        {
            throw new ArgumentException(
                "Chọn sinh viên, môn và học kỳ.");
        }

        using var db = new StudentManagementContext();
        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F08");

        await AccessService.EnsureStudentAsync(
            db, access, studentId, "F08");

        bool exists = await db.CourseEnrollments.AnyAsync(enrollment =>
            enrollment.StudentId == studentId
            && enrollment.CourseId == courseId
            && enrollment.SemesterId == semesterId);

        if (exists)
        {
            throw new InvalidOperationException(
                "Đăng ký môn/học kỳ đã tồn tại, kể cả đăng ký đã hủy. "
                + "Hãy liên hệ học vụ để xử lý.");
        }

        var output = new SqlParameter(
            "@RegistrationId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.uspRegisterCourse "
                + "@StudentID=@s, @CourseID=@c, @SemesterID=@t, "
                + "@CourseEnrollmentID=@RegistrationId OUTPUT",
                new SqlParameter("@s", studentId),
                new SqlParameter("@c", courseId),
                new SqlParameter("@t", semesterId),
                output);

            await transaction.CommitAsync();

            return (int)output.Value;
        }
        catch (SqlException ex)
            when (ex.Number is 2601 or 2627 or 51123)
        {
            throw new InvalidOperationException(
                "Đăng ký đã tồn tại. Hãy tải lại danh sách.", ex);
        }
    }

    internal static IQueryable<VCourseEnrollmentStatus> Scoped(
        StudentManagementContext db,
        AccessContext access,
        string function)
    {
        return ScopeRows(
            db.VCourseEnrollmentStatuses.AsNoTracking(),
            db.Students,
            db.Classes,
            access,
            function);
    }

    internal static IQueryable<VCourseEnrollmentStatus> ScopeRows(
        IQueryable<VCourseEnrollmentStatus> rows,
        IQueryable<PRN212_GROUP4.Models.Entities.Student> allStudents,
        IQueryable<Class> classes,
        AccessContext access,
        string function)
    {
        var students = AccessService.ScopeStudents(
            allStudents, access, function);

        bool staff = access.CanStaff(function);
        bool own =
            function == "F09" && access.CanStudent(function);
        bool lecturer = access.CanLecturer(function);

        return rows.Where(row =>
            students.Any(student => student.StudentId == row.StudentId)
            &&
            (
                (staff && allStudents.Any(student =>
                    student.StudentId == row.StudentId
                    && student.DepartmentId == access.StaffDepartmentId))
                ||
                (own && row.StudentId == access.StudentId)
                ||
                (lecturer && classes.Any(@class =>
                    @class.ClassId == row.ClassId
                    && @class.LecturerId == access.LecturerId))
            ));
    }

    public async Task<List<EnrollmentRow>> ListAsync(
        int studentId = 0,
        int semesterId = 0,
        int classId = 0,
        string status = "ALL")
    {
        if (!new[] { "ALL", "REGISTERED", "DROPPED", "COMPLETED" }
            .Contains(status))
        {
            throw new ArgumentException("Trạng thái không hợp lệ.");
        }

        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, "F09");

        return await Scoped(db, access, "F09")
            .Where(row =>
                (studentId == 0 || row.StudentId == studentId)
                && (semesterId == 0 || row.SemesterId == semesterId)
                && (classId == 0 || row.ClassId == classId)
                && (status == "ALL" || row.RegistrationStatus == status))
            .OrderBy(row => row.StudentCode)
            .ThenBy(row => row.SemesterCode)
            .ThenBy(row => row.CourseCode)
            .Select(row => new EnrollmentRow
            {
                RegistrationId = row.CourseEnrollmentId,
                StudentId = row.StudentId,
                SemesterId = row.SemesterId,
                ClassId = row.ClassId,
                StudentCode = row.StudentCode,
                StudentName = row.FullName,
                Course = row.CourseCode + " - " + row.CourseName,
                Semester = row.SemesterCode,
                Status = row.RegistrationStatus,
                Class = row.ClassCode ?? "Chưa xếp lớp"
            })
            .ToListAsync();
    }

    public async Task<(
        List<Lookup> Students,
        List<Lookup> Courses,
        List<Lookup> Semesters,
        List<Lookup> Classes)> LookupsAsync(string function = "F09")
    {
        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, function);

        var students = await AccessService.Students(db, access, function)
            .OrderBy(student => student.StudentCode)
            .Select(student => new Lookup(
                student.StudentId,
                student.StudentCode + " - " + student.FullName))
            .ToListAsync();

        var courses = await db.Courses
            .Where(course => course.IsActive)
            .OrderBy(course => course.CourseCode)
            .Select(course => new Lookup(
                course.CourseId,
                course.CourseCode + " - " + course.CourseName))
            .ToListAsync();

        var semesters = await db.Semesters
            .OrderByDescending(semester => semester.StartDate)
            .Select(semester => new Lookup(
                semester.SemesterId,
                semester.SemesterCode))
            .ToListAsync();

        var rows = Scoped(db, access, function);

        var classes = await db.Classes
            .Where(@class => rows.Any(row => row.ClassId == @class.ClassId))
            .OrderBy(@class => @class.ClassCode)
            .Select(@class => new Lookup(
                @class.ClassId,
                @class.ClassCode))
            .ToListAsync();

        return (students, courses, semesters, classes);
    }

    public async Task<List<Lookup>> RegistrationStudentsAsync()
    {
        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, "F08");

        return await AccessService.Students(db, access, "F08")
            .Where(student => student.StudentStatus == "ACTIVE")
            .OrderBy(student => student.StudentCode)
            .Select(student => new Lookup(
                student.StudentId,
                student.StudentCode + " - " + student.FullName))
            .ToListAsync();
    }
}