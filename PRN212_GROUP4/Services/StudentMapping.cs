using System;
using System.Linq;
using E = PRN212_GROUP4.Models.Entities;
using D = PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services;

internal static class StudentMapping
{
    public static D.Student ToDto(E.Student student) => new()
    {
        StudentID = student.StudentId,
        StudentCode = student.StudentCode,
        UserID = student.UserId,
        FullName = student.FullName,

        Gender = student.Gender,
        Address = student.Address,

        MajorID = student.MajorId,
        MajorCode = student.Major?.MajorCode,
        MajorName = student.Major?.MajorName,

        Cohort = student.Cohort,

        EmergencyContactName = student.EmergencyContactName,
        EmergencyContactPhone = student.EmergencyContactPhone,

        Email = student.Email,
        Phone = student.Phone,

        DepartmentID = student.DepartmentId,
        DepartmentName = student.Department?.DepartmentName,

        DateOfBirth =
            student.DateOfBirth?.ToDateTime(TimeOnly.MinValue),

        EnrollmentDate =
            student.EnrollmentDate?.ToDateTime(TimeOnly.MinValue),

        StudentStatus = student.StudentStatus,
        CreatedAt = student.CreatedAt,
        RowVer = student.RowVer.ToArray()
    };

    public static D.StudentRecord ToDto(E.StudentRecord record) => new()
    {
        RecordID = record.RecordId,
        StudentID = record.StudentId,
        RecordType = record.RecordType,
        DocumentNumber = record.DocumentNumber,
        RecordTitle = record.RecordTitle,
        RecordContent = record.RecordContent,

        RecordDate =
            record.RecordDate.ToDateTime(TimeOnly.MinValue),

        IsArchived = record.IsArchived,
        CreatedByUserID = record.CreatedByUserId,
        CreatedAt = record.CreatedAt,

        RowVer = record.RowVer.ToArray()
    };
}