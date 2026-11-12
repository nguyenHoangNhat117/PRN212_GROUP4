using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models.Entities;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.Extensions.Services;

public sealed record FeaturePermission(string Code, string Name);
public sealed record StudentSearchRow(int StudentId, string StudentCode, string FullName,
    string? Department, string? Email, string? Phone, string Status, string Classes);
public sealed record CourseRow(int CourseId, string CourseCode, string CourseName,
    int Credits, string? Department, bool IsActive);
public sealed record ClassRow(int ClassId, string ClassCode, string Course,
    string Semester, string? Lecturer, int? Capacity, int ActiveCount,
    string? ScheduleNote, string Status, int CourseId, int SemesterId, int? LecturerId);
public sealed record LookupItem(int Id, string Name);
public sealed record RegistrationRow(int CourseEnrollmentId, string StudentCode,
    string StudentName, string CourseCode, string CourseName, string SemesterCode,
    string Status, int CourseId, int SemesterId);
public sealed record UserAccountRow(int UserId, string Username, string DisplayName,
    string? Email, bool IsActive, string Roles, int PrimaryRoleId);
public sealed class RolePermissionRow
{
    public int PermissionId { get; set; }
    public string FunctionCode { get; set; } = "";
    public string FunctionName { get; set; } = "";
    public bool Granted { get; set; }
}

/// <summary>
/// Data operations for F04, F06, F07, F10, F17 and F18.
/// Every operation re-checks its permission in the database; the UI is not a security boundary.
/// </summary>
public sealed class FeatureDataService
{
    private const int PasswordIterations = 210000;

    private static async Task<User> RequireAsync(StudentManagementContext db, string function)
    {
        if (AppSession.UserId is not int userId)
            throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");

        var user = await db.Users
            .Include(x => x.Roles).ThenInclude(x => x.Permissions)
            .Include(x => x.AcademicStaff)
            .Include(x => x.Lecturer)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive);

        if (user == null)
            throw new UnauthorizedAccessException("Tài khoản không còn hoạt động.");

        bool granted = user.Roles.Any(role => role.Permissions.Any(p => p.FunctionCode == function));
        if (!granted)
            throw new UnauthorizedAccessException($"Tài khoản không được cấp quyền {function}.");

        bool hasAdminRole = user.Roles.Any(r => r.RoleCode is "ADMIN" or "SYSTEM_ADMINISTRATOR");
        bool hasStaffRole = user.Roles.Any(r => r.RoleCode == "ACADEMIC_STAFF") && user.AcademicStaff?.IsActive == true;
        bool hasLecturerRole = user.Roles.Any(r => r.RoleCode == "LECTURER") && user.Lecturer?.IsActive == true;

        if (function is "F17" or "F18" or "F19")
        {
            if (!hasAdminRole)
                throw new UnauthorizedAccessException("Chức năng này chỉ dành cho System Administrator.");
        }
        else if (function is "F06" or "F07" or "F10")
        {
            if (!hasStaffRole)
                throw new UnauthorizedAccessException("Chức năng này chỉ dành cho Academic Staff đang hoạt động.");
            if (user.AcademicStaff?.DepartmentId == null)
                throw new UnauthorizedAccessException("Tài khoản học vụ chưa được gán khoa.");
        }
        else if (function == "F04" && !hasStaffRole && !hasLecturerRole)
        {
            throw new UnauthorizedAccessException("Chức năng tìm kiếm chỉ dành cho Academic Staff hoặc Lecturer.");
        }
        return user;
    }

    public async Task EnsurePermissionAsync(string functionCode)
    {
        await using var db = new StudentManagementContext();
        await RequireAsync(db, functionCode);
    }

    public async Task<HashSet<string>> GetAvailableFeaturesAsync()
    {
        if (AppSession.UserId is not int userId)
            throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
        await using var db = new StudentManagementContext();
        var user = await db.Users.AsNoTracking()
            .Include(x => x.Roles).ThenInclude(x => x.Permissions)
            .Include(x => x.AcademicStaff).Include(x => x.Lecturer)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (user == null) throw new UnauthorizedAccessException("Tài khoản không hoạt động.");
        var codes = user.Roles.SelectMany(r => r.Permissions).Select(p => p.FunctionCode).ToHashSet();
        bool admin = user.Roles.Any(r => r.RoleCode is "ADMIN" or "SYSTEM_ADMINISTRATOR");
        bool staff = user.Roles.Any(r => r.RoleCode == "ACADEMIC_STAFF") && user.AcademicStaff?.IsActive == true;
        bool lecturer = user.Roles.Any(r => r.RoleCode == "LECTURER") && user.Lecturer?.IsActive == true;
        if (!admin) codes.RemoveWhere(x => x is "F17" or "F18" or "F19");
        if (!staff) codes.RemoveWhere(x => x is "F06" or "F07" or "F10");
        if (!staff && !lecturer) codes.Remove("F04");
        return codes;
    }

    public async Task<List<StudentSearchRow>> SearchStudentsAsync(string keyword)
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F04");
        var query = db.Students.AsNoTracking().Include(s => s.Department).AsQueryable();
        var staff = user.Roles.Any(r => r.RoleCode == "ACADEMIC_STAFF");
        if (staff)
        {
            if (user.AcademicStaff?.DepartmentId is not int departmentId)
                throw new UnauthorizedAccessException("Tài khoản học vụ chưa được gán khoa.");
            query = query.Where(s => s.DepartmentId == departmentId);
        }
        else
        {
            if (user.Lecturer?.LecturerId is not int lecturerId)
                throw new UnauthorizedAccessException("Tài khoản giảng viên chưa được liên kết hồ sơ Lecturer.");
            query = query.Where(s => s.Enrollments.Any(e => e.Class.LecturerId == lecturerId && e.EnrollmentStatus != "DROPPED"));
        }

        keyword = (keyword ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = keyword;
            int parsedId = int.TryParse(keyword, out var id) ? id : -1;
            query = query.Where(s =>
                s.StudentCode.Contains(pattern) || s.FullName.Contains(pattern) ||
                (s.Email != null && s.Email.Contains(pattern)) ||
                (s.Phone != null && s.Phone.Contains(pattern)) ||
                (s.Department != null && s.Department.DepartmentName.Contains(pattern)) ||
                s.Enrollments.Any(e => e.Class.ClassCode.Contains(pattern)) ||
                s.StudentId == parsedId);
        }

        var rows = await query.Include(s => s.Enrollments).ThenInclude(e => e.Class)
            .OrderBy(s => s.StudentCode).Take(300).ToListAsync();
        return rows.Select(s => new StudentSearchRow(s.StudentId, s.StudentCode, s.FullName,
            s.Department?.DepartmentName, s.Email, s.Phone, s.StudentStatus,
            string.Join(", ", s.Enrollments.Where(e => e.EnrollmentStatus != "DROPPED")
                .Select(e => e.Class.ClassCode).Distinct()))).ToList();
    }

    public async Task<List<CourseRow>> ListCoursesAsync()
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F06");
        int departmentId = user.AcademicStaff!.DepartmentId!.Value;
        return await db.Courses.AsNoTracking().Include(c => c.Department)
            .Where(c => c.DepartmentId == departmentId)
            .OrderBy(c => c.CourseCode)
            .Select(c => new CourseRow(c.CourseId, c.CourseCode, c.CourseName, c.Credits,
                c.Department == null ? null : c.Department.DepartmentName, c.IsActive)).ToListAsync();
    }

    public async Task<List<LookupItem>> DepartmentsAsync()
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F06");
        int departmentId = user.AcademicStaff!.DepartmentId!.Value;
        return await db.Departments.AsNoTracking().Where(x => x.IsActive && x.DepartmentId == departmentId)
            .OrderBy(x => x.DepartmentName).Select(x => new LookupItem(x.DepartmentId, x.DepartmentName)).ToListAsync();
    }

    public async Task SaveCourseAsync(int? courseId, string code, string name, int credits, int? departmentId)
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F06");
        int staffDepartmentId = user.AcademicStaff!.DepartmentId!.Value;
        if (departmentId.HasValue && departmentId.Value != staffDepartmentId)
            throw new UnauthorizedAccessException("Academic Staff chỉ được quản lý môn học thuộc khoa mình.");
        departmentId = staffDepartmentId;
        code = (code ?? "").Trim().ToUpperInvariant(); name = (name ?? "").Trim();
        if (code.Length is < 2 or > 20) throw new ArgumentException("Mã môn học phải có 2–20 ký tự.");
        if (name.Length is < 2 or > 150) throw new ArgumentException("Tên môn học phải có 2–150 ký tự.");
        if (credits is < 1 or > 15) throw new ArgumentException("Số tín chỉ phải từ 1 đến 15.");
        if (await db.Courses.AnyAsync(c => c.CourseCode == code && c.CourseId != (courseId ?? 0)))
            throw new InvalidOperationException("Mã môn học đã tồn tại.");
        if (departmentId.HasValue && !await db.Departments.AnyAsync(d => d.DepartmentId == departmentId && d.IsActive))
            throw new ArgumentException("Khoa được chọn không hợp lệ.");

        Course course;
        if (courseId.HasValue)
        {
            course = await db.Courses.SingleOrDefaultAsync(c => c.CourseId == courseId.Value)
                ?? throw new InvalidOperationException("Không tìm thấy môn học.");
            if (course.DepartmentId != staffDepartmentId)
                throw new UnauthorizedAccessException("Không thể chỉnh sửa môn học ngoài khoa được cấp.");
        }
        else
        {
            course = new Course { IsActive = true };
            db.Courses.Add(course);
        }
        course.CourseCode = code; course.CourseName = name; course.Credits = checked((byte)credits); course.DepartmentId = departmentId;
        await db.SaveChangesAsync();
        await AuditAsync(db, "SAVE_COURSE", "Courses", course.CourseId.ToString(), $"Course {code} saved.");
    }

    public async Task SetCourseActiveAsync(int courseId, bool active)
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F06");
        int departmentId = user.AcademicStaff!.DepartmentId!.Value;
        var course = await db.Courses.SingleOrDefaultAsync(c => c.CourseId == courseId)
            ?? throw new InvalidOperationException("Không tìm thấy môn học.");
        if (course.DepartmentId != departmentId)
            throw new UnauthorizedAccessException("Không thể thay đổi môn học ngoài khoa được cấp.");
        course.IsActive = active;
        await db.SaveChangesAsync();
        await AuditAsync(db, active ? "ACTIVATE_COURSE" : "DEACTIVATE_COURSE", "Courses", courseId.ToString(), course.CourseCode);
    }

    public async Task<(List<LookupItem> Courses, List<LookupItem> Semesters, List<LookupItem> Lecturers)> ClassLookupsAsync()
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F07");
        int departmentId = user.AcademicStaff!.DepartmentId!.Value;
        var courses = await db.Courses.AsNoTracking().Where(x => x.IsActive && x.DepartmentId == departmentId).OrderBy(x => x.CourseCode)
            .Select(x => new LookupItem(x.CourseId, x.CourseCode + " - " + x.CourseName)).ToListAsync();
        var semesters = await db.Semesters.AsNoTracking().OrderByDescending(x => x.StartDate)
            .Select(x => new LookupItem(x.SemesterId, x.SemesterCode + " - " + x.SemesterName)).ToListAsync();
        var lecturers = await db.Lecturers.AsNoTracking().Where(x => x.IsActive && x.DepartmentId == departmentId).OrderBy(x => x.LecturerCode)
            .Select(x => new LookupItem(x.LecturerId, x.LecturerCode + " - " + x.FullName)).ToListAsync();
        return (courses, semesters, lecturers);
    }

    public async Task<List<ClassRow>> ListClassesAsync()
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F07");
        int departmentId = user.AcademicStaff!.DepartmentId!.Value;
        return await db.Classes.AsNoTracking().Include(x => x.Course).Include(x => x.Semester).Include(x => x.Lecturer)
            .Where(x => x.Course.DepartmentId == departmentId)
            .OrderBy(x => x.ClassCode)
            .Select(x => new ClassRow(x.ClassId, x.ClassCode, x.Course.CourseCode + " - " + x.Course.CourseName,
                x.Semester.SemesterCode, x.Lecturer == null ? null : x.Lecturer.FullName, x.Capacity,
                x.Enrollments.Count(e => e.EnrollmentStatus == "ENROLLED" || e.EnrollmentStatus == "COMPLETED"), x.ScheduleNote, x.ClassStatus,
                x.CourseId, x.SemesterId, x.LecturerId)).ToListAsync();
    }

    public async Task SaveClassAsync(int? classId, string code, int courseId, int semesterId,
        int? lecturerId, int? capacity, string? schedule, string status)
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F07");
        int departmentId = user.AcademicStaff!.DepartmentId!.Value;
        code = (code ?? "").Trim().ToUpperInvariant(); schedule = string.IsNullOrWhiteSpace(schedule) ? null : schedule.Trim();
        status = (status ?? "OPEN").Trim().ToUpperInvariant();
        if (code.Length is < 2 or > 30) throw new ArgumentException("Mã lớp phải có 2–30 ký tự.");
        if (!new[] { "OPEN", "CLOSED", "CANCELLED" }.Contains(status)) throw new ArgumentException("Trạng thái lớp không hợp lệ.");
        if (capacity.HasValue && capacity.Value <= 0) throw new ArgumentException("Sĩ số tối đa phải lớn hơn 0.");
        if (schedule?.Length > 250) throw new ArgumentException("Ghi chú lịch học không được quá 250 ký tự.");
        if (await db.Classes.AnyAsync(x => x.ClassCode == code && x.ClassId != (classId ?? 0))) throw new InvalidOperationException("Mã lớp đã tồn tại.");
        if (!await db.Courses.AnyAsync(x => x.CourseId == courseId && x.IsActive && x.DepartmentId == departmentId)) throw new ArgumentException("Môn học không hợp lệ, đã ngừng hoạt động hoặc ngoài khoa được cấp.");
        if (!await db.Semesters.AnyAsync(x => x.SemesterId == semesterId)) throw new ArgumentException("Học kỳ không hợp lệ.");
        if (lecturerId.HasValue && !await db.Lecturers.AnyAsync(x => x.LecturerId == lecturerId && x.IsActive && x.DepartmentId == departmentId)) throw new ArgumentException("Giảng viên không hợp lệ hoặc ngoài khoa được cấp.");
        if (lecturerId.HasValue && !string.IsNullOrWhiteSpace(schedule))
        {
            bool sameSlotNote = await db.Classes.AnyAsync(x => x.ClassId != (classId ?? 0)
                && x.LecturerId == lecturerId && x.SemesterId == semesterId
                && x.ClassStatus == "OPEN" && x.ScheduleNote == schedule);
            if (sameSlotNote)
                throw new InvalidOperationException("Giảng viên đã có lớp khác cùng học kỳ với ghi chú lịch học giống hệt. Hãy kiểm tra xung đột lịch trước khi lưu.");
        }

        Class entity;
        if (classId.HasValue)
        {
            entity = await db.Classes.SingleOrDefaultAsync(x => x.ClassId == classId.Value)
                ?? throw new InvalidOperationException("Không tìm thấy lớp.");
            if (!await db.Courses.AnyAsync(x => x.CourseId == entity.CourseId && x.DepartmentId == departmentId))
                throw new UnauthorizedAccessException("Không thể chỉnh sửa lớp ngoài khoa được cấp.");
            bool hasAssignments = await db.Enrollments.AnyAsync(x => x.ClassId == classId);
            if (hasAssignments && (entity.CourseId != courseId || entity.SemesterId != semesterId))
                throw new InvalidOperationException("Không thể đổi môn học/học kỳ của lớp đã có sinh viên đăng ký.");
            int currentCount = await db.Enrollments.CountAsync(x => x.ClassId == classId
                && (x.EnrollmentStatus == "ENROLLED" || x.EnrollmentStatus == "COMPLETED"));
            if (capacity.HasValue && capacity.Value < currentCount) throw new InvalidOperationException("Sức chứa mới nhỏ hơn số sinh viên đang học.");
        }
        else
        {
            entity = new Class(); db.Classes.Add(entity);
        }
        entity.ClassCode = code; entity.CourseId = courseId; entity.SemesterId = semesterId;
        entity.LecturerId = lecturerId; entity.Capacity = capacity; entity.ScheduleNote = schedule; entity.ClassStatus = status;
        await db.SaveChangesAsync();
        await AuditAsync(db, "SAVE_CLASS", "Classes", entity.ClassId.ToString(), $"Class {code} saved.");
    }

    public async Task<List<RegistrationRow>> ListUnassignedRegistrationsAsync()
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F10");
        int departmentId = user.AcademicStaff!.DepartmentId!.Value;
        return await db.CourseEnrollments.AsNoTracking()
            .Include(x => x.Student).Include(x => x.Course).Include(x => x.Semester)
            .Where(x => x.Student.DepartmentId == departmentId && x.Course.DepartmentId == departmentId
                && x.RegistrationStatus == "REGISTERED"
                && !db.Enrollments.Any(e => e.CourseEnrollmentId == x.CourseEnrollmentId
                    && e.EnrollmentStatus != "DROPPED"))
            .OrderBy(x => x.RegistrationDate)
            .Select(x => new RegistrationRow(x.CourseEnrollmentId, x.Student.StudentCode, x.Student.FullName,
                x.Course.CourseCode, x.Course.CourseName, x.Semester.SemesterCode, x.RegistrationStatus,
                x.CourseId, x.SemesterId)).ToListAsync();
    }

    public async Task<List<LookupItem>> MatchingClassesAsync(int courseId, int semesterId)
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F10");
        int departmentId = user.AcademicStaff!.DepartmentId!.Value;
        return await db.Classes.AsNoTracking()
            .Where(x => x.CourseId == courseId && x.SemesterId == semesterId && x.ClassStatus == "OPEN"
                && x.Course.IsActive && x.Course.DepartmentId == departmentId
                && (!x.Capacity.HasValue || x.Enrollments.Count(e => e.EnrollmentStatus == "ENROLLED" || e.EnrollmentStatus == "COMPLETED") < x.Capacity.Value))
            .OrderBy(x => x.ClassCode).Select(x => new LookupItem(x.ClassId,
                x.ClassCode + " (" + x.Enrollments.Count(e => e.EnrollmentStatus == "ENROLLED" || e.EnrollmentStatus == "COMPLETED") + "/" + (x.Capacity.HasValue ? x.Capacity.Value.ToString() : "∞") + ")"))
            .ToListAsync();
    }

    public async Task AssignStudentAsync(int courseEnrollmentId, int classId)
    {
        await using var db = new StudentManagementContext();
        var user = await RequireAsync(db, "F10");
        int departmentId = user.AcademicStaff!.DepartmentId!.Value;

        // The new database contract centralizes assignment invariants in
        // uspAssignStudentToClass. The application still checks department scope
        // before calling it; SQL then re-checks student, course, semester,
        // class status, capacity, duplicate assignment and grade history.
        var registration = await db.CourseEnrollments.AsNoTracking()
            .Include(x => x.Student).Include(x => x.Course)
            .SingleOrDefaultAsync(x => x.CourseEnrollmentId == courseEnrollmentId)
            ?? throw new InvalidOperationException("Không tìm thấy đăng ký môn học.");

        var target = await db.Classes.AsNoTracking().Include(x => x.Course)
            .SingleOrDefaultAsync(x => x.ClassId == classId)
            ?? throw new InvalidOperationException("Không tìm thấy lớp.");

        if (registration.Student.DepartmentId != departmentId
            || registration.Course.DepartmentId != departmentId
            || target.Course.DepartmentId != departmentId)
        {
            throw new UnauthorizedAccessException(
                "Đăng ký hoặc lớp nằm ngoài khoa được cấp cho Academic Staff.");
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.uspAssignStudentToClass @CourseEnrollmentID={courseEnrollmentId}, @ClassID={classId}");

        await AuditAsync(db, "ASSIGN_STUDENT_TO_CLASS", "Enrollments",
            courseEnrollmentId.ToString(),
            $"Assigned registration {courseEnrollmentId} to class {classId} via uspAssignStudentToClass.");
    }

    public async Task<List<LookupItem>> RolesAsync()
    {
        await using var db = new StudentManagementContext();
        await RequireAsync(db, "F17");
        return await db.Roles.AsNoTracking().OrderBy(x => x.RoleName)
            .Select(x => new LookupItem(x.RoleId, x.RoleCode + " - " + x.RoleName)).ToListAsync();
    }

    public async Task<List<UserAccountRow>> ListUsersAsync()
    {
        await using var db = new StudentManagementContext();
        await RequireAsync(db, "F17");
        var users = await db.Users.AsNoTracking().Include(x => x.Roles).OrderBy(x => x.Username).ToListAsync();
        return users.Select(x => new UserAccountRow(x.UserId, x.Username, x.DisplayName, x.Email, x.IsActive,
            string.Join(", ", x.Roles.Select(r => r.RoleCode)), x.Roles.Select(r => r.RoleId).FirstOrDefault()))
            .ToList();
    }

    public async Task CreateUserAsync(string username, string displayName, string? email,
        string temporaryPassword, int roleId)
    {
        await using var db = new StudentManagementContext();
        await RequireAsync(db, "F17");
        username = (username ?? "").Trim(); displayName = (displayName ?? "").Trim(); email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        ValidatePassword(temporaryPassword);
        if (username.Length is < 3 or > 80) throw new ArgumentException("Username phải có 3–80 ký tự.");
        if (displayName.Length is < 2 or > 120) throw new ArgumentException("Tên hiển thị phải có 2–120 ký tự.");
        if (email != null && email.Length > 255) throw new ArgumentException("Email không được quá 255 ký tự.");
        if (await db.Users.AnyAsync(x => x.Username == username)) throw new InvalidOperationException("Username đã tồn tại.");
        if (email != null && await db.Users.AnyAsync(x => x.Email == email)) throw new InvalidOperationException("Email đã được sử dụng.");
        var role = await db.Roles.SingleOrDefaultAsync(x => x.RoleId == roleId) ?? throw new ArgumentException("Role không hợp lệ.");
        var credentials = HashPassword(temporaryPassword);
        var user = new User
        {
            Username = username, DisplayName = displayName, Email = email, IsActive = true,
            MustChangePassword = true, PasswordHash = credentials.Hash, PasswordSalt = credentials.Salt,
            PasswordAlgorithm = "PBKDF2-SHA256", PasswordIterations = PasswordIterations,
            CreatedAt = DateTime.UtcNow
        };
        user.Roles.Add(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        await AuditAsync(db, "CREATE_USER", "Users", user.UserId.ToString(), $"Created account {username}.");
    }

    public async Task UpdateUserAsync(int userId, string displayName, string? email, int roleId, bool isActive)
    {
        await using var db = new StudentManagementContext();
        var actor = await RequireAsync(db, "F17");
        var user = await db.Users.Include(x => x.Roles).SingleOrDefaultAsync(x => x.UserId == userId)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");
        if (actor.UserId == userId && !isActive) throw new InvalidOperationException("Không thể tự vô hiệu hóa tài khoản đang đăng nhập.");
        displayName = (displayName ?? "").Trim(); email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (displayName.Length is < 2 or > 120) throw new ArgumentException("Tên hiển thị phải có 2–120 ký tự.");
        if (email != null && email.Length > 255) throw new ArgumentException("Email không được quá 255 ký tự.");
        if (email != null && await db.Users.AnyAsync(x => x.Email == email && x.UserId != userId)) throw new InvalidOperationException("Email đã được sử dụng.");
        var role = await db.Roles.SingleOrDefaultAsync(x => x.RoleId == roleId) ?? throw new ArgumentException("Role không hợp lệ.");
        bool wasAdmin = user.Roles.Any(r => r.RoleCode is "ADMIN" or "SYSTEM_ADMINISTRATOR");
        bool staysAdmin = role.RoleCode is "ADMIN" or "SYSTEM_ADMINISTRATOR";
        if (wasAdmin && (!isActive || !staysAdmin)) await EnsureAnotherActiveAdminAsync(db, userId);
        user.DisplayName = displayName; user.Email = email; user.IsActive = isActive;
        user.Roles.Clear(); user.Roles.Add(role);
        await db.SaveChangesAsync();
        await AuditAsync(db, "UPDATE_USER", "Users", userId.ToString(), $"Updated account {user.Username}.");
    }

    public async Task ResetUserPasswordAsync(int userId, string temporaryPassword)
    {
        await using var db = new StudentManagementContext();
        await RequireAsync(db, "F17");
        ValidatePassword(temporaryPassword);
        var user = await db.Users.SingleOrDefaultAsync(x => x.UserId == userId) ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");
        var credentials = HashPassword(temporaryPassword);
        user.PasswordHash = credentials.Hash; user.PasswordSalt = credentials.Salt;
        user.PasswordAlgorithm = "PBKDF2-SHA256"; user.PasswordIterations = PasswordIterations;
        user.MustChangePassword = true; user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await AuditAsync(db, "RESET_USER_PASSWORD", "Users", userId.ToString(), "Temporary password reset; change required at next login.");
    }

    public async Task<List<LookupItem>> ListRolesForPermissionsAsync()
    {
        await using var db = new StudentManagementContext();
        await RequireAsync(db, "F18");
        return await db.Roles.AsNoTracking().OrderBy(r => r.RoleCode)
            .Select(r => new LookupItem(r.RoleId, r.RoleCode + " - " + r.RoleName)).ToListAsync();
    }

    public async Task<List<RolePermissionRow>> ListRolePermissionsAsync(int roleId)
    {
        await using var db = new StudentManagementContext();
        await RequireAsync(db, "F18");
        var role = await db.Roles.Include(r => r.Permissions).SingleOrDefaultAsync(r => r.RoleId == roleId)
            ?? throw new InvalidOperationException("Không tìm thấy role.");
        var granted = role.Permissions.Select(p => p.PermissionId).ToHashSet();
        return await db.Permissions.AsNoTracking().OrderBy(p => p.FunctionCode)
            .Select(p => new RolePermissionRow { PermissionId = p.PermissionId, FunctionCode = p.FunctionCode,
                FunctionName = p.FunctionName, Granted = granted.Contains(p.PermissionId) })
            .ToListAsync();
    }

    public async Task SaveRolePermissionsAsync(int roleId, IEnumerable<RolePermissionRow> rows)
    {
        await using var db = new StudentManagementContext();
        await RequireAsync(db, "F18");
        var role = await db.Roles.Include(r => r.Permissions).SingleOrDefaultAsync(r => r.RoleId == roleId)
            ?? throw new InvalidOperationException("Không tìm thấy role.");
        var desired = rows.Where(r => r.Granted).Select(r => r.PermissionId).ToHashSet();
        var permissionIds = await db.Permissions.Select(p => p.PermissionId).ToListAsync();
        if (desired.Except(permissionIds).Any()) throw new InvalidOperationException("Danh sách quyền không hợp lệ.");
        if (role.RoleCode is "ADMIN" or "SYSTEM_ADMINISTRATOR")
        {
            var critical = await db.Permissions.Where(p => p.FunctionCode == "F01" || p.FunctionCode == "F18")
                .Select(p => p.PermissionId).ToListAsync();
            if (critical.Any(id => !desired.Contains(id)))
                throw new InvalidOperationException("Không thể gỡ quyền đăng nhập hoặc quản trị phân quyền khỏi role quản trị viên.");
        }
        role.Permissions.Clear();
        foreach (var id in desired)
        {
            var permission = await db.Permissions.SingleAsync(p => p.PermissionId == id);
            role.Permissions.Add(permission);
        }
        await db.SaveChangesAsync();
        await AuditAsync(db, "UPDATE_ROLE_PERMISSIONS", "Roles", roleId.ToString(), $"Updated permissions for {role.RoleCode}.");
    }

    private static async Task EnsureAnotherActiveAdminAsync(StudentManagementContext db, int excludedUserId)
    {
        bool another = await db.Users.AnyAsync(u => u.UserId != excludedUserId && u.IsActive
            && u.Roles.Any(r => (r.RoleCode == "ADMIN" || r.RoleCode == "SYSTEM_ADMINISTRATOR")
                && r.Permissions.Any(p => p.FunctionCode == "F18")));
        if (!another) throw new InvalidOperationException("Không thể vô hiệu hóa/chuyển role của System Administrator cuối cùng.");
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 128)
            throw new ArgumentException("Mật khẩu tạm phải có 8–128 ký tự.");
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, 32);
        return (Convert.ToHexString(hash), Convert.ToHexString(salt));
    }

    private static async Task AuditAsync(StudentManagementContext db, string action, string entity, string key, string details)
    {
        db.AuditLogs.Add(new AuditLog { ActorUserId = AppSession.UserId, ActionName = action,
            EntityName = entity, EntityKey = key, Details = details, HappenedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
}
