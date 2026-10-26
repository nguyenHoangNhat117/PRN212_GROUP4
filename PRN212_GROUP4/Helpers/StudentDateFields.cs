using System;
using System.Globalization;
using PRN212_GROUP4.Models;
using PRN212_GROUP4.ViewModels;

namespace PRN212_GROUP4.Helpers
{
    public class StudentDateFields : ViewModelBase
    {
        private static readonly string[] AcceptedFormats =
        {
            "dd/MM/yyyy",
            "d/MM/yyyy",
            "dd/M/yyyy",
            "d/M/yyyy"
        };

        private string dateOfBirthText;
        private string enrollmentDateText;

        public StudentDateFields(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            dateOfBirthText = FormatDate(student.DateOfBirth);
            enrollmentDateText = FormatDate(student.EnrollmentDate);
        }

        public string DateOfBirthText
        {
            get => dateOfBirthText;

            set
            {
                if (SetProperty(
                    ref dateOfBirthText,
                    value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DateOfBirthCalendar));
                }
            }
        }

        public string EnrollmentDateText
        {
            get => enrollmentDateText;

            set
            {
                if (SetProperty(
                    ref enrollmentDateText,
                    value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(EnrollmentDateCalendar));
                }
            }
        }

        // Dùng cho nút chọn lịch.
        public DateTime? DateOfBirthCalendar
        {
            get => ParseForCalendar(DateOfBirthText);

            set
            {
                if (value.HasValue)
                {
                    DateOfBirthText = FormatDate(value);
                }
            }
        }

        public DateTime? EnrollmentDateCalendar
        {
            get => ParseForCalendar(EnrollmentDateText);

            set
            {
                if (value.HasValue)
                {
                    EnrollmentDateText = FormatDate(value);
                }
            }
        }

        public void ValidateAndApply(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            // Kiểm tra cả hai trước khi thay đổi model.
            DateTime? birthDate = ParseOptionalDate(
                DateOfBirthText,
                "Ngày sinh");

            DateTime? enrollmentDate = ParseOptionalDate(
                EnrollmentDateText,
                "Ngày nhập học");

            if (birthDate.HasValue
                && birthDate.Value > DateTime.Today)
            {
                throw new ArgumentException(
                    "Ngày sinh không được lớn hơn ngày hiện tại.");
            }

            if (enrollmentDate.HasValue
                && enrollmentDate.Value > DateTime.Today)
            {
                throw new ArgumentException(
                    "Ngày nhập học không được lớn hơn ngày hiện tại.");
            }

            if (birthDate.HasValue
                && enrollmentDate.HasValue
                && enrollmentDate.Value <= birthDate.Value)
            {
                throw new ArgumentException(
                    "Ngày nhập học phải sau ngày sinh.");
            }

            student.DateOfBirth = birthDate;
            student.EnrollmentDate = enrollmentDate;

            // Chuẩn hóa hiển thị sau khi kiểm tra thành công.
            DateOfBirthText = FormatDate(birthDate);
            EnrollmentDateText = FormatDate(enrollmentDate);
        }

        private static DateTime? ParseOptionalDate(
            string? text,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (!TryParseDate(text, out DateTime date))
            {
                throw new ArgumentException(
                    $"{fieldName} không hợp lệ. "
                    + "Vui lòng nhập theo dạng dd/MM/yyyy, "
                    + "ví dụ 20/10/2004.");
            }

            return date.Date;
        }

        private static DateTime? ParseForCalendar(string? text)
        {
            return TryParseDate(text, out DateTime date)
                ? date.Date
                : null;
        }

        private static bool TryParseDate(
            string? text,
            out DateTime date)
        {
            return DateTime.TryParseExact(
                text?.Trim(),
                AcceptedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        private static string FormatDate(DateTime? date)
        {
            return date.HasValue
                ? date.Value.ToString(
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture)
                : string.Empty;
        }
    }
}