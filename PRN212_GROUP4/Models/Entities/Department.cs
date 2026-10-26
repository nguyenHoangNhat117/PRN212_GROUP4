using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class Department
{
    public int DepartmentId { get; set; }

    public string DepartmentCode { get; set; } = null!;

    public string DepartmentName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<AcademicStaff> AcademicStaffs { get; set; } = new List<AcademicStaff>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ICollection<Lecturer> Lecturers { get; set; } = new List<Lecturer>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
