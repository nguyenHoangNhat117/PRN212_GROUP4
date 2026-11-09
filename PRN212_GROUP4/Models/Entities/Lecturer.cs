using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class Lecturer
{
    public int LecturerId { get; set; }

    public string LecturerCode { get; set; } = null!;

    public int? UserId { get; set; }

    public string FullName { get; set; } = null!;

    public int? DepartmentId { get; set; }

    public bool IsActive { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Qualification { get; set; }

    public string? Specialization { get; set; }

    public DateOnly? HireDate { get; set; }

    public byte[] RowVer { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual Department? Department { get; set; }

    public virtual User? User { get; set; }
}
