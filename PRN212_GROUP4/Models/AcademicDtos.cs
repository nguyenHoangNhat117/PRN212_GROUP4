namespace PRN212_GROUP4.Models;

public sealed record Lookup(int Id, string Name);

public sealed class EnrollmentRow
{
    public int RegistrationId { get; set; }
    public int StudentId { get; set; }
    public int SemesterId { get; set; }
    public int? ClassId { get; set; }

    public string StudentCode { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Course { get; set; } = "";
    public string Semester { get; set; } = "";
    public string Status { get; set; } = "";
    public string Class { get; set; } = "";
}

public sealed class AcademicReportRow
{
    public string StudentCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Department { get; set; } = "";
    public string Status { get; set; } = "";
    public string Semester { get; set; } = "";
    public string Class { get; set; } = "";
    public string Course { get; set; } = "";

    public int? Credits { get; set; }
    public decimal? Score { get; set; }
    public string? Letter { get; set; }
    public decimal? SemesterGpa { get; set; }
    public decimal? CumulativeGpa { get; set; }
}

public sealed record ReportCriteria(
    string Type,
    int StudentId = 0,
    int SemesterId = 0,
    int ClassId = 0);