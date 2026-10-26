using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class Class
{
    public int ClassId { get; set; }

    public string ClassCode { get; set; } = null!;

    public int CourseId { get; set; }

    public int SemesterId { get; set; }

    public int? LecturerId { get; set; }

    public int? Capacity { get; set; }

    public string? ScheduleNote { get; set; }

    public string ClassStatus { get; set; } = null!;

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual Lecturer? Lecturer { get; set; }

    public virtual Semester Semester { get; set; } = null!;
}
