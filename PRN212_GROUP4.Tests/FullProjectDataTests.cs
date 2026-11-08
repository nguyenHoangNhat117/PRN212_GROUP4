using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Services;
namespace PRN212_GROUP4.Tests;

public class FullProjectDataTests : SqlTestBase
{
    private static Task<string> Script(string name)=>File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"Database",name));
    private async Task Seed()
    {
        await ExecuteAsync("EXEC sys.sp_addextendedproperty @name=N'G4.DemoSeedAllowed',@value=N'1';");
        await ExecuteAsync(await Script("FullProject_Demo.sql"));
    }
    [Fact]
    public async Task Demo_requires_explicit_opt_in()
    {
        var ex=await Assert.ThrowsAsync<SqlException>(async()=>await ExecuteAsync(await Script("FullProject_Demo.sql")));
        Assert.Equal(51600,ex.Number);
    }
    [Fact]
    public async Task Demo_is_repeatable_and_has_consistent_repeat_GPA()
    {
        await Seed();
        using var db=Db();
        var before=await db.CourseEnrollments.CountAsync();
        var auth=await AccountAsync("demo_student_a");
        await ExecuteAsync(await Script("FullProject_Demo.sql"));
        Assert.Equal(before,await db.CourseEnrollments.CountAsync());
        Assert.Equal(LoginOutcome.SignedIn,await auth.LoginAsync("demo_student_a",InitialPassword));
        var a=await db.VStudentGpas.SingleAsync(x=>x.StudentCode=="DEMO-A");
        Assert.Equal(2.69m,a.CumulativeGpa);
        Assert.Equal(8,a.CountedCredits);
        Assert.False(await db.VStudentGpas.AnyAsync(x=>x.StudentCode=="DEMO-B"));
        await ExecuteAsync(await Script("Check_Readiness.sql"));
    }
    [Theory]
    [InlineData("demo_staff_se","DEMO-A","DEMO-BA")]
    [InlineData("demo_staff_ba","DEMO-BA","DEMO-A")]
    public async Task Staff_is_scoped_to_own_department(string user,string own,string other)
    {
        await Seed(); await SignInAsync(user);
        var students=await new StudentProfileService(()=>Db()).GetAsync();
        Assert.Contains(students,x=>x.StudentCode==own);
        Assert.DoesNotContain(students,x=>x.StudentCode==other);
    }
    [Fact]
    public async Task Demo_covers_full_class_drop_and_unpublished_grade()
    {
        await Seed(); using var db=Db();
        var full=await db.Classes.SingleAsync(x=>x.ClassCode=="DEMO-FULL");
        Assert.Equal(full.Capacity,await db.Enrollments.CountAsync(x=>x.ClassId==full.ClassId && x.EnrollmentStatus!="DROPPED"));
        var c=await db.Students.SingleAsync(x=>x.StudentCode=="DEMO-C");
        var dropped=await db.CourseEnrollments.SingleAsync(x=>x.StudentId==c.StudentId && x.Course.CourseCode=="DEMO-B");
        await SignInAsync("demo_staff_se");
        var service=new CourseEnrollmentService(()=>Db());
        Assert.Equal(dropped.CourseEnrollmentId,await service.RegisterAsync(c.StudentId,dropped.CourseId,dropped.SemesterId));
        var graded=await db.CourseEnrollments.SingleAsync(x=>x.StudentId==c.StudentId && x.Course.CourseCode=="DEMO-C");
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.RegisterAsync(c.StudentId,graded.CourseId,graded.SemesterId));
        var report=await new AcademicReportService(createDb:()=>Db()).GenerateAsync(new("Kết quả học tập"));
        Assert.All(report.Where(x=>x.StudentCode=="DEMO-B"),x=>Assert.Null(x.Score));
    }
    [Fact]
    public async Task Readiness_detects_mismatched_completion_without_modifying_history()
    {
        await ExecuteAsync("UPDATE dbo.CourseEnrollments SET RegistrationStatus='REGISTERED' WHERE CourseEnrollmentID=1");
        var ex=await Assert.ThrowsAsync<SqlException>(async()=>await ExecuteAsync(await Script("Check_Readiness.sql")));
        Assert.Equal(51621,ex.Number);
        using var db=Db();
        Assert.Equal(4,await db.Grades.CountAsync());
    }
    [Theory]
    [InlineData("Install_Production.sql",false)]
    [InlineData("Install_FullProject_Demo.sql",true)]
    public async Task Standalone_installers_create_expected_databases(string filename,bool demo)
    {
        var original=new SqlConnectionStringBuilder(ConnectionString);
        var name="PackageTests_"+Guid.NewGuid().ToString("N");
        original.InitialCatalog="master";
        await using var connection=new SqlConnection(original.ConnectionString); await connection.OpenAsync();
        try
        {
            var sql=await Script(filename);
            string defaultName=demo?"PRN212_StudentManagement_G4_Demo":"PRN212_StudentManagement_G4_Production";
            sql=sql.Replace($"@DatabaseName sysname=N'{defaultName}'",$"@DatabaseName sysname=N'{name}'");
            if(!demo)
            {
                await using var denied=new SqlCommand(sql,connection);
                var error=await Assert.ThrowsAsync<SqlException>(()=>denied.ExecuteNonQueryAsync());
                Assert.Equal(51610,error.Number);
                sql=sql.Replace("@GradePolicyConfirmed bit=0","@GradePolicyConfirmed bit=1");
            }
            await using var install=new SqlCommand(sql,connection){CommandTimeout=120}; await install.ExecuteNonQueryAsync();
            await using var check=new SqlCommand($"USE [{name}]; SELECT COUNT(*) FROM dbo.Students",connection);
            Assert.Equal(demo?14:0,(int)(await check.ExecuteScalarAsync())!);
            await using var ready=new SqlCommand($"USE [{name}];\n"+await Script("Check_Readiness.sql"),connection){CommandTimeout=120};
            await ready.ExecuteNonQueryAsync();
        }
        finally
        {
            await using var drop=new SqlCommand($"USE master; IF DB_ID(N'{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END",connection);
            await drop.ExecuteNonQueryAsync();
        }
    }
    [Fact]
    public async Task Runtime_role_can_use_registration_API_but_not_edit_audit_or_history()
    {
        await ExecuteAsync(await Script("Provision_RuntimeRole.sql"));
        await ExecuteAsync(await Script("Provision_RuntimeRole.sql"));
        await ExecuteAsync("CREATE USER RuntimeTest WITHOUT LOGIN; ALTER ROLE G4_AppRuntime ADD MEMBER RuntimeTest;");
        await ExecuteAsync(@"EXECUTE AS USER='RuntimeTest';
BEGIN TRY
 IF HAS_PERMS_BY_NAME('dbo.AuditLogs','OBJECT','UPDATE')<>0 THROW 51999,'Audit write exposed',1;
 IF HAS_PERMS_BY_NAME('dbo.Grades','OBJECT','DELETE')<>0 THROW 51999,'History delete exposed',1;
 IF HAS_PERMS_BY_NAME('dbo.CourseEnrollments','OBJECT','INSERT')<>0 THROW 51999,'Direct registration exposed',1;
 DECLARE @r int; EXEC dbo.uspRegisterCourse 1,2,2,@r OUTPUT;
 IF @r IS NULL THROW 51999,'Runtime registration failed',1;
 UPDATE dbo.Students SET Phone='0901111111' WHERE StudentID=1;
 REVERT;
END TRY
BEGIN CATCH
 REVERT;
 THROW;
END CATCH;");
        using var db=Db();
        Assert.True(await db.AuditLogs.AnyAsync(x=>x.EntityName=="Students"));
    }

}
