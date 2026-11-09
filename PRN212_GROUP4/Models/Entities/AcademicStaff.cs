using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class AcademicStaff
{
    public int StaffId { get; set; }

    public string StaffCode { get; set; } = null!;

    public int? UserId { get; set; }

    public string FullName { get; set; } = null!;

    public int? DepartmentId { get; set; }

    public bool IsActive { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Position { get; set; }

    public DateOnly? HireDate { get; set; }

    public byte[] RowVer { get; set; } = null!;

    public virtual Department? Department { get; set; }

    public virtual User? User { get; set; }
}
