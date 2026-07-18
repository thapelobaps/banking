using Kape.Api.Domain;

namespace Kape.Api.Repositories.Interfaces;

public interface IBankAccountRepository
{
    Task<IReadOnlyList<BankAccount>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<BankAccount?> GetOwnedDemoAccountAsync(Guid accountId, Guid userId, CancellationToken cancellationToken);
    Task<BankAccount?> GetDemoAccountAsync(Guid accountId, CancellationToken cancellationToken);
    Task<bool> ExistsForUserAsync(Guid accountId, Guid userId, CancellationToken cancellationToken);
    void Add(BankAccount account);
}
