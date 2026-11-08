/* Run by database administrator IN THE APPLICATION DATABASE.
   Creates a role only. No login/user/password is created or automatically assigned.
   Desktop service layer still enforces actor permissions; this is not per-user SQL isolation. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DATABASE_PRINCIPAL_ID(N'G4_AppRuntime') IS NULL CREATE ROLE G4_AppRuntime AUTHORIZATION dbo;
GRANT SELECT ON dbo.Roles TO G4_AppRuntime;
GRANT SELECT ON dbo.Permissions TO G4_AppRuntime;
GRANT SELECT ON dbo.UserRoles TO G4_AppRuntime;
GRANT SELECT ON dbo.RolePermissions TO G4_AppRuntime;
GRANT SELECT ON dbo.Users TO G4_AppRuntime;
GRANT SELECT ON dbo.Departments TO G4_AppRuntime;
GRANT SELECT ON dbo.Students TO G4_AppRuntime;
GRANT SELECT ON dbo.Lecturers TO G4_AppRuntime;
GRANT SELECT ON dbo.AcademicStaff TO G4_AppRuntime;
GRANT SELECT ON dbo.Courses TO G4_AppRuntime;
GRANT SELECT ON dbo.Semesters TO G4_AppRuntime;
GRANT SELECT ON dbo.Classes TO G4_AppRuntime;
GRANT SELECT ON dbo.CourseEnrollments TO G4_AppRuntime;
GRANT SELECT ON dbo.Enrollments TO G4_AppRuntime;
GRANT SELECT ON dbo.Grades TO G4_AppRuntime;
GRANT SELECT ON dbo.StudentRecords TO G4_AppRuntime;
GRANT SELECT ON dbo.GradeScale TO G4_AppRuntime;
GRANT SELECT ON dbo.vCourseEnrollmentStatus TO G4_AppRuntime;
GRANT SELECT ON dbo.vStudentCourseResults TO G4_AppRuntime;
GRANT SELECT ON dbo.vStudentGPA TO G4_AppRuntime;
GRANT SELECT ON dbo.vStudentSemesterGPA TO G4_AppRuntime;
GRANT INSERT,UPDATE ON dbo.Users TO G4_AppRuntime;
GRANT INSERT,UPDATE ON dbo.Students TO G4_AppRuntime;
GRANT INSERT,UPDATE ON dbo.StudentRecords TO G4_AppRuntime;
GRANT INSERT,UPDATE ON dbo.Courses TO G4_AppRuntime;
GRANT INSERT,UPDATE ON dbo.Classes TO G4_AppRuntime;
GRANT INSERT,UPDATE ON dbo.Grades TO G4_AppRuntime;
GRANT INSERT,UPDATE,DELETE ON dbo.UserRoles TO G4_AppRuntime;
GRANT INSERT,UPDATE,DELETE ON dbo.RolePermissions TO G4_AppRuntime;
GRANT EXECUTE ON dbo.uspRegisterCourse TO G4_AppRuntime;
GRANT EXECUTE ON dbo.uspAssignStudentToClass TO G4_AppRuntime;
GRANT EXECUTE ON dbo.uspSetCourseEnrollmentStatus TO G4_AppRuntime;
-- Intentionally no direct DML on registrations/assignments, GradeScale or AuditLogs,
-- no schema/server privileges, and no deletion of academic history.
COMMIT;
-- DBA then explicitly creates/maps a dedicated runtime user and runs:
-- ALTER ROLE G4_AppRuntime ADD MEMBER [your_runtime_database_user];
-- Do not also assign db_owner/db_datawriter or broad grants; they bypass this scope.
