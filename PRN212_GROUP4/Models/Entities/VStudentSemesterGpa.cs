using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class VStudentSemesterGpa
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = null!;

    public string SemesterCode { get; set; } = null!;

    public decimal? SemesterGpa { get; set; }

    public int? CountedCredits { get; set; }
}
