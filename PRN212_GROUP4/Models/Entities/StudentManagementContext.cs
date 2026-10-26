using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PRN212_GROUP4.Models.Entities;

public partial class StudentManagementContext : DbContext
{
    public StudentManagementContext()
    {
    }

    public StudentManagementContext(DbContextOptions<StudentManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicStaff> AcademicStaffs { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseEnrollment> CourseEnrollments { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<GradeScale> GradeScales { get; set; }

    public virtual DbSet<Lecturer> Lecturers { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentRecord> StudentRecords { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VCourseEnrollmentStatus> VCourseEnrollmentStatuses { get; set; }

    public virtual DbSet<VStudentCourseResult> VStudentCourseResults { get; set; }

    public virtual DbSet<VStudentGpa> VStudentGpas { get; set; }

    public virtual DbSet<VStudentSemesterGpa> VStudentSemesterGpas { get; set; }

    protected override void OnConfiguring(
    DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        string connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình chuỗi kết nối DefaultConnection.");

        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicStaff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Academic__96D4AAF780881CC4");

            entity.ToTable("AcademicStaff");

            entity.HasIndex(e => e.StaffCode, "UQ__Academic__D83AD8120D06D6A3").IsUnique();

            entity.HasIndex(e => e.UserId, "UX_AcademicStaff_UserID_NotNull")
                .IsUnique()
                .HasFilter("([UserID] IS NOT NULL)");

            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StaffCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Department).WithMany(p => p.AcademicStaffs)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__AcademicS__Depar__74AE54BC");

            entity.HasOne(d => d.User).WithOne(p => p.AcademicStaff)
                .HasForeignKey<AcademicStaff>(d => d.UserId)
                .HasConstraintName("FK__AcademicS__UserI__73BA3083");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK__AuditLog__A17F23B857034839");

            entity.HasIndex(e => e.HappenedAt, "IX_AuditLogs_Time").IsDescending();

            entity.Property(e => e.AuditId).HasColumnName("AuditID");
            entity.Property(e => e.ActionName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ActorUserId).HasColumnName("ActorUserID");
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.EntityKey).HasMaxLength(100);
            entity.Property(e => e.EntityName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.HappenedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ActorUserId)
                .HasConstraintName("FK__AuditLogs__Actor__2EDAF651");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927A0AF0C8730");

            entity.ToTable(tb => tb.HasTrigger("trg_Classes_ProtectRegistrations"));

            entity.HasIndex(e => new { e.CourseId, e.SemesterId }, "IX_Classes_Course_Semester");

            entity.HasIndex(e => e.ClassCode, "UQ__Classes__2ECD4A55DB5EC1EF").IsUnique();

            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.ClassCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ClassStatus)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("OPEN");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.LecturerId).HasColumnName("LecturerID");
            entity.Property(e => e.ScheduleNote).HasMaxLength(250);
            entity.Property(e => e.SemesterId).HasColumnName("SemesterID");

            entity.HasOne(d => d.Course).WithMany(p => p.Classes)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Classes__CourseI__02FC7413");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.Classes)
                .HasForeignKey(d => d.LecturerId)
                .HasConstraintName("FK__Classes__Lecture__04E4BC85");

            entity.HasOne(d => d.Semester).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Classes__Semeste__03F0984C");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Courses__C92D7187176DB146");

            entity.HasIndex(e => e.CourseCode, "UQ__Courses__FC00E000D1FB0FCE").IsUnique();

            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.CourseCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CourseName).HasMaxLength(150);
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Department).WithMany(p => p.Courses)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__Courses__Departm__7A672E12");
        });

        modelBuilder.Entity<CourseEnrollment>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_CourseEnrollments_ProtectAssignments"));

            entity.HasIndex(e => new { e.CourseId, e.SemesterId }, "IX_CourseEnrollments_Course_Term");

            entity.HasIndex(e => new { e.StudentId, e.CourseId, e.SemesterId }, "UQ_CourseEnrollments_Student_Course_Term").IsUnique();

            entity.Property(e => e.CourseEnrollmentId).HasColumnName("CourseEnrollmentID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.RegistrationDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RegistrationStatus)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("REGISTERED");
            entity.Property(e => e.RowVer)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SemesterId).HasColumnName("SemesterID");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseEnrollments)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CourseEnr__Cours__0C85DE4D");

            entity.HasOne(d => d.Semester).WithMany(p => p.CourseEnrollments)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CourseEnr__Semes__0D7A0286");

            entity.HasOne(d => d.Student).WithMany(p => p.CourseEnrollments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CourseEnr__Stude__0B91BA14");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BCD21382C5F");

            entity.HasIndex(e => e.DepartmentCode, "UQ__Departme__6EA8896DC121CBBC").IsUnique();

            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.DepartmentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DepartmentName).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Enrollme__7F6877FB13A86647");

            entity.ToTable(tb => tb.HasTrigger("trg_Enrollments_RegistrationMatch"));

            entity.HasIndex(e => e.ClassId, "IX_Enrollments_Class");

            entity.HasIndex(e => e.StudentId, "IX_Enrollments_Student");

            entity.HasIndex(e => new { e.StudentId, e.ClassId }, "UQ_Enrollments_Student_Class").IsUnique();

            entity.HasIndex(e => e.CourseEnrollmentId, "UX_Enrollments_ActiveRegistration")
                .IsUnique()
                .HasFilter("([EnrollmentStatus]<>'DROPPED')");

            entity.Property(e => e.EnrollmentId).HasColumnName("EnrollmentID");
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CourseEnrollmentId).HasColumnName("CourseEnrollmentID");
            entity.Property(e => e.EnrollmentDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EnrollmentStatus)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("ENROLLED");
            entity.Property(e => e.RowVer)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Class).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__Class__160F4887");

            entity.HasOne(d => d.CourseEnrollment).WithOne(p => p.Enrollment)
                .HasForeignKey<Enrollment>(d => d.CourseEnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__Cours__14270015");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__Stude__151B244E");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradeId).HasName("PK__Grades__54F87A37F191AA78");

            entity.HasIndex(e => e.IsPublished, "IX_Grades_Published");

            entity.HasIndex(e => e.EnrollmentId, "UQ__Grades__7F6877FAA640D958").IsUnique();

            entity.Property(e => e.GradeId).HasColumnName("GradeID");
            entity.Property(e => e.EnrollmentId).HasColumnName("EnrollmentID");
            entity.Property(e => e.FinalScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.RowVer)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("UpdatedByUserID");

            entity.HasOne(d => d.Enrollment).WithOne(p => p.Grade)
                .HasForeignKey<Grade>(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Grades__Enrollme__1CBC4616");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.Grades)
                .HasForeignKey(d => d.UpdatedByUserId)
                .HasConstraintName("FK__Grades__UpdatedB__1F98B2C1");
        });

        modelBuilder.Entity<GradeScale>(entity =>
        {
            entity.HasKey(e => e.GradeScaleId).HasName("PK__GradeSca__B5AD3A47E517ACC8");

            entity.ToTable("GradeScale");

            entity.Property(e => e.GradeScaleId).HasColumnName("GradeScaleID");
            entity.Property(e => e.GradeLetter)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.GradePoint).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.MinScore).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<Lecturer>(entity =>
        {
            entity.HasKey(e => e.LecturerId).HasName("PK__Lecturer__5A78B91DBB0F8C65");

            entity.HasIndex(e => e.LecturerCode, "UQ__Lecturer__BB9CDAB30BC0C58E").IsUnique();

            entity.HasIndex(e => e.UserId, "UX_Lecturers_UserID_NotNull")
                .IsUnique()
                .HasFilter("([UserID] IS NOT NULL)");

            entity.Property(e => e.LecturerId).HasColumnName("LecturerID");
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LecturerCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Department).WithMany(p => p.Lecturers)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__Lecturers__Depar__6EF57B66");

            entity.HasOne(d => d.User).WithOne(p => p.Lecturer)
                .HasForeignKey<Lecturer>(d => d.UserId)
                .HasConstraintName("FK__Lecturers__UserI__6E01572D");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK__Permissi__EFA6FB0F3B34DAA8");

            entity.HasIndex(e => e.FunctionCode, "UQ__Permissi__E037746C4E601DBE").IsUnique();

            entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
            entity.Property(e => e.FunctionCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.FunctionName).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3AA88CDA12");

            entity.HasIndex(e => e.RoleCode, "UQ__Roles__D62CB59C45987640").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.RoleName).HasMaxLength(80);

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__RolePermi__Permi__52593CB8"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__RolePermi__RoleI__5165187F"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId");
                        j.ToTable("RolePermissions");
                        j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                        j.IndexerProperty<int>("PermissionId").HasColumnName("PermissionID");
                    });
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("PK__Semester__043301BD8554D983");

            entity.HasIndex(e => e.SemesterCode, "UQ__Semester__513151C99FDB766B").IsUnique();

            entity.Property(e => e.SemesterId).HasColumnName("SemesterID");
            entity.Property(e => e.SemesterCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SemesterName).HasMaxLength(100);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52A79C94409F3");

            entity.HasIndex(e => e.StudentCode, "UQ__Students__1FC88604B9CD4BC7").IsUnique();

            entity.HasIndex(e => e.UserId, "UX_Students_UserID_NotNull")
                .IsUnique()
                .HasFilter("([UserID] IS NOT NULL)");

            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.RowVer)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.StudentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StudentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Department).WithMany(p => p.Students)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__Students__Depart__6754599E");

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.UserId)
                .HasConstraintName("FK__Students__UserID__66603565");
        });

        modelBuilder.Entity<StudentRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__StudentR__FBDF78C91F64FE1A");

            entity.HasIndex(e => new { e.StudentId, e.IsArchived }, "IX_Records_Student");

            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.RecordDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.RecordTitle).HasMaxLength(180);
            entity.Property(e => e.RecordType).HasMaxLength(60);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.StudentRecords)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK__StudentRe__Creat__2B0A656D");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentRecords)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentRe__Stude__282DF8C2");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACD11FF5B2");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4F0EC1AA3").IsUnique();

            entity.HasIndex(e => e.Email, "UX_Users_Email_NotNull")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL)");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DisplayName).HasMaxLength(120);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MustChangePassword).HasDefaultValue(true);
            entity.Property(e => e.PasswordAlgorithm)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PBKDF2-SHA256");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(256)
                .IsUnicode(false);
            entity.Property(e => e.PasswordIterations).HasDefaultValue(210000);
            entity.Property(e => e.PasswordSalt)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.RowVer)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.Username).HasMaxLength(80);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__RoleI__5EBF139D"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__UserI__5DCAEF64"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                        j.IndexerProperty<int>("UserId").HasColumnName("UserID");
                        j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                    });
        });

        modelBuilder.Entity<VCourseEnrollmentStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vCourseEnrollmentStatus");

            entity.Property(e => e.ClassCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ClassId).HasColumnName("ClassID");
            entity.Property(e => e.CourseCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CourseEnrollmentId).HasColumnName("CourseEnrollmentID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.CourseName).HasMaxLength(150);
            entity.Property(e => e.EnrollmentId).HasColumnName("EnrollmentID");
            entity.Property(e => e.EnrollmentStatus)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.RegistrationDate).HasPrecision(0);
            entity.Property(e => e.RegistrationStatus)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.SemesterCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SemesterId).HasColumnName("SemesterID");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
        });

        modelBuilder.Entity<VStudentCourseResult>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vStudentCourseResults");

            entity.Property(e => e.ClassCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CourseCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CourseName).HasMaxLength(150);
            entity.Property(e => e.FinalScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.GradeLetter)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.GradePoint).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.SemesterCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.StudentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
        });

        modelBuilder.Entity<VStudentGpa>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vStudentGPA");

            entity.Property(e => e.CumulativeGpa)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("CumulativeGPA");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
        });

        modelBuilder.Entity<VStudentSemesterGpa>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vStudentSemesterGPA");

            entity.Property(e => e.SemesterCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SemesterGpa)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("SemesterGPA");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
