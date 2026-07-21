using System.Linq.Expressions;
using Kape.Api.Domain;

namespace Kape.Api.Repositories.Interfaces;

public interface IWalletPlatformRepository
{
    Task<T?> GetAsync<T>(
        Expression<Func<T, bool>> predicate,
        bool tracking = false,
        CancellationToken cancellationToken = default)
        where T : class;

    Task<IReadOnlyList<T>> ListAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task<int> CountAsync<T>(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task<bool> AnyAsync<T>(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : class;

    void Add<T>(T entity) where T : class;
    void AddRange<T>(IEnumerable<T> entities) where T : class;
    void Remove<T>(T entity) where T : class;

    Task<decimal> GetLedgerAccountBalanceAsync(
        Guid ledgerAccountId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetWalletPostedTransactionBalanceAsync(
        Guid walletId,
        CancellationToken cancellationToken = default);

    Task<ApplicationUser?> ResolveUserAsync(
        string identifier,
        CancellationToken cancellationToken = default);

    Task<QueueMessage?> TryDequeueAsync(
        string queueName,
        string workerId,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
