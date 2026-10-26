using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PRN212_GROUP4.Models
{
    public class DatabaseService
    {
        // Không còn đọc biến môi trường tại đây.
        // StudentDataContext đọc kết nối từ appsettings.json.

        public bool TestConnection()
        {
            using var db = new StudentDataContext();

            return db.Database.CanConnect();
        }

        public async Task<bool> TestConnectionAsync()
        {
            using var db = new StudentDataContext();

            return await db.Database.CanConnectAsync();
        }

        // ==========================================
        // SINH VIÊN
        // ==========================================

        public async Task<List<Student>> GetStudentsAsync()
        {
            using var db = new StudentDataContext();

            // LEFT JOIN để vẫn lấy sinh viên chưa có khoa.
            var query =
                from student in db.Students.AsNoTracking()
                join department in db.Departments.AsNoTracking()
                    on student.DepartmentID
                    equals (int?)department.DepartmentID
                    into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                orderby student.StudentCode
                select new Student
                {
                    StudentID = student.StudentID,
                    StudentCode = student.StudentCode,
                    UserID = student.UserID,
                    FullName = student.FullName,
                    DateOfBirth = student.DateOfBirth,
                    Email = student.Email,
                    Phone = student.Phone,
                    DepartmentID = student.DepartmentID,

                    DepartmentName = department == null
                        ? null
                        : department.DepartmentName,

                    EnrollmentDate = student.EnrollmentDate,
                    StudentStatus = student.StudentStatus,
                    CreatedAt = student.CreatedAt,
                    RowVer = student.RowVer
                };

            return await query.ToListAsync();
        }

        public async Task<List<Department>> GetDepartmentsAsync()
        {
            using var db = new StudentDataContext();

            return await db.Departments
                .AsNoTracking()
                .Where(department => department.IsActive)
                .OrderBy(department => department.DepartmentName)
                .ToListAsync();
        }

        public async Task<int> AddStudentAsync(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            ValidateStudent(student, isNew: true);

            using var db = new StudentDataContext();

            // Giữ việc kiểm tra khoa và thêm sinh viên
            // trong cùng một transaction.
            using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            if (student.DepartmentID.HasValue)
            {
                int departmentID = student.DepartmentID.Value;

                bool departmentExists =
                    await db.Departments.AnyAsync(
                        department =>
                            department.DepartmentID == departmentID
                            && department.IsActive);

                if (!departmentExists)
                {
                    throw new InvalidOperationException(
                        "Khoa đã chọn không tồn tại "
                        + "hoặc đã ngừng hoạt động.");
                }
            }

            bool duplicateCode =
                await db.Students.AnyAsync(
                    existing =>
                        existing.StudentCode == student.StudentCode);

            if (duplicateCode)
            {
                throw new InvalidOperationException(
                    "Mã sinh viên đã tồn tại. "
                    + "Vui lòng nhập mã khác.");
            }

            // Tạo entity riêng, không thay đổi ID của object trên UI
            // trước khi lưu thành công.
            var entity = new Student
            {
                StudentCode = student.StudentCode,

                // Giữ hành vi cũ: thêm hồ sơ chưa tạo tài khoản.
                UserID = null,

                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Email = student.Email,
                Phone = student.Phone,
                DepartmentID = student.DepartmentID,
                EnrollmentDate = student.EnrollmentDate,
                StudentStatus = student.StudentStatus
            };

            db.Students.Add(entity);

            try
            {
                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                return entity.StudentID;
            }
            catch (DbUpdateException ex)
                when (IsUniqueConstraintError(ex))
            {
                throw new InvalidOperationException(
                    "Dữ liệu bị trùng với hồ sơ đã tồn tại. "
                    + "Hãy kiểm tra mã sinh viên và email.",
                    ex);
            }
        }

        public async Task UpdateStudentAsync(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            if (student.StudentID <= 0)
            {
                throw new ArgumentException(
                    "Sinh viên không hợp lệ.");
            }

            if (student.RowVer == null
                || student.RowVer.Length != 8)
            {
                throw new ArgumentException(
                    "Thiếu phiên bản dữ liệu. "
                    + "Hãy tải lại danh sách sinh viên.");
            }

            ValidateStudent(student, isNew: false);

            using var db = new StudentDataContext();

            using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // Lấy sinh viên hiện tại để EF theo dõi thay đổi.
            var entity = await db.Students.SingleOrDefaultAsync(
                existing => existing.StudentID == student.StudentID);

            if (entity == null)
            {
                throw new InvalidOperationException(
                    "Sinh viên không còn tồn tại. "
                    + "Hãy tải lại danh sách.");
            }

            // So sánh với phiên bản khi người dùng mở form Edit.
            if (!entity.RowVer.SequenceEqual(student.RowVer))
            {
                throw new InvalidOperationException(
                    "Hồ sơ đã được người khác thay đổi.\n"
                    + "Hãy đóng form, tải lại danh sách "
                    + "và mở lại hồ sơ.");
            }

            // Chỉ kiểm tra khoa hoạt động khi chuyển sang khoa khác.
            // Sinh viên thuộc khoa cũ đã ngừng hoạt động
            // vẫn có thể sửa các thông tin khác.
            if (student.DepartmentID != entity.DepartmentID
                && student.DepartmentID.HasValue)
            {
                int departmentID = student.DepartmentID.Value;

                bool validDepartment =
                    await db.Departments.AnyAsync(
                        department =>
                            department.DepartmentID == departmentID
                            && department.IsActive);

                if (!validDepartment)
                {
                    throw new InvalidOperationException(
                        "Khoa đã chọn không tồn tại "
                        + "hoặc đã ngừng hoạt động.");
                }
            }

            entity.FullName = student.FullName;
            entity.DateOfBirth = student.DateOfBirth;
            entity.Email = student.Email;
            entity.Phone = student.Phone;

            // Cập nhật khoa được chọn.
            entity.DepartmentID = student.DepartmentID;

            entity.EnrollmentDate = student.EnrollmentDate;
            entity.StudentStatus = student.StudentStatus;

            db.Entry(entity)
                .Property(existing => existing.RowVer)
                .OriginalValue = student.RowVer.ToArray();

            try
            {
                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                student.RowVer = entity.RowVer.ToArray();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException(
                    "Không thể cập nhật vì hồ sơ đã được thay đổi "
                    + "hoặc xóa sau khi bạn tải dữ liệu.\n"
                    + "Hãy đóng form, tải lại danh sách "
                    + "và mở lại hồ sơ.",
                    ex);
            }
            catch (DbUpdateException ex)
                when (IsUniqueConstraintError(ex))
            {
                throw new InvalidOperationException(
                    "Thông tin cập nhật bị trùng với hồ sơ khác. "
                    + "Hãy kiểm tra lại dữ liệu.",
                    ex);
            }
        }

        // ==========================================
        // HỒ SƠ HỌC VỤ
        // ==========================================

        public async Task<List<StudentRecord>> GetStudentRecordsAsync(
            int studentID,
            bool includeArchived = false)
        {
            if (studentID <= 0)
            {
                throw new ArgumentException(
                    "Mã định danh sinh viên không hợp lệ.");
            }

            using var db = new StudentDataContext();

            return await db.StudentRecords
                .AsNoTracking()
                .Where(record =>
                    record.StudentID == studentID
                    && (includeArchived || !record.IsArchived))
                .OrderByDescending(record => record.RecordDate)
                .ThenByDescending(record => record.RecordID)
                .ToListAsync();
        }

        public async Task<int> AddStudentRecordAsync(
            StudentRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            if (record.StudentID <= 0)
            {
                throw new ArgumentException(
                    "Vui lòng chọn sinh viên hợp lệ.");
            }

            ValidateStudentRecord(record);

            if (record.CreatedByUserID.HasValue
                && record.CreatedByUserID.Value <= 0)
            {
                throw new ArgumentException(
                    "Tài khoản người tạo không hợp lệ.");
            }

            using var db = new StudentDataContext();

            using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            bool studentExists =
                await db.Students.AnyAsync(
                    student =>
                        student.StudentID == record.StudentID);

            if (!studentExists)
            {
                throw new InvalidOperationException(
                    "Sinh viên không còn tồn tại. "
                    + "Hãy tải lại danh sách sinh viên.");
            }

            var entity = new StudentRecord
            {
                StudentID = record.StudentID,
                RecordType = record.RecordType,
                RecordTitle = record.RecordTitle,
                RecordContent = record.RecordContent,
                RecordDate = record.RecordDate.Date,
                IsArchived = false,
                CreatedByUserID = record.CreatedByUserID
            };

            db.StudentRecords.Add(entity);

            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            return entity.RecordID;
        }

        public async Task UpdateStudentRecordAsync(
            StudentRecord edited,
            StudentRecord original)
        {
            ArgumentNullException.ThrowIfNull(edited);
            ArgumentNullException.ThrowIfNull(original);

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

            ValidateStudentRecord(edited);

            using var db = new StudentDataContext();

            int recordID = original.RecordID;
            int studentID = original.StudentID;

            string originalType = original.RecordType;
            string originalTitle = original.RecordTitle;
            string? originalContent = original.RecordContent;
            DateTime originalDate = original.RecordDate.Date;

            string newType = edited.RecordType;
            string newTitle = edited.RecordTitle;
            string? newContent = edited.RecordContent;
            DateTime newDate = edited.RecordDate.Date;

            // So sánh dữ liệu gốc ngay trong điều kiện UPDATE.
            // Nếu người khác sửa hoặc lưu trữ hồ sơ, sẽ không ghi đè.
            int affectedRows = await db.StudentRecords
                .Where(record =>
                    record.RecordID == recordID
                    && record.StudentID == studentID
                    && !record.IsArchived
                    && record.RecordType == originalType
                    && record.RecordTitle == originalTitle
                    && record.RecordDate == originalDate
                    &&
                    (
                        record.RecordContent == originalContent
                        ||
                        (
                            record.RecordContent == null
                            && originalContent == null
                        )
                    ))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        record => record.RecordType,
                        newType)
                    .SetProperty(
                        record => record.RecordTitle,
                        newTitle)
                    .SetProperty(
                        record => record.RecordContent,
                        newContent)
                    .SetProperty(
                        record => record.RecordDate,
                        newDate));

            if (affectedRows == 0)
            {
                throw new InvalidOperationException(
                    "Hồ sơ đã được thay đổi, lưu trữ hoặc xóa "
                    + "sau khi bạn tải dữ liệu.\n"
                    + "Hãy đóng form, bấm Refresh "
                    + "rồi mở lại hồ sơ.");
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

            using var db = new StudentDataContext();

            int affectedRows = await db.StudentRecords
                .Where(record =>
                    record.RecordID == recordID
                    && record.StudentID == studentID
                    && !record.IsArchived)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        record => record.IsArchived,
                        true));

            if (affectedRows == 0)
            {
                throw new InvalidOperationException(
                    "Hồ sơ không còn tồn tại, không thuộc sinh viên "
                    + "đang chọn hoặc đã được lưu trữ.\n"
                    + "Hãy bấm Refresh để cập nhật danh sách.");
            }
        }

        // ==========================================
        // KIỂM TRA DỮ LIỆU
        // ==========================================

        private static void ValidateStudent(
            Student student,
            bool isNew)
        {
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

            student.Email = NormalizeOptionalText(student.Email);
            student.Phone = NormalizeOptionalText(student.Phone);

            if (student.Email != null)
            {
                bool validEmail =
                    student.Email.Length <= 255
                    && MailAddress.TryCreate(
                        student.Email,
                        out var parsedEmail)
                    && string.Equals(
                        parsedEmail.Address,
                        student.Email,
                        StringComparison.OrdinalIgnoreCase);

                if (!validEmail)
                {
                    throw new ArgumentException(
                        "Email không hợp lệ.");
                }
            }

            if (student.Phone != null
                && student.Phone.Length > 30)
            {
                throw new ArgumentException(
                    "Số điện thoại không được vượt quá 30 ký tự.");
            }

            if (student.DateOfBirth.HasValue)
            {
                student.DateOfBirth =
                    student.DateOfBirth.Value.Date;

                if (student.DateOfBirth.Value > DateTime.Today)
                {
                    throw new ArgumentException(
                        "Ngày sinh không được lớn hơn "
                        + "ngày hiện tại.");
                }
            }

            if (student.EnrollmentDate.HasValue)
            {
                student.EnrollmentDate =
                    student.EnrollmentDate.Value.Date;
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

        private static void ValidateStudentRecord(
            StudentRecord record)
        {
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

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static bool IsUniqueConstraintError(
            DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException
                && (sqlException.Number == 2601
                    || sqlException.Number == 2627);
        }
    }
}