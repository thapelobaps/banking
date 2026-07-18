using Kape.Api.Domain;

namespace Kape.Api.Services;

public interface IDemoBankingService
{
    BankAccount CreateDefaultAccount(Guid userId, string email);
    IReadOnlyCollection<BankTransaction> CreateStarterTransactions(Guid bankAccountId);
}

public sealed class DemoBankingService : IDemoBankingService
{
    private static readonly DemoBank[] Banks =
    [
        new("capitec", "Capitec", "470010"),
        new("fnb", "FNB", "250655"),
        new("absa", "Absa", "632005"),
        new("standard-bank", "Standard Bank", "051001"),
        new("nedbank", "Nedbank", "198765"),
        new("tymebank", "TymeBank", "678910"),
        new("discovery-bank", "Discovery Bank", "679000"),
    ];

    public BankAccount CreateDefaultAccount(Guid userId, string email)
    {
        var hash = StableHash(email);
        var bank = Banks[hash % Banks.Length];
        var suffix = (hash % 1_000_000).ToString("D6");
        var accountId = Guid.NewGuid();

        return new BankAccount
        {
            Id = accountId,
            UserId = userId,
            ProviderId = "south-african-demo",
            BankId = bank.Id,
            BankName = bank.Name,
            AccountNumber = $"DEMO-{bank.Id.ToUpperInvariant()}-{suffix}",
            BranchCode = bank.BranchCode,
            AccountType = hash % 4 == 0 ? "savings" : "transaction",
            CurrentBalance = 12_500m + (hash % 7_500),
            AvailableBalance = 11_900m + (hash % 7_000),
            Currency = "ZAR",
            IsDemo = true,
        };
    }

    public IReadOnlyCollection<BankTransaction> CreateStarterTransactions(Guid bankAccountId)
    {
        return
        [
            new BankTransaction
            {
                BankAccountId = bankAccountId,
                Name = "Salary payment",
                StatementDescription = "DEMO SALARY PAYMENT",
                Amount = 24_500m,
                Direction = "credit",
                Category = "Income",
                Channel = "EFT",
                TransactionDate = DateTimeOffset.UtcNow.AddDays(-5),
            },
            new BankTransaction
            {
                BankAccountId = bankAccountId,
                Name = "Pick n Pay groceries",
                StatementDescription = "DEMO PICK N PAY",
                Amount = 786.45m,
                Direction = "debit",
                Category = "Groceries",
                Channel = "card",
                TransactionDate = DateTimeOffset.UtcNow.AddDays(-3),
            },
            new BankTransaction
            {
                BankAccountId = bankAccountId,
                Name = "Vodacom debit order",
                StatementDescription = "DEMO VODACOM DEBIT ORDER",
                Beneficiary = "Vodacom",
                Amount = 599m,
                Direction = "debit",
                Category = "Communication",
                Channel = "debit order",
                TransactionDate = DateTimeOffset.UtcNow.AddDays(-2),
            },
            new BankTransaction
            {
                BankAccountId = bankAccountId,
                Name = "Emergency fund transfer",
                StatementDescription = "DEMO SAVINGS TRANSFER",
                Beneficiary = "Emergency Fund",
                Amount = 1_250m,
                Direction = "debit",
                Category = "Savings",
                Channel = "internal transfer",
                TransactionDate = DateTimeOffset.UtcNow.AddDays(-1),
            },
        ];
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var character in value.Trim().ToLowerInvariant())
            {
                hash = (hash * 31) + character;
            }

            return Math.Abs(hash == int.MinValue ? 0 : hash);
        }
    }

    private sealed record DemoBank(string Id, string Name, string BranchCode);
}
