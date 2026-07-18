namespace Kape.Api.Contracts;

public sealed record RegisterRequest(
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

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string MobileNumber,
    string Country);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthUserResponse User);
