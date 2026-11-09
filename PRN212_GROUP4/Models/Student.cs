using System;

namespace PRN212_GROUP4.Models;

public class Student
{
    public int StudentID { get; set; }

    public string StudentCode { get; set; } = "";

    public int? UserID { get; set; }

    public string FullName { get; set; } = "";

    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public int? DepartmentID { get; set; }

    public string? DepartmentName { get; set; }

    public int? MajorID { get; set; }

    public string? MajorCode { get; set; }

    public string? MajorName { get; set; }

    public string? Cohort { get; set; }

    public DateTime? EnrollmentDate { get; set; }

    public string StudentStatus { get; set; } = "ACTIVE";

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public DateTime CreatedAt { get; set; }

    public byte[] RowVer { get; set; } = Array.Empty<byte>();
}