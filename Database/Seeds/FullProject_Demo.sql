/* DEMO ONLY. Enable G4.DemoSeedAllowed only on a disposable demo database.
   Never run on production. No known password is installed: use AccountBootstrap.
   Atomic, versioned, re-runnable; repeat does not overwrite edits/passwords. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'G4.DemoSeedAllowed' AND CONVERT(nvarchar(20),value)=N'1')
 THROW 51600,'Demo seed disabled. Use Install_FullProject_Demo.sql for a separate demo database.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version='002_RuntimeHardening')
 THROW 51601,'Apply migrations 001 and 002 first.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @lock int;
 EXEC @lock=sys.sp_getapplock @Resource=N'G4.FullProjectDemo',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
 IF @lock<0 THROW 51602,'Demo seed is busy.',1;
 IF EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version='Seed_FullProjectDemo_v1')
 BEGIN COMMIT; PRINT 'Demo v1 already installed; no records changed.'; RETURN; END;
 IF EXISTS(SELECT 1 FROM dbo.Users WHERE Username LIKE 'demo[_]%')
 OR EXISTS(SELECT 1 FROM dbo.Departments WHERE DepartmentCode LIKE 'DEMO-%')
 OR EXISTS(SELECT 1 FROM dbo.Students WHERE StudentCode LIKE 'DEMO-%')
 OR EXISTS(SELECT 1 FROM dbo.Courses WHERE CourseCode LIKE 'DEMO-%')
 OR EXISTS(SELECT 1 FROM dbo.Classes WHERE ClassCode LIKE 'DEMO-%')
 OR EXISTS(SELECT 1 FROM dbo.Semesters WHERE SemesterCode LIKE 'DEMO-%')
 THROW 51603,'Demo identifiers already exist without seed marker. Review, do not overwrite.',1;
 INSERT dbo.Departments(DepartmentCode,DepartmentName) VALUES('DEMO-SE',N'DEMO Software'),('DEMO-BA',N'DEMO Business');
 DECLARE @se int=(SELECT DepartmentID FROM dbo.Departments WHERE DepartmentCode='DEMO-SE'),
         @ba int=(SELECT DepartmentID FROM dbo.Departments WHERE DepartmentCode='DEMO-BA');
 DECLARE @accounts TABLE(Username nvarchar(80),RoleCode varchar(30),Active bit);
 INSERT @accounts VALUES('demo_admin','ADMIN',1),('demo_staff_se','ACADEMIC_STAFF',1),('demo_staff_ba','ACADEMIC_STAFF',1),
 ('demo_lecturer_se','LECTURER',1),('demo_lecturer_ba','LECTURER',1),
 ('demo_student_a','STUDENT',1),('demo_student_b','STUDENT',1),('demo_student_c','STUDENT',1),
 ('demo_student_ba','STUDENT',1),('demo_student_zero','STUDENT',1),('demo_student_ten','STUDENT',1),
 ('demo_student_inactive','STUDENT',0),('demo_reset','STUDENT',1);
 INSERT dbo.Users(Username,DisplayName,PasswordHash,PasswordSalt,IsActive,MustChangePassword)
 SELECT Username,Username,CONVERT(varchar(64),CRYPT_GEN_RANDOM(32),2),CONVERT(varchar(32),CRYPT_GEN_RANDOM(16),2),Active,1 FROM @accounts;
 INSERT dbo.UserRoles(UserID,RoleID) SELECT u.UserID,r.RoleID FROM @accounts a JOIN dbo.Users u ON u.Username=a.Username JOIN dbo.Roles r ON r.RoleCode=a.RoleCode;
 INSERT dbo.AcademicStaff(StaffCode,UserID,FullName,DepartmentID)
 SELECT CASE WHEN Username='demo_staff_se' THEN 'DEMO-NV-SE' ELSE 'DEMO-NV-BA' END,UserID,DisplayName,
 CASE WHEN Username='demo_staff_se' THEN @se ELSE @ba END FROM dbo.Users WHERE Username IN('demo_staff_se','demo_staff_ba');
 INSERT dbo.Lecturers(LecturerCode,UserID,FullName,DepartmentID)
 SELECT CASE WHEN Username='demo_lecturer_se' THEN 'DEMO-GV-SE' ELSE 'DEMO-GV-BA' END,UserID,DisplayName,
 CASE WHEN Username='demo_lecturer_se' THEN @se ELSE @ba END FROM dbo.Users WHERE Username IN('demo_lecturer_se','demo_lecturer_ba');
 INSERT dbo.Students(StudentCode,UserID,FullName,DepartmentID,Email,Phone,StudentStatus,EnrollmentDate)
 SELECT s.Code,u.UserID,s.Name,CASE WHEN s.Code='DEMO-BA' THEN @ba ELSE @se END,
 u.Username+'@example.invalid','0900000000',CASE WHEN s.Code='DEMO-INACTIVE' THEN 'INACTIVE' ELSE 'ACTIVE' END,'2026-09-01'
 FROM (VALUES('DEMO-A','demo_student_a',N'DEMO Nguyen An'),('DEMO-B','demo_student_b',N'DEMO Nguyen Binh'),
 ('DEMO-C','demo_student_c',N'DEMO Tran An'),('DEMO-BA','demo_student_ba',N'DEMO Le Mai'),
 ('DEMO-ZERO','demo_student_zero',N'DEMO Zero'),('DEMO-TEN','demo_student_ten',N'DEMO Ten'),
 ('DEMO-INACTIVE','demo_student_inactive',N'DEMO Inactive'),('DEMO-RESET','demo_reset',N'DEMO Reset'))s(Code,Username,Name)
 JOIN dbo.Users u ON u.Username=s.Username;
 INSERT dbo.Semesters(SemesterCode,SemesterName,StartDate,EndDate) VALUES
 ('DEMO-T1',N'DEMO completed term','2026-01-01','2026-04-30'),('DEMO-T2',N'DEMO repeat term','2026-05-01','2026-08-31'),
 ('DEMO-T3',N'DEMO empty term','2026-09-01','2026-12-31');
 INSERT dbo.Courses(CourseCode,CourseName,Credits,DepartmentID,IsActive) VALUES
 ('DEMO-A',N'DEMO Course A',3,@se,1),('DEMO-B',N'DEMO Course B',2,@se,1),
 ('DEMO-C',N'DEMO Course C',1,@se,1),('DEMO-BA',N'DEMO Business',4,@ba,1),
 ('DEMO-OFF',N'DEMO inactive course',3,@se,0),('DEMO-NEW',N'DEMO no class yet',3,@se,1);
 INSERT dbo.Classes(ClassCode,CourseID,SemesterID,LecturerID,Capacity,ClassStatus,ScheduleNote)
 SELECT x.Code,c.CourseID,t.SemesterID,l.LecturerID,x.Capacity,x.Status,N'DEMO textual schedule; no automatic conflict policy'
 FROM (VALUES('DEMO-A1','DEMO-A','DEMO-T1',20,'OPEN','DEMO-GV-SE'),('DEMO-A2','DEMO-A','DEMO-T2',20,'OPEN','DEMO-GV-SE'),
 ('DEMO-B1','DEMO-B','DEMO-T1',20,'OPEN','DEMO-GV-SE'),('DEMO-C1','DEMO-C','DEMO-T1',20,'OPEN','DEMO-GV-SE'),
 ('DEMO-BA1','DEMO-BA','DEMO-T1',20,'OPEN','DEMO-GV-BA'),('DEMO-FULL','DEMO-C','DEMO-T2',1,'OPEN','DEMO-GV-SE'),
 ('DEMO-CLOSED','DEMO-B','DEMO-T2',20,'CLOSED','DEMO-GV-SE'),('DEMO-CANCELLED','DEMO-C','DEMO-T3',20,'CANCELLED','DEMO-GV-SE'),
 ('DEMO-EMPTY','DEMO-A','DEMO-T3',20,'OPEN','DEMO-GV-SE'))x(Code,Course,Term,Capacity,Status,Lecturer)
 JOIN dbo.Courses c ON c.CourseCode=x.Course JOIN dbo.Semesters t ON t.SemesterCode=x.Term JOIN dbo.Lecturers l ON l.LecturerCode=x.Lecturer;
 -- Explicit fixtures, not runtime enrollment rules. All keys resolved by stable codes.
 DECLARE @cases TABLE(Student varchar(20),Course varchar(20),Term varchar(30),Class varchar(30),State varchar(15),Score decimal(5,2),Published bit);
 INSERT @cases VALUES
 ('DEMO-A','DEMO-A','DEMO-T1','DEMO-A1','COMPLETED',8.60,1),
 ('DEMO-A','DEMO-B','DEMO-T1','DEMO-B1','COMPLETED',4,1),
 ('DEMO-A','DEMO-A','DEMO-T2','DEMO-A2','COMPLETED',6.80,1),
 ('DEMO-B','DEMO-A','DEMO-T1','DEMO-A1','COMPLETED',7.99,0),
 ('DEMO-B','DEMO-B','DEMO-T1',NULL,'REGISTERED',NULL,0),
 ('DEMO-C','DEMO-A','DEMO-T1','DEMO-A1','ENROLLED',NULL,0),
 ('DEMO-C','DEMO-B','DEMO-T1','DEMO-B1','DROPPED',NULL,0),
 ('DEMO-C','DEMO-C','DEMO-T1','DEMO-C1','DROPPED',5,1),
 ('DEMO-BA','DEMO-BA','DEMO-T1','DEMO-BA1','COMPLETED',8.49,1),
 ('DEMO-ZERO','DEMO-A','DEMO-T1','DEMO-A1','COMPLETED',0,1),
 ('DEMO-TEN','DEMO-A','DEMO-T1','DEMO-A1','COMPLETED',10,1),
 ('DEMO-TEN','DEMO-C','DEMO-T2','DEMO-FULL','ENROLLED',NULL,0),
 ('DEMO-A','DEMO-C','DEMO-T2',NULL,'REGISTERED',NULL,0);
 INSERT dbo.CourseEnrollments(StudentID,CourseID,SemesterID,RegistrationStatus)
 SELECT s.StudentID,c.CourseID,t.SemesterID,CASE WHEN x.State IN('COMPLETED','DROPPED') THEN x.State ELSE 'REGISTERED' END
 FROM @cases x JOIN dbo.Students s ON s.StudentCode=x.Student JOIN dbo.Courses c ON c.CourseCode=x.Course JOIN dbo.Semesters t ON t.SemesterCode=x.Term;
 INSERT dbo.Enrollments(CourseEnrollmentID,StudentID,ClassID,EnrollmentStatus)
 SELECT r.CourseEnrollmentID,s.StudentID,cl.ClassID,x.State FROM @cases x JOIN dbo.Students s ON s.StudentCode=x.Student
 JOIN dbo.Classes cl ON cl.ClassCode=x.Class JOIN dbo.CourseEnrollments r ON r.StudentID=s.StudentID AND r.CourseID=cl.CourseID AND r.SemesterID=cl.SemesterID;
 INSERT dbo.Grades(EnrollmentID,FinalScore,IsPublished,UpdatedByUserID)
 SELECT e.EnrollmentID,x.Score,x.Published,l.UserID FROM @cases x JOIN dbo.Students s ON s.StudentCode=x.Student
 JOIN dbo.Classes cl ON cl.ClassCode=x.Class JOIN dbo.Enrollments e ON e.StudentID=s.StudentID AND e.ClassID=cl.ClassID
 JOIN dbo.Lecturers l ON l.LecturerID=cl.LecturerID WHERE x.Score IS NOT NULL;
 INSERT dbo.StudentRecords(StudentID,RecordType,DocumentNumber,RecordTitle,RecordContent,IsArchived)
 SELECT s.StudentID,N'DEMO Academic',v.Doc,v.Title,N'DEMO fictional document',v.Archived FROM dbo.Students s
 CROSS JOIN(VALUES(N'DEMO-DOC-001',N'DEMO first record',0),(N'DEMO-DOC-001',N'DEMO duplicate warning',0),(N'DEMO-DOC-ARCH',N'DEMO archive',1))v(Doc,Title,Archived)
 WHERE s.StudentCode='DEMO-A';
 INSERT dbo.SchemaMigrations(Version) VALUES('Seed_FullProjectDemo_v1');
 COMMIT;
 PRINT 'Full-project demo installed. Reset chosen active accounts with AccountBootstrap before login.';
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
