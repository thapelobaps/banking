using System.Text.RegularExpressions;
using Kape.Api.Contracts;

namespace Kape.Api.Validation;

public static partial class SouthAfricanRegistrationValidator
{
    private static readonly HashSet<string> Provinces =
    [
        "Eastern Cape",
        "Free State",
        "Gauteng",
        "KwaZulu-Natal",
        "Limpopo",
        "Mpumalanga",
        "North West",
        "Northern Cape",
        "Western Cape",
    ];

    public static Dictionary<string, string[]> Validate(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Trim().Length < 2)
            errors["firstName"] = ["First name must contain at least two characters."];

        if (string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Trim().Length < 2)
            errors["lastName"] = ["Surname must contain at least two characters."];

        if (!EmailRegex().IsMatch(request.Email.Trim()))
            errors["email"] = ["Enter a valid email address."];

        if (NormaliseMobile(request.MobileNumber) is null)
            errors["mobileNumber"] = ["Enter a valid South African mobile number."];

        if (request.Password.Length < 8 || !request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
            errors["password"] = ["Password must be at least eight characters and contain a letter and number."];

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            errors["confirmPassword"] = ["Passwords do not match."];

        if (string.IsNullOrWhiteSpace(request.AddressLine1))
            errors["addressLine1"] = ["Address line 1 is required."];

        if (string.IsNullOrWhiteSpace(request.Suburb))
            errors["suburb"] = ["Suburb is required."];

        if (string.IsNullOrWhiteSpace(request.City))
            errors["city"] = ["City or town is required."];

        if (!Provinces.Contains(request.Province))
            errors["province"] = ["Select a valid South African province."];

        if (!PostalCodeRegex().IsMatch(request.PostalCode.Trim()))
            errors["postalCode"] = ["Postal code must contain exactly four digits."];

        if (!DateOnly.TryParseExact(request.DateOfBirth, "yyyy-MM-dd", out var dateOfBirth) || dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            errors["dateOfBirth"] = ["Enter a valid date of birth."];

        if (!string.Equals(request.Country, "South Africa", StringComparison.Ordinal))
            errors["country"] = ["Country must be South Africa."];

        if (!request.TermsAccepted)
            errors["termsAccepted"] = ["You must accept the terms of use."];

        if (!request.PrivacyAccepted)
            errors["privacyAccepted"] = ["You must acknowledge the privacy notice."];

        return errors;
    }

    public static string? NormaliseMobile(string value)
    {
        var compact = Regex.Replace(value.Trim(), "[\\s()-]", string.Empty);

        if (LocalMobileRegex().IsMatch(compact))
            return $"+27{compact[1..]}";

        return InternationalMobileRegex().IsMatch(compact) ? compact : null;
    }

    [GeneratedRegex("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex("^\\d{4}$")]
    private static partial Regex PostalCodeRegex();

    [GeneratedRegex("^0[6-8]\\d{8}$")]
    private static partial Regex LocalMobileRegex();

    [GeneratedRegex("^\\+27[6-8]\\d{8}$")]
    private static partial Regex InternationalMobileRegex();
}
