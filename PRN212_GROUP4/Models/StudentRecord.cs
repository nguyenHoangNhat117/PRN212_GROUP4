using System;

namespace PRN212_GROUP4.Models;

public class StudentRecord
{
    public int RecordID { get; set; }

    public int StudentID { get; set; }

    public string RecordType { get; set; } = "";

    public string? DocumentNumber { get; set; }

    public string RecordTitle { get; set; } = "";

    public string? RecordContent { get; set; }

    public DateTime RecordDate { get; set; }

    public bool IsArchived { get; set; }

    public int? CreatedByUserID { get; set; }

    public DateTime CreatedAt { get; set; }

    public byte[] RowVer { get; set; } = Array.Empty<byte>();

    public string ArchiveStatus =>
        IsArchived ? "ARCHIVED" : "ACTIVE";
}