using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Data;

namespace PRN212_GROUP4.Models
{
    public class DatabaseService
    {
        private readonly string connectionString =
    Environment.GetEnvironmentVariable("PRN212_DB_CONNECTION")
    ?? throw new InvalidOperationException(
        "Chưa cấu hình biến môi trường PRN212_DB_CONNECTION.");

        // Kiểm tra kết nối SQL Server
        public bool TestConnection()
        {
            using SqlConnection connection =
                new SqlConnection(connectionString);

            connection.Open();

            return true;
        }

        // Lấy danh sách sinh viên từ database
        public async Task<List<Student>> GetStudentsAsync()
        {
            List<Student> students = new List<Student>();

            const string query = @"
        SELECT
            s.StudentID,
            s.StudentCode,
            s.UserID,
            s.FullName,
            s.DateOfBirth,
            s.Email,
            s.Phone,
            s.DepartmentID,
            s.EnrollmentDate,
            s.StudentStatus,
            s.CreatedAt,
            s.RowVer,
            d.DepartmentName
        FROM dbo.Students AS s
        LEFT JOIN dbo.Departments AS d
            ON d.DepartmentID = s.DepartmentID
        ORDER BY s.StudentCode;";

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Student student = new Student
                {
                    StudentID = reader.GetInt32(0),

                    StudentCode = reader.GetString(1),

                    UserID = reader.IsDBNull(2)
                        ? null
                        : reader.GetInt32(2),

                    FullName = reader.GetString(3),

                    DateOfBirth = reader.IsDBNull(4)
                        ? null
                        : reader.GetDateTime(4),

                    Email = reader.IsDBNull(5)
                        ? null
                        : reader.GetString(5),

                    Phone = reader.IsDBNull(6)
                        ? null
                        : reader.GetString(6),

                    DepartmentID = reader.IsDBNull(7)
                        ? null
                        : reader.GetInt32(7),

                    EnrollmentDate = reader.IsDBNull(8)
                        ? null
                        : reader.GetDateTime(8),

                    StudentStatus = reader.GetString(9),

                    CreatedAt = reader.GetDateTime(10),

                    RowVer = (byte[])reader.GetValue(11),

                    DepartmentName = reader.IsDBNull(12)
                        ? null
                        : reader.GetString(12)
                };

                students.Add(student);
            }

            return students;
        }
        // Lấy các khoa đang hoạt động để chọn khi thêm sinh viên.
        public async Task<List<Department>> GetDepartmentsAsync()
        {
            List<Department> departments = new List<Department>();

            const string query = @"
        SELECT
            DepartmentID,
            DepartmentCode,
            DepartmentName,
            IsActive
        FROM dbo.Departments
        WHERE IsActive = 1
        ORDER BY DepartmentName;";

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Department department = new Department
                {
                    DepartmentID = reader.GetInt32(0),

                    DepartmentCode = reader.GetString(1),

                    DepartmentName = reader.GetString(2),

                    IsActive = reader.GetBoolean(3)
                };

                departments.Add(department);
            }

            return departments;
        }
        // Thêm hồ sơ sinh viên, chưa tạo tài khoản đăng nhập.
        // Trả về StudentID vừa được tạo.
        public async Task<int> AddStudentAsync(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException(nameof(student));
            }

            string studentCode = student.StudentCode.Trim();
            string fullName = student.FullName.Trim();

            string? email = string.IsNullOrWhiteSpace(student.Email)
                ? null
                : student.Email.Trim();

            string? phone = string.IsNullOrWhiteSpace(student.Phone)
                ? null
                : student.Phone.Trim();

            string status = string.IsNullOrWhiteSpace(student.StudentStatus)
                ? "ACTIVE"
                : student.StudentStatus.Trim().ToUpperInvariant();

            // Kiểm tra thông tin bắt buộc.
            if (string.IsNullOrWhiteSpace(studentCode))
            {
                throw new ArgumentException("Vui lòng nhập mã sinh viên.");
            }

            if (studentCode.Length > 20)
            {
                throw new ArgumentException(
                    "Mã sinh viên không được vượt quá 20 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Vui lòng nhập họ tên sinh viên.");
            }

            if (fullName.Length > 120)
            {
                throw new ArgumentException(
                    "Họ tên không được vượt quá 120 ký tự.");
            }

            if (email != null)
            {
                if (email.Length > 255
                    || !System.Net.Mail.MailAddress.TryCreate(
                        email,
                        out var parsedEmail)
                    || !string.Equals(
                        parsedEmail.Address,
                        email,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Email không hợp lệ.");
                }
            }

            if (phone != null && phone.Length > 30)
            {
                throw new ArgumentException(
                    "Số điện thoại không được vượt quá 30 ký tự.");
            }

            if (student.DateOfBirth.HasValue
                && student.DateOfBirth.Value.Date > DateTime.Today)
            {
                throw new ArgumentException(
                    "Ngày sinh không được lớn hơn ngày hiện tại.");
            }

            if (status != "ACTIVE"
                && status != "INACTIVE"
                && status != "GRADUATED"
                && status != "SUSPENDED")
            {
                throw new ArgumentException(
                    "Trạng thái sinh viên không hợp lệ.");
            }

            const string query = @"
        INSERT INTO dbo.Students
        (
            StudentCode,
            UserID,
            FullName,
            DateOfBirth,
            Email,
            Phone,
            DepartmentID,
            EnrollmentDate,
            StudentStatus
        )
        OUTPUT INSERTED.StudentID
        SELECT
            @StudentCode,
            NULL,
            @FullName,
            @DateOfBirth,
            @Email,
            @Phone,
            @DepartmentID,
            @EnrollmentDate,
            @StudentStatus
        WHERE
            @DepartmentID IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM dbo.Departments
                WHERE DepartmentID = @DepartmentID
                  AND IsActive = 1
            );";

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.Add(
                "@StudentCode",
                SqlDbType.VarChar,
                20).Value = studentCode;

            command.Parameters.Add(
                "@FullName",
                SqlDbType.NVarChar,
                120).Value = fullName;

            command.Parameters.Add(
                "@DateOfBirth",
                SqlDbType.Date).Value =
                (object?)student.DateOfBirth?.Date ?? DBNull.Value;

            command.Parameters.Add(
                "@Email",
                SqlDbType.NVarChar,
                255).Value =
                (object?)email ?? DBNull.Value;

            command.Parameters.Add(
                "@Phone",
                SqlDbType.VarChar,
                30).Value =
                (object?)phone ?? DBNull.Value;

            command.Parameters.Add(
                "@DepartmentID",
                SqlDbType.Int).Value =
                (object?)student.DepartmentID ?? DBNull.Value;

            command.Parameters.Add(
                "@EnrollmentDate",
                SqlDbType.Date).Value =
                (object?)student.EnrollmentDate?.Date ?? DBNull.Value;

            command.Parameters.Add(
                "@StudentStatus",
                SqlDbType.VarChar,
                20).Value = status;

            await connection.OpenAsync();

            try
            {
                object? result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "Khoa đã chọn không tồn tại hoặc đã ngừng hoạt động.");
                }

                return Convert.ToInt32(result);
            }
            catch (SqlException ex)
                when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new InvalidOperationException(
                    "Mã sinh viên đã tồn tại. Vui lòng nhập mã khác.",
                    ex);
            }
        }
        public async Task UpdateStudentAsync(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException(nameof(student));
            }

            if (student.StudentID <= 0)
            {
                throw new ArgumentException("Sinh viên không hợp lệ.");
            }

            if (student.RowVer == null || student.RowVer.Length != 8)
            {
                throw new ArgumentException(
                    "Thiếu phiên bản dữ liệu. Hãy tải lại danh sách sinh viên.");
            }

            string fullName = student.FullName.Trim();

            string? email = string.IsNullOrWhiteSpace(student.Email)
                ? null
                : student.Email.Trim();

            string? phone = string.IsNullOrWhiteSpace(student.Phone)
                ? null
                : student.Phone.Trim();

            string status = student.StudentStatus.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Vui lòng nhập họ tên.");
            }

            if (fullName.Length > 120)
            {
                throw new ArgumentException(
                    "Họ tên không được vượt quá 120 ký tự.");
            }

            if (email != null)
            {
                if (email.Length > 255
                    || !System.Net.Mail.MailAddress.TryCreate(
                        email,
                        out var parsedEmail)
                    || !string.Equals(
                        parsedEmail.Address,
                        email,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Email không hợp lệ.");
                }
            }

            if (phone != null && phone.Length > 30)
            {
                throw new ArgumentException(
                    "Số điện thoại không được vượt quá 30 ký tự.");
            }

            if (student.DateOfBirth.HasValue
                && student.DateOfBirth.Value.Date > DateTime.Today)
            {
                throw new ArgumentException(
                    "Ngày sinh không được lớn hơn ngày hiện tại.");
            }

            if (status != "ACTIVE"
                && status != "INACTIVE"
                && status != "GRADUATED"
                && status != "SUSPENDED")
            {
                throw new ArgumentException("Trạng thái không hợp lệ.");
            }

            const string query = @"
        UPDATE dbo.Students
        SET
            FullName = @FullName,
            DateOfBirth = @DateOfBirth,
            Email = @Email,
            Phone = @Phone,
            EnrollmentDate = @EnrollmentDate,
            StudentStatus = @StudentStatus
        OUTPUT INSERTED.RowVer
        WHERE StudentID = @StudentID
          AND RowVer = @RowVer;";

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.Add(
                "@StudentID",
                SqlDbType.Int).Value = student.StudentID;

            command.Parameters.Add(
                "@FullName",
                SqlDbType.NVarChar,
                120).Value = fullName;

            command.Parameters.Add(
                "@DateOfBirth",
                SqlDbType.Date).Value =
                (object?)student.DateOfBirth?.Date ?? DBNull.Value;

            command.Parameters.Add(
                "@Email",
                SqlDbType.NVarChar,
                255).Value =
                (object?)email ?? DBNull.Value;

            command.Parameters.Add(
                "@Phone",
                SqlDbType.VarChar,
                30).Value =
                (object?)phone ?? DBNull.Value;

            command.Parameters.Add(
                "@EnrollmentDate",
                SqlDbType.Date).Value =
                (object?)student.EnrollmentDate?.Date ?? DBNull.Value;

            command.Parameters.Add(
                "@StudentStatus",
                SqlDbType.VarChar,
                20).Value = status;

            command.Parameters.Add(
                "@RowVer",
                SqlDbType.Binary,
                8).Value = student.RowVer;

            await connection.OpenAsync();

            object? result = await command.ExecuteScalarAsync();

            if (result is not byte[] newRowVersion)
            {
                throw new InvalidOperationException(
                    "Không thể cập nhật vì hồ sơ đã được thay đổi "
                    + "hoặc xóa sau khi bạn tải dữ liệu.\n"
                    + "Hãy đóng form, tải lại danh sách và mở lại hồ sơ.");
            }

            student.RowVer = newRowVersion;
        }
        // Lấy hồ sơ học vụ của một sinh viên.
        // Mặc định chỉ lấy hồ sơ chưa lưu trữ.
        // includeArchived = true: lấy cả hồ sơ đã lưu trữ.
        public async Task<List<StudentRecord>> GetStudentRecordsAsync(
            int studentID,
            bool includeArchived = false)
        {
            if (studentID <= 0)
            {
                throw new ArgumentException(
                    "Mã định danh sinh viên không hợp lệ.");
            }

            List<StudentRecord> records = new List<StudentRecord>();

            const string query = @"
        SELECT
            RecordID,
            StudentID,
            RecordType,
            RecordTitle,
            RecordContent,
            RecordDate,
            IsArchived,
            CreatedByUserID,
            CreatedAt
        FROM dbo.StudentRecords
        WHERE StudentID = @StudentID
          AND
          (
              @IncludeArchived = 1
              OR IsArchived = 0
          )
        ORDER BY RecordDate DESC, RecordID DESC;";

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.Add(
                "@StudentID",
                SqlDbType.Int).Value = studentID;

            command.Parameters.Add(
                "@IncludeArchived",
                SqlDbType.Bit).Value = includeArchived;

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                StudentRecord record = new StudentRecord
                {
                    RecordID = reader.GetInt32(0),

                    StudentID = reader.GetInt32(1),

                    RecordType = reader.GetString(2),

                    RecordTitle = reader.GetString(3),

                    RecordContent = reader.IsDBNull(4)
                        ? null
                        : reader.GetString(4),

                    RecordDate = reader.GetDateTime(5),

                    IsArchived = reader.GetBoolean(6),

                    CreatedByUserID = reader.IsDBNull(7)
                        ? null
                        : reader.GetInt32(7),

                    CreatedAt = reader.GetDateTime(8)
                };

                records.Add(record);
            }

            return records;
        }

        // Thêm bản ghi học vụ cho một sinh viên.
        // Trả về RecordID vừa được tạo.
        public async Task<int> AddStudentRecordAsync(StudentRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (record.StudentID <= 0)
            {
                throw new ArgumentException(
                    "Vui lòng chọn sinh viên hợp lệ.");
            }

            string recordType = record.RecordType?.Trim() ?? string.Empty;

            string recordTitle = record.RecordTitle?.Trim() ?? string.Empty;

            string? recordContent =
                string.IsNullOrWhiteSpace(record.RecordContent)
                    ? null
                    : record.RecordContent.Trim();

            if (string.IsNullOrWhiteSpace(recordType))
            {
                throw new ArgumentException(
                    "Vui lòng nhập loại hồ sơ.");
            }

            if (recordType.Length > 60)
            {
                throw new ArgumentException(
                    "Loại hồ sơ không được vượt quá 60 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(recordTitle))
            {
                throw new ArgumentException(
                    "Vui lòng nhập tiêu đề hồ sơ.");
            }

            if (recordTitle.Length > 180)
            {
                throw new ArgumentException(
                    "Tiêu đề không được vượt quá 180 ký tự.");
            }

            if (record.RecordDate == default)
            {
                throw new ArgumentException(
                    "Vui lòng chọn ngày ghi nhận hồ sơ.");
            }

            if (record.CreatedByUserID.HasValue
                && record.CreatedByUserID.Value <= 0)
            {
                throw new ArgumentException(
                    "Tài khoản người tạo không hợp lệ.");
            }

            const string query = @"
        INSERT INTO dbo.StudentRecords
        (
            StudentID,
            RecordType,
            RecordTitle,
            RecordContent,
            RecordDate,
            IsArchived,
            CreatedByUserID
        )
        OUTPUT INSERTED.RecordID
        SELECT
            @StudentID,
            @RecordType,
            @RecordTitle,
            @RecordContent,
            @RecordDate,
            0,
            @CreatedByUserID
        FROM dbo.Students
        WHERE StudentID = @StudentID;";

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.Add(
                "@StudentID",
                SqlDbType.Int).Value = record.StudentID;

            command.Parameters.Add(
                "@RecordType",
                SqlDbType.NVarChar,
                60).Value = recordType;

            command.Parameters.Add(
                "@RecordTitle",
                SqlDbType.NVarChar,
                180).Value = recordTitle;

            command.Parameters.Add(
                "@RecordContent",
                SqlDbType.NVarChar,
                -1).Value =
                (object?)recordContent ?? DBNull.Value;

            command.Parameters.Add(
                "@RecordDate",
                SqlDbType.Date).Value = record.RecordDate.Date;

            command.Parameters.Add(
                "@CreatedByUserID",
                SqlDbType.Int).Value =
                (object?)record.CreatedByUserID ?? DBNull.Value;

            await connection.OpenAsync();

            object? result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Sinh viên không còn tồn tại. "
                    + "Hãy tải lại danh sách sinh viên.");
            }

            return Convert.ToInt32(result);
        }

        public async Task UpdateStudentRecordAsync(
            StudentRecord edited,
            StudentRecord original)
        {
            if (edited == null || original == null)
            {
                throw new ArgumentException("Hồ sơ không hợp lệ.");
            }

            if (original.RecordID <= 0
                || original.StudentID <= 0
                || edited.RecordID != original.RecordID
                || edited.StudentID != original.StudentID)
            {
                throw new ArgumentException(
                    "Thông tin định danh hồ sơ không hợp lệ.");
            }

            if (original.IsArchived)
            {
                throw new InvalidOperationException(
                    "Không thể chỉnh sửa hồ sơ đã lưu trữ.");
            }

            string recordType =
                edited.RecordType?.Trim() ?? string.Empty;

            string recordTitle =
                edited.RecordTitle?.Trim() ?? string.Empty;

            string? recordContent =
                string.IsNullOrWhiteSpace(edited.RecordContent)
                    ? null
                    : edited.RecordContent.Trim();

            if (string.IsNullOrWhiteSpace(recordType))
            {
                throw new ArgumentException("Vui lòng nhập loại hồ sơ.");
            }

            if (recordType.Length > 60)
            {
                throw new ArgumentException(
                    "Loại hồ sơ không được vượt quá 60 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(recordTitle))
            {
                throw new ArgumentException("Vui lòng nhập tiêu đề hồ sơ.");
            }

            if (recordTitle.Length > 180)
            {
                throw new ArgumentException(
                    "Tiêu đề không được vượt quá 180 ký tự.");
            }

            if (edited.RecordDate == default)
            {
                throw new ArgumentException(
                    "Vui lòng chọn ngày ghi nhận hồ sơ.");
            }

            const string query = @"
        UPDATE dbo.StudentRecords
        SET
            RecordType = @RecordType,
            RecordTitle = @RecordTitle,
            RecordContent = @RecordContent,
            RecordDate = @RecordDate
        OUTPUT INSERTED.RecordID
        WHERE RecordID = @RecordID
          AND StudentID = @StudentID
          AND IsArchived = 0
          AND RecordType = @OriginalType
          AND RecordTitle = @OriginalTitle
          AND RecordDate = @OriginalDate
          AND
          (
              RecordContent = @OriginalContent
              OR
              (
                  RecordContent IS NULL
                  AND @OriginalContent IS NULL
              )
          );";

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.Add(
                "@RecordID",
                SqlDbType.Int).Value = original.RecordID;

            command.Parameters.Add(
                "@StudentID",
                SqlDbType.Int).Value = original.StudentID;

            command.Parameters.Add(
                "@RecordType",
                SqlDbType.NVarChar,
                60).Value = recordType;

            command.Parameters.Add(
                "@RecordTitle",
                SqlDbType.NVarChar,
                180).Value = recordTitle;

            command.Parameters.Add(
                "@RecordContent",
                SqlDbType.NVarChar,
                -1).Value =
                (object?)recordContent ?? DBNull.Value;

            command.Parameters.Add(
                "@RecordDate",
                SqlDbType.Date).Value = edited.RecordDate.Date;

            // Thông tin gốc dùng để phát hiện thay đổi trước khi lưu.
            command.Parameters.Add(
                "@OriginalType",
                SqlDbType.NVarChar,
                60).Value = original.RecordType;

            command.Parameters.Add(
                "@OriginalTitle",
                SqlDbType.NVarChar,
                180).Value = original.RecordTitle;

            command.Parameters.Add(
                "@OriginalContent",
                SqlDbType.NVarChar,
                -1).Value =
                (object?)original.RecordContent ?? DBNull.Value;

            command.Parameters.Add(
                "@OriginalDate",
                SqlDbType.Date).Value = original.RecordDate.Date;

            await connection.OpenAsync();

            object? result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Hồ sơ đã được thay đổi, lưu trữ hoặc xóa "
                    + "sau khi bạn tải dữ liệu.\n"
                    + "Hãy đóng form, bấm Refresh rồi mở lại hồ sơ.");
            }
        }

        public async Task ArchiveStudentRecordAsync(
    int studentID,
    int recordID)
        {
            if (studentID <= 0 || recordID <= 0)
            {
                throw new ArgumentException(
                    "Thông tin hồ sơ không hợp lệ.");
            }

            const string query = @"
        UPDATE dbo.StudentRecords
        SET IsArchived = 1
        OUTPUT INSERTED.RecordID
        WHERE RecordID = @RecordID
          AND StudentID = @StudentID
          AND IsArchived = 0;";

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.Add(
                "@StudentID",
                SqlDbType.Int).Value = studentID;

            command.Parameters.Add(
                "@RecordID",
                SqlDbType.Int).Value = recordID;

            await connection.OpenAsync();

            object? result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Hồ sơ không còn tồn tại, không thuộc sinh viên "
                    + "đang chọn hoặc đã được lưu trữ.\n"
                    + "Hãy bấm Refresh để cập nhật danh sách.");
            }
        }
    }
}