using Kape.Api.Domain;

namespace Kape.Api.Services.Interfaces;

public interface IBankingProvider
{
    string ProviderId { get; }
    BankAccount CreateDefaultDemoAccount(Guid userId, string email);
    BankAccount CreateSecondaryDemoAccount(Guid userId, string email);
    BankAccount CreateCompanionDemoAccount(Guid userId, string email, string accountType);
    IReadOnlyCollection<BankTransaction> CreateStarterTransactions(Guid bankAccountId);
}
