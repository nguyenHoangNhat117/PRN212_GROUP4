using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.Models
{
    public class StudentRecord
    {
        public int RecordID { get; set; }

        public int StudentID { get; set; }

        public string RecordType { get; set; } = string.Empty;

        public string RecordTitle { get; set; } = string.Empty;

        public string? RecordContent { get; set; }

        public DateTime RecordDate { get; set; }

        public bool IsArchived { get; set; }

        public int? CreatedByUserID { get; set; }

        public DateTime CreatedAt { get; set; }

        // Dùng để hiển thị trên giao diện.
        public string ArchiveStatus =>
            IsArchived ? "ARCHIVED" : "ACTIVE";
    }
}
