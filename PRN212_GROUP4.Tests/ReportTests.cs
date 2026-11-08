using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Services;
using PRN212_GROUP4.Models;
namespace PRN212_GROUP4.Tests;

public class ReportTests : SqlTestBase
{
    // All completed published attempts remain the source policy. Tests do not invent
    // a highest/latest-repeat rule that the team has not approved.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Shared_summary_report_and_csv_share_GPA_including_repeat_attempts(bool repeat)
    {
        await SignInAsync();
        if (repeat)
            await ExecuteAsync(@"INSERT dbo.Classes(ClassCode,CourseID,SemesterID,LecturerID) VALUES('PRN212-REPEAT',1,2,1);
DECLARE @r int; EXEC dbo.uspRegisterCourse 1,1,2,@r OUTPUT;
DECLARE @c int=(SELECT ClassID FROM dbo.Classes WHERE ClassCode='PRN212-REPEAT');
EXEC dbo.uspAssignStudentToClass @r,@c;
UPDATE dbo.Enrollments SET EnrollmentStatus='COMPLETED' WHERE CourseEnrollmentID=@r;
UPDATE dbo.CourseEnrollments SET RegistrationStatus='COMPLETED' WHERE CourseEnrollmentID=@r;
INSERT dbo.Grades(EnrollmentID,FinalScore,IsPublished) SELECT EnrollmentID,4.5,1 FROM dbo.Enrollments WHERE CourseEnrollmentID=@r;");
        IAcademicGpaProvider provider = new SqlViewGpaProvider();
        var summary = new AcademicSummaryService(provider, () => Db());
        var display = await summary.GetAsync(1, "2026-FALL");
        var report = await new AcademicReportService(provider, () => Db())
            .GenerateAsync(new ReportCriteria("Kết quả học tập", 1));
        Assert.NotEmpty(report);
        Assert.Equal(repeat ? 2.67m : 3.50m, display.CumulativeGpa);
        Assert.All(report, row => Assert.Equal(display.CumulativeGpa, row.CumulativeGpa));
        Assert.All(report.Where(x => x.Semester == "2026-FALL"), row => Assert.Equal(display.SemesterGpa, row.SemesterGpa));
        var csv = ParseCsv(AcademicCsvFormatter.Format(report));
        Assert.Equal(report.Count, csv.Count);
        for (int i = 0; i < report.Count; i++)
        {
            Assert.Equal(report[i].StudentCode, csv[i][0]);
            Assert.Equal(report[i].Score?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", csv[i][8]);
            Assert.Equal(report[i].SemesterGpa?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", csv[i][10]);
            Assert.Equal(report[i].CumulativeGpa?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", csv[i][11]);
        }
    }

    [Fact]
    public async Task Missing_and_unpublished_grades_stay_blank_in_report_and_csv()
    {
        await SignInAsync();
        await ExecuteAsync("UPDATE dbo.Grades SET IsPublished=0 WHERE EnrollmentID=1;");
        var report = await new AcademicReportService(createDb: () => Db())
            .GenerateAsync(new ReportCriteria("Kết quả học tập", 1));
        Assert.Null(report.Single(x => x.Course == "PRN212").Score);
        Assert.Null(report.Single(x => x.Course == "DBI202").Score);
        var summary = await new AcademicSummaryService(createDb: () => Db()).GetAsync(1, "2026-FALL");
        Assert.Equal(3.00m, summary.CumulativeGpa);
        var missing = report.Single(x => x.Course == "DBI202");
        var csv = Assert.Single(ParseCsv(AcademicCsvFormatter.Format(new[] { missing })));
        Assert.Equal("", csv[8]); Assert.Equal("", csv[9]);
    }

    [Fact]
    public async Task No_results_produce_null_GPA_instead_of_zero()
    {
        await SignInAsync();
        var row = await new AcademicSummaryService(createDb: () => Db()).GetAsync(6, "2027-SPRING");
        Assert.Null(row.CumulativeGpa); Assert.Null(row.SemesterGpa);
        var report = await new AcademicReportService(createDb: () => Db())
            .GenerateAsync(new ReportCriteria("Kết quả học tập", 6));
        Assert.NotEmpty(report);
        Assert.All(report, x => { Assert.Null(x.CumulativeGpa); Assert.Null(x.Score); });
        Assert.All(ParseCsv(AcademicCsvFormatter.Format(report)), x => Assert.Equal("", x[11]));
    }

    [Fact]
    public async Task Lecturer_reports_do_not_expose_unassigned_classes_or_global_GPA()
    {
        await SignInAsync("lecturer01");
        var rows = await new AcademicReportService(createDb: () => Db()).GenerateAsync(new ReportCriteria("Kết quả học tập"));
        Assert.NotEmpty(rows);
        Assert.DoesNotContain(rows, x => x.Class == "BUS101-01");
        Assert.All(rows, x => Assert.Null(x.CumulativeGpa));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new AcademicSummaryService(createDb: () => Db()).GetAsync(1, "2026-FALL"));
    }
    [Fact]
    public async Task Shared_summary_enforces_student_ownership_and_F14_permission()
    {
        await SignInAsync("student01");
        var summary = new AcademicSummaryService(createDb: () => Db());
        Assert.Equal(3.50m, (await summary.GetAsync(1, "2026-FALL")).CumulativeGpa);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => summary.GetAsync(2, "2026-FALL"));
        await ExecuteAsync("DELETE rp FROM dbo.RolePermissions rp JOIN dbo.Roles r ON r.RoleID=rp.RoleID JOIN dbo.Permissions p ON p.PermissionID=rp.PermissionID WHERE r.RoleCode='STUDENT' AND p.FunctionCode='F14'");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => summary.GetAsync(1, "2026-FALL"));
    }

    private static List<string[]> ParseCsv(string text)
    {
        using var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(new StringReader(text));
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.ReadFields(); // header
        var rows = new List<string[]>();
        while (!parser.EndOfData) rows.Add(parser.ReadFields()!);
        return rows;
    }
}
