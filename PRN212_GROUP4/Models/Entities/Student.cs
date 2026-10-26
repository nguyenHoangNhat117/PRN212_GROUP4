using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class Student
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = null!;

    public int? UserId { get; set; }

    public string FullName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int? DepartmentId { get; set; }

    public DateOnly? EnrollmentDate { get; set; }

    public string StudentStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public byte[] RowVer { get; set; } = null!;

    public virtual ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<StudentRecord> StudentRecords { get; set; } = new List<StudentRecord>();

    public virtual User? User { get; set; }
}
