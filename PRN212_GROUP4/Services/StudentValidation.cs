using System.Net.Mail;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services;

internal static class StudentValidation
{
    internal static void ValidateStudent(Student student, bool isNew)
    {
        if (isNew)
        {
            student.StudentCode =
                student.StudentCode?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(student.StudentCode))
            {
                throw new ArgumentException("Vui lòng nhập mã sinh viên.");
            }

            if (student.StudentCode.Length > 20)
            {
                throw new ArgumentException(
                    "Mã sinh viên không được vượt quá 20 ký tự.");
            }
        }

        student.FullName = student.FullName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(student.FullName))
        {
            throw new ArgumentException("Vui lòng nhập họ tên sinh viên.");
        }

        if (student.FullName.Length > 120)
        {
            throw new ArgumentException(
                "Họ tên không được vượt quá 120 ký tự.");
        }

        student.Email = NormalizeOptionalText(student.Email);
        student.Phone = NormalizeOptionalText(student.Phone);

        if (student.Email != null)
        {
            bool valid =
                student.Email.Length <= 255
                && MailAddress.TryCreate(student.Email, out var parsed)
                && string.Equals(
                    parsed.Address,
                    student.Email,
                    StringComparison.OrdinalIgnoreCase);

            if (!valid)
            {
                throw new ArgumentException("Email không hợp lệ.");
            }
        }

        if (student.Phone != null && student.Phone.Length > 30)
        {
            throw new ArgumentException(
                "Số điện thoại không được vượt quá 30 ký tự.");
        }

        if (student.DateOfBirth.HasValue)
        {
            student.DateOfBirth = student.DateOfBirth.Value.Date;

            if (student.DateOfBirth.Value > DateTime.Today)
            {
                throw new ArgumentException(
                    "Ngày sinh không được lớn hơn ngày hiện tại.");
            }
        }

        if (student.EnrollmentDate.HasValue)
        {
            student.EnrollmentDate = student.EnrollmentDate.Value.Date;
        }

        string status =
            student.StudentStatus?.Trim().ToUpperInvariant()
            ?? string.Empty;

        if (isNew && string.IsNullOrWhiteSpace(status))
        {
            status = "ACTIVE";
        }

        if (status != "ACTIVE"
            && status != "INACTIVE"
            && status != "GRADUATED"
            && status != "SUSPENDED")
        {
            throw new ArgumentException(
                "Trạng thái sinh viên không hợp lệ.");
        }

        student.StudentStatus = status;
    }

    internal static void ValidateStudentRecord(StudentRecord record)
    {
        record.RecordType = record.RecordType?.Trim() ?? string.Empty;
        record.RecordTitle = record.RecordTitle?.Trim() ?? string.Empty;
        record.RecordContent = NormalizeOptionalText(record.RecordContent);

        if (string.IsNullOrWhiteSpace(record.RecordType))
        {
            throw new ArgumentException("Vui lòng nhập loại hồ sơ.");
        }

        if (record.RecordType.Length > 60)
        {
            throw new ArgumentException(
                "Loại hồ sơ không được vượt quá 60 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(record.RecordTitle))
        {
            throw new ArgumentException("Vui lòng nhập tiêu đề hồ sơ.");
        }

        if (record.RecordTitle.Length > 180)
        {
            throw new ArgumentException(
                "Tiêu đề không được vượt quá 180 ký tự.");
        }

        if (record.RecordDate == default)
        {
            throw new ArgumentException("Vui lòng chọn ngày ghi nhận hồ sơ.");
        }

        record.RecordDate = record.RecordDate.Date;
    }

    internal static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    internal static bool IsUniqueConstraintError(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && (sqlException.Number == 2601
                || sqlException.Number == 2627);
    }
}