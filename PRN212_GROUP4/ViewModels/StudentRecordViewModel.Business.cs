using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Services;
using E = PRN212_GROUP4.Models.Entities;
using D = PRN212_GROUP4.Models;

namespace PRN212_GROUP4.ViewModels;

public sealed record RecordSaveResult(
    int RecordId,
    string? Warning);

public class StudentRecordViewModel : ViewModelBase
{
    private readonly Func<E.StudentManagementContext> createDb;

    public StudentRecordViewModel(
        Func<E.StudentManagementContext>? createDb = null)
    {
        this.createDb =
            createDb ?? (() => new E.StudentManagementContext());
    }

    public async Task<List<D.StudentRecord>> GetAsync(
        int studentId,
        bool archived)
    {
        using var db = createDb();

        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, studentId, "F05");

        var rows = await db.StudentRecords
            .AsNoTracking()
            .Where(record =>
                record.StudentId == studentId
                && (archived || !record.IsArchived))
            .OrderByDescending(record => record.RecordDate)
            .ThenByDescending(record => record.RecordId)
            .ToListAsync();

        return rows.Select(StudentMapping.ToDto).ToList();
    }

    public async Task<RecordSaveResult> AddAsync(
        D.StudentRecord record)
    {
        StudentValidation.ValidateStudentRecord(record);

        using var db = createDb();

        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, record.StudentID, "F05");

        string? warning = await DuplicateWarningAsync(
            db, record, excludeId: 0);

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

        record.RecordID = entity.RecordId;
        record.RowVer = entity.RowVer.ToArray();

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
                "Hồ sơ không hợp lệ hoặc đã lưu trữ.");
        }

        RequireVersion(original.RowVer);

        StudentValidation.ValidateStudentRecord(edited);

        using var db = createDb();

        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, original.StudentID, "F05");

        var entity = await db.StudentRecords.SingleOrDefaultAsync(
            record =>
                record.RecordId == original.RecordID
                && record.StudentId == original.StudentID);

        if (entity == null
            || entity.IsArchived
            || !entity.RowVer.SequenceEqual(original.RowVer))
        {
            throw new InvalidOperationException(
                "Hồ sơ đã thay đổi hoặc lưu trữ. Hãy tải lại.");
        }

        string? warning = await DuplicateWarningAsync(
            db, edited, original.RecordID);

        entity.RecordType = edited.RecordType;
        entity.DocumentNumber = edited.DocumentNumber;
        entity.RecordTitle = edited.RecordTitle;
        entity.RecordContent = edited.RecordContent;
        entity.RecordDate = DateOnly.FromDateTime(edited.RecordDate);

        db.Entry(entity)
            .Property(record => record.RowVer)
            .OriginalValue = original.RowVer.ToArray();

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            edited.RowVer = entity.RowVer.ToArray();

            return new RecordSaveResult(entity.RecordId, warning);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "Hồ sơ vừa thay đổi. Hãy tải lại.", ex);
        }
    }

    public async Task ArchiveAsync(
        int studentId,
        int recordId,
        byte[] expectedRowVer)
    {
        RequireVersion(expectedRowVer);

        using var db = createDb();

        using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var access = await AccessService.RequireAsync(db, "F05");

        await AccessService.EnsureStudentAsync(
            db, access, studentId, "F05");

        var entity = await db.StudentRecords.SingleOrDefaultAsync(
            record =>
                record.RecordId == recordId
                && record.StudentId == studentId);

        if (entity == null
            || entity.IsArchived
            || !entity.RowVer.SequenceEqual(expectedRowVer))
        {
            throw new InvalidOperationException(
                "Hồ sơ đã thay đổi hoặc lưu trữ. "
                + "Hãy tải lại trước khi xác nhận.");
        }

        entity.IsArchived = true;

        db.Entry(entity)
            .Property(record => record.RowVer)
            .OriginalValue = expectedRowVer.ToArray();

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "Hồ sơ vừa thay đổi. Hãy tải lại.", ex);
        }
    }

    private static void RequireVersion(byte[]? version)
    {
        if (version?.Length != 8)
        {
            throw new InvalidOperationException(
                "Thiếu phiên bản hồ sơ. Hãy tải lại danh sách.");
        }
    }

    private static async Task<string?> DuplicateWarningAsync(
        E.StudentManagementContext db,
        D.StudentRecord record,
        int excludeId)
    {
        // Kiểm tra cả hồ sơ đã archive để giữ lịch sử.
        // Khóa phạm vi trong transaction tránh kiểm tra trùng
        // không nhất quán khi có nhiều người cùng thêm.
        bool duplicate = await db.StudentRecords
            .FromSqlInterpolated($@"
                SELECT *
                FROM dbo.StudentRecords WITH (UPDLOCK, HOLDLOCK)
                WHERE StudentID = {record.StudentID}
                  AND RecordType = {record.RecordType}
                  AND DocumentNumber = {record.DocumentNumber}
                  AND RecordID <> {excludeId}")
            .AnyAsync();

        return duplicate
            ? "Cảnh báo: đã có hồ sơ cùng sinh viên, loại hồ sơ "
              + "và mã chứng từ. Hồ sơ vẫn được lưu; "
              + "hãy kiểm tra tính hợp lệ."
            : null;
    }
}