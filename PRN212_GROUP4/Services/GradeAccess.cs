using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

/// <summary>
/// Phạm vi dữ liệu cho F11–F15 (Student: của mình; Lecturer: lớp phụ trách;
/// Academic Staff: theo khoa được cấp, cùng quy ước với AccessService).
/// AccessService.ScopeStudents chỉ biết F03/F08/F09/F16 nên module điểm
/// dùng bộ lọc riêng này, không sửa hàm dùng chung.
/// </summary>
internal static class GradeAccess
{
    /// <summary>Các lớp xếp (Enrollments, không gồm DROPPED) trong phạm vi.</summary>
    internal static IQueryable<Enrollment> Enrollments(
        StudentManagementContext db,
        AccessContext access,
        string function)
    {
        bool staff = access.CanStaff(function);
        bool own = access.CanStudent(function);
        bool lecturer = access.CanLecturer(function);

        int? departmentId = access.StaffDepartmentId;
        int? studentId = access.StudentId;
        int? lecturerId = access.LecturerId;

        return db.Enrollments.Where(enrollment =>
            enrollment.EnrollmentStatus != "DROPPED"
            &&
            (
                (staff && enrollment.Student.DepartmentId == departmentId)
                || (own && enrollment.StudentId == studentId)
                || (lecturer && enrollment.Class.LecturerId == lecturerId)
            ));
    }

    /// <summary>Sinh viên trong phạm vi (dùng cho F14, F15).</summary>
    internal static IQueryable<PRN212_GROUP4.Models.Entities.Student> Students(
        StudentManagementContext db,
        AccessContext access,
        string function)
    {
        bool staff = access.CanStaff(function);
        bool own = access.CanStudent(function);
        bool lecturer = access.CanLecturer(function);

        int? departmentId = access.StaffDepartmentId;
        int? studentId = access.StudentId;
        int? lecturerId = access.LecturerId;

        return db.Students.Where(student =>
            (staff && student.DepartmentId == departmentId)
            || (own && student.StudentId == studentId)
            || (lecturer && student.Enrollments.Any(enrollment =>
                enrollment.Class.LecturerId == lecturerId
                && enrollment.EnrollmentStatus != "DROPPED")));
    }

    internal static async Task EnsureStudentAsync(
        StudentManagementContext db,
        AccessContext access,
        int studentId,
        string function)
    {
        bool allowed = await Students(db, access, function)
            .AnyAsync(student => student.StudentId == studentId);

        if (!allowed)
        {
            throw new UnauthorizedAccessException(
                "Sinh viên không tồn tại hoặc nằm ngoài phạm vi được cấp.");
        }
    }

    internal static void RequireLecturer(
        AccessContext access,
        string function)
    {
        if (!access.CanLecturer(function))
        {
            throw new UnauthorizedAccessException(
                "Chỉ giảng viên đang hoạt động mới thực hiện được "
                + function + ".");
        }
    }
}

