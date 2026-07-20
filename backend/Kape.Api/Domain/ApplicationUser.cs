using Microsoft.AspNetCore.Identity;

namespace Kape.Api.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string MobileNumber { get; set; }
    public required string AddressLine1 { get; set; }
    public required string Suburb { get; set; }
    public required string City { get; set; }
    public required string Province { get; set; }
    public required string PostalCode { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string Country { get; set; } = "South Africa";
    public DateTimeOffset TermsAcceptedAt { get; set; }
    public DateTimeOffset PrivacyAcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<BankAccount> BankAccounts { get; set; } = [];
}
