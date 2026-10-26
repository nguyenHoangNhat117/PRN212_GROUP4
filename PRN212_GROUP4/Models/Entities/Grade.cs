using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class Grade
{
    public int GradeId { get; set; }

    public int EnrollmentId { get; set; }

    public decimal FinalScore { get; set; }

    public bool IsPublished { get; set; }

    public int? UpdatedByUserId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVer { get; set; } = null!;

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual User? UpdatedByUser { get; set; }
}
