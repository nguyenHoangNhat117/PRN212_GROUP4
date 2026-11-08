using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using PRN212_GROUP4.Services;
namespace PRN212_GROUP4.Tests;

public class EnrollmentTests : SqlTestBase
{
    private CourseEnrollmentService Service => new(() => Db());

    [Fact]
    public async Task New_registration_returns_id_without_assigning_a_class()
    {
        await SignInAsync();
        int id = await Service.RegisterAsync(4, 2, 2);
        await using var db = Db();
        var row = await db.CourseEnrollments.SingleAsync(x => x.CourseEnrollmentId == id);
        Assert.Equal("REGISTERED", row.RegistrationStatus);
        Assert.False(await db.Enrollments.AnyAsync(x => x.CourseEnrollmentId == id));
    }

    [Theory]
    [InlineData(6, 1, 2)] // REGISTERED
    [InlineData(1, 1, 1)] // COMPLETED
    public async Task Existing_active_or_completed_registration_is_rejected(int student, int course, int semester)
    {
        await SignInAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.RegisterAsync(student, course, semester));
    }

    [Fact]
    public async Task Dropped_without_grades_reuses_the_old_id()
    {
        await SignInAsync();
        await ExecuteAsync("UPDATE dbo.CourseEnrollments SET RegistrationStatus='DROPPED' WHERE CourseEnrollmentID=11");
        Assert.Equal(11, await Service.RegisterAsync(6, 1, 2));
        await using var db = Db();
        Assert.Equal(11, await db.CourseEnrollments.CountAsync());
        Assert.Equal("REGISTERED", (await db.CourseEnrollments.FindAsync(11))!.RegistrationStatus);
    }

    [Fact]
    public async Task Dropped_with_grades_requires_academic_review()
    {
        await SignInAsync();
        await ExecuteAsync("UPDATE dbo.Enrollments SET EnrollmentStatus='DROPPED' WHERE EnrollmentID=1; UPDATE dbo.CourseEnrollments SET RegistrationStatus='DROPPED' WHERE CourseEnrollmentID=1;");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Service.RegisterAsync(1, 1, 1));
        Assert.Contains("học vụ", ex.Message);
        await using var db = Db();
        Assert.Equal("DROPPED", (await db.CourseEnrollments.FindAsync(1))!.RegistrationStatus);
        Assert.Equal(4, await db.Grades.CountAsync());
    }

    [Theory]
    [InlineData("UPDATE dbo.Students SET StudentStatus='INACTIVE' WHERE StudentID=4", 2, 2, 51120)]
    [InlineData("UPDATE dbo.Courses SET IsActive=0 WHERE CourseID=2", 2, 2, 51121)]
    [InlineData("", 2, 9999, 51122)]
    public async Task Database_rejects_invalid_data_even_when_UI_is_bypassed(string setup, int course, int semester, int error)
    {
        await SignInAsync();
        if (setup.Length > 0) await ExecuteAsync(setup);
        var ex = await Assert.ThrowsAsync<SqlException>(() => Service.RegisterAsync(4, course, semester));
        Assert.Equal(error, ex.Number);
    }

    [Fact]
    public async Task Concurrent_requests_create_only_one_registration()
    {
        await SignInAsync();
        async Task<bool> Register()
        {
            try { await Service.RegisterAsync(4, 2, 2); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var outcomes = await Task.WhenAll(Register(), Register());
        Assert.Single(outcomes, x => x);
        await using var db = Db();
        Assert.Equal(1, await db.CourseEnrollments.CountAsync(x => x.StudentId == 4 && x.CourseId == 2 && x.SemesterId == 2));
    }

    [Theory]
    [InlineData("student01", 1)]
    [InlineData("lecturer01", 0)]
    [InlineData("staff01", 0)]
    public async Task Enrollment_queries_respect_actor_scope(string username, int ownId)
    {
        await SignInAsync(username);
        var rows = await Service.ListAsync();
        Assert.NotEmpty(rows);
        if (ownId != 0) Assert.All(rows, x => Assert.Equal(ownId, x.StudentId));
        else if (username == "lecturer01") Assert.DoesNotContain(rows, x => x.Class == "BUS101-01" || x.Class == "Chưa xếp lớp");
        else Assert.DoesNotContain(rows, x => x.StudentId == 5);
    }

    [Fact]
    public async Task Student_cannot_register_another_student()
    {
        await SignInAsync("student01");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service.RegisterAsync(4, 2, 2));
    }
    [Fact]
    public async Task Concurrent_reactivation_reuses_one_id_without_creating_a_new_row()
    {
        await SignInAsync();
        await ExecuteAsync("UPDATE dbo.CourseEnrollments SET RegistrationStatus='DROPPED' WHERE CourseEnrollmentID=11");
        async Task<int?> Register()
        {
            try { return await Service.RegisterAsync(6, 1, 2); }
            catch (InvalidOperationException) { return null; }
        }
        var outcomes = await Task.WhenAll(Register(), Register());
        Assert.Equal(11, Assert.Single(outcomes, x => x != null));
        await using var db = Db();
        Assert.Equal(11, await db.CourseEnrollments.CountAsync());
    }
}
