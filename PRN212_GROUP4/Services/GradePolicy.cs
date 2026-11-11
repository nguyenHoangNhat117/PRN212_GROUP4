using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

/// <summary>
/// Quy tắc điểm dùng chung F11–F15 (BL-021, BL-027).
///
/// ĐÂY LÀ QUY TẮC TẠM THỜI, khớp với ràng buộc của database:
///   - Thang điểm 0–10, tối đa 2 chữ số thập phân (Grades.FinalScore decimal(5,2)).
///   - Điểm chữ / điểm hệ 4 lấy từ bảng GradeScale (không hard-code trong code).
/// Nhóm cần họp để chốt chính thức; khi đổi chỉ sửa tại đây và bảng GradeScale.
/// </summary>
public static class GradePolicy
{
    public const decimal MinScore = 0m;
    public const decimal MaxScore = 10m;
    public const int Decimals = 2;

    /// <summary>
    /// Tạm thời: môn "đạt" khi điểm hệ 4 lớn hơn 0 (chỉ mức F bị tính là rớt).
    /// Chưa có quy định chính thức của nhóm/trường.
    /// </summary>
    public static bool IsPassed(decimal? gradePoint) => gradePoint > 0m;

    public static string? Validate(decimal score)
    {
        if (score < MinScore || score > MaxScore)
        {
            return $"Điểm phải nằm trong khoảng {MinScore:0}–{MaxScore:0}.";
        }

        if (decimal.Round(score, Decimals) != score)
        {
            return $"Điểm tối đa {Decimals} chữ số thập phân.";
        }

        return null;
    }

    /// <summary>Chấp nhận cả "8.5" và "8,5".</summary>
    public static bool TryParse(
        string? text,
        out decimal score,
        out string error)
    {
        score = 0;
        error = "";

        var normalized = (text ?? "").Trim().Replace(',', '.');

        if (normalized.Length == 0)
        {
            error = "Nhập điểm.";
            return false;
        }

        if (!decimal.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out score))
        {
            error = "Điểm không hợp lệ.";
            return false;
        }

        var problem = Validate(score);

        if (problem != null)
        {
            error = problem;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Mức điểm của một điểm số. Cùng cách chọn với view vStudentCourseResults:
    /// mức đang hoạt động chứa điểm, ưu tiên MinScore cao nhất.
    /// </summary>
    public static GradeScale? Band(
        decimal? score,
        IEnumerable<GradeScale> scale)
    {
        if (score is not decimal value)
        {
            return null;
        }

        return scale
            .Where(band =>
                band.IsActive
                && value >= band.MinScore
                && value <= band.MaxScore)
            .OrderByDescending(band => band.MinScore)
            .FirstOrDefault();
    }

    public static string Describe(IEnumerable<GradeScale> scale)
    {
        return string.Join(
            "  ·  ",
            scale
                .Where(band => band.IsActive)
                .OrderByDescending(band => band.MinScore)
                .Select(band =>
                    $"{band.GradeLetter}: "
                    + $"{band.MinScore:0.##}–{band.MaxScore:0.##} "
                    + $"→ {band.GradePoint:0.00}"));
    }
}

