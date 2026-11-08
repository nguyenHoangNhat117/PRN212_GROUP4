using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using PRN212_GROUP4.Services;
namespace PRN212_GROUP4.Tests;

public class ProfileAndSessionTests : SqlTestBase
{
    [Fact]
    public async Task Student_can_update_only_own_contact_fields()
    {
        await SignInAsync("student01");
        var service = new StudentProfileService(() => Db());
        var student = Assert.Single(await service.GetAsync());
        Assert.Equal(1, student.StudentID);
        student.Email = "updated@example.test"; student.Phone = "0901234567";
        await service.UpdateAsync(student);
        Assert.Equal("updated@example.test", Assert.Single(await service.GetAsync()).Email);
        student.FullName = "Unauthorized name";
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateAsync(student));
    }

    [Fact]
    public async Task Stale_profile_update_is_rejected()
    {
        await SignInAsync();
        var service = new StudentProfileService(() => Db());
        var a = (await service.GetAsync()).First();
        var b = (await service.GetAsync()).First();
        a.Phone = "0901111111"; await service.UpdateAsync(a);
        b.Phone = "0902222222";
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(b));
        Assert.Equal("0901111111", (await service.GetAsync()).First().Phone);
    }

    [Fact]
    public async Task Logout_rejects_protected_calls_and_next_user_gets_only_own_data()
    {
        await SignInAsync("student01");
        var service = new StudentProfileService(() => Db());
        var previousVersion = AppSession.Version;
        AppSession.Clear();
        Assert.True(AppSession.Version > previousVersion);
        Assert.Null(AppSession.DisplayAccess);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAsync());
        await SignInAsync("student02");
        Assert.Equal(2, Assert.Single(await service.GetAsync()).StudentID);
    }
    [Fact]
    public async Task Logout_during_authorization_query_invalidates_the_pending_request()
    {
        await SignInAsync("student01");
        var gate = new QueryGate();
        var service = new StudentProfileService(() => Db(gate));
        var loading = service.GetAsync();
        await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        AppSession.Clear();
        gate.Release.TrySetResult();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => loading);
        Assert.Null(AppSession.UserId);
        Assert.Null(AppSession.DisplayAccess);
    }

    private sealed class QueryGate : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(15));
            return result;
        }
    }
}
