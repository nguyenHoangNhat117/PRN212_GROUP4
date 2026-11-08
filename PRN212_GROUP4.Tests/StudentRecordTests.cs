using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Services;
using D = PRN212_GROUP4.Models;
namespace PRN212_GROUP4.Tests;

public class StudentRecordTests : SqlTestBase
{
    private StudentRecordService Service => new(() => Db());
    private static D.StudentRecord Record(string number = "DOC-001", int student = 1, string type = "Academic") => new()
    { StudentID = student, RecordType = type, DocumentNumber = number, RecordTitle = "Quyết định", RecordDate = DateTime.Today };

    [Fact]
    public async Task Duplicate_key_warns_but_still_persists_both_records()
    {
        await SignInAsync();
        var first = await Service.AddAsync(Record());
        var second = await Service.AddAsync(Record(" doc-001 "));
        Assert.Null(first.Warning);
        Assert.NotNull(second.Warning);
        Assert.NotEqual(first.RecordId, second.RecordId);
        await using var db = Db();
        Assert.Equal(2, await db.StudentRecords.CountAsync(x => x.DocumentNumber == "DOC-001"));
    }

    [Theory]
    [InlineData("DOC-002", 1, "Academic")]
    [InlineData("DOC-001", 2, "Academic")]
    [InlineData("DOC-001", 1, "Discipline")]
    public async Task Every_key_component_is_used(string number, int student, string type)
    {
        await SignInAsync();
        await Service.AddAsync(Record());
        Assert.Null((await Service.AddAsync(Record(number, student, type))).Warning);
    }

    [Fact]
    public async Task Updating_own_key_does_not_warn_but_matching_another_key_does()
    {
        await SignInAsync();
        var first = await Service.AddAsync(Record());
        var second = await Service.AddAsync(Record("DOC-002"));
        var rows = await Service.GetAsync(1, false);
        var original = rows.Single(x => x.RecordID == first.RecordId);
        Assert.Null((await Service.UpdateAsync(original, original)).Warning);
        var other = rows.Single(x => x.RecordID == second.RecordId);
        var edited = Record(); edited.RecordID = other.RecordID;
        Assert.NotNull((await Service.UpdateAsync(edited, other)).Warning);
    }

    [Fact]
    public async Task Archive_preserves_history_and_still_participates_in_warning()
    {
        await SignInAsync();
        var saved = await Service.AddAsync(Record());
        await Service.ArchiveAsync(1, saved.RecordId);
        Assert.DoesNotContain(await Service.GetAsync(1, false), x => x.RecordID == saved.RecordId);
        Assert.Contains(await Service.GetAsync(1, true), x => x.RecordID == saved.RecordId && x.IsArchived);
        Assert.NotNull((await Service.AddAsync(Record())).Warning);
        await using var db = Db();
        Assert.Equal(10, await db.Enrollments.CountAsync());
        Assert.Equal(4, await db.Grades.CountAsync());
    }

    [Fact]
    public async Task Missing_document_number_is_invalid()
    {
        await SignInAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => Service.AddAsync(Record(" ")));
    }

    [Fact]
    public async Task Student_cannot_manage_records()
    {
        await SignInAsync("student01");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service.AddAsync(Record()));
    }

    [Fact]
    public async Task Additive_upgrade_is_repeatable_and_preserves_existing_records()
    {
        await ExecuteAsync("DROP INDEX IX_Records_BusinessKey ON dbo.StudentRecords; ALTER TABLE dbo.StudentRecords DROP COLUMN DocumentNumber;");
        string sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Database", "001_RecordDocumentNumber.sql"));
        await ExecuteAsync(sql); await ExecuteAsync(sql);
        await using var db = Db();
        Assert.Equal(2, await db.StudentRecords.CountAsync());
        Assert.All(await db.StudentRecords.ToListAsync(), x => Assert.Null(x.DocumentNumber));
    }
    [Fact]
    public async Task Concurrent_duplicate_records_are_both_saved_with_a_warning()
    {
        await SignInAsync();
        var results = await Task.WhenAll(Service.AddAsync(Record()), Service.AddAsync(Record()));
        Assert.Single(results, x => x.Warning != null);
        await using var db = Db();
        Assert.Equal(2, await db.StudentRecords.CountAsync(x => x.DocumentNumber == "DOC-001"));
    }

    [Fact]
    public async Task Stale_record_update_is_rejected_after_another_editor_saves()
    {
        await SignInAsync();
        var saved = await Service.AddAsync(Record());
        var a = (await Service.GetAsync(1, false)).Single(x => x.RecordID == saved.RecordId);
        var b = (await Service.GetAsync(1, false)).Single(x => x.RecordID == saved.RecordId);
        var changed = Record(); changed.RecordID = saved.RecordId; changed.RecordTitle = "First editor";
        await Service.UpdateAsync(changed, a);
        var stale = Record(); stale.RecordID = saved.RecordId; stale.RecordTitle = "Stale editor";
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.UpdateAsync(stale, b));
        Assert.Equal("First editor", (await Service.GetAsync(1, false)).Single(x => x.RecordID == saved.RecordId).RecordTitle);
    }
}
