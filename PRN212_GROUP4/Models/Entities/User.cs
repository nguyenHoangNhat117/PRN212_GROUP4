using System;
using System.Collections.Generic;

namespace PRN212_GROUP4.Models.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string PasswordSalt { get; set; } = null!;

    public string PasswordAlgorithm { get; set; } = null!;

    public int PasswordIterations { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public bool MustChangePassword { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVer { get; set; } = null!;

    public virtual AcademicStaff? AcademicStaff { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual Lecturer? Lecturer { get; set; }

    public virtual Student? Student { get; set; }

    public virtual ICollection<StudentRecord> StudentRecords { get; set; } = new List<StudentRecord>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
