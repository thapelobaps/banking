using Kape.Api.Domain;

namespace Kape.Api.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<BankTransaction>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    void Add(BankTransaction transaction);
    void AddRange(IEnumerable<BankTransaction> transactions);
}
