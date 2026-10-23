/*
    PRN212 GROUP 4
    Bổ sung dữ liệu mẫu cho sinh viên hiện có.

    - Không xóa database hoặc sinh viên.
    - Chỉ bổ sung thông tin đang NULL hoặc trống.
    - Có thể chạy lại mà không ghi đè thông tin đã có.
    - Ngày sinh, email và số điện thoại sinh ra là dữ liệu giả.
*/

USE [PRN212_StudentManagement_G4];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE dbo.Students
    SET
        DateOfBirth = COALESCE(
            DateOfBirth,
            DATEADD(
                DAY,
                StudentID % 365,
                CONVERT(date, '20050101', 112)
            )
        ),

        Email = CASE
            WHEN NULLIF(LTRIM(RTRIM(Email)), N'') IS NULL
            THEN LOWER(StudentCode) + N'@example.com'
            ELSE Email
        END,

        Phone = CASE
            WHEN NULLIF(LTRIM(RTRIM(Phone)), '') IS NULL
            THEN '09' + RIGHT(
                '00000000' + CONVERT(varchar(10), StudentID),
                8
            )
            ELSE Phone
        END,

        EnrollmentDate = COALESCE(
            EnrollmentDate,
            CONVERT(date, '20260901', 112)
        )
    WHERE
        DateOfBirth IS NULL
        OR NULLIF(LTRIM(RTRIM(Email)), N'') IS NULL
        OR NULLIF(LTRIM(RTRIM(Phone)), '') IS NULL
        OR EnrollmentDate IS NULL;

    DECLARE @UpdatedRows int = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT
        N'Cập nhật dữ liệu mẫu thành công' AS Result,
        @UpdatedRows AS UpdatedStudents;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

-- Xem dữ liệu sau khi cập nhật
SELECT
    s.StudentCode,
    s.FullName,
    s.DateOfBirth,
    s.Email,
    s.Phone,
    d.DepartmentName,
    s.EnrollmentDate,
    s.StudentStatus
FROM dbo.Students AS s
LEFT JOIN dbo.Departments AS d
    ON d.DepartmentID = s.DepartmentID
ORDER BY s.StudentCode;