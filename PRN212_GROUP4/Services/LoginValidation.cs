namespace PRN212_GROUP4.Services;

public sealed record LoginErrors(string? Username, string? Password)
{
    public bool IsValid => Username == null && Password == null;
}

public static class LoginValidation
{
    public static LoginErrors Validate(string? username, string? password) => new(
        string.IsNullOrWhiteSpace(username) ? "Vui lòng nhập tài khoản." : null,
        string.IsNullOrEmpty(password) ? "Vui lòng nhập mật khẩu." : null);
}
