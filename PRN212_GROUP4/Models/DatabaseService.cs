using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

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
                    StudentID,
                    StudentCode,
                    UserID,
                    FullName,
                    DateOfBirth,
                    Email,
                    Phone,
                    DepartmentID,
                    EnrollmentDate,
                    StudentStatus,
                    CreatedAt,
                    RowVer
                FROM dbo.Students
                ORDER BY StudentCode;";

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

                    RowVer = (byte[])reader.GetValue(11)
                };

                students.Add(student);
            }

            return students;
        }
    }
}