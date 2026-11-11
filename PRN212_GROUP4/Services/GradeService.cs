using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

/// <summary>
/// F11 Record Grades, F12 Update Grades, F13 View Grades.
/// Chủ chức năng: Nguyễn Tấn Lực (CE190710).
/// Nhận EnrollmentID từ F10; Grades.EnrollmentID tham chiếu Enrollments.EnrollmentID.
/// </summary>
public sealed class GradeService
{
    private static readonly string[] ViewFunctions = { "F11", "F12", "F13" };

    // ------------------------------------------------------------------
    // Danh mục cho bộ lọc
    // ------------------------------------------------------------------

    public async Task<GradeLookups> LookupsAsync(string function)
    {
        EnsureKnown(function);

        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, function);

        var enrollments = GradeAccess.Enrollments(db, access, function);

        var students = await db.Students
            .Where(student => enrollments.Any(
                enrollment => enrollment.StudentId == student.StudentId))
            .OrderBy(student => student.StudentCode)
            .Select(student => new Lookup(
                student.StudentId,
                student.StudentCode + " - " + student.FullName))
            .ToListAsync();

        var semesters = await db.Semesters
            .Where(semester => enrollments.Any(
                enrollment => enrollment.Class.SemesterId
                    == semester.SemesterId))
            .OrderByDescending(semester => semester.StartDate)
            .Select(semester => new Lookup(
                semester.SemesterId,
                semester.SemesterCode))
            .ToListAsync();

        var classes = await db.Classes
            .Where(@class => enrollments.Any(
                enrollment => enrollment.ClassId == @class.ClassId))
            .OrderBy(@class => @class.ClassCode)
            .Select(@class => new Lookup(
                @class.ClassId,
                @class.ClassCode))
            .ToListAsync();

        return new GradeLookups(students, semesters, classes);
    }

    // ------------------------------------------------------------------
    // F11: danh sách sinh viên của lớp chưa có điểm
    // ------------------------------------------------------------------

    public async Task<List<GradeRow>> ListUngradedAsync(int classId)
    {
        if (classId <= 0)
        {
            throw new ArgumentException("Chọn lớp.");
        }

        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, "F11");
        GradeAccess.RequireLecturer(access, "F11");

        var rows = await Project(
                GradeAccess.Enrollments(db, access, "F11")
                    .Where(enrollment =>
                        enrollment.ClassId == classId
                        && enrollment.Grade == null
                        && enrollment.Class.ClassStatus != "CANCELLED"))
            .ToListAsync();

        return rows;
    }

    // ------------------------------------------------------------------
    // F12 / F13: danh sách điểm đã có
    // ------------------------------------------------------------------

    /// <param name="publication">ALL, PUBLISHED hoặc DRAFT.</param>
    public async Task<List<GradeRow>> ListAsync(
        string function,
        int studentId = 0,
        int semesterId = 0,
        int classId = 0,
        string publication = "ALL")
    {
        if (function is not ("F12" or "F13"))
        {
            throw new ArgumentException("Chức năng không hợp lệ.");
        }

        if (publication is not ("ALL" or "PUBLISHED" or "DRAFT"))
        {
            throw new ArgumentException("Trạng thái công bố không hợp lệ.");
        }

        using var db = new StudentManagementContext();
        var access = await AccessService.RequireAsync(db, function);

        // Lecturer thấy cả điểm chưa công bố của lớp mình phụ trách.
        // Student và Academic Staff chỉ thấy điểm đã công bố.
        bool lecturer = access.CanLecturer(function);
        int? lecturerId = access.LecturerId;

        var query = GradeAccess.Enrollments(db, access, function)
            .Where(enrollment => enrollment.Grade != null)
            .Where(enrollment =>
                (lecturer && enrollment.Class.LecturerId == lecturerId)
                || enrollment.Grade!.IsPublished)
            .Where(enrollment =>
                (studentId == 0 || enrollment.StudentId == studentId)
                && (semesterId == 0
                    || enrollment.Class.SemesterId == semesterId)
                && (classId == 0 || enrollment.ClassId == classId)
                && (publication == "ALL"
                    || (publication == "PUBLISHED"
                        && enrollment.Grade!.IsPublished)
                    || (publication == "DRAFT"
                        && !enrollment.Grade!.IsPublished)));

        var rows = await Project(query).ToListAsync();

        var scale = await db.GradeScales
            .AsNoTracking()
            .Where(band => band.IsActive)
            .ToListAsync();

        foreach (var row in rows)
        {
            row.Letter =
                GradePolicy.Band(row.Score, scale)?.GradeLetter ?? "";
        }

        return rows;
    }

    // ------------------------------------------------------------------
    // F11: nhập điểm mới
    // ------------------------------------------------------------------

    /// <param name="markCompleted">
    /// Đánh dấu lớp xếp + đăng ký môn là COMPLETED để điểm được tính vào GPA
    /// (vStudentCourseResults chỉ lấy COMPLETED + đã công bố).
    /// Quy tắc này chưa được nhóm chốt nên mặc định tắt trên giao diện.
    /// </param>
    public async Task<int> RecordAsync(
        int enrollmentId,
        decimal score,
        bool publish,
        bool markCompleted)
    {
        var problem = GradePolicy.Validate(score);

        if (problem != null)
        {
            throw new ArgumentException(problem);
        }

        using var db = new StudentManagementContext();
        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F11");
        GradeAccess.RequireLecturer(access, "F11");

        var enrollment = await GradeAccess
            .Enrollments(db, access, "F11")
            .Include(item => item.Grade)
            .Include(item => item.Class)
            .Include(item => item.CourseEnrollment)
            .SingleOrDefaultAsync(item =>
                item.EnrollmentId == enrollmentId);

        if (enrollment == null)
        {
            throw new InvalidOperationException(
                "Sinh viên không thuộc lớp bạn phụ trách "
                + "hoặc không còn được xếp lớp.");
        }

        if (enrollment.Class.ClassStatus == "CANCELLED")
        {
            throw new InvalidOperationException(
                "Lớp đã bị hủy, không thể nhập điểm.");
        }

        if (enrollment.Grade != null)
        {
            throw new InvalidOperationException(
                "Sinh viên đã có điểm. Dùng chức năng Sửa điểm (F12).");
        }

        var grade = new Grade
        {
            EnrollmentId = enrollment.EnrollmentId,
            FinalScore = score,
            IsPublished = publish,
            UpdatedByUserId = access.UserId,
            UpdatedAt = DateTime.UtcNow
        };

        db.Grades.Add(grade);

        if (markCompleted)
        {
            MarkCompleted(enrollment);
        }

        try
        {
            await db.SaveChangesAsync();

            db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = access.UserId,
                ActionName = "GRADE_RECORD",
                EntityName = "Grades",
                EntityKey = grade.GradeId.ToString(),
                Details = FormattableString.Invariant(
                    $"EnrollmentID={enrollment.EnrollmentId}; score={score:0.00}; published={publish}; completed={markCompleted}")
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException
            { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException(
                "Sinh viên vừa được nhập điểm bởi người khác. "
                + "Hãy tải lại danh sách và dùng Sửa điểm (F12).", ex);
        }

        return grade.GradeId;
    }

    // ------------------------------------------------------------------
    // F12: sửa điểm hiện có
    // ------------------------------------------------------------------

    public async Task UpdateAsync(
        int enrollmentId,
        byte[]? rowVersion,
        decimal newScore,
        bool publish,
        bool markCompleted)
    {
        var problem = GradePolicy.Validate(newScore);

        if (problem != null)
        {
            throw new ArgumentException(problem);
        }

        using var db = new StudentManagementContext();
        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F12");
        GradeAccess.RequireLecturer(access, "F12");

        var enrollment = await GradeAccess
            .Enrollments(db, access, "F12")
            .Include(item => item.Grade)
            .Include(item => item.Class)
            .Include(item => item.CourseEnrollment)
            .SingleOrDefaultAsync(item =>
                item.EnrollmentId == enrollmentId);

        if (enrollment?.Grade == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy bản ghi điểm trong lớp bạn phụ trách. "
                + "Hãy tải lại danh sách.");
        }

        if (enrollment.Class.ClassStatus == "CANCELLED")
        {
            throw new InvalidOperationException(
                "Lớp đã bị hủy, không thể sửa điểm.");
        }

        var grade = enrollment.Grade;

        bool completeNow =
            markCompleted && enrollment.EnrollmentStatus == "ENROLLED";

        if (grade.FinalScore == newScore
            && grade.IsPublished == publish
            && !completeNow)
        {
            throw new InvalidOperationException(
                "Không có thay đổi nào để lưu.");
        }

        // So sánh với phiên bản người dùng đang thấy trên màn hình.
        if (rowVersion != null)
        {
            db.Entry(grade).Property(item => item.RowVer)
                .OriginalValue = rowVersion;
        }

        decimal oldScore = grade.FinalScore;
        bool oldPublished = grade.IsPublished;

        grade.FinalScore = newScore;
        grade.IsPublished = publish;
        grade.UpdatedByUserId = access.UserId;
        grade.UpdatedAt = DateTime.UtcNow;

        if (completeNow)
        {
            MarkCompleted(enrollment);
        }

        try
        {
            await db.SaveChangesAsync();

            db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = access.UserId,
                ActionName = "GRADE_UPDATE",
                EntityName = "Grades",
                EntityKey = grade.GradeId.ToString(),
                Details = FormattableString.Invariant(
                    $"EnrollmentID={enrollment.EnrollmentId}; score {oldScore:0.00}->{newScore:0.00}; published {oldPublished}->{publish}; completed={completeNow}")
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "Điểm vừa được thay đổi hoặc bản ghi không còn tồn tại. "
                + "Điểm đang lưu không bị ghi đè; hãy tải lại và thử lại.",
                ex);
        }
    }

    // ------------------------------------------------------------------
    // Hỗ trợ
    // ------------------------------------------------------------------

    private static void EnsureKnown(string function)
    {
        if (!ViewFunctions.Contains(function))
        {
            throw new ArgumentException("Chức năng không hợp lệ.");
        }
    }

    private static void MarkCompleted(Enrollment enrollment)
    {
        if (enrollment.EnrollmentStatus == "ENROLLED")
        {
            enrollment.EnrollmentStatus = "COMPLETED";
        }

        if (enrollment.CourseEnrollment.RegistrationStatus == "REGISTERED")
        {
            enrollment.CourseEnrollment.RegistrationStatus = "COMPLETED";
        }
    }

    private static IQueryable<GradeRow> Project(
        IQueryable<Enrollment> source)
    {
        return source
            .OrderBy(enrollment => enrollment.Student.StudentCode)
            .ThenBy(enrollment => enrollment.Class.Semester.SemesterCode)
            .ThenBy(enrollment => enrollment.Class.Course.CourseCode)
            .Select(enrollment => new GradeRow
            {
                EnrollmentId = enrollment.EnrollmentId,
                GradeId = enrollment.Grade == null
                    ? (int?)null
                    : enrollment.Grade.GradeId,
                StudentId = enrollment.StudentId,
                ClassId = enrollment.ClassId,
                StudentCode = enrollment.Student.StudentCode,
                StudentName = enrollment.Student.FullName,
                Course = enrollment.Class.Course.CourseCode
                    + " - " + enrollment.Class.Course.CourseName,
                Credits = enrollment.Class.Course.Credits,
                Class = enrollment.Class.ClassCode,
                Semester = enrollment.Class.Semester.SemesterCode,
                EnrollmentStatus = enrollment.EnrollmentStatus,
                Score = enrollment.Grade == null
                    ? (decimal?)null
                    : enrollment.Grade.FinalScore,
                IsPublished = enrollment.Grade != null
                    && enrollment.Grade.IsPublished,
                RowVer = enrollment.Grade == null
                    ? null
                    : enrollment.Grade.RowVer
            });
    }
}

