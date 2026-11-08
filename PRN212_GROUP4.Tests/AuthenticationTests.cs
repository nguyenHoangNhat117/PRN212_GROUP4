using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PRN212_GROUP4.Services;
namespace PRN212_GROUP4.Tests;

public class AuthenticationTests : SqlTestBase
{
    [Theory]
    [InlineData("student01", "STUDENT")]
    [InlineData("lecturer01", "LECTURER")]
    [InlineData("staff01", "ACADEMIC_STAFF")]
    [InlineData("admin01", "ADMIN")]
    public async Task All_four_actors_can_sign_in(string username, string role)
    {
        var auth = await AccountAsync(username);
        Assert.Equal(LoginOutcome.SignedIn, await auth.LoginAsync(username, InitialPassword));
        await using var db = Db();
        var account = await db.Users.Include(x => x.Roles).SingleAsync(x => x.UserId == AppSession.UserId);
        Assert.Contains(account.Roles, x => x.RoleCode == role);
        Assert.NotNull(AppSession.DisplayAccess);
    }

    [Fact]
    public async Task Inactive_and_unknown_accounts_use_the_same_generic_error()
    {
        var auth = await AccountAsync("student01");
        await ExecuteAsync("UPDATE dbo.Users SET IsActive=0 WHERE Username=N'student01'");
        var inactive = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync("student01", InitialPassword));
        var unknown = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync("missing", InitialPassword));
        Assert.Equal(unknown.Message, inactive.Message);
        Assert.Null(AppSession.UserId);
    }

    [Theory]
    [InlineData("student01")]
    [InlineData("lecturer01")]
    [InlineData("staff01")]
    [InlineData("admin01")]
    public async Task First_login_cannot_create_session_until_password_changes(string username)
    {
        var auth = await AccountAsync(username, mustChange: true);
        Assert.Equal(LoginOutcome.PasswordChangeRequired, await auth.LoginAsync(username, InitialPassword));
        Assert.Null(AppSession.UserId);
        await using (var db = Db())
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => AccessService.RequireAsync(db, "F09"));
        await auth.CompleteRequiredChangeAsync(NewPassword, NewPassword);
        Assert.NotNull(AppSession.UserId);
        AppSession.Clear();
        Assert.Equal(LoginOutcome.SignedIn, await auth.LoginAsync(username, NewPassword));
    }

    [Fact]
    public async Task Cancelled_first_change_cannot_be_reused()
    {
        var auth = await AccountAsync("student01", true);
        await auth.LoginAsync("student01", InitialPassword);
        auth.CancelPendingChange();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.CompleteRequiredChangeAsync(NewPassword, NewPassword));
        Assert.Null(AppSession.UserId);
    }

    [Theory]
    [InlineData("wrong", "Changed!456", "Changed!456")]
    [InlineData("Initial!123", "Changed!456", "mismatch")]
    public async Task Invalid_password_change_preserves_hash(string current, string password, string confirmation)
    {
        var auth = await AccountAsync("student01");
        await auth.LoginAsync("student01", InitialPassword);
        await using var before = Db();
        string oldHash = await before.Users.Where(x => x.Username == "student01").Select(x => x.PasswordHash).SingleAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => auth.ChangeOwnPasswordAsync(current, password, confirmation));
        await using var after = Db();
        Assert.Equal(oldHash, await after.Users.Where(x => x.Username == "student01").Select(x => x.PasswordHash).SingleAsync());
    }

    [Fact]
    public async Task Failed_save_keeps_old_hash_and_old_login_working()
    {
        var auth = await AccountAsync("student01");
        await auth.LoginAsync("student01", InitialPassword);
        await using var before = Db();
        string oldHash = await before.Users.Where(x => x.Username == "student01").Select(x => x.PasswordHash).SingleAsync();
        var failing = new AuthenticationService(() => Db(new RejectSave()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failing.ChangeOwnPasswordAsync(InitialPassword, NewPassword, NewPassword));
        await using var after = Db();
        Assert.Equal(oldHash, await after.Users.Where(x => x.Username == "student01").Select(x => x.PasswordHash).SingleAsync());
        Assert.Equal(LoginOutcome.SignedIn, await auth.LoginAsync("student01", InitialPassword));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync("student01", NewPassword));
    }

    [Fact]
    public async Task Successful_change_accepts_new_password_and_rejects_old_one()
    {
        var auth = await AccountAsync("student01");
        await auth.LoginAsync("student01", InitialPassword);
        await auth.ChangeOwnPasswordAsync(InitialPassword, NewPassword, NewPassword);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync("student01", InitialPassword));
        Assert.Equal(LoginOutcome.SignedIn, await auth.LoginAsync("student01", NewPassword));
    }

    [Fact]
    public async Task First_change_save_failure_keeps_password_flag_and_session_blocked()
    {
        await AccountAsync("student01", mustChange: true);
        var auth = new AuthenticationService(() => Db(new RejectSave()));
        Assert.Equal(LoginOutcome.PasswordChangeRequired, await auth.LoginAsync("student01", InitialPassword));
        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.CompleteRequiredChangeAsync(NewPassword, NewPassword));
        Assert.Null(AppSession.UserId);
        await using var db = Db();
        var user = await db.Users.SingleAsync(x => x.Username == "student01");
        Assert.True(user.MustChangePassword);
        Assert.True(PasswordCodec.Verify(InitialPassword, user.PasswordHash, user.PasswordSalt, user.PasswordAlgorithm, user.PasswordIterations));
    }

    [Fact]
    public async Task SQL_rejected_password_write_preserves_the_stored_hash()
    {
        var auth = await AccountAsync("student01");
        await auth.LoginAsync("student01", InitialPassword);
        await using var before = Db();
        string hash = await before.Users.Where(x => x.Username == "student01").Select(x => x.PasswordHash).SingleAsync();
        // Hash is server-generated hexadecimal, not user-controlled SQL input.
        await ExecuteAsync($"ALTER TABLE dbo.Users ADD CONSTRAINT CK_Test_PreserveHash CHECK (Username <> N'student01' OR PasswordHash='{hash}')");
        await Assert.ThrowsAsync<DbUpdateException>(() => auth.ChangeOwnPasswordAsync(InitialPassword, NewPassword, NewPassword));
        await using var after = Db();
        Assert.Equal(hash, await after.Users.Where(x => x.Username == "student01").Select(x => x.PasswordHash).SingleAsync());
        Assert.Equal(LoginOutcome.SignedIn, await auth.LoginAsync("student01", InitialPassword));
    }

    private sealed class RejectSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated database write failure");
    }
}
