using Kape.Api.Data;
using Kape.Api.Domain;
using Kape.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kape.Api.Repositories;

public sealed class BankAccountRepository(KapeDbContext dbContext) : IBankAccountRepository
{
    public async Task<IReadOnlyList<BankAccount>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.BankAccounts
            .AsNoTracking()
            .Where(account => account.UserId == userId)
            .OrderBy(account => account.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<BankAccount?> GetOwnedDemoAccountAsync(
        Guid accountId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.BankAccounts.SingleOrDefaultAsync(
            account => account.Id == accountId &&
                       account.UserId == userId &&
                       account.IsDemo,
            cancellationToken);

    public Task<BankAccount?> GetDemoAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        dbContext.BankAccounts.SingleOrDefaultAsync(
            account => account.Id == accountId && account.IsDemo,
            cancellationToken);

    public Task<bool> ExistsForUserAsync(
        Guid accountId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.BankAccounts.AnyAsync(
            account => account.Id == accountId && account.UserId == userId,
            cancellationToken);

    public void Add(BankAccount account) => dbContext.BankAccounts.Add(account);
}
