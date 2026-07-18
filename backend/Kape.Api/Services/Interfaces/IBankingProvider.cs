using Kape.Api.Domain;

namespace Kape.Api.Services.Interfaces;

public interface IBankingProvider
{
    string ProviderId { get; }
    BankAccount CreateDefaultDemoAccount(Guid userId, string email);
    IReadOnlyCollection<BankTransaction> CreateStarterTransactions(Guid bankAccountId);
}
