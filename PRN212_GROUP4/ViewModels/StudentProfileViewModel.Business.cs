using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Services;
using E = PRN212_GROUP4.Models.Entities;
using D = PRN212_GROUP4.Models;

namespace PRN212_GROUP4.ViewModels;

public class StudentProfileViewModel : ViewModelBase
{
    private readonly Func<E.StudentManagementContext> createDb;

    public StudentProfileViewModel(
        Func<E.StudentManagementContext>? createDb = null)
    {
        this.createDb =
            createDb ?? (() => new E.StudentManagementContext());
    }

    public async Task<List<D.Student>> GetAsync()
    {
        using var db = createDb();

        var access = await AccessService.RequireAsync(db, "F03");

        var rows = await AccessService
            .Students(db, access, "F03")
            .AsNoTracking()
            .Include(student => student.Department)
            .Include(student => student.Major)
            .OrderBy(student => student.StudentCode)
            .ToListAsync();

        return rows.Select(StudentMapping.ToDto).ToList();
    }

    public async Task<List<D.Department>> DepartmentsAsync()
    {
        using var db = createDb();

        var access = await AccessService.RequireAsync(db, "F03");

        return await db.Departments
            .AsNoTracking()
            .Where(department =>
                department.IsActive
                && department.DepartmentId == access.StaffDepartmentId)
            .Select(department => new D.Department
            {
                DepartmentID = department.DepartmentId,
                DepartmentCode = department.DepartmentCode,
                DepartmentName = department.DepartmentName,
                IsActive = department.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<D.Lookup>> MajorsAsync(int departmentId)
    {
        using var db = createDb();

        var access = await AccessService.RequireAsync(db, "F03");

        if (!access.CanStaff("F03")
            || departmentId != access.StaffDepartmentId)
        {
            throw new UnauthorizedAccessException(
                "Ngành học nằm ngoài khoa phụ trách.");
        }

        return await db.Majors
            .AsNoTracking()
            .Where(major =>
                major.IsActive
                && major.DepartmentId == departmentId
                && major.Department.IsActive)
            .OrderBy(major => major.MajorCode)
            .Select(major => new D.Lookup(
                major.MajorId,
                major.MajorCode + " - " + major.MajorName))
            .ToListAsync();
    }

    public async Task<int> AddAsync(D.Student student)
    {
        StudentValidation.ValidateStudent(student, true);

        NormalizeExtended(student);
        RequireCompleteProfile(student);

        using var db = createDb();

        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F05");

        if (!access.CanStaff("F05")
            || student.DepartmentID != access.StaffDepartmentId)
        {
            throw new UnauthorizedAccessException(
                "Chỉ thêm sinh viên vào khoa được cấp.");
        }

        await CheckMajorAsync(db, student, requireActive: true);

        if (await db.Students.AnyAsync(
            existing => existing.StudentCode == student.StudentCode))
        {
            throw new ArgumentException("Mã sinh viên đã tồn tại.");
        }

        var entity = new E.Student
        {
            StudentCode = student.StudentCode
        };

        ApplyAcademic(entity, student);

        entity.Email = student.Email;
        entity.Phone = student.Phone;

        db.Students.Add(entity);

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return entity.StudentId;
        }
        catch (DbUpdateException ex)
            when (StudentValidation.IsUniqueConstraintError(ex))
        {
            throw new InvalidOperationException(
                "Mã sinh viên đã tồn tại.", ex);
        }
    }

    public async Task UpdateAsync(D.Student student)
    {
        StudentValidation.ValidateStudent(student, false);
        NormalizeExtended(student);

        using var db = createDb();

        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F03");

        await AccessService.EnsureStudentAsync(
            db, access, student.StudentID, "F03");

        var entity = await db.Students.SingleAsync(
            existing => existing.StudentId == student.StudentID);

        if (student.RowVer?.Length != 8
            || !entity.RowVer.SequenceEqual(student.RowVer))
        {
            throw new InvalidOperationException(
                "Hồ sơ đã thay đổi. Hãy tải lại trước khi sửa.");
        }

        if (student.StudentCode != entity.StudentCode
            || student.UserID != entity.UserId)
        {
            throw new UnauthorizedAccessException(
                "Không được đổi mã sinh viên hoặc tài khoản liên kết.");
        }

        bool staff =
            access.CanStaff("F03")
            && entity.DepartmentId == access.StaffDepartmentId;

        bool fullNameChanged =
            student.FullName != entity.FullName;

        bool birthChanged =
            Date(student.DateOfBirth) != entity.DateOfBirth;

        bool genderChanged =
            student.Gender != entity.Gender;

        bool addressChanged =
            student.Address != entity.Address;

        if (staff)
        {
            if (student.DepartmentID != entity.DepartmentId
                || student.DepartmentID != access.StaffDepartmentId)
            {
                throw new UnauthorizedAccessException(
                    "Không được chuyển khoa qua form này.");
            }

            RequireCompleteProfile(student);

            await CheckMajorAsync(
                db,
                student,
                requireActive: student.MajorID != entity.MajorId);

            ApplyAcademic(entity, student);
        }
        else if (
            fullNameChanged
            || birthChanged
            || genderChanged
            || addressChanged
            || student.DepartmentID != entity.DepartmentId
            || student.MajorID != entity.MajorId
            || student.Cohort != entity.Cohort
            || Date(student.EnrollmentDate) != entity.EnrollmentDate
            || student.StudentStatus != entity.StudentStatus
            || student.EmergencyContactName != entity.EmergencyContactName
            || student.EmergencyContactPhone != entity.EmergencyContactPhone)
        {
            throw new UnauthorizedAccessException(
                "Sinh viên chỉ được sửa email và số điện thoại.");
        }

        bool emailChanged = entity.Email != student.Email;
        bool phoneChanged = entity.Phone != student.Phone;

        entity.Email = student.Email;
        entity.Phone = student.Phone;

        db.Entry(entity)
            .Property(existing => existing.RowVer)
            .OriginalValue = student.RowVer!.ToArray();

        // Đồng bộ thông tin đã thay đổi với tài khoản liên kết.
        // Không đổi Username, mật khẩu, vai trò hoặc trạng thái tài khoản.
        if (entity.UserId is int userId)
        {
            var user = await db.Users.SingleAsync(
                existing => existing.UserId == userId);

            if (emailChanged)
            {
                user.Email = student.Email;
            }

            if (phoneChanged)
            {
                user.Phone = student.Phone;
            }

            if (staff && fullNameChanged)
            {
                user.FullName = student.FullName;
                user.DisplayName = student.FullName;
            }

            if (staff && birthChanged)
            {
                user.DateOfBirth = Date(student.DateOfBirth);
            }

            if (staff && genderChanged)
            {
                user.Gender = student.Gender;
            }

            if (staff && addressChanged)
            {
                user.Address = student.Address;
            }

            if (emailChanged
                || phoneChanged
                || fullNameChanged
                || birthChanged
                || genderChanged
                || addressChanged)
            {
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            student.RowVer = entity.RowVer.ToArray();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "Hồ sơ hoặc tài khoản vừa thay đổi. Hãy tải lại.", ex);
        }
        catch (DbUpdateException ex)
            when (StudentValidation.IsUniqueConstraintError(ex))
        {
            throw new InvalidOperationException(
                "Email đã được tài khoản khác sử dụng.", ex);
        }
    }

    private static async Task CheckMajorAsync(
        E.StudentManagementContext db,
        D.Student student,
        bool requireActive)
    {
        bool validDepartment = await db.Departments.AnyAsync(
            department =>
                department.DepartmentId == student.DepartmentID
                && department.IsActive);

        if (!validDepartment)
        {
            throw new ArgumentException("Khoa không hoạt động.");
        }

        bool validMajor = await db.Majors.AnyAsync(
            major =>
                major.MajorId == student.MajorID
                && major.DepartmentId == student.DepartmentID
                && (!requireActive || major.IsActive));

        if (!validMajor)
        {
            throw new ArgumentException(
                "Ngành không hợp lệ, ngừng hoạt động "
                + "hoặc không thuộc khoa.");
        }
    }

    private static void ApplyAcademic(
        E.Student entity,
        D.Student student)
    {
        entity.FullName = student.FullName;
        entity.DateOfBirth = Date(student.DateOfBirth);
        entity.Gender = student.Gender;
        entity.Address = student.Address;

        entity.DepartmentId = student.DepartmentID;
        entity.MajorId = student.MajorID;
        entity.Cohort = student.Cohort;

        entity.EnrollmentDate = Date(student.EnrollmentDate);
        entity.StudentStatus = student.StudentStatus;

        entity.EmergencyContactName =
            student.EmergencyContactName;

        entity.EmergencyContactPhone =
            student.EmergencyContactPhone;
    }

    private static void NormalizeExtended(D.Student student)
    {
        student.Gender = Clean(
            student.Gender, 10, "Giới tính");

        student.Address = Clean(
            student.Address, 250, "Địa chỉ");

        student.Cohort = Clean(
            student.Cohort, 20, "Khóa tuyển sinh");

        student.EmergencyContactName = Clean(
            student.EmergencyContactName, 120, "Người liên hệ");

        student.EmergencyContactPhone = Clean(
            student.EmergencyContactPhone, 30, "Điện thoại liên hệ");

        if (student.Gender != null
            && student.Gender is not ("Nam" or "Nữ" or "Khác"))
        {
            throw new ArgumentException("Giới tính không hợp lệ.");
        }

        if (student.DateOfBirth.HasValue
            && student.EnrollmentDate.HasValue
            && student.DateOfBirth.Value.Date
                >= student.EnrollmentDate.Value.Date)
        {
            throw new ArgumentException(
                "Ngày nhập học phải sau ngày sinh.");
        }
    }

    private static void RequireCompleteProfile(D.Student student)
    {
        if (!student.DateOfBirth.HasValue
            || student.Gender == null
            || string.IsNullOrWhiteSpace(student.Email)
            || string.IsNullOrWhiteSpace(student.Phone)
            || student.Address == null
            || student.Cohort == null
            || !student.DepartmentID.HasValue
            || !student.MajorID.HasValue
            || !student.EnrollmentDate.HasValue)
        {
            throw new ArgumentException(
                "Nhập đủ ngày sinh, giới tính, email, điện thoại, "
                + "địa chỉ, khoa/ngành, khóa và ngày nhập học.");
        }

        if (!Regex.IsMatch(student.Phone, @"^\+?[0-9]{8,15}$"))
        {
            throw new ArgumentException(
                "Điện thoại cần 8–15 chữ số, "
                + "có thể bắt đầu bằng dấu +.");
        }

        if ((student.EmergencyContactName == null)
            != (student.EmergencyContactPhone == null))
        {
            throw new ArgumentException(
                "Nhập đủ tên và điện thoại liên hệ khẩn cấp, "
                + "hoặc để trống cả hai.");
        }

        if (student.EmergencyContactPhone != null
            && !Regex.IsMatch(
                student.EmergencyContactPhone,
                @"^\+?[0-9]{8,15}$"))
        {
            throw new ArgumentException(
                "Điện thoại liên hệ khẩn cấp không hợp lệ.");
        }
    }

    private static string? Clean(
        string? value,
        int maximumLength,
        string label)
    {
        string? result = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

        if (result?.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{label} không vượt quá {maximumLength} ký tự.");
        }

        return result;
    }

    private static DateOnly? Date(DateTime? value)
    {
        return value.HasValue
            ? DateOnly.FromDateTime(value.Value)
            : null;
    }
}