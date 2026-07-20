using Kape.Api.Data;
using Kape.Api.Domain;
using Kape.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kape.Api.Repositories;

public sealed class TransactionRepository(KapeDbContext dbContext) : ITransactionRepository
{
    public async Task<IReadOnlyList<BankTransaction>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.BankTransactions
            .AsNoTracking()
            .Where(transaction => transaction.BankAccountId == accountId)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ToListAsync(cancellationToken);

    public void Add(BankTransaction transaction) =>
        dbContext.BankTransactions.Add(transaction);

    public void AddRange(IEnumerable<BankTransaction> transactions) =>
        dbContext.BankTransactions.AddRange(transactions);
}
