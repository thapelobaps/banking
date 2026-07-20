using Kape.Api.DTOs.Auth;
using Kape.Api.Validation;
using Xunit;

namespace Kape.Api.Tests.Validation;

public sealed class SouthAfricanRegistrationValidatorTests
{
    [Theory]
    [InlineData("082 123 4567", "+27821234567")]
    [InlineData("082-123-4567", "+27821234567")]
    [InlineData("(082) 123 4567", "+27821234567")]
    [InlineData("+27821234567", "+27821234567")]
    public void NormaliseMobile_WithSupportedSouthAfricanFormat_ReturnsInternationalNumber(
        string input,
        string expected)
    {
        var result = SouthAfricanRegistrationValidator.NormaliseMobile(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0123456789")]
    [InlineData("+27123456789")]
    [InlineData("082123456")]
    [InlineData("not-a-number")]
    public void NormaliseMobile_WithInvalidNumber_ReturnsNull(string input)
    {
        var result = SouthAfricanRegistrationValidator.NormaliseMobile(input);

        Assert.Null(result);
    }

    [Fact]
    public void Validate_WithValidSouthAfricanRegistration_ReturnsNoErrors()
    {
        var request = CreateValidRequest();

        var errors = SouthAfricanRegistrationValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithInvalidRegistration_ReturnsExpectedFieldErrors()
    {
        var request = new RegisterRequestDto(
            FirstName: "T",
            LastName: string.Empty,
            Email: "invalid-email",
            MobileNumber: "123",
            Password: "weak",
            ConfirmPassword: "different",
            AddressLine1: string.Empty,
            Suburb: string.Empty,
            City: string.Empty,
            Province: "Pretoria",
            PostalCode: "123",
            DateOfBirth: DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
            Country: "ZA",
            TermsAccepted: false,
            PrivacyAccepted: false);

        var errors = SouthAfricanRegistrationValidator.Validate(request);

        var expectedKeys = new[]
        {
            "firstName",
            "lastName",
            "email",
            "mobileNumber",
            "password",
            "confirmPassword",
            "addressLine1",
            "suburb",
            "city",
            "province",
            "postalCode",
            "dateOfBirth",
            "country",
            "termsAccepted",
            "privacyAccepted",
        };

        Assert.All(expectedKeys, key => Assert.True(errors.ContainsKey(key), $"Expected validation error for '{key}'."));
    }

    [Fact]
    public void Validate_WithMismatchedPasswords_ReturnsConfirmPasswordErrorOnly()
    {
        var valid = CreateValidRequest();
        var request = valid with { ConfirmPassword = "Different123" };

        var errors = SouthAfricanRegistrationValidator.Validate(request);

        var error = Assert.Single(errors);
        Assert.Equal("confirmPassword", error.Key);
    }

    private static RegisterRequestDto CreateValidRequest() =>
        new(
            FirstName: "Thapelo",
            LastName: "Bapela",
            Email: "thapelo@example.com",
            MobileNumber: "082 123 4567",
            Password: "Secure123",
            ConfirmPassword: "Secure123",
            AddressLine1: "12 Church Street",
            Suburb: "Hatfield",
            City: "Pretoria",
            Province: "Gauteng",
            PostalCode: "0083",
            DateOfBirth: "1995-05-17",
            Country: "South Africa",
            TermsAccepted: true,
            PrivacyAccepted: true);
}
