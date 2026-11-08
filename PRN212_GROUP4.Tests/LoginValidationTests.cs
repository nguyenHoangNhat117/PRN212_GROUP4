using PRN212_GROUP4.Services;
namespace PRN212_GROUP4.Tests;

public class LoginValidationTests
{
    [Theory]
    [InlineData("", "", true, true)]
    [InlineData("  ", "password", true, false)]
    [InlineData("student01", "", false, true)]
    [InlineData("student01", "password", false, false)]
    public void Each_missing_field_has_its_own_message(string username, string password,
        bool usernameMissing, bool passwordMissing)
    {
        var errors = LoginValidation.Validate(username, password);
        Assert.Equal(usernameMissing, errors.Username != null);
        Assert.Equal(passwordMissing, errors.Password != null);
        Assert.Equal(!usernameMissing && !passwordMissing, errors.IsValid);
    }
}
