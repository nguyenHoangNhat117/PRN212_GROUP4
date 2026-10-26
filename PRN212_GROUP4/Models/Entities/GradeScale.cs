using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class GradeScale
{
    public int GradeScaleId { get; set; }

    public decimal MinScore { get; set; }

    public decimal MaxScore { get; set; }

    public decimal GradePoint { get; set; }

    public string GradeLetter { get; set; } = null!;

    public bool IsActive { get; set; }
}
