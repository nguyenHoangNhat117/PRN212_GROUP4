using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class VStudentGpa
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = null!;

    public decimal? CumulativeGpa { get; set; }

    public int? CountedCredits { get; set; }
}
