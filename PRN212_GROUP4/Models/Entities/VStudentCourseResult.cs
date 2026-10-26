using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class VStudentCourseResult
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = null!;

    public string CourseCode { get; set; } = null!;

    public string CourseName { get; set; } = null!;

    public byte Credits { get; set; }

    public string ClassCode { get; set; } = null!;

    public string SemesterCode { get; set; } = null!;

    public decimal FinalScore { get; set; }

    public string? GradeLetter { get; set; }

    public decimal? GradePoint { get; set; }
}
