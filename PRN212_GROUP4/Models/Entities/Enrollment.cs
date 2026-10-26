using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class Enrollment
{
    public int EnrollmentId { get; set; }

    public int CourseEnrollmentId { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public DateTime EnrollmentDate { get; set; }

    public string EnrollmentStatus { get; set; } = null!;

    public byte[] RowVer { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual CourseEnrollment CourseEnrollment { get; set; } = null!;

    public virtual Grade? Grade { get; set; }

    public virtual Student Student { get; set; } = null!;
}
