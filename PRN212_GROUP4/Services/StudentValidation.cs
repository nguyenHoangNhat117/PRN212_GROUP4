using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models;

namespace PRN212_GROUP4.Services;

internal static class StudentValidation
{
    internal static void ValidateStudent(Student student, bool isNew)
    {
        // Validate the student code when creating a new student.
        if (isNew)
        {
            student.StudentCode =
                student.StudentCode?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(student.StudentCode))
            {
                throw new ArgumentException(
                    "Vui lòng nhập mã sinh viên.");
            }

            if (student.StudentCode.Length > 20)
            {
                throw new ArgumentException(
                    "Mã sinh viên không được vượt quá 20 ký tự.");
            }
        }

        student.FullName =
            student.FullName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(student.FullName))
        {
            throw new ArgumentException(
                "Vui lòng nhập họ tên sinh viên.");
        }

        if (student.FullName.Length > 120)
        {
            throw new ArgumentException(
                "Họ tên không được vượt quá 120 ký tự.");
        }

        // Remove outer spaces and convert blank values to null.
        student.Email = NormalizeOptionalText(student.Email);
        student.Phone = NormalizeOptionalText(student.Phone);

        if (student.Email != null)
        {
            // Check the email format and reject display-name input.
            if (student.Email.Length > 255
                || !MailAddress.TryCreate(
                    student.Email,
                    out var parsed)
                || !string.Equals(
                    parsed.Address,
                    student.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Email không hợp lệ.");
            }

            // Only the exact gmail.com domain is allowed.
            if (!string.Equals(
                parsed.Host,
                "gmail.com",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Email phải sử dụng đuôi @gmail.com.");
            }
        }

        // Allow 8–15 digits and an optional leading plus sign.
        // Letters, internal spaces, and separators are not allowed.
        if (student.Phone != null
            && !Regex.IsMatch(
                student.Phone,
                @"^\+?[0-9]{8,15}$"))
        {
            throw new ArgumentException(
                "Điện thoại cần 8–15 chữ số, "
                + "có thể bắt đầu bằng dấu +.");
        }

        if (student.DateOfBirth.HasValue)
        {
            // Compare dates without the time component.
            student.DateOfBirth =
                student.DateOfBirth.Value.Date;

            if (student.DateOfBirth.Value > DateTime.Today)
            {
                throw new ArgumentException(
                    "Ngày sinh không được lớn hơn ngày hiện tại.");
            }
        }

        if (student.EnrollmentDate.HasValue)
        {
            student.EnrollmentDate =
                student.EnrollmentDate.Value.Date;
        }

        // Normalize the status before checking allowed values.
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

    internal static void ValidateStudentRecord(
        StudentRecord record)
    {
        // Normalize the document number for consistent storage.
        record.DocumentNumber =
            NormalizeOptionalText(record.DocumentNumber)
                ?.ToUpperInvariant();

        if (record.DocumentNumber == null
            || record.DocumentNumber.Length > 80)
        {
            throw new ArgumentException(
                "Mã chứng từ là bắt buộc và không vượt quá 80 ký tự.");
        }

        record.RecordType =
            record.RecordType?.Trim() ?? string.Empty;

        record.RecordTitle =
            record.RecordTitle?.Trim() ?? string.Empty;

        record.RecordContent =
            NormalizeOptionalText(record.RecordContent);

        if (string.IsNullOrWhiteSpace(record.RecordType))
        {
            throw new ArgumentException(
                "Vui lòng nhập loại hồ sơ.");
        }

        if (record.RecordType.Length > 60)
        {
            throw new ArgumentException(
                "Loại hồ sơ không được vượt quá 60 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(record.RecordTitle))
        {
            throw new ArgumentException(
                "Vui lòng nhập tiêu đề hồ sơ.");
        }

        if (record.RecordTitle.Length > 180)
        {
            throw new ArgumentException(
                "Tiêu đề không được vượt quá 180 ký tự.");
        }

        if (record.RecordDate == default)
        {
            throw new ArgumentException(
                "Vui lòng chọn ngày ghi nhận hồ sơ.");
        }

        record.RecordDate = record.RecordDate.Date;
    }

    internal static string? NormalizeOptionalText(string? value)
    {
        // Keep optional fields null when no text is provided.
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    internal static bool IsUniqueConstraintError(
        DbUpdateException exception)
    {
        // SQL Server uses these error codes for duplicate values
        // in a unique index or a unique constraint.
        return exception.InnerException is SqlException sqlException
            && (sqlException.Number == 2601
                || sqlException.Number == 2627);
    }
}