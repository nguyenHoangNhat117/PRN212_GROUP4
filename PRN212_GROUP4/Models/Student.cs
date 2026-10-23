using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.Models
{
    public class Student
    {
        public int StudentID { get; set; }

        public string StudentCode { get; set; } = string.Empty;

        public int? UserID { get; set; }

        public string FullName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public int? DepartmentID { get; set; }

        public string? DepartmentName { get; set; }

        public DateTime? EnrollmentDate { get; set; }

        public string StudentStatus { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public byte[] RowVer { get; set; } = Array.Empty<byte>();
    }
}
