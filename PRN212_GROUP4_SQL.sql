-- SQL Server 2019+.
-- Chạy toàn bộ script một lần trong SSMS.
-- Tất cả tài khoản mẫu có mật khẩu ban đầu: Group4@2026

USE master;

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF @@TRANCOUNT <> 0
    THROW 51400, 'Run outside an existing transaction.', 1;

IF DB_ID(N'PRN212_StudentManagement_G4') IS NOT NULL
    THROW 51000,
        'Database already exists. Back it up and rename it first. Nothing changed.',
        1;

EXEC(N'CREATE DATABASE [PRN212_StudentManagement_G4]');

EXEC sys.sp_executesql N'
USE [PRN212_StudentManagement_G4];

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRANSACTION;

-- =========================================================
-- A. TÀI KHOẢN VÀ PHÂN QUYỀN
-- =========================================================

CREATE TABLE dbo.Roles (
    RoleID int IDENTITY PRIMARY KEY,
    RoleCode varchar(30) NOT NULL UNIQUE,
    RoleName nvarchar(80) NOT NULL
);

CREATE TABLE dbo.Permissions (
    PermissionID int IDENTITY PRIMARY KEY,
    FunctionCode char(3) NOT NULL UNIQUE,
    FunctionName nvarchar(100) NOT NULL,
    CONSTRAINT CK_Permissions_FunctionCode CHECK (
        FunctionCode LIKE ''F0[1-9]''
        OR FunctionCode LIKE ''F1[0-9]''
        OR FunctionCode = ''F20''
    )
);

CREATE TABLE dbo.RolePermissions (
    RoleID int NOT NULL REFERENCES dbo.Roles(RoleID),
    PermissionID int NOT NULL REFERENCES dbo.Permissions(PermissionID),
    CONSTRAINT PK_RolePermissions PRIMARY KEY(RoleID, PermissionID)
);

CREATE TABLE dbo.Users (
    UserID int IDENTITY PRIMARY KEY,
    Username nvarchar(80) NOT NULL UNIQUE,
    PasswordHash varchar(256) NOT NULL,
    PasswordSalt varchar(64) NOT NULL,
    PasswordAlgorithm varchar(50) NOT NULL DEFAULT ''PBKDF2-SHA256'',
    PasswordIterations int NOT NULL DEFAULT 210000
        CHECK (PasswordIterations >= 100000),
    DisplayName nvarchar(120) NOT NULL,
    Email nvarchar(255) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    MustChangePassword bit NOT NULL DEFAULT 1,
    CreatedAt datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2(0) NULL,
    RowVer rowversion
);

CREATE UNIQUE INDEX UX_Users_Email_NotNull
ON dbo.Users(Email) WHERE Email IS NOT NULL;

CREATE TABLE dbo.UserRoles (
    UserID int NOT NULL REFERENCES dbo.Users(UserID),
    RoleID int NOT NULL REFERENCES dbo.Roles(RoleID),
    CONSTRAINT PK_UserRoles PRIMARY KEY(UserID, RoleID)
);

-- =========================================================
-- B. KHOA, NGÀNH VÀ HỒ SƠ
-- =========================================================

CREATE TABLE dbo.Departments (
    DepartmentID int IDENTITY PRIMARY KEY,
    DepartmentCode varchar(20) NOT NULL UNIQUE,
    DepartmentName nvarchar(120) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1
);

CREATE TABLE dbo.Students (
    StudentID int IDENTITY PRIMARY KEY,
    StudentCode varchar(20) NOT NULL UNIQUE,
    UserID int NULL REFERENCES dbo.Users(UserID),
    FullName nvarchar(120) NOT NULL,
    DateOfBirth date NULL,
    Email nvarchar(255) NULL,
    Phone varchar(30) NULL,
    DepartmentID int NULL REFERENCES dbo.Departments(DepartmentID),
    EnrollmentDate date NULL,
    StudentStatus varchar(20) NOT NULL DEFAULT ''ACTIVE'',
    CreatedAt datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVer rowversion,
    CONSTRAINT CK_Students_Status CHECK (
        StudentStatus IN (
            ''ACTIVE'', ''INACTIVE'', ''GRADUATED'', ''SUSPENDED''
        )
    )
);

CREATE UNIQUE INDEX UX_Students_UserID_NotNull
ON dbo.Students(UserID) WHERE UserID IS NOT NULL;

CREATE TABLE dbo.Lecturers (
    LecturerID int IDENTITY PRIMARY KEY,
    LecturerCode varchar(20) NOT NULL UNIQUE,
    UserID int NULL REFERENCES dbo.Users(UserID),
    FullName nvarchar(120) NOT NULL,
    DepartmentID int NULL REFERENCES dbo.Departments(DepartmentID),
    IsActive bit NOT NULL DEFAULT 1
);

CREATE UNIQUE INDEX UX_Lecturers_UserID_NotNull
ON dbo.Lecturers(UserID) WHERE UserID IS NOT NULL;

CREATE TABLE dbo.AcademicStaff (
    StaffID int IDENTITY PRIMARY KEY,
    StaffCode varchar(20) NOT NULL UNIQUE,
    UserID int NULL REFERENCES dbo.Users(UserID),
    FullName nvarchar(120) NOT NULL,
    DepartmentID int NULL REFERENCES dbo.Departments(DepartmentID),
    IsActive bit NOT NULL DEFAULT 1
);

CREATE UNIQUE INDEX UX_AcademicStaff_UserID_NotNull
ON dbo.AcademicStaff(UserID) WHERE UserID IS NOT NULL;

CREATE TABLE dbo.Courses (
    CourseID int IDENTITY PRIMARY KEY,
    CourseCode varchar(20) NOT NULL UNIQUE,
    CourseName nvarchar(150) NOT NULL,
    Credits tinyint NOT NULL CHECK (Credits BETWEEN 1 AND 15),
    DepartmentID int NULL REFERENCES dbo.Departments(DepartmentID),
    IsActive bit NOT NULL DEFAULT 1
);

CREATE TABLE dbo.Semesters (
    SemesterID int IDENTITY PRIMARY KEY,
    SemesterCode varchar(30) NOT NULL UNIQUE,
    SemesterName nvarchar(100) NOT NULL,
    StartDate date NOT NULL,
    EndDate date NOT NULL,
    CONSTRAINT CK_Semesters_Dates CHECK (EndDate >= StartDate)
);

CREATE TABLE dbo.Classes (
    ClassID int IDENTITY PRIMARY KEY,
    ClassCode varchar(30) NOT NULL UNIQUE,
    CourseID int NOT NULL REFERENCES dbo.Courses(CourseID),
    SemesterID int NOT NULL REFERENCES dbo.Semesters(SemesterID),
    LecturerID int NULL REFERENCES dbo.Lecturers(LecturerID),
    Capacity int NULL CHECK (Capacity > 0),
    ScheduleNote nvarchar(250) NULL,
    ClassStatus varchar(15) NOT NULL DEFAULT ''OPEN'',
    CONSTRAINT CK_Classes_Status CHECK (
        ClassStatus IN (''OPEN'', ''CLOSED'', ''CANCELLED'')
    )
);

-- =========================================================
-- C. ĐĂNG KÝ MÔN VÀ XẾP LỚP
-- =========================================================

CREATE TABLE dbo.CourseEnrollments (
    CourseEnrollmentID int IDENTITY
        CONSTRAINT PK_CourseEnrollments PRIMARY KEY,
    StudentID int NOT NULL REFERENCES dbo.Students(StudentID),
    CourseID int NOT NULL REFERENCES dbo.Courses(CourseID),
    SemesterID int NOT NULL REFERENCES dbo.Semesters(SemesterID),
    RegistrationDate datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    RegistrationStatus varchar(15) NOT NULL DEFAULT ''REGISTERED'',
    RowVer rowversion,
    CONSTRAINT UQ_CourseEnrollments_Student_Course_Term
        UNIQUE(StudentID, CourseID, SemesterID),
    CONSTRAINT CK_CourseEnrollments_Status CHECK (
        RegistrationStatus IN (''REGISTERED'', ''DROPPED'', ''COMPLETED'')
    )
);

CREATE INDEX IX_CourseEnrollments_Course_Term
ON dbo.CourseEnrollments(CourseID, SemesterID);

CREATE TABLE dbo.Enrollments (
    EnrollmentID int IDENTITY PRIMARY KEY,
    CourseEnrollmentID int NOT NULL
        REFERENCES dbo.CourseEnrollments(CourseEnrollmentID),
    StudentID int NOT NULL REFERENCES dbo.Students(StudentID),
    ClassID int NOT NULL REFERENCES dbo.Classes(ClassID),
    EnrollmentDate datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    EnrollmentStatus varchar(15) NOT NULL DEFAULT ''ENROLLED'',
    RowVer rowversion,
    CONSTRAINT UQ_Enrollments_Student_Class UNIQUE(StudentID, ClassID),
    CONSTRAINT CK_Enrollments_Status CHECK (
        EnrollmentStatus IN (''ENROLLED'', ''DROPPED'', ''COMPLETED'')
    )
);

CREATE UNIQUE INDEX UX_Enrollments_ActiveRegistration
ON dbo.Enrollments(CourseEnrollmentID)
WHERE EnrollmentStatus <> ''DROPPED'';

-- =========================================================
-- D. ĐIỂM, HỒ SƠ HỌC VỤ, AUDIT
-- =========================================================

CREATE TABLE dbo.Grades (
    GradeID int IDENTITY PRIMARY KEY,
    EnrollmentID int NOT NULL UNIQUE
        REFERENCES dbo.Enrollments(EnrollmentID),
    FinalScore decimal(5,2) NOT NULL CHECK (FinalScore BETWEEN 0 AND 10),
    IsPublished bit NOT NULL DEFAULT 0,
    UpdatedByUserID int NULL REFERENCES dbo.Users(UserID),
    UpdatedAt datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVer rowversion
);

CREATE TABLE dbo.GradeScale (
    GradeScaleID int IDENTITY PRIMARY KEY,
    MinScore decimal(5,2) NOT NULL,
    MaxScore decimal(5,2) NOT NULL,
    GradePoint decimal(3,2) NOT NULL,
    GradeLetter varchar(5) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT CK_GradeScale_Range CHECK (
        MinScore BETWEEN 0 AND 10
        AND MaxScore BETWEEN 0 AND 10
        AND MinScore <= MaxScore
    ),
    CONSTRAINT CK_GradeScale_Point CHECK (GradePoint BETWEEN 0 AND 4)
);

CREATE TABLE dbo.StudentRecords (
    RecordID int IDENTITY PRIMARY KEY,
    StudentID int NOT NULL REFERENCES dbo.Students(StudentID),
    RecordType nvarchar(60) NOT NULL,
    DocumentNumber nvarchar(80) NULL,
    RecordTitle nvarchar(180) NOT NULL,
    RecordContent nvarchar(max) NULL,
    RecordDate date NOT NULL DEFAULT CONVERT(date, GETDATE()),
    IsArchived bit NOT NULL DEFAULT 0,
    CreatedByUserID int NULL REFERENCES dbo.Users(UserID),
    CreatedAt datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.AuditLogs (
    AuditID bigint IDENTITY PRIMARY KEY,
    ActorUserID int NULL REFERENCES dbo.Users(UserID),
    ActionName varchar(80) NOT NULL,
    EntityName varchar(80) NOT NULL,
    EntityKey nvarchar(100) NULL,
    Details nvarchar(2000) NULL,
    HappenedAt datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE INDEX IX_Enrollments_Student ON dbo.Enrollments(StudentID);
CREATE INDEX IX_Enrollments_Class ON dbo.Enrollments(ClassID);
CREATE INDEX IX_Classes_Course_Semester
    ON dbo.Classes(CourseID, SemesterID);
CREATE INDEX IX_Grades_Published ON dbo.Grades(IsPublished);
CREATE INDEX IX_Records_BusinessKey
    ON dbo.StudentRecords(StudentID, RecordType, DocumentNumber);
CREATE INDEX IX_Records_Student
    ON dbo.StudentRecords(StudentID, IsArchived);
CREATE INDEX IX_AuditLogs_Time ON dbo.AuditLogs(HappenedAt DESC);

-- Giữ các cột cũ để code hiện tại tiếp tục chạy.
-- Bổ sung thông tin cá nhân và ngành học.

CREATE TABLE dbo.Majors (
    MajorID int IDENTITY PRIMARY KEY,
    MajorCode varchar(20) NOT NULL UNIQUE,
    MajorName nvarchar(120) NOT NULL,
    DepartmentID int NOT NULL REFERENCES dbo.Departments(DepartmentID),
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Majors_Department UNIQUE(MajorID, DepartmentID)
);

ALTER TABLE dbo.Users ADD
    FullName nvarchar(120) NULL,
    DateOfBirth date NULL,
    Gender nvarchar(10) NULL,
    Phone varchar(30) NULL,
    Address nvarchar(250) NULL,
    AvatarUrl nvarchar(500) NULL;

ALTER TABLE dbo.Students ADD
    Gender nvarchar(10) NULL,
    Address nvarchar(250) NULL,
    MajorID int NULL,
    Cohort varchar(20) NULL,
    EmergencyContactName nvarchar(120) NULL,
    EmergencyContactPhone varchar(30) NULL;

ALTER TABLE dbo.Students ADD CONSTRAINT FK_Students_Major
    FOREIGN KEY(MajorID, DepartmentID)
    REFERENCES dbo.Majors(MajorID, DepartmentID);

ALTER TABLE dbo.Students ADD CONSTRAINT CK_Students_MajorDepartment
    CHECK (MajorID IS NULL OR DepartmentID IS NOT NULL);

ALTER TABLE dbo.Lecturers ADD
    DateOfBirth date NULL,
    Gender nvarchar(10) NULL,
    Email nvarchar(255) NULL,
    Phone varchar(30) NULL,
    Address nvarchar(250) NULL,
    Qualification nvarchar(80) NULL,
    Specialization nvarchar(120) NULL,
    HireDate date NULL,
    RowVer rowversion;

ALTER TABLE dbo.AcademicStaff ADD
    DateOfBirth date NULL,
    Gender nvarchar(10) NULL,
    Email nvarchar(255) NULL,
    Phone varchar(30) NULL,
    Address nvarchar(250) NULL,
    Position nvarchar(80) NULL,
    HireDate date NULL,
    RowVer rowversion;

ALTER TABLE dbo.StudentRecords ADD RowVer rowversion;

ALTER TABLE dbo.Users ADD CONSTRAINT CK_Users_Credentials CHECK (
    PasswordAlgorithm = ''PBKDF2-SHA256''
    AND PasswordIterations BETWEEN 100000 AND 2000000
    AND DATALENGTH(PasswordHash) = 64
    AND PasswordHash COLLATE Latin1_General_100_BIN2
        NOT LIKE ''%[^0-9a-fA-F]%''
    AND DATALENGTH(PasswordSalt) = 32
    AND PasswordSalt COLLATE Latin1_General_100_BIN2
        NOT LIKE ''%[^0-9a-fA-F]%''
);

ALTER TABLE dbo.Students ADD CONSTRAINT CK_Students_Dates CHECK (
    DateOfBirth IS NULL
    OR EnrollmentDate IS NULL
    OR DateOfBirth < EnrollmentDate
);

CREATE INDEX IX_Students_Department
    ON dbo.Students(DepartmentID, StudentCode);

CREATE INDEX IX_Classes_Lecturer
    ON dbo.Classes(LecturerID, SemesterID);

-- =========================================================
-- E. PHÂN QUYỀN MẪU
-- =========================================================

INSERT dbo.Roles(RoleCode, RoleName) VALUES
(''STUDENT'', N''Student''),
(''LECTURER'', N''Lecturer''),
(''ACADEMIC_STAFF'', N''Academic Staff''),
(''ADMIN'', N''System Administrator'');

INSERT dbo.Permissions(FunctionCode, FunctionName) VALUES
(''F01'', N''Login''),
(''F02'', N''Logout''),
(''F03'', N''Manage Student Profile''),
(''F04'', N''Search Student''),
(''F05'', N''Manage Student Records''),
(''F06'', N''Manage Courses''),
(''F07'', N''Manage Classes''),
(''F08'', N''Enroll in Courses''),
(''F09'', N''View Course Enrollment''),
(''F10'', N''Assign Students to Classes''),
(''F11'', N''Record Grades''),
(''F12'', N''Update Grades''),
(''F13'', N''View Grades''),
(''F14'', N''Calculate GPA''),
(''F15'', N''View Academic Progress''),
(''F16'', N''Generate Academic Reports''),
(''F17'', N''Manage User Accounts''),
(''F18'', N''Manage Roles and Permissions''),
(''F19'', N''Backup and Restore Data''),
(''F20'', N''Change Password'');

INSERT dbo.RolePermissions(RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.Roles r
CROSS JOIN dbo.Permissions p
WHERE p.FunctionCode IN (''F01'', ''F02'', ''F20'')
OR (
    r.RoleCode = ''STUDENT''
    AND p.FunctionCode IN (
        ''F03'', ''F08'', ''F09'', ''F13'', ''F14'', ''F15''
    )
)
OR (
    r.RoleCode = ''LECTURER''
    AND p.FunctionCode IN (
        ''F04'', ''F09'', ''F11'', ''F12'', ''F13'', ''F15'', ''F16''
    )
)
OR (
    r.RoleCode = ''ACADEMIC_STAFF''
    AND p.FunctionCode IN (
        ''F03'', ''F04'', ''F05'', ''F06'', ''F07'',
        ''F08'', ''F09'', ''F10'', ''F13'', ''F14'', ''F15'', ''F16''
    )
)
OR (
    r.RoleCode = ''ADMIN''
    AND p.FunctionCode IN (''F17'', ''F18'', ''F19'')
);

INSERT dbo.Departments(DepartmentCode, DepartmentName) VALUES
(''CNTT'', N''Công nghệ thông tin''),
(''KT'', N''Kinh tế'');

INSERT dbo.Majors(MajorCode, MajorName, DepartmentID) VALUES
(''SE'', N''Kỹ thuật phần mềm'', 1),
(''IS'', N''Hệ thống thông tin'', 1),
(''BA'', N''Quản trị kinh doanh'', 2),
(''MKT'', N''Marketing'', 2);

INSERT dbo.Semesters(
    SemesterCode, SemesterName, StartDate, EndDate
) VALUES
(''2025-FALL'', N''Thu 2025'', ''20250901'', ''20251231''),
(''2026-SPRING'', N''Xuân 2026'', ''20260105'', ''20260430''),
(''2026-FALL'', N''Thu 2026'', ''20260901'', ''20261231'');

INSERT dbo.GradeScale(
    MinScore, MaxScore, GradePoint, GradeLetter
) VALUES
(0, 3.99, 0, ''F''),
(4, 4.99, 1, ''D''),
(5, 5.49, 1.5, ''D+''),
(5.5, 6.49, 2, ''C''),
(6.5, 6.99, 2.5, ''C+''),
(7, 7.99, 3, ''B''),
(8, 8.49, 3.5, ''B+''),
(8.5, 10, 4, ''A'');

-- =========================================================
-- F. 33 TÀI KHOẢN, HASH PBKDF2-SHA256, SALT RIÊNG
-- =========================================================

CREATE TABLE #People (
    Username nvarchar(80),
    FullName nvarchar(120),
    DateOfBirth date,
    Gender nvarchar(10),
    Email nvarchar(255),
    Phone varchar(30),
    Address nvarchar(250),
    PasswordHash varchar(64),
    PasswordSalt varchar(32)
);

INSERT #People VALUES
(N''student01'',N''Nguyễn Minh Anh'',N''20050101'',N''Nam'',N''prn212.g4.student01@gmail.com'',N''0900000001'',N''11 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''3db28cbbdd737a270b23de2d2410cf894eda3aa2f4b5ad642caab4847ee27eae'',''9912c5db51307fc28d4a53bf5eeec2e4''),
(N''student02'',N''Trần Bảo Châu'',N''20050202'',N''Nữ'',N''prn212.g4.student02@gmail.com'',N''0900000002'',N''12 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''2434934502e8497c5c96fd4b44d01db6ff73a67d3ae00b7d0aca36ab1a5dacef'',''e1a0e84ba109cfed3f54e132640af9ef''),
(N''student03'',N''Lê Quốc Bảo'',N''20050303'',N''Nam'',N''prn212.g4.student03@gmail.com'',N''0900000003'',N''13 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''385ef409fd79958bbf2d943211c73be7e965148ba1f5caf84bf2690005fff40f'',''14784a24d53ba303772abc83b604c1fe''),
(N''student04'',N''Phạm Gia Hân'',N''20050404'',N''Nữ'',N''prn212.g4.student04@gmail.com'',N''0900000004'',N''14 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''4b4aa42159707ba7bb6ea5e60073f64b74213aaeafda89590b946a3010682d01'',''a4b76a5801f0942b174ee4074a2c95e9''),
(N''student05'',N''Võ Tuấn Kiệt'',N''20050505'',N''Nam'',N''prn212.g4.student05@gmail.com'',N''0900000005'',N''15 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''2bf8b65898210ddfdaa055f046a6a2455b6aa3b7dab65e5469a7a1e3f9b5efed'',''78e711b056ec417a0592bee5e9106e4f''),
(N''student06'',N''Đỗ Thanh Trúc'',N''20050606'',N''Nữ'',N''prn212.g4.student06@gmail.com'',N''0900000006'',N''16 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''8f3d9f90f776364502e990d9ee75f55130305e722779d044c684f563088f78dd'',''a949c6781d853baa5146487041d61b99''),
(N''student07'',N''Bùi Ngọc Linh'',N''20050707'',N''Nam'',N''prn212.g4.student07@gmail.com'',N''0900000007'',N''17 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''83f3f074476e09f5dbd5ac3600cfd9269e1f1926740b7c623374cb906ddc47d0'',''736b25c11497bd4ec349623e8c5253af''),
(N''student08'',N''Đặng Hoàng Nam'',N''20050808'',N''Nữ'',N''prn212.g4.student08@gmail.com'',N''0900000008'',N''18 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''f9457617a5cb5ec277898496e3c5e8959f644fc2cfac2b2a8ebe7a6b5ba5724a'',''5ccb0aa12525f52f2a9bce1c7f590a36''),
(N''student09'',N''Huỳnh Khánh Vy'',N''20050909'',N''Nam'',N''prn212.g4.student09@gmail.com'',N''0900000009'',N''19 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''35198d98a980cdecd2985f9a6cf2a9fa5e813e0fbd5b5b9d231d19b343d86440'',''d1d2c65fc817ece924b7c5f2a9683a6a''),
(N''student10'',N''Phan Đức Huy'',N''20051010'',N''Nữ'',N''prn212.g4.student10@gmail.com'',N''0900000010'',N''20 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''9dada5b9fb7dc4399e6d7a47f4b53fbaf6f551142d50867900c69fc632e2564f'',''abc69aaff36bce8bc32b3869a0e29174''),
(N''student11'',N''Vũ Thảo My'',N''20051111'',N''Nam'',N''prn212.g4.student11@gmail.com'',N''0900000011'',N''21 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''8a758423829332a87c340fde6564834df6c5133f9ddd6428d04d69711686592a'',''4dcd35b707337caf9dd03a22b8110f9b''),
(N''student12'',N''Hoàng Anh Khoa'',N''20051212'',N''Nữ'',N''prn212.g4.student12@gmail.com'',N''0900000012'',N''22 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''66156440bdca42691445b1fecadef119090ad8727b43d31dd9ca06d22fb42a0c'',''7cd5af17c17e5f9621270781fdc3003e''),
(N''student13'',N''Ngô Phương Nhi'',N''20050113'',N''Nam'',N''prn212.g4.student13@gmail.com'',N''0900000013'',N''23 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''c878664fd86b568c507d4714b0e851f3ea241d8e0aa685d74fad3ba67a7dc5cf'',''6931eb7641b8ee3bb9a934f64661bb99''),
(N''student14'',N''Dương Hải Đăng'',N''20050214'',N''Nữ'',N''prn212.g4.student14@gmail.com'',N''0900000014'',N''24 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''69d5d3a639328a6f3977e50d794ce918607a60a3a85dfadd1771f936a09c98c0'',''bcb80717be0d9ca31c00ba472592f6b7''),
(N''student15'',N''Lý Minh Thư'',N''20050315'',N''Nam'',N''prn212.g4.student15@gmail.com'',N''0900000015'',N''25 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''a481370880830b961727f9614116aabf0f6cb286d984d3de7bf33d114f8c1418'',''6f8a63b7d5999fe1975a9ce9b40fd795''),
(N''student16'',N''Trương Nhật Minh'',N''20050416'',N''Nữ'',N''prn212.g4.student16@gmail.com'',N''0900000016'',N''26 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''b961c249dd74ad13183535823db0611e46efd6e5fd9642aef4ebdba99ad96995'',''8b8756e5b0bfcf3b5e942c1a86dbd390''),
(N''student17'',N''Đinh Bảo Ngọc'',N''20050517'',N''Nam'',N''prn212.g4.student17@gmail.com'',N''0900000017'',N''27 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''5a84d22ee758cc0b696e2bd232a016727f60d6a501df3633e0680ac1baad7e80'',''967eaf74a0ad0d264a8a2251d4185c70''),
(N''student18'',N''Mai Quang Huy'',N''20050618'',N''Nữ'',N''prn212.g4.student18@gmail.com'',N''0900000018'',N''28 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''f3f7458b1b9781e87b15f0a50265a09d883b296e94432ef70af8b8e441cb26e5'',''aa72102ed2ab9d96e9b8d6c2358948ec''),
(N''student19'',N''Cao Thanh Hà'',N''20050719'',N''Nam'',N''prn212.g4.student19@gmail.com'',N''0900000019'',N''29 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''5b4921b8cbb1bef68d5fda4e41f5443b8283963091f99049010bed690ec8581b'',''80a86f43b9af2d9a6acee5b43261c6ed''),
(N''student20'',N''Tạ Đức Anh'',N''20050820'',N''Nữ'',N''prn212.g4.student20@gmail.com'',N''0900000020'',N''30 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''706038273abaa803ded577d1d5b18410ebb89af97b56161e63ee0c8036bba809'',''25ba389a85bb189822cb38de60353e7c''),
(N''student21'',N''Hồ Ngọc Mai'',N''20050921'',N''Nam'',N''prn212.g4.student21@gmail.com'',N''0900000021'',N''31 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''19c5fc240ee528567b3ed2d5111a5e2f2e38cd8718f3cbd5289c705063ae3297'',''323e715ee7438adc5afea466b1fa881b''),
(N''student22'',N''Lâm Gia Bảo'',N''20051022'',N''Nữ'',N''prn212.g4.student22@gmail.com'',N''0900000022'',N''32 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''dd122db82a146495c7b5bbd599afec5eb23f8c7c6ab339ad92c2790815f3f7f6'',''de8813b91d8185f91c0d19b6984855ed''),
(N''student23'',N''Châu Minh Tâm'',N''20051123'',N''Nam'',N''prn212.g4.student23@gmail.com'',N''0900000023'',N''33 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''9de74047fb3a5b11a7319aa4bce228a80026353815b9b4bd8d2b567e26931a62'',''fefc9441266d26d5753d5d28a522fe0e''),
(N''student24'',N''Tô Khánh Linh'',N''20051224'',N''Nữ'',N''prn212.g4.student24@gmail.com'',N''0900000024'',N''34 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''fbb70b2b51212d2eaa9fda584a47420daacc78af02036ce8570bfe94fa7ae2d2'',''fc0e5d79bfb8006c681f23dfc372d04e''),
(N''student25'',N''Lưu Hoài An'',N''20050125'',N''Nam'',N''prn212.g4.student25@gmail.com'',N''0900000025'',N''35 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''10df15178528e4068eb8ca8762fc0535e4a62e996ce22e424380afa48685efb8'',''6bec8ca51cf9e42f801adcb53c576c91''),
(N''lecturer01'',N''Nguyễn Văn Long'',N''19850215'',N''Nam'',N''prn212.g4.lecturer01@gmail.com'',N''0900000026'',N''36 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''9c930fc557d46895fc76b1bac5fbbe98fdde5cdadcdddc3c0f2845c6ac8f4776'',''c3660ad6c93d35ab10340360b9ea299a''),
(N''lecturer02'',N''Trần Thị Mai'',N''19850315'',N''Nữ'',N''prn212.g4.lecturer02@gmail.com'',N''0900000027'',N''37 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''8c9c9759ca177ac5767c1413c61eb6d005f4c0b2d3563987887de103fe6e08ea'',''e9b31d0f5237e1fabc87c08957cb8851''),
(N''lecturer03'',N''Lê Hoàng Phúc'',N''19850415'',N''Nam'',N''prn212.g4.lecturer03@gmail.com'',N''0900000028'',N''38 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''ac40fb851b8053fa17ca5e7dc08a414abd1102197e5c3121cba673378d2af855'',''19892bb2c396959e51b7dc344fae1d56''),
(N''lecturer04'',N''Phạm Thu Hương'',N''19850515'',N''Nữ'',N''prn212.g4.lecturer04@gmail.com'',N''0900000029'',N''39 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''a55079b375712082a3b40a70851c89e6448a7b23a9c02854eb89815a4d20921d'',''7d8ee92bf27e6cb5938827dca959a66a''),
(N''lecturer05'',N''Võ Minh Đức'',N''19850615'',N''Nam'',N''prn212.g4.lecturer05@gmail.com'',N''0900000030'',N''40 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''fa72e9b69a03d10cde6b023d209f84b48cf2ccd90a984861a5a148869974876b'',''54ba7d39f2744cc25851bc67b36c8f25''),
(N''staff01'',N''Lê Thị Hoa'',N''19900110'',N''Nữ'',N''prn212.g4.staff01@gmail.com'',N''0900000031'',N''41 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''c197cc9e43a930642a74f9bb9ebffbbee248c183c3820c668066f9064ade3132'',''e307af6d651b7e9e01fef2229751b08d''),
(N''staff02'',N''Trần Quốc Dũng'',N''19900210'',N''Nam'',N''prn212.g4.staff02@gmail.com'',N''0900000032'',N''42 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''c082689f81a4dee57fe8c5a84e0a95f446b1ff53a55c4fecc4ac746d59c7ea8d'',''1c5fcb86e0e40eb58e720ca9fd647205''),
(N''admin01'',N''Nguyễn Hoàng Quản'',N''19880115'',N''Nam'',N''prn212.g4.admin01@gmail.com'',N''0900000033'',N''43 đường Nguyễn Văn Cừ, TP. Hồ Chí Minh'',''bccea3e1a879530bb7b5f74ae198be777ad22c819e30e93878045cacb5552a4d'',''53f4b5d565eec417d2cf84d7b1133cfc'');

INSERT dbo.Users(
    Username, DisplayName, FullName, DateOfBirth,
    Gender, Email, Phone, Address, PasswordHash, PasswordSalt
)
SELECT
    Username, FullName, FullName, DateOfBirth,
    Gender, Email, Phone, Address, PasswordHash, PasswordSalt
FROM #People;

INSERT dbo.UserRoles(UserID, RoleID)
SELECT u.UserID, r.RoleID
FROM dbo.Users u
JOIN dbo.Roles r ON r.RoleCode =
    CASE
        WHEN u.Username LIKE ''student%'' THEN ''STUDENT''
        WHEN u.Username LIKE ''lecturer%'' THEN ''LECTURER''
        WHEN u.Username LIKE ''staff%'' THEN ''ACADEMIC_STAFF''
        ELSE ''ADMIN''
    END;

INSERT dbo.Students(
    StudentCode, UserID, FullName, DateOfBirth, Email, Phone,
    DepartmentID, EnrollmentDate, Gender, Address, MajorID, Cohort,
    EmergencyContactName, EmergencyContactPhone
)
SELECT
    ''ST'' + RIGHT(''000'' + CONVERT(varchar(3), n.No), 3),
    u.UserID, p.FullName, p.DateOfBirth, p.Email, p.Phone,
    CASE WHEN n.No <= 15 THEN 1 ELSE 2 END,
    ''20250901'', p.Gender, p.Address,
    CASE
        WHEN n.No <= 8 THEN 1
        WHEN n.No <= 15 THEN 2
        WHEN n.No <= 20 THEN 3
        ELSE 4
    END,
    ''K2025'',
    N''Người liên hệ '' + p.FullName,
    ''08'' + RIGHT(''00000000'' + CONVERT(varchar(8), n.No), 8)
FROM #People p
JOIN dbo.Users u ON u.Username = p.Username
CROSS APPLY (
    SELECT TRY_CONVERT(int, RIGHT(p.Username, 2)) No
) n
WHERE p.Username LIKE ''student%'';

INSERT dbo.Lecturers(
    LecturerCode, UserID, FullName, DepartmentID,
    DateOfBirth, Gender, Email, Phone, Address,
    Qualification, Specialization, HireDate
)
SELECT
    ''GV'' + RIGHT(''000'' + CONVERT(varchar(3), n.No), 3),
    u.UserID, p.FullName,
    CASE WHEN n.No <= 3 THEN 1 ELSE 2 END,
    p.DateOfBirth, p.Gender, p.Email, p.Phone, p.Address,
    CASE WHEN n.No IN (1, 4) THEN N''Tiến sĩ'' ELSE N''Thạc sĩ'' END,
    CASE
        WHEN n.No <= 3 THEN N''Công nghệ thông tin''
        ELSE N''Kinh tế và quản trị''
    END,
    ''20200901''
FROM #People p
JOIN dbo.Users u ON u.Username = p.Username
CROSS APPLY (
    SELECT TRY_CONVERT(int, RIGHT(p.Username, 2)) No
) n
WHERE p.Username LIKE ''lecturer%'';

INSERT dbo.AcademicStaff(
    StaffCode, UserID, FullName, DepartmentID,
    DateOfBirth, Gender, Email, Phone, Address, Position, HireDate
)
SELECT
    ''NV00'' + RIGHT(p.Username, 1),
    u.UserID, p.FullName,
    CONVERT(int, RIGHT(p.Username, 1)),
    p.DateOfBirth, p.Gender, p.Email, p.Phone, p.Address,
    N''Chuyên viên học vụ'', ''20210901''
FROM #People p
JOIN dbo.Users u ON u.Username = p.Username
WHERE p.Username LIKE ''staff%'';

DROP TABLE #People;

-- =========================================================
-- G. MÔN, LỚP, ĐĂNG KÝ, ĐIỂM, HỒ SƠ MẪU
-- =========================================================

INSERT dbo.Courses(
    CourseCode, CourseName, Credits, DepartmentID
) VALUES
(''PRN212'', N''Lập trình ứng dụng desktop'', 3, 1),
(''PRO192'', N''Lập trình hướng đối tượng'', 3, 1),
(''DBI202'', N''Cơ sở dữ liệu'', 3, 1),
(''SWT301'', N''Kiểm thử phần mềm'', 3, 1),
(''BUS101'', N''Nhập môn kinh doanh'', 3, 2),
(''MKT101'', N''Marketing căn bản'', 3, 2),
(''ACC101'', N''Nguyên lý kế toán'', 3, 2),
(''ECO101'', N''Kinh tế học'', 3, 2);

INSERT dbo.Classes(
    ClassCode, CourseID, SemesterID,
    LecturerID, Capacity, ScheduleNote
)
SELECT
    c.CourseCode + ''-'' + t.SemesterCode + ''-01'',
    c.CourseID, t.SemesterID,
    CASE c.CourseCode
        WHEN ''PRN212'' THEN
            (SELECT LecturerID FROM dbo.Lecturers WHERE LecturerCode=''GV001'')
        WHEN ''PRO192'' THEN
            (SELECT LecturerID FROM dbo.Lecturers WHERE LecturerCode=''GV002'')
        WHEN ''DBI202'' THEN
            (SELECT LecturerID FROM dbo.Lecturers WHERE LecturerCode=''GV003'')
        WHEN ''SWT301'' THEN
            (SELECT LecturerID FROM dbo.Lecturers WHERE LecturerCode=''GV001'')
        WHEN ''BUS101'' THEN
            (SELECT LecturerID FROM dbo.Lecturers WHERE LecturerCode=''GV004'')
        WHEN ''MKT101'' THEN
            (SELECT LecturerID FROM dbo.Lecturers WHERE LecturerCode=''GV005'')
        ELSE
            (SELECT LecturerID FROM dbo.Lecturers WHERE LecturerCode=''GV004'')
    END,
    30,
    N''Lịch mẫu: cập nhật trước khi sử dụng thực tế''
FROM dbo.Courses c
CROSS JOIN dbo.Semesters t;

INSERT dbo.CourseEnrollments(
    StudentID, CourseID, SemesterID, RegistrationStatus
)
SELECT
    s.StudentID, c.CourseID, t.SemesterID,
    CASE
        WHEN t.SemesterCode = ''2026-FALL'' THEN ''REGISTERED''
        ELSE ''COMPLETED''
    END
FROM dbo.Students s
JOIN dbo.Courses c ON c.DepartmentID = s.DepartmentID
CROSS JOIN dbo.Semesters t
WHERE
    (s.DepartmentID = 1 AND c.CourseCode IN (''PRN212'', ''DBI202''))
    OR
    (s.DepartmentID = 2 AND c.CourseCode IN (''BUS101'', ''MKT101''));

INSERT dbo.Enrollments(
    CourseEnrollmentID, StudentID, ClassID, EnrollmentStatus
)
SELECT
    r.CourseEnrollmentID, r.StudentID, c.ClassID,
    CASE
        WHEN r.RegistrationStatus = ''COMPLETED'' THEN ''COMPLETED''
        ELSE ''ENROLLED''
    END
FROM dbo.CourseEnrollments r
JOIN dbo.Classes c
    ON c.CourseID = r.CourseID
    AND c.SemesterID = r.SemesterID;

INSERT dbo.Grades(
    EnrollmentID, FinalScore, IsPublished, UpdatedByUserID
)
SELECT
    e.EnrollmentID,
    CASE
        WHEN s.StudentCode = ''ST001'' AND c.CourseCode = ''PRN212''
        THEN
            CASE
                WHEN t.SemesterCode = ''2025-FALL'' THEN 8.6
                ELSE 4.5
            END
        ELSE CONVERT(
            decimal(5,2),
            4.0 + (
                (
                    CONVERT(int, RIGHT(s.StudentCode, 3)) * 7
                    + c.CourseID * 3 + t.SemesterID
                ) % 60
            ) / 10.0
        )
    END,
    1, l.UserID
FROM dbo.Enrollments e
JOIN dbo.Students s ON s.StudentID = e.StudentID
JOIN dbo.Classes cl ON cl.ClassID = e.ClassID
JOIN dbo.Courses c ON c.CourseID = cl.CourseID
JOIN dbo.Semesters t ON t.SemesterID = cl.SemesterID
JOIN dbo.Lecturers l ON l.LecturerID = cl.LecturerID
WHERE e.EnrollmentStatus = ''COMPLETED'';

INSERT dbo.StudentRecords(
    StudentID, RecordType, DocumentNumber,
    RecordTitle, RecordContent, RecordDate, CreatedByUserID
)
SELECT
    s.StudentID,
    N''Academic'',
    ''HS-'' + s.StudentCode,
    N''Hồ sơ nhập học '' + s.StudentCode,
    N''Dữ liệu giả phục vụ kiểm thử: đã xác nhận thông tin cá nhân và ngành học.'',
    ''20250901'',
    a.UserID
FROM dbo.Students s
JOIN dbo.AcademicStaff a ON a.DepartmentID = s.DepartmentID;

INSERT dbo.AuditLogs(ActionName, EntityName, Details)
VALUES(
    ''INSTALL'', ''Database'',
    N''33 tài khoản mẫu; toàn bộ dữ liệu cá nhân là giả lập.''
);

-- =========================================================
-- H. VIEW ĐIỂM VÀ GPA
-- =========================================================

EXEC sys.sp_executesql N''
CREATE VIEW dbo.vStudentCourseResults AS
SELECT
    s.StudentID, s.StudentCode,
    c.CourseID, cl.SemesterID,
    sem.StartDate AS SemesterStartDate,
    e.EnrollmentDate, e.EnrollmentID,
    c.CourseCode, c.CourseName, c.Credits,
    cl.ClassCode, sem.SemesterCode,
    g.FinalScore, gs.GradeLetter, gs.GradePoint
FROM dbo.Students s
JOIN dbo.Enrollments e
    ON e.StudentID = s.StudentID
    AND e.EnrollmentStatus = ''''COMPLETED''''
JOIN dbo.Classes cl ON cl.ClassID = e.ClassID
JOIN dbo.Courses c ON c.CourseID = cl.CourseID
JOIN dbo.Semesters sem ON sem.SemesterID = cl.SemesterID
JOIN dbo.Grades g
    ON g.EnrollmentID = e.EnrollmentID
    AND g.IsPublished = 1
OUTER APPLY (
    SELECT TOP (1) sc.GradeLetter, sc.GradePoint
    FROM dbo.GradeScale sc
    WHERE sc.IsActive = 1
        AND g.FinalScore BETWEEN sc.MinScore AND sc.MaxScore
    ORDER BY sc.MinScore DESC
) gs;
'';

EXEC sys.sp_executesql N''
CREATE VIEW dbo.vStudentGPA AS
WITH Ranked AS (
    SELECT *,
        ROW_NUMBER() OVER (
            PARTITION BY StudentID, CourseID
            ORDER BY SemesterStartDate DESC,
                     EnrollmentDate DESC,
                     EnrollmentID DESC
        ) rn
    FROM dbo.vStudentCourseResults
)
SELECT
    StudentID, StudentCode,
    CAST(
        SUM(CASE WHEN GradePoint IS NOT NULL THEN Credits*GradePoint END)
        /
        NULLIF(
            SUM(
                CASE WHEN GradePoint IS NOT NULL
                THEN CONVERT(decimal(18,4), Credits) END
            ),
            0
        )
        AS decimal(5,2)
    ) CumulativeGPA,
    SUM(CASE WHEN GradePoint IS NOT NULL THEN Credits ELSE 0 END)
        CountedCredits
FROM Ranked
WHERE rn = 1
GROUP BY StudentID, StudentCode;
'';

EXEC sys.sp_executesql N''
CREATE VIEW dbo.vStudentSemesterGPA AS
SELECT
    StudentID, StudentCode, SemesterCode,
    CAST(
        SUM(CASE WHEN GradePoint IS NOT NULL THEN Credits*GradePoint END)
        /
        NULLIF(
            SUM(
                CASE WHEN GradePoint IS NOT NULL
                THEN CONVERT(decimal(18,4), Credits) END
            ),
            0
        )
        AS decimal(5,2)
    ) SemesterGPA,
    SUM(CASE WHEN GradePoint IS NOT NULL THEN Credits ELSE 0 END)
        CountedCredits
FROM dbo.vStudentCourseResults
GROUP BY StudentID, StudentCode, SemesterCode;
'';

EXEC sys.sp_executesql N''
CREATE VIEW dbo.vCourseEnrollmentStatus AS
SELECT
    r.CourseEnrollmentID, r.StudentID, s.StudentCode, s.FullName,
    r.CourseID, c.CourseCode, c.CourseName,
    r.SemesterID, t.SemesterCode,
    r.RegistrationDate, r.RegistrationStatus,
    e.EnrollmentID, e.ClassID, cl.ClassCode, e.EnrollmentStatus
FROM dbo.CourseEnrollments r
JOIN dbo.Students s ON s.StudentID = r.StudentID
JOIN dbo.Courses c ON c.CourseID = r.CourseID
JOIN dbo.Semesters t ON t.SemesterID = r.SemesterID
LEFT JOIN dbo.Enrollments e
    ON e.CourseEnrollmentID = r.CourseEnrollmentID
    AND e.EnrollmentStatus <> ''''DROPPED''''
LEFT JOIN dbo.Classes cl ON cl.ClassID = e.ClassID;
'';

-- =========================================================
-- I. RÀNG BUỘC NGHIỆP VỤ
-- =========================================================

EXEC sys.sp_executesql N''
CREATE TRIGGER dbo.trg_Enrollments_RegistrationMatch
ON dbo.Enrollments AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted e
        JOIN dbo.CourseEnrollments r
            ON r.CourseEnrollmentID = e.CourseEnrollmentID
        JOIN dbo.Classes c ON c.ClassID = e.ClassID
        WHERE e.StudentID <> r.StudentID
            OR c.CourseID <> r.CourseID
            OR c.SemesterID <> r.SemesterID
            OR (
                e.EnrollmentStatus <> ''''DROPPED''''
                AND r.RegistrationStatus = ''''DROPPED''''
            )
    )
    THROW 51110, ''''Assignment does not match registration.'''', 1;
END;
'';

EXEC sys.sp_executesql N''
CREATE TRIGGER dbo.trg_Classes_ProtectRegistrations
ON dbo.Classes AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted c
        JOIN dbo.Enrollments e ON e.ClassID = c.ClassID
        JOIN dbo.CourseEnrollments r
            ON r.CourseEnrollmentID = e.CourseEnrollmentID
        WHERE c.CourseID <> r.CourseID
            OR c.SemesterID <> r.SemesterID
    )
    THROW 51112,
        ''''Cannot change course or semester of an assigned class.'''', 1;
END;
'';

EXEC sys.sp_executesql N''
CREATE TRIGGER dbo.trg_GradeScale_Validate
ON dbo.GradeScale AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.GradeScale WHERE IsActive = 1
    )
    OR EXISTS (
        SELECT 1
        FROM dbo.GradeScale a
        JOIN dbo.GradeScale b
            ON a.GradeScaleID < b.GradeScaleID
            AND a.IsActive = 1 AND b.IsActive = 1
            AND a.MinScore <= b.MaxScore
            AND b.MinScore <= a.MaxScore
    )
    OR (SELECT MIN(MinScore) FROM dbo.GradeScale WHERE IsActive=1) <> 0
    OR (SELECT MAX(MaxScore) FROM dbo.GradeScale WHERE IsActive=1) <> 10
    OR EXISTS (
        SELECT 1
        FROM (
            SELECT MinScore,
                LAG(MaxScore) OVER(ORDER BY MinScore) Prev
            FROM dbo.GradeScale
            WHERE IsActive = 1
        ) b
        WHERE Prev IS NOT NULL AND MinScore <> Prev + 0.01
    )
    THROW 51200,
        ''''Active grade bands must cover 0..10 without gaps or overlaps.'''', 1;
END;
'';

EXEC sys.sp_executesql N''
CREATE TRIGGER dbo.trg_Grades_Protect
ON dbo.Grades AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM deleted d
        LEFT JOIN inserted i ON i.GradeID = d.GradeID
        WHERE i.GradeID IS NULL
    )
    THROW 51201,
        ''''Do not delete grades. Correct the existing result.'''', 1;

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN deleted d ON d.GradeID = i.GradeID
        WHERE i.EnrollmentID <> d.EnrollmentID
    )
    THROW 51202,
        ''''A grade cannot be moved to another enrollment.'''', 1;

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.Enrollments e ON e.EnrollmentID = i.EnrollmentID
        JOIN dbo.CourseEnrollments r
            ON r.CourseEnrollmentID = e.CourseEnrollmentID
        WHERE e.EnrollmentStatus = ''''DROPPED''''
            OR r.RegistrationStatus = ''''DROPPED''''
    )
    THROW 51203, ''''Cannot grade a dropped enrollment.'''', 1;

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.Enrollments e ON e.EnrollmentID = i.EnrollmentID
        WHERE e.EnrollmentStatus = ''''COMPLETED'''' AND i.IsPublished = 0
    )
    THROW 51204, ''''A completed result must remain published.'''', 1;

    INSERT dbo.AuditLogs(
        ActorUserID, ActionName, EntityName, EntityKey, Details
    )
    SELECT
        i.UpdatedByUserID,
        CASE WHEN d.GradeID IS NULL THEN ''''INSERT'''' ELSE ''''UPDATE'''' END,
        ''''Grades'''',
        CONVERT(nvarchar(100), i.GradeID),
        CONCAT(
            ''''OldScore='''', d.FinalScore,
            ''''; NewScore='''', i.FinalScore,
            ''''; Published='''', i.IsPublished
        )
    FROM inserted i
    LEFT JOIN deleted d ON d.GradeID = i.GradeID;
END;
'';

EXEC sys.sp_executesql N''
CREATE TRIGGER dbo.trg_CourseEnrollments_ProtectAssignments
ON dbo.CourseEnrollments AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted r
        JOIN dbo.Enrollments e
            ON e.CourseEnrollmentID = r.CourseEnrollmentID
        JOIN dbo.Classes c ON c.ClassID = e.ClassID
        WHERE e.StudentID <> r.StudentID
            OR c.CourseID <> r.CourseID
            OR c.SemesterID <> r.SemesterID
            OR (
                r.RegistrationStatus = ''''DROPPED''''
                AND e.EnrollmentStatus <> ''''DROPPED''''
            )
            OR (
                e.EnrollmentStatus = ''''COMPLETED''''
                AND r.RegistrationStatus <> ''''COMPLETED''''
            )
            OR (
                r.RegistrationStatus = ''''COMPLETED''''
                AND e.EnrollmentStatus = ''''ENROLLED''''
            )
    )
    THROW 51111,
        ''''Registration conflicts with its class assignment.'''', 1;

    IF EXISTS (
        SELECT 1 FROM inserted r
        WHERE r.RegistrationStatus = ''''COMPLETED''''
        AND NOT EXISTS (
            SELECT 1 FROM dbo.Enrollments e
            WHERE e.CourseEnrollmentID = r.CourseEnrollmentID
                AND e.EnrollmentStatus = ''''COMPLETED''''
        )
    )
    THROW 51208,
        ''''A completed registration needs a completed assignment.'''', 1;
END;
'';

EXEC sys.sp_executesql N''
CREATE TRIGGER dbo.trg_Enrollments_Workflow
ON dbo.Enrollments AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.Grades g ON g.EnrollmentID = i.EnrollmentID
        WHERE i.EnrollmentStatus = ''''DROPPED''''
    )
    THROW 51205, ''''An enrollment with grades cannot be dropped.'''', 1;

    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE i.EnrollmentStatus = ''''COMPLETED''''
        AND NOT EXISTS (
            SELECT 1 FROM dbo.Grades g
            WHERE g.EnrollmentID = i.EnrollmentID AND g.IsPublished = 1
        )
    )
    THROW 51206, ''''Publish a grade before completing an enrollment.'''', 1;

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN deleted d ON d.EnrollmentID = i.EnrollmentID
        WHERE d.EnrollmentStatus = ''''COMPLETED''''
            AND i.EnrollmentStatus <> ''''COMPLETED''''
    )
    THROW 51207,
        ''''A completed enrollment cannot return to an unfinished state.'''', 1;

    UPDATE r
    SET RegistrationStatus =
        CASE
            WHEN i.EnrollmentStatus = ''''COMPLETED'''' THEN ''''COMPLETED''''
            ELSE ''''REGISTERED''''
        END
    FROM dbo.CourseEnrollments r
    JOIN inserted i ON i.CourseEnrollmentID = r.CourseEnrollmentID
    WHERE i.EnrollmentStatus <> ''''DROPPED'''';

    INSERT dbo.AuditLogs(ActionName, EntityName, EntityKey, Details)
    SELECT
        ''''STATE'''', ''''Enrollments'''',
        CONVERT(nvarchar(100), i.EnrollmentID),
        CONCAT(
            ''''Old='''', d.EnrollmentStatus,
            ''''; New='''', i.EnrollmentStatus
        )
    FROM inserted i
    LEFT JOIN deleted d ON d.EnrollmentID = i.EnrollmentID;
END;
'';

EXEC sys.sp_executesql N''
CREATE TRIGGER dbo.trg_Students_Audit
ON dbo.Students AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;

    INSERT dbo.AuditLogs(ActionName, EntityName, EntityKey, Details)
    SELECT
        CASE
            WHEN d.StudentID IS NULL THEN ''''INSERT''''
            WHEN i.StudentID IS NULL THEN ''''DELETE''''
            ELSE ''''UPDATE''''
        END,
        ''''Students'''',
        CONVERT(nvarchar(100), COALESCE(i.StudentID, d.StudentID)),
        CONCAT(
            ''''Code='''', COALESCE(i.StudentCode, d.StudentCode),
            ''''; OldStatus='''', d.StudentStatus,
            ''''; NewStatus='''', i.StudentStatus
        )
    FROM inserted i
    FULL JOIN deleted d ON d.StudentID = i.StudentID;
END;
'';

EXEC sys.sp_executesql N''
CREATE TRIGGER dbo.trg_StudentRecords_Audit
ON dbo.StudentRecords AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM deleted d
        LEFT JOIN inserted i ON i.RecordID = d.RecordID
        WHERE i.RecordID IS NULL
    )
    THROW 51216, ''''Archive student records instead of deleting them.'''', 1;

    INSERT dbo.AuditLogs(ActionName, EntityName, EntityKey, Details)
    SELECT
        CASE WHEN d.RecordID IS NULL THEN ''''INSERT'''' ELSE ''''UPDATE'''' END,
        ''''StudentRecords'''',
        CONVERT(nvarchar(100), i.RecordID),
        CONCAT(''''Archived='''', i.IsArchived)
    FROM inserted i
    LEFT JOIN deleted d ON d.RecordID = i.RecordID;
END;
'';

-- =========================================================
-- J. ĐĂNG KÝ MÔN VÀ XẾP LỚP
-- =========================================================

EXEC sys.sp_executesql N''
CREATE PROCEDURE dbo.uspRegisterCourse
    @StudentID int,
    @CourseID int,
    @SemesterID int,
    @CourseEnrollmentID int = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @StudentStatus varchar(20), @State varchar(15);

        SELECT @StudentStatus = StudentStatus
        FROM dbo.Students WITH(UPDLOCK, HOLDLOCK)
        WHERE StudentID = @StudentID;

        IF @StudentStatus IS NULL OR @StudentStatus <> ''''ACTIVE''''
            THROW 51120, ''''Student is missing or inactive.'''', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.Courses
            WHERE CourseID = @CourseID AND IsActive = 1
        )
            THROW 51121, ''''Course is missing or inactive.'''', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.Semesters WHERE SemesterID = @SemesterID
        )
            THROW 51122, ''''Semester not found.'''', 1;

        SET @CourseEnrollmentID = NULL;

        SELECT
            @CourseEnrollmentID = CourseEnrollmentID,
            @State = RegistrationStatus
        FROM dbo.CourseEnrollments WITH(UPDLOCK, HOLDLOCK)
        WHERE StudentID = @StudentID
            AND CourseID = @CourseID
            AND SemesterID = @SemesterID;

        IF @CourseEnrollmentID IS NOT NULL AND @State <> ''''DROPPED''''
            THROW 51123, ''''Course is already registered in this semester.'''', 1;

        IF @CourseEnrollmentID IS NULL
        BEGIN
            INSERT dbo.CourseEnrollments(StudentID, CourseID, SemesterID)
            VALUES(@StudentID, @CourseID, @SemesterID);

            SET @CourseEnrollmentID = CONVERT(int, SCOPE_IDENTITY());
        END
        ELSE
        BEGIN
            IF EXISTS (
                SELECT 1 FROM dbo.Enrollments e
                JOIN dbo.Grades g ON g.EnrollmentID = e.EnrollmentID
                WHERE e.CourseEnrollmentID = @CourseEnrollmentID
            )
                THROW 51124,
                    ''''Dropped registration has grades. Academic review required.'''', 1;

            UPDATE dbo.CourseEnrollments
            SET RegistrationStatus = ''''REGISTERED'''',
                RegistrationDate = SYSUTCDATETIME()
            WHERE CourseEnrollmentID = @CourseEnrollmentID;
        END;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
'';

EXEC sys.sp_executesql N''
CREATE PROCEDURE dbo.uspAssignStudentToClass
    @CourseEnrollmentID int,
    @ClassID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @StudentID int, @CourseID int, @SemesterID int,
            @State varchar(15), @ClassCourse int, @ClassSemester int,
            @Capacity int, @ClassState varchar(15),
            @StudentState varchar(20);

        SELECT
            @StudentID = StudentID,
            @CourseID = CourseID,
            @SemesterID = SemesterID,
            @State = RegistrationStatus
        FROM dbo.CourseEnrollments WITH(UPDLOCK, HOLDLOCK)
        WHERE CourseEnrollmentID = @CourseEnrollmentID;

        IF @StudentID IS NULL OR @State <> ''''REGISTERED''''
            THROW 51130, ''''Registration is not available for assignment.'''', 1;

        SELECT @StudentState = StudentStatus
        FROM dbo.Students WITH(HOLDLOCK)
        WHERE StudentID = @StudentID;

        IF @StudentState <> ''''ACTIVE''''
            THROW 51131, ''''Student is inactive.'''', 1;

        SELECT
            @ClassCourse = CourseID,
            @ClassSemester = SemesterID,
            @Capacity = Capacity,
            @ClassState = ClassStatus
        FROM dbo.Classes WITH(UPDLOCK, HOLDLOCK)
        WHERE ClassID = @ClassID;

        IF @ClassCourse IS NULL OR @ClassState <> ''''OPEN''''
            THROW 51132, ''''Class is missing or not open.'''', 1;

        IF @ClassCourse <> @CourseID OR @ClassSemester <> @SemesterID
            THROW 51133, ''''Class course/semester does not match.'''', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.Courses
            WHERE CourseID = @CourseID AND IsActive = 1
        )
            THROW 51134, ''''Course is inactive.'''', 1;

        IF EXISTS (
            SELECT 1 FROM dbo.Enrollments
            WHERE CourseEnrollmentID = @CourseEnrollmentID
                AND EnrollmentStatus <> ''''DROPPED''''
        )
            THROW 51135, ''''Registration already has an active assignment.'''', 1;

        IF @Capacity IS NOT NULL AND (
            SELECT COUNT_BIG(*) FROM dbo.Enrollments
            WHERE ClassID = @ClassID
                AND EnrollmentStatus IN (''''ENROLLED'''', ''''COMPLETED'''')
        ) >= @Capacity
            THROW 51136, ''''Class is full.'''', 1;

        IF EXISTS (
            SELECT 1 FROM dbo.Enrollments e
            JOIN dbo.Grades g ON g.EnrollmentID = e.EnrollmentID
            WHERE e.StudentID = @StudentID AND e.ClassID = @ClassID
        )
            THROW 51137, ''''Previous assignment has a grade.'''', 1;

        IF EXISTS (
            SELECT 1 FROM dbo.Enrollments
            WHERE StudentID = @StudentID AND ClassID = @ClassID
        )
            UPDATE dbo.Enrollments
            SET EnrollmentStatus = ''''ENROLLED'''',
                EnrollmentDate = SYSUTCDATETIME()
            WHERE StudentID = @StudentID AND ClassID = @ClassID;
        ELSE
            INSERT dbo.Enrollments(StudentID, ClassID, CourseEnrollmentID)
            VALUES(@StudentID, @ClassID, @CourseEnrollmentID);

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
'';

EXEC sys.sp_executesql N''
CREATE PROCEDURE dbo.uspEnrollStudent
    @StudentID int, @ClassID int
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51140,
        ''''Use uspRegisterCourse, then uspAssignStudentToClass.'''', 1;
END;
'';

-- =========================================================
-- K. NHẬP/SỬA ĐIỂM, CÔNG BỐ, HỦY ĐĂNG KÝ
-- =========================================================

EXEC sys.sp_executesql N''
CREATE PROCEDURE dbo.uspSaveGrade
    @EnrollmentID int,
    @FinalScore decimal(5,2),
    @ActorUserID int,
    @ExpectedRowVer binary(8) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @ClassID int, @GradeID int,
            @Current binary(8), @Function char(3);

        SELECT @ClassID = ClassID
        FROM dbo.Enrollments WITH(UPDLOCK, HOLDLOCK)
        WHERE EnrollmentID = @EnrollmentID
            AND EnrollmentStatus <> ''''DROPPED'''';

        IF @ClassID IS NULL
            THROW 51210, ''''Enrollment not found or dropped.'''', 1;

        SELECT @GradeID = GradeID, @Current = RowVer
        FROM dbo.Grades WITH(UPDLOCK, HOLDLOCK)
        WHERE EnrollmentID = @EnrollmentID;

        SET @Function =
            CASE WHEN @GradeID IS NULL THEN ''''F11'''' ELSE ''''F12'''' END;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Users u
            JOIN dbo.UserRoles ur ON ur.UserID = u.UserID
            JOIN dbo.Roles r ON r.RoleID = ur.RoleID
            JOIN dbo.RolePermissions rp ON rp.RoleID = r.RoleID
            JOIN dbo.Permissions p ON p.PermissionID = rp.PermissionID
            JOIN dbo.Lecturers l ON l.UserID = u.UserID
            JOIN dbo.Classes c ON c.LecturerID = l.LecturerID
            WHERE u.UserID = @ActorUserID
                AND u.IsActive = 1
                AND u.MustChangePassword = 0
                AND l.IsActive = 1
                AND r.RoleCode = ''''LECTURER''''
                AND p.FunctionCode = @Function
                AND c.ClassID = @ClassID
        )
            THROW 51211,
                ''''Only the assigned lecturer with permission may save grades.'''', 1;

        IF @GradeID IS NULL
        BEGIN
            IF @ExpectedRowVer IS NOT NULL
                THROW 51212, ''''Grade no longer matches the original version.'''', 1;

            INSERT dbo.Grades(EnrollmentID, FinalScore, UpdatedByUserID)
            VALUES(@EnrollmentID, @FinalScore, @ActorUserID);
        END
        ELSE
        BEGIN
            IF @ExpectedRowVer IS NULL OR @ExpectedRowVer <> @Current
                THROW 51212, ''''Grade was changed. Refresh before saving.'''', 1;

            UPDATE dbo.Grades
            SET FinalScore = @FinalScore,
                UpdatedByUserID = @ActorUserID,
                UpdatedAt = SYSUTCDATETIME()
            WHERE GradeID = @GradeID;
        END;

        COMMIT;

        SELECT GradeID, FinalScore, IsPublished, RowVer
        FROM dbo.Grades
        WHERE EnrollmentID = @EnrollmentID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
'';

EXEC sys.sp_executesql N''
CREATE PROCEDURE dbo.uspPublishGrade
    @EnrollmentID int,
    @ActorUserID int,
    @ExpectedRowVer binary(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @GradeID int, @Current binary(8);

        SELECT @GradeID = g.GradeID, @Current = g.RowVer
        FROM dbo.Grades g WITH(UPDLOCK, HOLDLOCK)
        JOIN dbo.Enrollments e ON e.EnrollmentID = g.EnrollmentID
        WHERE g.EnrollmentID = @EnrollmentID
            AND e.EnrollmentStatus <> ''''DROPPED'''';

        IF @GradeID IS NULL
            THROW 51213, ''''Grade not found.'''', 1;

        IF @ExpectedRowVer IS NULL OR @ExpectedRowVer <> @Current
            THROW 51212, ''''Grade was changed. Refresh before publishing.'''', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Users u
            JOIN dbo.Lecturers l ON l.UserID = u.UserID
            JOIN dbo.Classes c ON c.LecturerID = l.LecturerID
            JOIN dbo.Enrollments e ON e.ClassID = c.ClassID
            JOIN dbo.UserRoles ur ON ur.UserID = u.UserID
            JOIN dbo.Roles r ON r.RoleID = ur.RoleID
            JOIN dbo.RolePermissions rp ON rp.RoleID = r.RoleID
            JOIN dbo.Permissions p ON p.PermissionID = rp.PermissionID
            WHERE u.UserID = @ActorUserID
                AND u.IsActive = 1
                AND u.MustChangePassword = 0
                AND l.IsActive = 1
                AND e.EnrollmentID = @EnrollmentID
                AND r.RoleCode = ''''LECTURER''''
                AND p.FunctionCode = ''''F12''''
        )
            THROW 51211,
                ''''Only the assigned lecturer with F12 may publish.'''', 1;

        UPDATE dbo.Grades
        SET IsPublished = 1,
            UpdatedByUserID = @ActorUserID,
            UpdatedAt = SYSUTCDATETIME()
        WHERE GradeID = @GradeID;

        UPDATE dbo.Enrollments
        SET EnrollmentStatus = ''''COMPLETED''''
        WHERE EnrollmentID = @EnrollmentID;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
'';

EXEC sys.sp_executesql N''
CREATE PROCEDURE dbo.uspDropCourse
    @CourseEnrollmentID int,
    @ActorUserID int,
    @ExpectedRowVer binary(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @StudentID int, @Current binary(8),
            @State varchar(15), @DepartmentID int;

        SELECT
            @StudentID = r.StudentID,
            @Current = r.RowVer,
            @State = r.RegistrationStatus,
            @DepartmentID = s.DepartmentID
        FROM dbo.CourseEnrollments r WITH(UPDLOCK, HOLDLOCK)
        JOIN dbo.Students s ON s.StudentID = r.StudentID
        WHERE r.CourseEnrollmentID = @CourseEnrollmentID;

        IF @StudentID IS NULL OR @State <> ''''REGISTERED''''
            THROW 51214, ''''Only a registered course can be dropped.'''', 1;

        IF @ExpectedRowVer IS NULL OR @ExpectedRowVer <> @Current
            THROW 51212, ''''Registration changed. Refresh.'''', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Users u
            JOIN dbo.AcademicStaff a ON a.UserID = u.UserID
            JOIN dbo.UserRoles ur ON ur.UserID = u.UserID
            JOIN dbo.Roles r ON r.RoleID = ur.RoleID
            JOIN dbo.RolePermissions rp ON rp.RoleID = r.RoleID
            JOIN dbo.Permissions p ON p.PermissionID = rp.PermissionID
            WHERE u.UserID = @ActorUserID
                AND u.IsActive = 1
                AND u.MustChangePassword = 0
                AND a.IsActive = 1
                AND a.DepartmentID = @DepartmentID
                AND r.RoleCode = ''''ACADEMIC_STAFF''''
                AND p.FunctionCode = ''''F08''''
        )
            THROW 51215,
                ''''Authorized academic staff in the same department required.'''', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.Enrollments e
            JOIN dbo.Grades g ON g.EnrollmentID = e.EnrollmentID
            WHERE e.CourseEnrollmentID = @CourseEnrollmentID
        )
            THROW 51205,
                ''''A registration with grades requires academic review.'''', 1;

        UPDATE dbo.Enrollments
        SET EnrollmentStatus = ''''DROPPED''''
        WHERE CourseEnrollmentID = @CourseEnrollmentID
            AND EnrollmentStatus = ''''ENROLLED'''';

        UPDATE dbo.CourseEnrollments
        SET RegistrationStatus = ''''DROPPED''''
        WHERE CourseEnrollmentID = @CourseEnrollmentID;

        INSERT dbo.AuditLogs(
            ActorUserID, ActionName, EntityName, EntityKey
        )
        VALUES(
            @ActorUserID, ''''DROP'''', ''''CourseEnrollments'''',
            CONVERT(nvarchar(100), @CourseEnrollmentID)
        );

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
'';

-- =========================================================
-- L. KIỂM TRA CÀI ĐẶT
-- =========================================================

IF (SELECT COUNT(*) FROM dbo.Students) <> 25
OR (SELECT COUNT(*) FROM dbo.Lecturers) <> 5
OR (SELECT COUNT(*) FROM dbo.AcademicStaff) <> 2
OR (SELECT COUNT(*) FROM dbo.Users) <> 33
    THROW 51290, ''Unexpected seed counts.'', 1;

IF EXISTS (
    SELECT 1 FROM dbo.Students
    WHERE UserID IS NULL
        OR MajorID IS NULL
        OR Email NOT LIKE ''%@gmail.com''
)
    THROW 51291, ''Incomplete student seed.'', 1;

COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;

SELECT N''INSTALL COMPLETE'' AS Result;

SELECT ''Students'' AS Entity, COUNT(*) AS Total FROM dbo.Students
UNION ALL SELECT ''Lecturers'', COUNT(*) FROM dbo.Lecturers
UNION ALL SELECT ''Staff'', COUNT(*) FROM dbo.AcademicStaff
UNION ALL SELECT ''Users'', COUNT(*) FROM dbo.Users;

SELECT Username, DisplayName, Email, MustChangePassword
FROM dbo.Users
ORDER BY Username;

SELECT * FROM dbo.vStudentGPA;
';