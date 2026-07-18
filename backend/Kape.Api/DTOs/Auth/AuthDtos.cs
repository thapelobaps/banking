namespace Kape.Api.DTOs.Auth;

public sealed record RegisterRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string MobileNumber,
    string Password,
    string ConfirmPassword,
    string AddressLine1,
    string Suburb,
    string City,
    string Province,
    string PostalCode,
    string DateOfBirth,
    string Country,
    bool TermsAccepted,
    bool PrivacyAccepted);

public sealed record LoginRequestDto(string Email, string Password);

public sealed record UserResponseDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string MobileNumber,
    string Country);

public sealed record AuthResponseDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserResponseDto User);
