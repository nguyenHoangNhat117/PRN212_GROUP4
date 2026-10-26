using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PRN212_GROUP4.Models
{
    public class StudentDataContext : DbContext
    {
        public DbSet<Student> Students => Set<Student>();

        public DbSet<Department> Departments => Set<Department>();

        public DbSet<StudentRecord> StudentRecords =>
            Set<StudentRecord>();

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                return;
            }

            IConfiguration configuration =
                new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile(
                        "appsettings.json",
                        optional: false)
                    .Build();

            string? connectionString =
                configuration.GetConnectionString(
                    "DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Thiếu chuỗi kết nối DefaultConnection "
                    + "trong appsettings.json.");
            }

            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bảng Students
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable(
                    "Students",
                    "dbo",
                    table => table.UseSqlOutputClause(false));

                entity.HasKey(student => student.StudentID);

                entity.Property(student => student.StudentID)
                    .ValueGeneratedOnAdd();

                entity.Property(student => student.StudentCode)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .IsRequired();

                entity.Property(student => student.FullName)
                    .HasMaxLength(120)
                    .IsRequired();

                entity.Property(student => student.DateOfBirth)
                    .HasColumnType("date");

                entity.Property(student => student.Email)
                    .HasMaxLength(255);

                entity.Property(student => student.Phone)
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.Property(student => student.EnrollmentDate)
                    .HasColumnType("date");

                entity.Property(student => student.StudentStatus)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .IsRequired();

                // Khi thêm mới, sử dụng default của database.
                entity.Property(student => student.CreatedAt)
                    .ValueGeneratedOnAdd();

                // Dùng để phát hiện hồ sơ bị người khác sửa.
                entity.Property(student => student.RowVer)
                    .IsRowVersion();

                // Thuộc tính phục vụ hiển thị, không phải cột Students.
                entity.Ignore(student => student.DepartmentName);
            });

            // Bảng Departments
            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("Departments", "dbo");

                entity.HasKey(
                    department => department.DepartmentID);

                entity.Property(
                        department => department.DepartmentID)
                    .ValueGeneratedOnAdd();

                entity.Property(
                        department => department.DepartmentCode)
                    .IsRequired();

                entity.Property(
                        department => department.DepartmentName)
                    .IsRequired();
            });

            // Bảng StudentRecords
            modelBuilder.Entity<StudentRecord>(entity =>
            {
                entity.ToTable(
                    "StudentRecords",
                    "dbo",
                    table => table.UseSqlOutputClause(false));

                entity.HasKey(record => record.RecordID);

                entity.Property(record => record.RecordID)
                    .ValueGeneratedOnAdd();

                entity.Property(record => record.RecordType)
                    .HasMaxLength(60)
                    .IsRequired();

                entity.Property(record => record.RecordTitle)
                    .HasMaxLength(180)
                    .IsRequired();

                entity.Property(record => record.RecordContent)
                    .HasColumnType("nvarchar(max)");

                entity.Property(record => record.RecordDate)
                    .HasColumnType("date");

                entity.Property(record => record.CreatedAt)
                    .ValueGeneratedOnAdd();

                // Thuộc tính tính toán để hiển thị trên UI.
                entity.Ignore(record => record.ArchiveStatus);
            });
        }
    }
}