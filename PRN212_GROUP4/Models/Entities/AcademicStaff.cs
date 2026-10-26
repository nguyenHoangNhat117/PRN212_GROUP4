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

    public virtual Department? Department { get; set; }

    public virtual User? User { get; set; }
}
