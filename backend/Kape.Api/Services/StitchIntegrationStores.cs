using System.Security.Cryptography;
using System.Text.Json;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

namespace Kape.Api.Services;

public sealed class ProtectedMemoryStitchAuthorizationRequestStore : IStitchAuthorizationRequestStore
{
    private const string CachePrefix = "stitch:authorization:";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IMemoryCache _cache;
    private readonly IDataProtector _protector;

    public ProtectedMemoryStitchAuthorizationRequestStore(
        IMemoryCache cache,
        IDataProtectionProvider dataProtectionProvider)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector("Kape.Stitch.AuthorizationRequest.v1");
    }

    public Task SaveAsync(StitchAuthorizationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(request, SerializerOptions);
        var protectedPayload = _protector.Protect(payload);
        _cache.Set(
            CachePrefix + request.State,
            protectedPayload,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = request.ExpiresAt,
                Size = 1,
            });

        return Task.CompletedTask;
    }

    public Task<StitchAuthorizationRequest?> TakeAsync(string state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(state))
        {
            return Task.FromResult<StitchAuthorizationRequest?>(null);
        }

        var key = CachePrefix + state.Trim();
        if (!_cache.TryGetValue<string>(key, out var protectedPayload) || string.IsNullOrWhiteSpace(protectedPayload))
        {
            return Task.FromResult<StitchAuthorizationRequest?>(null);
        }

        _cache.Remove(key);

        try
        {
            var payload = _protector.Unprotect(protectedPayload);
            var request = JsonSerializer.Deserialize<StitchAuthorizationRequest>(payload, SerializerOptions);
            if (request is null || request.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Task.FromResult<StitchAuthorizationRequest?>(null);
            }

            return Task.FromResult<StitchAuthorizationRequest?>(request);
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            return Task.FromResult<StitchAuthorizationRequest?>(null);
        }
    }
}

public sealed class ProtectedMemoryStitchConnectionSecretStore : IStitchConnectionSecretStore
{
    private const string CachePrefix = "stitch:connection-secret:";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IMemoryCache _cache;
    private readonly IDataProtector _protector;

    public ProtectedMemoryStitchConnectionSecretStore(
        IMemoryCache cache,
        IDataProtectionProvider dataProtectionProvider)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector("Kape.Stitch.ConnectionSecret.v1");
    }

    public Task SaveAsync(StitchConnectionSecret secret, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(secret, SerializerOptions);
        var protectedPayload = _protector.Protect(payload);
        _cache.Set(
            CachePrefix + secret.ExternalConnectionId,
            protectedPayload,
            new MemoryCacheEntryOptions
            {
                Size = 1,
                Priority = CacheItemPriority.High,
            });

        return Task.CompletedTask;
    }

    public Task<StitchConnectionSecret?> GetAsync(
        string externalConnectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(externalConnectionId) ||
            !_cache.TryGetValue<string>(CachePrefix + externalConnectionId.Trim(), out var protectedPayload) ||
            string.IsNullOrWhiteSpace(protectedPayload))
        {
            return Task.FromResult<StitchConnectionSecret?>(null);
        }

        try
        {
            var payload = _protector.Unprotect(protectedPayload);
            return Task.FromResult(
                JsonSerializer.Deserialize<StitchConnectionSecret>(payload, SerializerOptions));
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            return Task.FromResult<StitchConnectionSecret?>(null);
        }
    }

    public Task DeleteAsync(string externalConnectionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(externalConnectionId))
        {
            _cache.Remove(CachePrefix + externalConnectionId.Trim());
        }

        return Task.CompletedTask;
    }
}
