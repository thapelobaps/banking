namespace Kape.Api.Services.Interfaces;

public sealed record StitchAuthorizationSession(
    string AuthorizationUrl,
    string State,
    string Nonce,
    string CodeVerifier,
    DateTimeOffset ExpiresAt);

public sealed record StitchAuthorizationRequest(
    Guid UserId,
    string State,
    string Nonce,
    string CodeVerifier,
    string RedirectUri,
    string? InstitutionId,
    DateTimeOffset ExpiresAt);

public sealed record StitchUserTokenSet(
    string Subject,
    string AccessToken,
    string RefreshToken,
    string? IdToken,
    string GrantedScopes,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset UpdatedAt);

public sealed record StitchConnectionSecret(
    string ExternalConnectionId,
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
        string expectedNonce,
        CancellationToken cancellationToken);

    Task<StitchUserTokenSet> RefreshUserTokenAsync(
        StitchUserTokenSet currentTokens,
        CancellationToken cancellationToken);

    Task<string> GetClientTokenAsync(
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken);
}

public interface IStitchAuthorizationRequestStore
{
    Task SaveAsync(StitchAuthorizationRequest request, CancellationToken cancellationToken);
    Task<StitchAuthorizationRequest?> TakeAsync(string state, CancellationToken cancellationToken);
}

public interface IStitchConnectionSecretStore
{
    Task SaveAsync(StitchConnectionSecret secret, CancellationToken cancellationToken);
    Task<StitchConnectionSecret?> GetAsync(string externalConnectionId, CancellationToken cancellationToken);
    Task DeleteAsync(string externalConnectionId, CancellationToken cancellationToken);
}

public interface IStitchFinancialDataClient
{
    Task<BankProviderSyncResult> SyncAsync(
        StitchUserTokenSet tokens,
        string? syncCursor,
        CancellationToken cancellationToken);
}
