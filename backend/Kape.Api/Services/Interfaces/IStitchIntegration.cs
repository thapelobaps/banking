using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services.Interfaces;

public sealed record StitchAuthorizationSession(
    string AuthorizationUrl,
    string State,
    string Nonce,
    string CodeVerifier,
    DateTimeOffset ExpiresAt);

public sealed record StitchUserTokenSet(
    string AccessToken,
    string RefreshToken,
    string? IdToken,
    string GrantedScopes,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset UpdatedAt);

public sealed record StitchConnectionSecret(
    Guid BankConnectionId,
    string StitchUserId,
    StitchUserTokenSet Tokens,
    string? SyncCursor);

public interface IStitchOAuthClient
{
    StitchAuthorizationSession CreateAuthorizationSession(string redirectUri);

    Task<StitchUserTokenSet> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken);

    Task<StitchUserTokenSet> RefreshUserTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<string> GetClientTokenAsync(
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken);
}

public interface IStitchConnectionSecretStore
{
    Task SaveAsync(StitchConnectionSecret secret, CancellationToken cancellationToken);
    Task<StitchConnectionSecret?> GetAsync(Guid bankConnectionId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid bankConnectionId, CancellationToken cancellationToken);
}

public interface IStitchFinancialDataClient
{
    Task<BankProviderSyncResult> SyncAsync(
        StitchUserTokenSet tokens,
        string? syncCursor,
        CancellationToken cancellationToken);
}
