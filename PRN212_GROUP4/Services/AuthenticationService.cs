using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PRN212_GROUP4.Models.Entities;

namespace PRN212_GROUP4.Services;

public enum LoginOutcome
{
    SignedIn,
    PasswordChangeRequired
}

public static class PasswordCodec
{
    public static bool Verify(
        string password,
        string hash,
        string salt,
        string algorithm,
        int iterations)
    {
        if (algorithm != "PBKDF2-SHA256"
            || iterations < 10000
            || iterations > 2000000)
        {
            return false;
        }

        try
        {
            var expected = Convert.FromHexString(hash);
            var saltBytes = Convert.FromHexString(salt);

            if (expected.Length != 32 || saltBytes.Length < 16)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                iterations,
                HashAlgorithmName.SHA256,
                32);

            return CryptographicOperations.FixedTimeEquals(
                actual,
                expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    internal static void ValidateNew(
    string password,
    string confirmation)
    {
        if (string.IsNullOrWhiteSpace(password)
            || password.Length < 6
            || password.Length > 128)
        {
            throw new ArgumentException(
                "Mật khẩu mới phải có 6–128 ký tự.");
        }

        if (password != confirmation)
        {
            throw new ArgumentException(
                "Xác nhận mật khẩu mới chưa khớp.");
        }
    }

    internal static void Assign(User user, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);

        user.PasswordSalt = Convert.ToHexString(salt);
        user.PasswordHash = Convert.ToHexString(
            Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                210000,
                HashAlgorithmName.SHA256,
                32));

        user.PasswordAlgorithm = "PBKDF2-SHA256";
        user.PasswordIterations = 210000;
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
    }
}

public sealed class AuthenticationService
{
    // Chỉ dùng sau khi đã xác thực mật khẩu nhưng tài khoản
    // bắt buộc đổi mật khẩu trước khi được vào ứng dụng.
    private int? pendingUserId;
    private byte[]? pendingVersion;
    private DateTime pendingExpiry;

    private static bool HasPermission(User user, string code)
    {
        return user.Roles.Any(role =>
            role.Permissions.Any(permission =>
                permission.FunctionCode == code));
    }

    private static bool Matches(User user, string password)
    {
        return PasswordCodec.Verify(
            password,
            user.PasswordHash,
            user.PasswordSalt,
            user.PasswordAlgorithm,
            user.PasswordIterations);
    }

    private static IQueryable<User> Accounts(
        StudentManagementContext db)
    {
        return db.Users
            .Include(user => user.Roles)
            .ThenInclude(role => role.Permissions);
    }

    public void CancelPendingChange()
    {
        pendingUserId = null;
        pendingVersion = null;
        pendingExpiry = default;
    }

    public async Task<LoginOutcome> LoginAsync(
        string username,
        string password)
    {
        AppSession.Clear();
        CancelPendingChange();

        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrEmpty(password))
        {
            throw new ArgumentException(
                "Nhập tài khoản và mật khẩu.");
        }

        using var db = new StudentManagementContext();

        var user = await Accounts(db).SingleOrDefaultAsync(
            account => account.Username == username.Trim());

        if (user == null
            || !user.IsActive
            || !Matches(user, password))
        {
            throw new UnauthorizedAccessException(
                "Tài khoản hoặc mật khẩu không hợp lệ.");
        }

        if (!HasPermission(user, "F01"))
        {
            throw new UnauthorizedAccessException(
                "Tài khoản không được phép đăng nhập.");
        }

        if (user.MustChangePassword)
        {
            if (!HasPermission(user, "F20"))
            {
                throw new UnauthorizedAccessException(
                    "Liên hệ quản trị viên để được cấp quyền đổi mật khẩu.");
            }

            pendingUserId = user.UserId;
            pendingVersion = user.RowVer.ToArray();
            pendingExpiry = DateTime.UtcNow.AddMinutes(5);

            // Chưa tạo AppSession và chưa mở MainWindow.
            return LoginOutcome.PasswordChangeRequired;
        }

        var access = await AccessService.LoadAsync(db, user.UserId);

        AppSession.SignIn(user.UserId, access);

        return LoginOutcome.SignedIn;
    }

    public async Task ChangeOwnPasswordAsync(
        string current,
        string password,
        string confirmation)
    {
        PasswordCodec.ValidateNew(password, confirmation);

        if (AppSession.UserId is not int id)
        {
            throw new UnauthorizedAccessException(
                "Vui lòng đăng nhập lại.");
        }

        using var db = new StudentManagementContext();

        // Lấy tài khoản từ phiên, không nhận UserID từ form.
        var user = await Accounts(db).SingleOrDefaultAsync(
            account => account.UserId == id);

        if (user == null
            || !user.IsActive
            || user.MustChangePassword)
        {
            throw new UnauthorizedAccessException(
                "Phiên không hợp lệ. Hãy đăng xuất và đăng nhập lại.");
        }

        if (!HasPermission(user, "F20"))
        {
            throw new UnauthorizedAccessException(
                "Bạn không có quyền đổi mật khẩu.");
        }

        if (string.IsNullOrEmpty(current)
            || !Matches(user, current))
        {
            throw new ArgumentException(
                "Mật khẩu hiện tại không đúng.");
        }

        if (Matches(user, password))
        {
            throw new ArgumentException(
                "Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        PasswordCodec.Assign(user, password);

        await SaveAsync(db);
    }

    public async Task CompleteRequiredChangeAsync(
        string password,
        string confirmation)
    {
        PasswordCodec.ValidateNew(password, confirmation);

        if (pendingUserId is not int id
            || pendingVersion == null
            || DateTime.UtcNow > pendingExpiry)
        {
            CancelPendingChange();

            throw new UnauthorizedAccessException(
                "Yêu cầu đã hết hạn. Đóng cửa sổ và đăng nhập lại.");
        }

        using var db = new StudentManagementContext();

        var user = await Accounts(db).SingleOrDefaultAsync(
            account => account.UserId == id);

        if (user == null
            || !user.IsActive
            || !user.MustChangePassword
            || !user.RowVer.SequenceEqual(pendingVersion))
        {
            CancelPendingChange();

            throw new UnauthorizedAccessException(
                "Tài khoản đã thay đổi. Đóng cửa sổ và đăng nhập lại.");
        }

        if (!HasPermission(user, "F01")
            || !HasPermission(user, "F20"))
        {
            throw new UnauthorizedAccessException(
                "Tài khoản không đủ quyền. Liên hệ quản trị viên.");
        }

        if (Matches(user, password))
        {
            throw new ArgumentException(
                "Mật khẩu mới phải khác mật khẩu tạm/hiện tại.");
        }

        PasswordCodec.Assign(user, password);

        await SaveAsync(db);

        CancelPendingChange();

        var access = await AccessService.LoadAsync(db, user.UserId);

        AppSession.SignIn(user.UserId, access);
    }

    private static async Task SaveAsync(
        StudentManagementContext db)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "Tài khoản vừa được thay đổi ở nơi khác. "
                + "Hãy đăng nhập lại trước khi thử tiếp.");
        }
    }
}