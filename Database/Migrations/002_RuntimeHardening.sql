-- Run in the existing application database after migration 001. Re-runnable; no data deletion.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF OBJECT_ID('dbo.CourseEnrollments','U') IS NULL OR COL_LENGTH('dbo.StudentRecords','DocumentNumber') IS NULL
 THROW 51500, 'Unsupported schema. Install registration schema and migration 001 first.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @lock int;
 EXEC @lock=sys.sp_getapplock @Resource=N'G4.DatabaseUpgrade',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
 IF @lock<0 THROW 51501,'Cannot obtain migration lock.',1;
 IF COL_LENGTH('dbo.AuditLogs','ChangeData') IS NULL ALTER TABLE dbo.AuditLogs ADD ChangeData nvarchar(max) NULL;
 -- Reject invalid policy; never silently change historic grades or capacity.
 IF EXISTS(SELECT 1 FROM dbo.GradeScale a JOIN dbo.GradeScale b ON a.GradeScaleID<b.GradeScaleID
   AND a.IsActive=1 AND b.IsActive=1 AND a.MinScore<=b.MaxScore AND b.MinScore<=a.MaxScore)
 THROW 51502,'Overlapping grade bands. Review policy before upgrading.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.GradeScale WHERE IsActive=1 AND MinScore=0)
 OR NOT EXISTS(SELECT 1 FROM dbo.GradeScale WHERE IsActive=1 AND MaxScore=10)
 OR EXISTS(SELECT 1 FROM (SELECT MinScore,LAG(MaxScore) OVER(ORDER BY MinScore) AS Prev FROM dbo.GradeScale WHERE IsActive=1) b WHERE Prev IS NOT NULL AND MinScore<>Prev+0.01)
 THROW 51503,'Active grade bands must cover 0.00 through 10.00 without gaps.',1;
 IF EXISTS(SELECT 1 FROM dbo.Classes c WHERE c.Capacity IS NOT NULL AND c.Capacity<(SELECT COUNT_BIG(*) FROM dbo.Enrollments e WHERE e.ClassID=c.ClassID AND e.EnrollmentStatus<>'DROPPED'))
 THROW 51504,'Existing class exceeds capacity. Review before upgrading.',1;
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_GradeScale_Validate ON dbo.GradeScale AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @n int;
 SELECT @n=COUNT(*) FROM dbo.GradeScale WITH(UPDLOCK,HOLDLOCK);
 IF EXISTS(SELECT 1 FROM dbo.GradeScale a JOIN dbo.GradeScale b ON a.GradeScaleID<b.GradeScaleID
 AND a.IsActive=1 AND b.IsActive=1 AND a.MinScore<=b.MaxScore AND b.MinScore<=a.MaxScore)
 THROW 51502,''Active grade bands overlap.'',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.GradeScale WHERE IsActive=1 AND MinScore=0)
 OR NOT EXISTS(SELECT 1 FROM dbo.GradeScale WHERE IsActive=1 AND MaxScore=10)
 OR EXISTS(SELECT 1 FROM (SELECT MinScore,LAG(MaxScore) OVER(ORDER BY MinScore) AS Prev FROM dbo.GradeScale WHERE IsActive=1) b WHERE Prev IS NOT NULL AND MinScore<>Prev+0.01)
 THROW 51503,''Active grade bands must cover 0.00 through 10.00 without gaps.'',1;
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_Classes_Capacity ON dbo.Classes AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF UPDATE(Capacity) AND EXISTS(SELECT 1 FROM inserted c WHERE c.Capacity IS NOT NULL
 AND c.Capacity<(SELECT COUNT_BIG(*) FROM dbo.Enrollments e WITH(UPDLOCK,HOLDLOCK) WHERE e.ClassID=c.ClassID AND e.EnrollmentStatus<>''DROPPED''))
 THROW 51504,''Capacity cannot be lower than current assignments.'',1;
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_Enrollments_Capacity ON dbo.Enrollments AFTER INSERT,UPDATE AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @n int;
 SELECT @n=COUNT(*) FROM dbo.Classes c WITH(UPDLOCK,HOLDLOCK) WHERE c.ClassID IN(SELECT ClassID FROM inserted);
 IF EXISTS(SELECT 1 FROM dbo.Classes c JOIN (SELECT DISTINCT ClassID FROM inserted) i ON i.ClassID=c.ClassID
 WHERE c.Capacity IS NOT NULL AND c.Capacity<(SELECT COUNT_BIG(*) FROM dbo.Enrollments e WITH(UPDLOCK,HOLDLOCK) WHERE e.ClassID=c.ClassID AND e.EnrollmentStatus<>''DROPPED''))
 THROW 51504,''Class capacity exceeded.'',1;
END';
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.uspSetCourseEnrollmentStatus
 @CourseEnrollmentID int, @Status varchar(15)
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @Status NOT IN (''COMPLETED'',''DROPPED'') OR @Status IS NULL
 THROW 51505,''Use registration API for registration; status must be COMPLETED or DROPPED.'',1;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @id int,@state varchar(15);
 SELECT @state=RegistrationStatus FROM dbo.CourseEnrollments WITH(UPDLOCK,HOLDLOCK) WHERE CourseEnrollmentID=@CourseEnrollmentID;
 IF @state IS NULL THROW 51506,''Registration not found.'',1;
 IF @state=@Status BEGIN COMMIT; RETURN; END;
 IF @state<>''REGISTERED'' THROW 51507,''Academic review required for this transition.'',1;
 SELECT @id=EnrollmentID FROM dbo.Enrollments WITH(UPDLOCK,HOLDLOCK)
 WHERE CourseEnrollmentID=@CourseEnrollmentID AND EnrollmentStatus<>''DROPPED'';
 IF @Status=''COMPLETED'' AND (@id IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Grades WITH(UPDLOCK,HOLDLOCK) WHERE EnrollmentID=@id))
 THROW 51508,''Completion requires an assigned class and a recorded grade.'',1;
 IF @Status=''DROPPED'' AND EXISTS(SELECT 1 FROM dbo.Enrollments e JOIN dbo.Grades g ON g.EnrollmentID=e.EnrollmentID WHERE e.CourseEnrollmentID=@CourseEnrollmentID)
 THROW 51509,''Registration has grades; academic review required.'',1;
 UPDATE dbo.Enrollments SET EnrollmentStatus=@Status WHERE CourseEnrollmentID=@CourseEnrollmentID AND EnrollmentStatus<>''DROPPED'';
 UPDATE dbo.CourseEnrollments SET RegistrationStatus=@Status WHERE CourseEnrollmentID=@CourseEnrollmentID;
 COMMIT;
 END TRY
 BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
 END CATCH;
END';
EXEC sys.sp_executesql N'CREATE OR ALTER VIEW dbo.vStudentCourseResults AS
SELECT s.StudentID,s.StudentCode,c.CourseCode,c.CourseName,c.Credits,
 cl.ClassCode, sem.SemesterCode,g.FinalScore,gs.GradeLetter,gs.GradePoint
FROM dbo.Students s
JOIN dbo.Enrollments e ON e.StudentID=s.StudentID AND e.EnrollmentStatus=''COMPLETED''
JOIN dbo.CourseEnrollments reg ON reg.CourseEnrollmentID=e.CourseEnrollmentID AND reg.RegistrationStatus=''COMPLETED''
JOIN dbo.Classes cl ON cl.ClassID=e.ClassID
JOIN dbo.Courses c ON c.CourseID=cl.CourseID
JOIN dbo.Semesters sem ON sem.SemesterID=cl.SemesterID
JOIN dbo.Grades g ON g.EnrollmentID=e.EnrollmentID AND g.IsPublished=1
OUTER APPLY (SELECT TOP (1) sc.GradeLetter,sc.GradePoint
 FROM dbo.GradeScale sc WHERE sc.IsActive=1 AND g.FinalScore BETWEEN sc.MinScore AND sc.MaxScore
 ORDER BY sc.MinScore DESC) gs;';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_Students_Audit ON dbo.Students AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''Students'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_StudentRecords_Audit ON dbo.StudentRecords AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''StudentRecords'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_CourseEnrollments_Audit ON dbo.CourseEnrollments AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''CourseEnrollments'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_Enrollments_Audit ON dbo.Enrollments AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''Enrollments'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_Grades_Audit ON dbo.Grades AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''Grades'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_Classes_Audit ON dbo.Classes AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''Classes'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_Courses_Audit ON dbo.Courses AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''Courses'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_GradeScale_Audit ON dbo.GradeScale AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''GradeScale'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_Users_Audit ON dbo.Users AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT UserID,Username,DisplayName,IsActive,MustChangePassword FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT UserID,Username,DisplayName,IsActive,MustChangePassword FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''Users'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_UserRoles_Audit ON dbo.UserRoles AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''UserRoles'',N''Database change; see ChangeData.'',@details);
END';
EXEC sys.sp_executesql N'CREATE OR ALTER TRIGGER dbo.trg_RolePermissions_Audit ON dbo.RolePermissions AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM inserted) AND NOT EXISTS(SELECT 1 FROM deleted) RETURN;
 DECLARE @actor int=TRY_CONVERT(int,SESSION_CONTEXT(N''ActorUserID''));
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserID=@actor) SET @actor=NULL;
 DECLARE @details nvarchar(max)=(SELECT JSON_QUERY((SELECT * FROM deleted FOR JSON PATH)) AS beforeRows,
 JSON_QUERY((SELECT * FROM inserted FOR JSON PATH)) AS afterRows, ORIGINAL_LOGIN() AS databaseLogin FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 -- Legacy Details is nvarchar(2000): full snapshots go in a new column.
 INSERT dbo.AuditLogs(ActorUserID,ActionName,EntityName,Details,ChangeData)
 VALUES(@actor,CASE WHEN NOT EXISTS(SELECT 1 FROM deleted) THEN ''INSERT'' WHEN NOT EXISTS(SELECT 1 FROM inserted) THEN ''DELETE'' ELSE ''UPDATE'' END,
 ''RolePermissions'',N''Database change; see ChangeData.'',@details);
END';
IF OBJECT_ID('dbo.SchemaMigrations','U') IS NULL
 CREATE TABLE dbo.SchemaMigrations(Version varchar(80) NOT NULL PRIMARY KEY,AppliedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version='002_RuntimeHardening')
 INSERT dbo.SchemaMigrations(Version) VALUES('002_RuntimeHardening');
COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
