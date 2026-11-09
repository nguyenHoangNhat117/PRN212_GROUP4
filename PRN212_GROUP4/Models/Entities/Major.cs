using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class Major
{
    public int MajorId { get; set; }

    public string MajorCode { get; set; } = null!;

    public string MajorName { get; set; } = null!;

    public int DepartmentId { get; set; }

    public bool IsActive { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
