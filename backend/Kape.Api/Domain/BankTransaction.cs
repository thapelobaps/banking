namespace Kape.Api.Domain;

public sealed class BankTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BankAccountId { get; set; }
    public Guid? RelatedBankAccountId { get; set; }
    public required string Name { get; set; }
    public required string StatementDescription { get; set; }
    public string? Beneficiary { get; set; }
    public decimal Amount { get; set; }
    public required string Direction { get; set; }
    public required string Category { get; set; }
    public required string Channel { get; set; }
    public string Status { get; set; } = "completed";
    public bool IsDemo { get; set; } = true;
    public DateTimeOffset TransactionDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public BankAccount? BankAccount { get; set; }
}
