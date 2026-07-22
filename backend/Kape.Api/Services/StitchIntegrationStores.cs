using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kape.Api.Configuration;
using Kape.Api.Data;
using Kape.Api.Domain;
using Kape.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kape.Api.Services;

public sealed class AesGcmStitchSecretProtector : IStitchSecretProtector
{
    private const byte EnvelopeVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes("Kape.Stitch.Secret.v1");

    private readonly byte[] _key;

    public AesGcmStitchSecretProtector(IOptions<StitchIntegrationOptions> options)
    {
        var configured = options.Value;
        if (!configured.HasValidStorageEncryptionKey)
        {
            throw new InvalidOperationException(
                "Providers:Stitch:StorageEncryptionKey must be a base64-encoded 256-bit key.");
        }

        _key = Convert.FromBase64String(configured.StorageEncryptionKey.Trim());
    }

    public string Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, AssociatedData);

        var envelope = new byte[1 + NonceSize + TagSize + ciphertext.Length];
        envelope[0] = EnvelopeVersion;
        nonce.CopyTo(envelope.AsSpan(1, NonceSize));
        tag.CopyTo(envelope.AsSpan(1 + NonceSize, TagSize));
        ciphertext.CopyTo(envelope.AsSpan(1 + NonceSize + TagSize));
        CryptographicOperations.ZeroMemory(plaintextBytes);

        return Convert.ToBase64String(envelope);
    }

    public string Unprotect(string protectedPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedPayload);

        byte[] envelope;
        try
        {
            envelope = Convert.FromBase64String(protectedPayload.Trim());
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("The Stitch secret envelope is invalid.", exception);
        }

        if (envelope.Length < 1 + NonceSize + TagSize || envelope[0] != EnvelopeVersion)
        {
            throw new CryptographicException("The Stitch secret envelope version is invalid.");
        }

        var nonce = envelope.AsSpan(1, NonceSize);
        var tag = envelope.AsSpan(1 + NonceSize, TagSize);
        var ciphertext = envelope.AsSpan(1 + NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData);

        try
        {
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(envelope);
        }
    }
}

public sealed class SqlStitchAuthorizationRequestStore(
    KapeDbContext dbContext,
    IStitchSecretProtector protector) : IStitchAuthorizationRequestStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(StitchAuthorizationRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.State);
        var now = DateTimeOffset.UtcNow;

        await dbContext.StitchAuthorizationRequests
            .Where(record => record.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var state = request.State.Trim();
        var record = await dbContext.StitchAuthorizationRequests
            .SingleOrDefaultAsync(item => item.State == state, cancellationToken);
        var protectedPayload = protector.Protect(JsonSerializer.Serialize(request, SerializerOptions));

        if (record is null)
        {
            dbContext.StitchAuthorizationRequests.Add(new StitchAuthorizationRequestRecord
            {
                State = state,
                UserId = request.UserId,
                ProtectedPayload = protectedPayload,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = now,
            });
        }
        else
        {
            record.UserId = request.UserId;
            record.ProtectedPayload = protectedPayload;
            record.ExpiresAt = request.ExpiresAt;
            record.CreatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StitchAuthorizationRequest?> TakeAsync(
        string state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        var normalizedState = state.Trim();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var record = await dbContext.StitchAuthorizationRequests
            .SingleOrDefaultAsync(item => item.State == normalizedState, cancellationToken);
        if (record is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        dbContext.StitchAuthorizationRequests.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        try
        {
            var payload = protector.Unprotect(record.ProtectedPayload);
            var request = JsonSerializer.Deserialize<StitchAuthorizationRequest>(payload, SerializerOptions);
            return request is not null &&
                   string.Equals(request.State, normalizedState, StringComparison.Ordinal)
                ? request
                : null;
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            return null;
        }
    }
}

public sealed class SqlStitchConnectionSecretStore(
    KapeDbContext dbContext,
    IStitchSecretProtector protector) : IStitchConnectionSecretStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(StitchConnectionSecret secret, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret.ExternalConnectionId);
        var externalConnectionId = secret.ExternalConnectionId.Trim();
        var now = DateTimeOffset.UtcNow;
        var record = await dbContext.StitchConnectionSecrets
            .SingleOrDefaultAsync(
                item => item.ExternalConnectionId == externalConnectionId,
                cancellationToken);
        var protectedPayload = protector.Protect(JsonSerializer.Serialize(secret, SerializerOptions));

        if (record is null)
        {
            dbContext.StitchConnectionSecrets.Add(new StitchConnectionSecretRecord
            {
                ExternalConnectionId = externalConnectionId,
                ProtectedPayload = protectedPayload,
                AccessTokenExpiresAt = secret.Tokens.AccessTokenExpiresAt,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            record.ProtectedPayload = protectedPayload;
            record.AccessTokenExpiresAt = secret.Tokens.AccessTokenExpiresAt;
            record.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StitchConnectionSecret?> GetAsync(
        string externalConnectionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalConnectionId))
        {
            return null;
        }

        var normalizedId = externalConnectionId.Trim();
        var record = await dbContext.StitchConnectionSecrets
            .SingleOrDefaultAsync(
                item => item.ExternalConnectionId == normalizedId,
                cancellationToken);
        if (record is null)
        {
            return null;
        }

        try
        {
            var payload = protector.Unprotect(record.ProtectedPayload);
            var secret = JsonSerializer.Deserialize<StitchConnectionSecret>(payload, SerializerOptions);
            return secret is not null &&
                   string.Equals(secret.ExternalConnectionId, normalizedId, StringComparison.Ordinal)
                ? secret
                : null;
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            dbContext.StitchConnectionSecrets.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
    }

    public async Task DeleteAsync(
        string externalConnectionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalConnectionId))
        {
            return;
        }

        var normalizedId = externalConnectionId.Trim();
        await dbContext.StitchConnectionSecrets
            .Where(item => item.ExternalConnectionId == normalizedId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
