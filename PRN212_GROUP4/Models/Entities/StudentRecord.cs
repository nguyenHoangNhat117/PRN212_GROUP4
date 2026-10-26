using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class StudentRecord
{
    public int RecordId { get; set; }

    public int StudentId { get; set; }

    public string RecordType { get; set; } = null!;

    public string RecordTitle { get; set; } = null!;

    public string? RecordContent { get; set; }

    public DateOnly RecordDate { get; set; }

    public bool IsArchived { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual Student Student { get; set; } = null!;
}
