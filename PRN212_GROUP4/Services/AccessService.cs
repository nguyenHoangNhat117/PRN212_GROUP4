using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

public static class AppSession
{
    public static int? UserId { get; private set; }
    public static long Version { get; private set; }
    public static AccessContext? DisplayAccess { get; private set; }

    internal static void SignIn(int id, AccessContext access)
    {
        Version++;
        UserId = id;
        DisplayAccess = access;
    }

    public static void Clear()
    {
        Version++;
        UserId = null;
        DisplayAccess = null;
    }
}

public sealed record AccessContext(
    int UserId,
    string DisplayName,
    HashSet<string> Functions,
    int? StudentId,
    int? LecturerId,
    int? StaffDepartmentId,
    HashSet<string> StudentFunctions,
    HashSet<string> LecturerFunctions,
    HashSet<string> StaffFunctions)
{
    public bool Can(string function) => Functions.Contains(function);

    public bool CanStudent(string function) =>
        StudentId != null && StudentFunctions.Contains(function);

    public bool CanLecturer(string function) =>
        LecturerId != null && LecturerFunctions.Contains(function);

    public bool CanStaff(string function) =>
        StaffDepartmentId != null && StaffFunctions.Contains(function);
}

public static class AccessService
{
    public static readonly IReadOnlyDictionary<string, string[]> Baseline =
        new Dictionary<string, string[]>
        {
            ["STUDENT"] = new[] { "F03", "F08", "F09", "F14" },
            ["LECTURER"] = new[] { "F09", "F16" },
            ["ACADEMIC_STAFF"] =
                new[] { "F03", "F05", "F08", "F09", "F14", "F16" }
        };

    internal static async Task<AccessContext> LoadAsync(
        StudentManagementContext db,
        int id)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(x => x.Roles)
                .ThenInclude(x => x.Permissions)
            .Include(x => x.Student)
            .Include(x => x.Lecturer)
            .Include(x => x.AcademicStaff)
            .SingleOrDefaultAsync(x => x.UserId == id && x.IsActive);

        return FromUser(user);
    }

    internal static AccessContext FromUser(User? user)
    {
        if (user == null || !user.IsActive || user.MustChangePassword)
        {
            throw new UnauthorizedAccessException(
                "Phiên không hợp lệ hoặc cần đổi mật khẩu. Hãy đăng nhập lại.");
        }

        var functions = user.Roles
            .SelectMany(role => role.Permissions
                .Where(permission =>
                    Baseline.TryGetValue(role.RoleCode, out var allowed)
                    && allowed.Contains(permission.FunctionCode))
                .Select(permission => permission.FunctionCode))
            .ToHashSet();

        HashSet<string> Granted(string roleCode)
        {
            return user.Roles
                .Where(role => role.RoleCode == roleCode)
                .SelectMany(role => role.Permissions)
                .Where(permission =>
                    Baseline[roleCode].Contains(permission.FunctionCode))
                .Select(permission => permission.FunctionCode)
                .ToHashSet();
        }

        bool isStaff =
            user.Roles.Any(role => role.RoleCode == "ACADEMIC_STAFF")
            && user.AcademicStaff?.IsActive == true;

        int? studentId =
            user.Roles.Any(role => role.RoleCode == "STUDENT")
                ? user.Student?.StudentId
                : null;

        int? lecturerId =
            user.Roles.Any(role => role.RoleCode == "LECTURER")
            && user.Lecturer?.IsActive == true
                ? user.Lecturer.LecturerId
                : null;

        return new AccessContext(
            user.UserId,
            user.DisplayName,
            functions,
            studentId,
            lecturerId,
            isStaff ? user.AcademicStaff?.DepartmentId : null,
            Granted("STUDENT"),
            Granted("LECTURER"),
            Granted("ACADEMIC_STAFF"));
    }

    public static async Task<AccessContext> RequireAsync(
        StudentManagementContext db,
        string function)
    {
        if (AppSession.UserId is not int id)
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
        }

        long version = AppSession.Version;
        var access = await LoadAsync(db, id);
        if (AppSession.Version != version || AppSession.UserId != id)
        {
            throw new UnauthorizedAccessException("Phiên đã thay đổi. Vui lòng đăng nhập lại.");
        }

        if (!access.Can(function))
        {
            throw new UnauthorizedAccessException(
                "Bạn không có quyền " + function + ".");
        }

        return access;
    }

    public static IQueryable<Student> Students(
        StudentManagementContext db,
        AccessContext access,
        string function)
    {
        return ScopeStudents(db.Students, access, function);
    }

    internal static IQueryable<Student> ScopeStudents(
        IQueryable<Student> source,
        AccessContext access,
        string function)
    {
        bool allowOwn = function is "F03" or "F08" or "F09" or "F14";
        bool allowLecturer = function is "F09" or "F16";

        bool staffAllowed = access.CanStaff(function);
        bool ownAllowed = allowOwn && access.CanStudent(function);
        bool lecturerAllowed =
            allowLecturer && access.CanLecturer(function);

        return source.Where(student =>
            (staffAllowed
                && student.DepartmentId == access.StaffDepartmentId)
            ||
            (ownAllowed
                && student.StudentId == access.StudentId)
            ||
            (lecturerAllowed
                && student.Enrollments.Any(enrollment =>
                    enrollment.Class.LecturerId == access.LecturerId
                    && enrollment.EnrollmentStatus != "DROPPED")));
    }

    public static async Task EnsureStudentAsync(
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
}