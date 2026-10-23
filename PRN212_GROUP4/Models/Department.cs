using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.Models
{
    public class Department
    {
        public int DepartmentID { get; set; }

        public string DepartmentCode { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
