using System.Data;
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
        this.createDb = createDb ?? (() => new E.StudentManagementContext());
    }

    public async Task<List<D.Student>> GetAsync()
    {
        using var db = createDb();
        var access = await AccessService.RequireAsync(db, "F03");

        var entities = await AccessService.Students(db, access, "F03")
            .AsNoTracking()
            .Include(student => student.Department)
            .OrderBy(student => student.StudentCode)
            .ToListAsync();

        return entities.Select(StudentMapping.ToDto).ToList();
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

    public async Task<int> AddAsync(D.Student student)
    {
        StudentValidation.ValidateStudent(student, true);

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

        if (!await db.Departments.AnyAsync(department =>
            department.DepartmentId == student.DepartmentID
            && department.IsActive))
        {
            throw new ArgumentException("Khoa không hoạt động.");
        }

        if (await db.Students.AnyAsync(existing =>
            existing.StudentCode == student.StudentCode))
        {
            throw new ArgumentException("Mã sinh viên đã tồn tại.");
        }

        var entity = new E.Student
        {
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            Email = student.Email,
            Phone = student.Phone,
            DepartmentId = student.DepartmentID,
            DateOfBirth = Date(student.DateOfBirth),
            EnrollmentDate = Date(student.EnrollmentDate),
            StudentStatus = student.StudentStatus
        };

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

        using var db = createDb();
        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F03");

        await AccessService.EnsureStudentAsync(
            db, access, student.StudentID, "F03");

        var entity = await db.Students.SingleAsync(existing =>
            existing.StudentId == student.StudentID);

        if (student.RowVer?.Length != 8
            || !entity.RowVer.SequenceEqual(student.RowVer))
        {
            throw new InvalidOperationException(
                "Hồ sơ đã thay đổi. Hãy tải lại trước khi sửa.");
        }

        bool staff = access.CanStaff("F03")
            && entity.DepartmentId == access.StaffDepartmentId;

        if (!staff &&
            (student.StudentCode != entity.StudentCode
             || student.FullName != entity.FullName
             || Date(student.DateOfBirth) != entity.DateOfBirth
             || student.DepartmentID != entity.DepartmentId
             || Date(student.EnrollmentDate) != entity.EnrollmentDate
             || student.StudentStatus != entity.StudentStatus
             || student.UserID != entity.UserId))
        {
            throw new UnauthorizedAccessException(
                "Sinh viên chỉ được sửa email và số điện thoại.");
        }

        if (staff)
        {
            if (student.DepartmentID != access.StaffDepartmentId)
            {
                throw new UnauthorizedAccessException(
                    "Không được chuyển sinh viên ra ngoài khoa phụ trách.");
            }

            entity.FullName = student.FullName;
            entity.DateOfBirth = Date(student.DateOfBirth);
            entity.EnrollmentDate = Date(student.EnrollmentDate);
            entity.StudentStatus = student.StudentStatus;
        }

        entity.Email = student.Email;
        entity.Phone = student.Phone;

        db.Entry(entity)
            .Property(existing => existing.RowVer)
            .OriginalValue = student.RowVer!.ToArray();

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            student.RowVer = entity.RowVer.ToArray();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "Hồ sơ đã thay đổi. Hãy tải lại.", ex);
        }
    }

    private static DateOnly? Date(DateTime? value)
    {
        return value.HasValue
            ? DateOnly.FromDateTime(value.Value)
            : null;
    }
}