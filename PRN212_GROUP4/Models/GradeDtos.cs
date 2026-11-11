using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.Models;

// DTO dùng chung cho F11–F15 (Điểm, GPA, Tiến độ học tập).
// Phần của Nguyễn Tấn Lực (CE190710).

/// <summary>Một dòng điểm / một sinh viên được xếp lớp (F11, F12, F13).</summary>
public sealed class GradeRow
{
    public int EnrollmentId { get; set; }
    public int? GradeId { get; set; }
    public int StudentId { get; set; }
    public int ClassId { get; set; }

    public string StudentCode { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Course { get; set; } = "";
    public int Credits { get; set; }
    public string Class { get; set; } = "";
    public string Semester { get; set; } = "";

    /// <summary>ENROLLED / COMPLETED (trạng thái xếp lớp).</summary>
    public string EnrollmentStatus { get; set; } = "";

    public decimal? Score { get; set; }
    public string Letter { get; set; } = "";
    public bool IsPublished { get; set; }

    /// <summary>RowVer của Grades, dùng phát hiện sửa đồng thời (F12).</summary>
    public byte[]? RowVer { get; set; }

    public string ScoreText =>
        Score.HasValue ? Score.Value.ToString("0.00") : "Chưa có điểm";

    public string PublishedText =>
        !Score.HasValue ? "" : IsPublished ? "Đã công bố" : "Chưa công bố";

    public string StatusText => EnrollmentStatus switch
    {
        "ENROLLED" => "Đang học",
        "COMPLETED" => "Hoàn thành",
        _ => EnrollmentStatus
    };
}

public sealed record GradeLookups(
    List<Lookup> Students,
    List<Lookup> Semesters,
    List<Lookup> Classes);

// ---------- F14: GPA ----------

public sealed class SemesterGpaRow
{
    public string Semester { get; set; } = "";
    public decimal? Gpa { get; set; }
    public int Credits { get; set; }

    public string GpaText =>
        Gpa.HasValue ? Gpa.Value.ToString("0.00") : "Chưa có dữ liệu";
}

public sealed class GpaCourseRow
{
    public string Semester { get; set; } = "";
    public string Course { get; set; } = "";
    public string Class { get; set; } = "";
    public int Credits { get; set; }
    public decimal Score { get; set; }
    public string Letter { get; set; } = "";
    public decimal? Point { get; set; }

    public string ScoreText => Score.ToString("0.00");

    public string PointText =>
        Point.HasValue ? Point.Value.ToString("0.00") : "—";
}

public sealed class GpaSummary
{
    public string StudentCode { get; set; } = "";
    public string StudentName { get; set; } = "";
    public decimal? CumulativeGpa { get; set; }
    public int CountedCredits { get; set; }
    public bool HasRepeatedCourses { get; set; }
    public string ScaleText { get; set; } = "";

    public List<SemesterGpaRow> Semesters { get; set; } = new();
    public List<GpaCourseRow> Courses { get; set; } = new();

    public bool HasData => CumulativeGpa.HasValue;

    public string CumulativeText =>
        CumulativeGpa.HasValue
            ? CumulativeGpa.Value.ToString("0.00")
            : "Chưa có dữ liệu";
}

// ---------- F15: Tiến độ học tập ----------

public sealed class ProgressCourseRow
{
    public string Semester { get; set; } = "";
    public string Course { get; set; } = "";
    public string Class { get; set; } = "";
    public int Credits { get; set; }
    public string Status { get; set; } = "";
    public decimal? Score { get; set; }
    public string Letter { get; set; } = "";

    public string ScoreText =>
        Score.HasValue ? Score.Value.ToString("0.00") : "—";
}

public sealed class ProgressSummary
{
    public string StudentCode { get; set; } = "";
    public string StudentName { get; set; } = "";

    /// <summary>Lecturer không được xem GPA tổng hợp (có thể chứa lớp khác).</summary>
    public bool ShowGpa { get; set; }

    public decimal? CumulativeGpa { get; set; }

    public int CreditsRegistered { get; set; }
    public int CreditsInProgress { get; set; }
    public int CreditsWithResult { get; set; }
    public int CreditsPassed { get; set; }

    public List<ProgressCourseRow> Courses { get; set; } = new();

    public string GpaText =>
        !ShowGpa ? "Không áp dụng"
        : CumulativeGpa.HasValue ? CumulativeGpa.Value.ToString("0.00")
        : "Chưa có dữ liệu";
}

