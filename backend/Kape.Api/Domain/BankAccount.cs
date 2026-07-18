namespace Kape.Api.Domain;

public sealed class BankAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string ProviderId { get; set; }
    public required string BankId { get; set; }
    public required string BankName { get; set; }
    public required string AccountNumber { get; set; }
    public required string BranchCode { get; set; }
    public required string AccountType { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = "ZAR";
    public bool IsDemo { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser? User { get; set; }
    public ICollection<BankTransaction> Transactions { get; set; } = [];
}
