using System.Security.Cryptography;
using System.Text;
using Kape.Api.Configuration;
using Kape.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Kape.Api.Services;

public sealed class StitchBankAggregationProvider : IBankAggregationProvider
{
    private readonly IStitchOAuthClient _oauthClient;
    private readonly IStitchAuthorizationRequestStore _authorizationStore;
    private readonly IStitchConnectionSecretStore _secretStore;
    private readonly IStitchFinancialDataClient _financialDataClient;
    private readonly StitchIntegrationOptions _options;

    public StitchBankAggregationProvider(
        IStitchOAuthClient oauthClient,
        IStitchAuthorizationRequestStore authorizationStore,
        IStitchConnectionSecretStore secretStore,
        IStitchFinancialDataClient financialDataClient,
        IOptions<StitchIntegrationOptions> options)
    {
        _oauthClient = oauthClient;
        _authorizationStore = authorizationStore;
        _secretStore = secretStore;
        _financialDataClient = financialDataClient;
        _options = options.Value;
    }

    public string ProviderId => "stitch";

    public async Task<BankProviderLinkSession> CreateLinkSessionAsync(
        Guid userId,
        string? institutionId,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            !string.Equals(returnUrl.Trim(), _options.RedirectUri, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The requested callback does not match the Stitch redirect URI configured for Kape.");
        }

        var session = _oauthClient.CreateAuthorizationSession(_options.RedirectUri);
        await _authorizationStore.SaveAsync(
            new StitchAuthorizationRequest(
                userId,
                session.State,
                session.Nonce,
                session.CodeVerifier,
                _options.RedirectUri,
                institutionId?.Trim().ToLowerInvariant(),
                session.ExpiresAt),
            cancellationToken);

        return new BankProviderLinkSession(
            session.State,
            session.AuthorizationUrl,
            session.ExpiresAt);
    }

    public async Task<BankProviderConnectionResult> CompleteLinkAsync(
        Guid userId,
        string authorizationCode,
        string state,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var authorization = await _authorizationStore.TakeAsync(state, cancellationToken)
            ?? throw new InvalidOperationException(
                "The Stitch authorization request expired or has already been used. Start bank linking again.");
        if (authorization.UserId != userId)
        {
            throw new InvalidOperationException("The Stitch authorization request belongs to another user.");
        }

        var tokens = await _oauthClient.ExchangeAuthorizationCodeAsync(
            authorizationCode,
            authorization.RedirectUri,
            authorization.CodeVerifier,
            authorization.Nonce,
            cancellationToken);
        var externalConnectionId = BuildExternalConnectionId(tokens.Subject, authorization.State);

        await _secretStore.SaveAsync(
            new StitchConnectionSecret(
                externalConnectionId,
                tokens.Subject,
                tokens,
                SyncCursor: null),
            cancellationToken);

        var institutionId = string.IsNullOrWhiteSpace(authorization.InstitutionId)
            ? "stitch"
            : authorization.InstitutionId;
        return new BankProviderConnectionResult(
            externalConnectionId,
            institutionId,
            InstitutionName(institutionId),
            DateTimeOffset.UtcNow.AddDays(365));
    }

    public async Task<BankProviderSyncResult> SyncAsync(
        string externalConnectionId,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var secret = await _secretStore.GetAsync(externalConnectionId, cancellationToken)
            ?? throw new InvalidOperationException(
                "The Stitch connection secret is unavailable. Reconnect the bank before synchronising.");

        secret = await RefreshWhenNeededAsync(secret, force: false, cancellationToken);
        try
        {
            return await _financialDataClient.SyncAsync(
                secret.Tokens,
                secret.SyncCursor,
                cancellationToken);
        }
        catch (StitchUnauthenticatedException)
        {
            secret = await RefreshWhenNeededAsync(secret, force: true, cancellationToken);
            return await _financialDataClient.SyncAsync(
                secret.Tokens,
                secret.SyncCursor,
                cancellationToken);
        }
    }

    public Task DisconnectAsync(
        string externalConnectionId,
        CancellationToken cancellationToken) =>
        _secretStore.DeleteAsync(externalConnectionId, cancellationToken);

    private async Task<StitchConnectionSecret> RefreshWhenNeededAsync(
        StitchConnectionSecret secret,
        bool force,
        CancellationToken cancellationToken)
    {
        var refreshAt = secret.Tokens.AccessTokenExpiresAt
            .AddSeconds(-_options.ClientTokenRefreshSkewSeconds);
        if (!force && refreshAt > DateTimeOffset.UtcNow)
        {
            return secret;
        }

        var refreshedTokens = await _oauthClient.RefreshUserTokenAsync(
            secret.Tokens,
            cancellationToken);
        var refreshedSecret = secret with { Tokens = refreshedTokens };
        await _secretStore.SaveAsync(refreshedSecret, cancellationToken);
        return refreshedSecret;
    }

    private void EnsureEnabled()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Stitch is disabled in provider configuration.");
        }
    }

    private static string BuildExternalConnectionId(string subject, string state)
    {
        var subjectHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(subject)))[..24].ToLowerInvariant();
        var stateHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(state)))[..16].ToLowerInvariant();
        return $"stitch:{subjectHash}:{stateHash}";
    }

    private static string InstitutionName(string institutionId) =>
        institutionId.Trim().ToLowerInvariant() switch
        {
            "absa" => "Absa",
            "capitec" => "Capitec",
            "discovery" or "discovery-bank" => "Discovery Bank",
            "fnb" => "FNB",
            "investec" => "Investec",
            "nedbank" => "Nedbank",
            "standard-bank" or "standard_bank" or "standardbank" => "Standard Bank",
            "tymebank" or "tyme-bank" => "TymeBank",
            _ => "Stitch linked banks",
        };
}
