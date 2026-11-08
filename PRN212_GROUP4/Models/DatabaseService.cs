using PRN212_GROUP4.Services;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Models;

public class DatabaseService
{
    private readonly StudentProfileService profiles = new();
    private readonly StudentRecordService records = new();

    public bool TestConnection()
    {
        using var db = new StudentManagementContext();
        return db.Database.CanConnect();
    }

    public async Task<bool> TestConnectionAsync()
    {
        using var db = new StudentManagementContext();
        return await db.Database.CanConnectAsync();
    }

    public Task<List<Student>> GetStudentsAsync() =>
        profiles.GetAsync();

    public Task<List<Department>> GetDepartmentsAsync() =>
        profiles.DepartmentsAsync();

    public Task<int> AddStudentAsync(Student student) =>
        profiles.AddAsync(student);

    public Task UpdateStudentAsync(Student student) =>
        profiles.UpdateAsync(student);

    public Task<List<StudentRecord>> GetStudentRecordsAsync(
        int studentID,
        bool includeArchived = false) =>
        records.GetAsync(studentID, includeArchived);

    public Task<RecordSaveResult> AddStudentRecordAsync(StudentRecord record) =>
        records.AddAsync(record);

    public Task<RecordSaveResult> UpdateStudentRecordAsync(
        StudentRecord edited,
        StudentRecord original) =>
        records.UpdateAsync(edited, original);

    public Task ArchiveStudentRecordAsync(
        int studentID,
        int recordID) =>
        records.ArchiveAsync(studentID, recordID);
}