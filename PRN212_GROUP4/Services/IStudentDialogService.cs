using PRN212_GROUP4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN212_GROUP4.Services
{
    public interface IStudentDialogService
    {
        int? ShowAddStudent();

        bool ShowEditStudent(Student student);

        void ShowStudentRecords(Student student);

        void ShowInformation(string message);

        void ShowWarning(string message);
    }
}
