using System.Collections.Concurrent;
using System.Text.Json;
using Kape.Api.Domain;
using Kape.Api.Repositories.Interfaces;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

namespace Kape.Api.Services;

public sealed class MemoryWalletCache(IMemoryCache cache) : IWalletCache
{
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<T>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue<T>(key, out cached) && cached is not null)
            {
                return cached;
            }

            var value = await factory();
            cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
                Size = 1,
            });
            _keys[key] = 0;
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Remove(string key)
    {
        cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        foreach (var key in _keys.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
        {
            Remove(key);
        }
    }
}

public sealed class SqlWalletQueue(IWalletPlatformRepository repository) : IWalletQueue
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<Guid> EnqueueAsync<T>(
        string queueName,
        string messageType,
        T payload,
        DateTimeOffset? availableAt = null,
        CancellationToken cancellationToken = default)
    {
        var message = new QueueMessage
        {
            QueueName = queueName,
            MessageType = messageType,
            Payload = JsonSerializer.Serialize(payload, SerializerOptions),
            AvailableAt = availableAt ?? DateTimeOffset.UtcNow,
        };

        repository.Add(message);
        await repository.SaveChangesAsync(cancellationToken);
        return message.Id;
    }

    public Task<QueueMessage?> DequeueAsync(
        string queueName,
        string workerId,
        CancellationToken cancellationToken = default) =>
        repository.TryDequeueAsync(queueName, workerId, cancellationToken);

    public async Task CompleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await repository.GetAsync<QueueMessage>(
            item => item.Id == messageId,
            tracking: true,
            cancellationToken);
        if (message is null)
        {
            return;
        }

        message.Status = "completed";
        message.ProcessedAt = DateTimeOffset.UtcNow;
        message.LockedAt = null;
        message.LockedBy = null;
        message.LastError = null;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task FailAsync(
        Guid messageId,
        string error,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        var message = await repository.GetAsync<QueueMessage>(
            item => item.Id == messageId,
            tracking: true,
            cancellationToken);
        if (message is null)
        {
            return;
        }

        message.LastError = error.Length <= 2000 ? error : error[..2000];
        message.LockedAt = null;
        message.LockedBy = null;

        if (message.Attempts >= 5)
        {
            message.Status = "failed";
            message.ProcessedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            message.Status = "pending";
            message.AvailableAt = DateTimeOffset.UtcNow.Add(retryDelay);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DataProtectionVoucherCodeProtector : IVoucherCodeProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionVoucherCodeProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Kape.WalletPlatform.VoucherCodes.v1");
    }

    public string Protect(string value) => _protector.Protect(value);

    public string Unprotect(string value) => _protector.Unprotect(value);
}

public sealed class WalletQueueWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<WalletQueueWorker> logger) : BackgroundService
{
    private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IWalletQueue>();
                var service = scope.ServiceProvider.GetRequiredService<IWalletPlatformService>();
                var message = await queue.DequeueAsync("wallet-platform", _workerId, stoppingToken);

                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                try
                {
                    await service.ProcessQueueMessageAsync(message, stoppingToken);
                    await queue.CompleteAsync(message.Id, stoppingToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Wallet platform queue message {MessageId} failed on attempt {Attempt}",
                        message.Id,
                        message.Attempts);
                    var delay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, message.Attempts)));
                    await queue.FailAsync(message.Id, exception.Message, delay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Wallet platform queue worker loop failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
