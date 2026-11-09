using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PRN212_GROUP4.Models.Entities;

public partial class StudentManagementContext
{
    // Giữ tương thích với code hiện tại:
    // new StudentManagementContext()
    public StudentManagementContext()
    {
    }

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        // Nếu kết nối đã được truyền qua DbContextOptions
        // thì giữ nguyên, ví dụ khi chạy kiểm thử.
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        string? connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình ConnectionStrings:DefaultConnection "
                + "trong appsettings.json.");
        }

        optionsBuilder.UseSqlServer(connectionString);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Các bảng có trigger: không dùng OUTPUT trực tiếp.
        modelBuilder.Entity<Class>()
            .ToTable(
                "Classes",
                table => table.UseSqlOutputClause(false));

        modelBuilder.Entity<CourseEnrollment>()
            .ToTable(
                "CourseEnrollments",
                table => table.UseSqlOutputClause(false));

        modelBuilder.Entity<Enrollment>()
            .ToTable(
                "Enrollments",
                table => table.UseSqlOutputClause(false));

        modelBuilder.Entity<Grade>()
            .ToTable(
                "Grades",
                table => table.UseSqlOutputClause(false));

        modelBuilder.Entity<GradeScale>()
            .ToTable(
                "GradeScale",
                table => table.UseSqlOutputClause(false));

        modelBuilder.Entity<Student>()
            .ToTable(
                "Students",
                table => table.UseSqlOutputClause(false));

        modelBuilder.Entity<StudentRecord>()
            .ToTable(
                "StudentRecords",
                table => table.UseSqlOutputClause(false));

        // Một đăng ký môn có thể có nhiều lượt xếp lớp trong lịch sử,
        // nhưng chỉ được có một lượt chưa hủy.
        //
        // Scaffold nhận diện filtered unique index thành quan hệ 1-1.
        // Điều chỉnh quan hệ EF thành 1-n; filtered unique index trong
        // database vẫn bảo đảm chỉ có một lượt xếp lớp đang hoạt động.
        modelBuilder.Entity<CourseEnrollment>()
            .Ignore("Enrollment");

        modelBuilder.Entity<Enrollment>()
            .HasOne(enrollment => enrollment.CourseEnrollment)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.CourseEnrollmentId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}