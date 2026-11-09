using System.Data;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Services;
using E = PRN212_GROUP4.Models.Entities;
using D = PRN212_GROUP4.Models;

namespace PRN212_GROUP4.ViewModels;

public sealed record RecordSaveResult(int RecordId, string? Warning);

public class StudentRecordViewModel : ViewModelBase
{
    private readonly Func<E.StudentManagementContext> createDb;

    public StudentRecordViewModel(
        Func<E.StudentManagementContext>? createDb = null)
    {
        this.createDb = createDb ?? (() => new E.StudentManagementContext());
    }

    public async Task<List<D.StudentRecord>> GetAsync(
        int studentId,
        bool archived)
    {
        using var db = createDb();
        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, studentId, "F05");

        var entities = await db.StudentRecords
            .AsNoTracking()
            .Where(record =>
                record.StudentId == studentId
                && (archived || !record.IsArchived))
            .OrderByDescending(record => record.RecordDate)
            .ThenByDescending(record => record.RecordId)
            .ToListAsync();

        return entities.Select(StudentMapping.ToDto).ToList();
    }

    public async Task<RecordSaveResult> AddAsync(D.StudentRecord record)
    {
        StudentValidation.ValidateStudentRecord(record);

        using var db = createDb();
        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, record.StudentID, "F05");

        string? warning = await DuplicateWarningAsync(db, record);

        var entity = new E.StudentRecord
        {
            StudentId = record.StudentID,
            RecordType = record.RecordType,
            DocumentNumber = record.DocumentNumber,
            RecordTitle = record.RecordTitle,
            RecordContent = record.RecordContent,
            RecordDate = DateOnly.FromDateTime(record.RecordDate),
            CreatedByUserId = access.UserId,
            IsArchived = false
        };

        db.StudentRecords.Add(entity);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new RecordSaveResult(entity.RecordId, warning);
    }

    public async Task<RecordSaveResult> UpdateAsync(
        D.StudentRecord edited,
        D.StudentRecord original)
    {
        if (edited.RecordID != original.RecordID
            || edited.StudentID != original.StudentID
            || original.IsArchived)
        {
            throw new ArgumentException(
                "Hồ sơ không hợp lệ hoặc đã archive.");
        }

        StudentValidation.ValidateStudentRecord(edited);

        using var db = createDb();
        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, original.StudentID, "F05");

        string? warning = await DuplicateWarningAsync(db, edited);

        var originalDate = DateOnly.FromDateTime(original.RecordDate);
        var newDate = DateOnly.FromDateTime(edited.RecordDate);

        int count = await db.StudentRecords
            .Where(record =>
                record.RecordId == original.RecordID
                && record.StudentId == original.StudentID
                && !record.IsArchived
                && record.DocumentNumber == original.DocumentNumber
                && record.RecordTitle == original.RecordTitle
                && record.RecordType == original.RecordType
                && record.RecordContent == original.RecordContent
                && record.RecordDate == originalDate)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(record => record.RecordType, edited.RecordType)
                .SetProperty(
                    record => record.DocumentNumber, edited.DocumentNumber)
                .SetProperty(record => record.RecordTitle, edited.RecordTitle)
                .SetProperty(
                    record => record.RecordContent, edited.RecordContent)
                .SetProperty(record => record.RecordDate, newDate));

        if (count != 1)
        {
            throw new InvalidOperationException(
                "Hồ sơ đã thay đổi hoặc archive. Hãy tải lại.");
        }

        await transaction.CommitAsync();
        return new RecordSaveResult(edited.RecordID, warning);
    }

    private static async Task<string?> DuplicateWarningAsync(
        E.StudentManagementContext db,
        D.StudentRecord record)
    {
        bool duplicate = await db.StudentRecords.FromSqlInterpolated($@"
            SELECT * FROM dbo.StudentRecords WITH (UPDLOCK, HOLDLOCK)
            WHERE StudentID = {record.StudentID}
              AND RecordType = {record.RecordType}
              AND DocumentNumber = {record.DocumentNumber}
              AND RecordID <> {record.RecordID}").AnyAsync();

        return duplicate
            ? "Cảnh báo: đã có hồ sơ cùng sinh viên, loại hồ sơ và mã chứng từ. Hồ sơ vẫn được lưu; hãy kiểm tra tính hợp lệ."
            : null;
    }

    public async Task ArchiveAsync(int studentId, int recordId)
    {
        using var db = createDb();
        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, studentId, "F05");

        int count = await db.StudentRecords
            .Where(record =>
                record.RecordId == recordId
                && record.StudentId == studentId
                && !record.IsArchived)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(record => record.IsArchived, true));

        if (count != 1)
        {
            throw new InvalidOperationException(
                "Hồ sơ không còn tồn tại hoặc đã archive.");
        }

        await transaction.CommitAsync();
    }
}