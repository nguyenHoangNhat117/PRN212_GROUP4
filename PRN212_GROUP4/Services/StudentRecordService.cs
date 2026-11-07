using System.Data;
using Microsoft.EntityFrameworkCore;
using E = PRN212_GROUP4.Models.Entities;
using D = PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services;

public sealed class StudentRecordService
{
    public async Task<List<D.StudentRecord>> GetAsync(
        int studentId,
        bool archived)
    {
        using var db = new E.StudentManagementContext();
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

    public async Task<int> AddAsync(D.StudentRecord record)
    {
        StudentValidation.ValidateStudentRecord(record);

        using var db = new E.StudentManagementContext();
        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, record.StudentID, "F05");

        var entity = new E.StudentRecord
        {
            StudentId = record.StudentID,
            RecordType = record.RecordType,
            RecordTitle = record.RecordTitle,
            RecordContent = record.RecordContent,
            RecordDate = DateOnly.FromDateTime(record.RecordDate),
            CreatedByUserId = access.UserId,
            IsArchived = false
        };

        db.StudentRecords.Add(entity);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return entity.RecordId;
    }

    public async Task UpdateAsync(
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

        using var db = new E.StudentManagementContext();
        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, original.StudentID, "F05");

        var originalDate = DateOnly.FromDateTime(original.RecordDate);
        var newDate = DateOnly.FromDateTime(edited.RecordDate);

        int count = await db.StudentRecords
            .Where(record =>
                record.RecordId == original.RecordID
                && record.StudentId == original.StudentID
                && !record.IsArchived
                && record.RecordTitle == original.RecordTitle
                && record.RecordType == original.RecordType
                && record.RecordContent == original.RecordContent
                && record.RecordDate == originalDate)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(record => record.RecordType, edited.RecordType)
                .SetProperty(record => record.RecordTitle, edited.RecordTitle)
                .SetProperty(record => record.RecordContent, edited.RecordContent)
                .SetProperty(record => record.RecordDate, newDate));

        if (count != 1)
        {
            throw new InvalidOperationException(
                "Hồ sơ đã thay đổi hoặc archive. Hãy tải lại.");
        }

        await transaction.CommitAsync();
    }

    public async Task ArchiveAsync(int studentId, int recordId)
    {
        using var db = new E.StudentManagementContext();
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