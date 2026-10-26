using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class Semester
{
    public int SemesterId { get; set; }

    public string SemesterCode { get; set; } = null!;

    public string SemesterName { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();
}
