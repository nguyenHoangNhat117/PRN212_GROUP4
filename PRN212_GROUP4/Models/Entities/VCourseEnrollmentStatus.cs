using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class VCourseEnrollmentStatus
{
    public int CourseEnrollmentId { get; set; }

    public int StudentId { get; set; }

    public string StudentCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public int CourseId { get; set; }

    public string CourseCode { get; set; } = null!;

    public string CourseName { get; set; } = null!;

    public int SemesterId { get; set; }

    public string SemesterCode { get; set; } = null!;

    public DateTime RegistrationDate { get; set; }

    public string RegistrationStatus { get; set; } = null!;

    public int? EnrollmentId { get; set; }

    public int? ClassId { get; set; }

    public string? ClassCode { get; set; }

    public string? EnrollmentStatus { get; set; }
}
