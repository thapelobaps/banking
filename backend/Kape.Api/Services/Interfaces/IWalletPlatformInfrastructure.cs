using Kape.Api.Domain;

namespace Kape.Api.Services.Interfaces;

public interface IWalletCache
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    void Remove(string key);
    void RemoveByPrefix(string prefix);
}

public interface IWalletQueue
{
    Task<Guid> EnqueueAsync<T>(
        string queueName,
        string messageType,
        T payload,
        DateTimeOffset? availableAt = null,
        CancellationToken cancellationToken = default);

    Task<QueueMessage?> DequeueAsync(
        string queueName,
        string workerId,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task FailAsync(
        Guid messageId,
        string error,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default);
}

public interface IVoucherCodeProtector
{
    string Protect(string value);
    string Unprotect(string value);
}
