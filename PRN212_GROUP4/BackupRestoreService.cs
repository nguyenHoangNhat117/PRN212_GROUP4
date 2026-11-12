using Microsoft.Data.SqlClient;
using System.IO;
using Microsoft.Extensions.Configuration;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.Extensions.Services;

public sealed class BackupRestoreService
{
    private readonly FeatureDataService authorization = new();

    private static SqlConnectionStringBuilder ConnectionSettings()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
        var raw = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection trong appsettings.json.");
        return new SqlConnectionStringBuilder(raw);
    }

    public async Task BackupAsync(string destinationPath, IProgress<string>? progress = null)
    {
        await authorization.EnsurePermissionAsync("F19");
        if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Chọn đường dẫn file backup.");
        if (!string.Equals(Path.GetExtension(destinationPath), ".bak", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File backup phải có phần mở rộng .bak.");

        var builder = ConnectionSettings();
        string database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)) throw new InvalidOperationException("Connection String chưa có Database.");
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        string safeDatabase = QuoteIdentifier(database);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = $"BACKUP DATABASE {safeDatabase} TO DISK = @path WITH INIT, CHECKSUM, STATS = 10;";
        command.Parameters.AddWithValue("@path", destinationPath);
        progress?.Report("Đang sao lưu database...");
        await command.ExecuteNonQueryAsync();
        progress?.Report("Sao lưu hoàn tất.");
    }

    public async Task RestoreAsync(string backupPath, IProgress<string>? progress = null)
    {
        await authorization.EnsurePermissionAsync("F19");
        if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
            throw new FileNotFoundException("Không tìm thấy file backup.", backupPath);
        if (!string.Equals(Path.GetExtension(backupPath), ".bak", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Chỉ chấp nhận file .bak.");

        var builder = ConnectionSettings();
        string database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)) throw new InvalidOperationException("Connection String chưa có Database.");
        builder.InitialCatalog = "master";
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        string safeDatabase = QuoteIdentifier(database);

        progress?.Report("Đang kiểm tra file backup...");
        await using (var verify = connection.CreateCommand())
        {
            verify.CommandTimeout = 0;
            verify.CommandText = "RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM;";
            verify.Parameters.AddWithValue("@path", backupPath);
            await verify.ExecuteNonQueryAsync();
        }

        progress?.Report("Đang đưa database vào chế độ phục hồi...");
        try
        {
            await using var restore = connection.CreateCommand();
            restore.CommandTimeout = 0;
            restore.CommandText = $@"
ALTER DATABASE {safeDatabase} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE {safeDatabase} FROM DISK = @path WITH REPLACE, RECOVERY, CHECKSUM;
ALTER DATABASE {safeDatabase} SET MULTI_USER;";
            restore.Parameters.AddWithValue("@path", backupPath);
            await restore.ExecuteNonQueryAsync();
        }
        catch
        {
            try
            {
                await using var recover = connection.CreateCommand();
                recover.CommandText = $"IF DB_ID(@db) IS NOT NULL ALTER DATABASE {safeDatabase} SET MULTI_USER;";
                recover.Parameters.AddWithValue("@db", database);
                await recover.ExecuteNonQueryAsync();
            }
            catch { /* Preserve the original restore error. */ }
            throw;
        }
        progress?.Report("Khôi phục database hoàn tất. Hãy đăng nhập lại nếu phiên hiện tại bị ngắt.");
    }

    private static string QuoteIdentifier(string value) => "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";
}
