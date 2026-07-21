using System.Linq.Expressions;
using Kape.Api.Data;
using Kape.Api.Domain;
using Kape.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kape.Api.Repositories;

public sealed class WalletPlatformRepository(KapeDbContext dbContext) : IWalletPlatformRepository
{
    public Task<T?> GetAsync<T>(
        Expression<Func<T, bool>> predicate,
        bool tracking = false,
        CancellationToken cancellationToken = default)
        where T : class
    {
        IQueryable<T> query = dbContext.Set<T>();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        IQueryable<T> query = dbContext.Set<T>().AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        if (skip is > 0)
        {
            query = query.Skip(skip.Value);
        }

        if (take is > 0)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where T : class =>
        predicate is null
            ? dbContext.Set<T>().CountAsync(cancellationToken)
            : dbContext.Set<T>().CountAsync(predicate, cancellationToken);

    public Task<bool> AnyAsync<T>(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : class =>
        dbContext.Set<T>().AnyAsync(predicate, cancellationToken);

    public void Add<T>(T entity) where T : class => dbContext.Set<T>().Add(entity);

    public void AddRange<T>(IEnumerable<T> entities) where T : class =>
        dbContext.Set<T>().AddRange(entities);

    public void Remove<T>(T entity) where T : class => dbContext.Set<T>().Remove(entity);

    public async Task<decimal> GetLedgerAccountBalanceAsync(
        Guid ledgerAccountId,
        CancellationToken cancellationToken = default) =>
        await dbContext.LedgerEntries
            .AsNoTracking()
            .Where(entry => entry.LedgerAccountId == ledgerAccountId)
            .Select(entry => (decimal?)(entry.EntryType == "credit" ? entry.Amount : -entry.Amount))
            .SumAsync(cancellationToken) ?? 0m;

    public async Task<decimal> GetWalletPostedTransactionBalanceAsync(
        Guid walletId,
        CancellationToken cancellationToken = default) =>
        await dbContext.WalletTransactions
            .AsNoTracking()
            .Where(transaction => transaction.WalletId == walletId && transaction.Status == "completed")
            .Select(transaction => (decimal?)(
                transaction.Type == "top_up" ||
                transaction.Type == "transfer_in" ||
                transaction.Type == "refund"
                    ? transaction.NetAmount
                    : -transaction.NetAmount))
            .SumAsync(cancellationToken) ?? 0m;

    public Task<ApplicationUser?> ResolveUserAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var value = identifier.Trim();
        if (Guid.TryParse(value, out var userId))
        {
            return dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        }

        var normalized = value.ToUpperInvariant();
        return dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.NormalizedEmail == normalized ||
                        user.NormalizedUserName == normalized ||
                        user.MobileNumber == value,
                cancellationToken);
    }

    public async Task<QueueMessage?> TryDequeueAsync(
        string queueName,
        string workerId,
        CancellationToken cancellationToken = default)
    {
        var messages = await dbContext.QueueMessages
            .FromSqlInterpolated($"EXEC dbo.sp_DequeueWalletMessage @QueueName={queueName}, @WorkerId={workerId}")
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return messages.FirstOrDefault();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
