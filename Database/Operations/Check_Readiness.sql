/* Read-only diagnostics. Run in the application database after migration 002.
   No automatic corrections. WARN items require review; not all prevent operation. */
SET NOCOUNT ON;
IF OBJECT_ID('dbo.SchemaMigrations','U') IS NULL THROW 51620,'Apply migration 002 before readiness check.',1;
DECLARE @issues TABLE(Severity varchar(10),Code varchar(50),Details nvarchar(400));
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version='002_RuntimeHardening')
 INSERT @issues VALUES('ERROR','MIGRATION',N'Runtime migration 002 missing.');
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
 OR EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1)
 INSERT @issues VALUES('ERROR','CONSTRAINTS',N'Disabled or untrusted constraints require validation.');
IF EXISTS(SELECT 1 FROM dbo.Classes c WHERE c.Capacity IS NOT NULL AND c.Capacity<(SELECT COUNT_BIG(*) FROM dbo.Enrollments e WHERE e.ClassID=c.ClassID AND e.EnrollmentStatus<>'DROPPED'))
 INSERT @issues VALUES('ERROR','CAPACITY',N'Class capacity below current assignments.');
IF EXISTS(SELECT 1 FROM dbo.Enrollments e JOIN dbo.CourseEnrollments r ON r.CourseEnrollmentID=e.CourseEnrollmentID JOIN dbo.Classes c ON c.ClassID=e.ClassID
 WHERE e.StudentID<>r.StudentID OR c.CourseID<>r.CourseID OR c.SemesterID<>r.SemesterID OR (r.RegistrationStatus='DROPPED' AND e.EnrollmentStatus<>'DROPPED'))
 INSERT @issues VALUES('ERROR','ASSIGNMENT_MATCH',N'Assignment differs from registration or references dropped registration.');
IF EXISTS(SELECT 1 FROM dbo.CourseEnrollments r LEFT JOIN dbo.Enrollments e ON e.CourseEnrollmentID=r.CourseEnrollmentID AND e.EnrollmentStatus<>'DROPPED'
 WHERE (r.RegistrationStatus='COMPLETED' AND (e.EnrollmentID IS NULL OR e.EnrollmentStatus<>'COMPLETED'))
 OR (e.EnrollmentStatus='COMPLETED' AND r.RegistrationStatus<>'COMPLETED'))
 INSERT @issues VALUES('ERROR','COMPLETION_STATE',N'Completion states disagree. Review history and repair through academic process.');
IF EXISTS(SELECT 1 FROM dbo.Enrollments e LEFT JOIN dbo.Grades g ON g.EnrollmentID=e.EnrollmentID WHERE e.EnrollmentStatus='COMPLETED' AND g.GradeID IS NULL)
 INSERT @issues VALUES('ERROR','COMPLETION_GRADE',N'Completed assignment has no grade.');
IF EXISTS(SELECT 1 FROM dbo.GradeScale a JOIN dbo.GradeScale b ON a.GradeScaleID<b.GradeScaleID AND a.IsActive=1 AND b.IsActive=1 AND a.MinScore<=b.MaxScore AND b.MinScore<=a.MaxScore)
 INSERT @issues VALUES('ERROR','GRADE_OVERLAP',N'Active grade ranges overlap.');
IF NOT EXISTS(SELECT 1 FROM dbo.GradeScale WHERE IsActive=1 AND MinScore=0)
 OR NOT EXISTS(SELECT 1 FROM dbo.GradeScale WHERE IsActive=1 AND MaxScore=10)
 OR EXISTS(SELECT 1 FROM (SELECT MinScore,LAG(MaxScore) OVER(ORDER BY MinScore) Prev FROM dbo.GradeScale WHERE IsActive=1) x WHERE Prev IS NOT NULL AND MinScore<>Prev+0.01)
 INSERT @issues VALUES('ERROR','GRADE_GAP',N'Active policy does not cover every hundredth from 0 to 10.');
IF EXISTS(SELECT 1 FROM dbo.Users WHERE PasswordAlgorithm<>'PBKDF2-SHA256' OR PasswordIterations NOT BETWEEN 100000 AND 2000000
 OR LEN(PasswordHash)<>64 OR TRY_CONVERT(varbinary(32),PasswordHash,2) IS NULL OR LEN(PasswordSalt)<32 OR TRY_CONVERT(varbinary(32),PasswordSalt,2) IS NULL)
 INSERT @issues VALUES('ERROR','PASSWORD_FORMAT',N'Credential format incompatible with PasswordCodec.');
IF NOT EXISTS(SELECT 1 FROM dbo.Users u JOIN dbo.UserRoles ur ON ur.UserID=u.UserID JOIN dbo.Roles r ON r.RoleID=ur.RoleID WHERE r.RoleCode='ADMIN' AND u.IsActive=1)
 INSERT @issues VALUES('WARN','BOOTSTRAP_ADMIN',N'No active administrator. Bootstrap first admin using the protected administration tool.');
IF EXISTS(SELECT 1 FROM dbo.Users WHERE IsActive=1 AND MustChangePassword=1)
 INSERT @issues VALUES('WARN','FIRST_PASSWORD',N'Accounts require first-login password change; bootstrap generated accounts before login.');
IF EXISTS(SELECT 1 FROM dbo.Students WHERE UserID IS NULL AND StudentStatus='ACTIVE')
 INSERT @issues VALUES('WARN','STUDENT_NO_LOGIN',N'Some active students have no account. Staff can manage them; self-service requires account linkage.');
IF EXISTS(SELECT 1 FROM dbo.Users u JOIN dbo.UserRoles ur ON ur.UserID=u.UserID JOIN dbo.Roles r ON r.RoleID=ur.RoleID
 WHERE u.IsActive=1 AND ((r.RoleCode='STUDENT' AND NOT EXISTS(SELECT 1 FROM dbo.Students s WHERE s.UserID=u.UserID))
 OR (r.RoleCode='LECTURER' AND NOT EXISTS(SELECT 1 FROM dbo.Lecturers l WHERE l.UserID=u.UserID AND l.IsActive=1))
 OR (r.RoleCode='ACADEMIC_STAFF' AND NOT EXISTS(SELECT 1 FROM dbo.AcademicStaff a WHERE a.UserID=u.UserID AND a.IsActive=1 AND a.DepartmentID IS NOT NULL))))
 INSERT @issues VALUES('ERROR','ROLE_PROFILE',N'Active role has no usable linked student/lecturer/staff profile.');
IF EXISTS(SELECT 1 FROM dbo.StudentRecords WHERE DocumentNumber IS NULL)
 INSERT @issues VALUES('WARN','LEGACY_DOCUMENT',N'Historical records have no document number. Enter actual number on edit, never invent real identifiers.');
IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'G4.DemoSeedAllowed' AND CONVERT(nvarchar(20),value)=N'1')
 INSERT @issues VALUES('WARN','DEMO_DATABASE',N'Demo-only database; do not import real records here.');
SELECT * FROM @issues ORDER BY CASE WHEN Severity='ERROR' THEN 0 ELSE 1 END,Code;
IF EXISTS(SELECT 1 FROM @issues WHERE Severity='ERROR') THROW 51621,'Readiness errors detected. See result set; no data changed.',1;
PRINT 'Structural checks passed. Review WARN items, runtime permissions, backup/restore and Windows integration separately.';
