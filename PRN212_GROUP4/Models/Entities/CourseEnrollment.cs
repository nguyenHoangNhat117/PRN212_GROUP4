using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class CourseEnrollment
{
    public int CourseEnrollmentId { get; set; }

    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public int SemesterId { get; set; }

    public DateTime RegistrationDate { get; set; }

    public string RegistrationStatus { get; set; } = null!;

    public byte[] RowVer { get; set; } = null!;

    public virtual Course Course { get; set; } = null!;

    public virtual Enrollment? Enrollment { get; set; }

    public virtual Semester Semester { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
