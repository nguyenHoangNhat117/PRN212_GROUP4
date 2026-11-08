using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRN212_GROUP4.Models.Entities;
using PRN212_GROUP4.Services;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace PRN212_GROUP4.Tests;

// Every test installs the real schema in its own disposable database.
// Never point this at production: the configured login needs CREATE DATABASE.
public abstract class SqlTestBase : IAsyncLifetime
{
    protected const string InitialPassword = "Initial!123";
    protected const string NewPassword = "Changed!456";
    private readonly string databaseName = "NhatTests_" + Guid.NewGuid().ToString("N");
    private string master = "";
    protected string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        master = Environment.GetEnvironmentVariable("STUDENT_TEST_SQL")
            ?? throw new InvalidOperationException("Set STUDENT_TEST_SQL to an isolated SQL Server test instance. See docs/TESTING.md.");
        var builder = new SqlConnectionStringBuilder(master) { InitialCatalog = "master" };
        master = builder.ConnectionString;
        builder.InitialCatalog = databaseName;
        ConnectionString = builder.ConnectionString;
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Database", "StudentManagement_G4.sql"));
        script = script.Replace("@DatabaseName sysname=N'PRN212_StudentManagement_G4'", $"@DatabaseName sysname=N'{databaseName}'");
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using var command = new SqlCommand(script, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
        AppSession.Clear();
    }

    protected StudentManagementContext Db(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<StudentManagementContext>()
            .UseSqlServer(ConnectionString).AddInterceptors(interceptors).Options);

    protected async Task ExecuteAsync(string sql)
    {
        await using var db = Db();
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    protected async Task<AuthenticationService> AccountAsync(string username, bool mustChange = false)
    {
        await using var db = Db();
        var user = await db.Users.SingleAsync(x => x.Username == username);
        PasswordCodec.Assign(user, InitialPassword);
        user.MustChangePassword = mustChange;
        await db.SaveChangesAsync();
        return new AuthenticationService(() => Db());
    }

    protected async Task SignInAsync(string username = "staff01")
    {
        var auth = await AccountAsync(username);
        Assert.Equal(LoginOutcome.SignedIn, await auth.LoginAsync(username, InitialPassword));
    }

    public async Task DisposeAsync()
    {
        AppSession.Clear();
        SqlConnection.ClearAllPools();
        if (string.IsNullOrEmpty(master)) return;
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END", connection);
        await command.ExecuteNonQueryAsync();
    }
}
