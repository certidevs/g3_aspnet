using System.ComponentModel.DataAnnotations;
using G3AspNet.ViewModels.Account;

namespace G3AspNet.Tests;

public sealed class LoginViewModelTests
{
    [Fact]
    public void ValidCredentials_PassValidationWithoutRole()
    {
        var model = new LoginViewModel
        {
            Username = "user",
            Password = "User123!"
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }
}
