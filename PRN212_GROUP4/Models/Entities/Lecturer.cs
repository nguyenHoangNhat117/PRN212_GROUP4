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

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual Department? Department { get; set; }

    public virtual User? User { get; set; }
}
