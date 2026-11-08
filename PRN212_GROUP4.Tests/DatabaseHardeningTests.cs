using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Services;
namespace PRN212_GROUP4.Tests;

public class DatabaseHardeningTests : SqlTestBase
{
    [Fact]
    public async Task Migration_is_repeatable_and_preserves_history()
    {
        var migration = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"Database","002_RuntimeHardening.sql"));
        await ExecuteAsync(migration);
        await ExecuteAsync(migration);
        using var db=Db();
        Assert.Equal(4, await db.Grades.CountAsync());
        Assert.Equal(11, await db.CourseEnrollments.CountAsync());
    }
    [Theory]
    [InlineData("INSERT dbo.GradeScale(MinScore,MaxScore,GradePoint,GradeLetter) VALUES(8.6,8.7,0,'X')",51502)]
    [InlineData("DELETE dbo.GradeScale WHERE MinScore=0",51503)]
    [InlineData("UPDATE dbo.Classes SET Capacity=1 WHERE ClassID=1",51504)]
    public async Task Invalid_database_edits_are_rejected(string sql,int code)
    {
        var error=await Assert.ThrowsAsync<SqlException>(()=>ExecuteAsync(sql));
        Assert.Equal(code,error.Number);
    }
    [Fact]
    public async Task GPA_excludes_inconsistent_completion_status()
    {
        await ExecuteAsync("UPDATE dbo.CourseEnrollments SET RegistrationStatus='REGISTERED' WHERE CourseEnrollmentID=1");
        using var db=Db();
        Assert.False(await db.VStudentCourseResults.AnyAsync(x=>x.StudentId==1 && x.CourseCode=="PRN212"));
    }
    [Fact]
    public async Task Drop_updates_both_tables_and_preserves_registration_id()
    {
        await ExecuteAsync("EXEC dbo.uspSetCourseEnrollmentStatus 3,'DROPPED'");
        using var db=Db();
        Assert.Equal("DROPPED",(await db.CourseEnrollments.SingleAsync(x=>x.CourseEnrollmentId==3)).RegistrationStatus);
        Assert.Equal("DROPPED",(await db.Enrollments.SingleAsync(x=>x.EnrollmentId==3)).EnrollmentStatus);
        await ExecuteAsync("DECLARE @id int; EXEC dbo.uspRegisterCourse 1,3,1,@id OUTPUT; IF @id<>3 THROW 51999,'ID changed',1;");
    }
    [Fact]
    public async Task Drop_with_grades_is_atomic_and_rejected()
    {
        await ExecuteAsync("UPDATE dbo.CourseEnrollments SET RegistrationStatus='REGISTERED' WHERE CourseEnrollmentID=1");
        var ex=await Assert.ThrowsAsync<SqlException>(()=>ExecuteAsync("EXEC dbo.uspSetCourseEnrollmentStatus 1,'DROPPED'"));
        Assert.Equal(51509,ex.Number);
        using var db=Db();
        Assert.Equal("COMPLETED",(await db.Enrollments.SingleAsync(x=>x.EnrollmentId==1)).EnrollmentStatus);
    }
    [Fact]
    public async Task Profile_audit_contains_actor_and_rollback_does_not_leave_audit()
    {
        await SignInAsync();
        var service=new StudentProfileService(()=>Db());
        var student=(await service.GetAsync()).First(); student.Phone="0900000000";
        await service.UpdateAsync(student);
        using var db=Db();
        var audit=await db.AuditLogs.SingleAsync(x=>x.EntityName=="Students");
        Assert.NotNull(audit.ActorUserId);
        Assert.Contains("0900000000",audit.ChangeData);
        await ExecuteAsync("BEGIN TRAN; UPDATE dbo.Students SET Phone='rollback' WHERE StudentID=1; ROLLBACK;");
        Assert.Equal(1,await db.AuditLogs.CountAsync(x=>x.EntityName=="Students"));
    }
    [Fact]
    public async Task Direct_assignment_cannot_bypass_capacity()
    {
        await ExecuteAsync("UPDATE dbo.Classes SET Capacity=3 WHERE ClassID=1; DECLARE @r int; EXEC dbo.uspRegisterCourse 3,1,1,@r OUTPUT;");
        var ex=await Assert.ThrowsAsync<SqlException>(()=>ExecuteAsync("INSERT dbo.Enrollments(CourseEnrollmentID,StudentID,ClassID) SELECT CourseEnrollmentID,3,1 FROM dbo.CourseEnrollments WHERE StudentID=3 AND CourseID=1 AND SemesterID=1"));
        Assert.Equal(51504,ex.Number);
    }
    [Fact]
    public async Task Completion_requires_grade_then_updates_both_states()
    {
        var ex=await Assert.ThrowsAsync<SqlException>(()=>ExecuteAsync("EXEC dbo.uspSetCourseEnrollmentStatus 3,'COMPLETED'"));
        Assert.Equal(51508,ex.Number);
        await ExecuteAsync("INSERT dbo.Grades(EnrollmentID,FinalScore,IsPublished) VALUES(3,8,1); EXEC dbo.uspSetCourseEnrollmentStatus 3,'COMPLETED'");
        using var db=Db();
        Assert.Equal("COMPLETED",(await db.CourseEnrollments.SingleAsync(x=>x.CourseEnrollmentId==3)).RegistrationStatus);
        Assert.Equal("COMPLETED",(await db.Enrollments.SingleAsync(x=>x.EnrollmentId==3)).EnrollmentStatus);
        Assert.True(await db.VStudentCourseResults.AnyAsync(x=>x.StudentId==1 && x.CourseCode=="DBI202"));
    }

}
