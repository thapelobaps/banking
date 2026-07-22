using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kape.Api.Configuration;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Kape.Api.Services;

public sealed class StitchUnauthenticatedException(string message) : Exception(message);

public sealed class StitchOAuthClient : IStitchOAuthClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly StitchIntegrationOptions _options;
    private readonly SemaphoreSlim _clientTokenLock = new(1, 1);

    public StitchOAuthClient(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<StitchIntegrationOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value;
    }

    public StitchAuthorizationSession CreateAuthorizationSession(string redirectUri)
    {
        EnsureEnabled();
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedRedirect) ||
            !string.Equals(parsedRedirect.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(parsedRedirect.AbsoluteUri, _options.RedirectUri, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The Stitch callback URL must match the configured HTTPS redirect URI.");
        }

        var state = GenerateBase64Url(32);
        var nonce = GenerateBase64Url(32);
        var codeVerifier = GenerateBase64Url(64);
        var codeChallenge = Base64UrlEncode(
            SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)));

        var authorizationUrl = QueryHelpers.AddQueryString(
            _options.AuthorizationEndpoint,
            new Dictionary<string, string?>
            {
                ["client_id"] = _options.ClientId,
                ["scope"] = string.Join(' ', _options.UserScopes.Distinct(StringComparer.Ordinal)),
                ["response_type"] = "code",
                ["redirect_uri"] = redirectUri,
                ["nonce"] = nonce,
                ["state"] = state,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
            });

        return new StitchAuthorizationSession(
            authorizationUrl,
            state,
            nonce,
            codeVerifier,
            DateTimeOffset.UtcNow.AddMinutes(10));
    }

    public async Task<StitchUserTokenSet> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string redirectUri,
        string codeVerifier,
        string expectedNonce,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var response = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code"] = authorizationCode,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier,
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            throw new InvalidOperationException(
                "Stitch did not return a refresh token. Confirm that offline_access was granted.");
        }

        if (string.IsNullOrWhiteSpace(response.IdToken))
        {
            throw new InvalidOperationException("Stitch did not return the required OpenID identity token.");
        }

        var identity = ReadIdentity(response.IdToken, expectedNonce, requireNonce: true);
        return new StitchUserTokenSet(
            identity.Subject,
            response.AccessToken,
            response.RefreshToken,
            response.IdToken,
            response.Scope ?? string.Empty,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, response.ExpiresIn)),
            DateTimeOffset.UtcNow);
    }

    public async Task<StitchUserTokenSet> RefreshUserTokenAsync(
        StitchUserTokenSet currentTokens,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var response = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["refresh_token"] = currentTokens.RefreshToken,
            },
            cancellationToken);

        var subject = currentTokens.Subject;
        if (!string.IsNullOrWhiteSpace(response.IdToken))
        {
            var identity = ReadIdentity(response.IdToken, expectedNonce: null, requireNonce: false);
            if (!string.Equals(identity.Subject, currentTokens.Subject, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The refreshed Stitch token belongs to a different user.");
            }

            subject = identity.Subject;
        }

        if (string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            throw new InvalidOperationException("Stitch did not rotate the single-use refresh token.");
        }

        return new StitchUserTokenSet(
            subject,
            response.AccessToken,
            response.RefreshToken,
            response.IdToken ?? currentTokens.IdToken,
            response.Scope ?? currentTokens.GrantedScopes,
            DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, response.ExpiresIn)),
            DateTimeOffset.UtcNow);
    }

    public async Task<string> GetClientTokenAsync(
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var normalizedScopes = scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(scope => scope, StringComparer.Ordinal)
            .ToArray();
        if (normalizedScopes.Length == 0)
        {
            throw new ArgumentException("At least one Stitch client scope is required.", nameof(scopes));
        }

        var cacheKey = $"stitch:client-token:{string.Join('|', normalizedScopes)}";
        if (_cache.TryGetValue<CachedClientToken>(cacheKey, out var cached) &&
            cached is not null &&
            cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return cached.AccessToken;
        }

        await _clientTokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue<CachedClientToken>(cacheKey, out cached) &&
                cached is not null &&
                cached.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return cached.AccessToken;
            }

            var response = await RequestTokenAsync(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["scope"] = string.Join(' ', normalizedScopes),
                },
                cancellationToken);

            var safeLifetime = Math.Max(
                30,
                response.ExpiresIn - _options.ClientTokenRefreshSkewSeconds);
            var token = new CachedClientToken(
                response.AccessToken,
                DateTimeOffset.UtcNow.AddSeconds(safeLifetime));
            _cache.Set(
                cacheKey,
                token,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = token.ExpiresAt,
                    Size = 1,
                });

            return token.AccessToken;
        }
        finally
        {
            _clientTokenLock.Release();
        }
    }

    private async Task<StitchTokenResponse> RequestTokenAsync(
        IReadOnlyDictionary<string, string> formValues,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formValues),
        };
        using var response = await _httpClientFactory
            .CreateClient("Stitch")
            .SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new StitchUnauthenticatedException("Stitch rejected the supplied authentication token.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Stitch token request failed with HTTP {(int)response.StatusCode}: {SafeBody(body)}");
        }

        var token = JsonSerializer.Deserialize<StitchTokenResponse>(body, SerializerOptions)
            ?? throw new InvalidOperationException("Stitch returned an empty token response.");
        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("Stitch did not return an access token.");
        }

        return token;
    }

    private StitchIdentity ReadIdentity(string idToken, string? expectedNonce, bool requireNonce)
    {
        var parts = idToken.Split('.');
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("The Stitch identity token is malformed.");
        }

        using var document = JsonDocument.Parse(Base64UrlDecode(parts[1]));
        var root = document.RootElement;
        var subject = root.TryGetProperty("sub", out var subjectElement)
            ? subjectElement.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("The Stitch identity token does not contain a subject.");
        }

        var nonce = root.TryGetProperty("nonce", out var nonceElement)
            ? nonceElement.GetString()
            : null;
        if (requireNonce && !string.Equals(nonce, expectedNonce, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The Stitch identity nonce did not match the authorization request.");
        }

        if (!AudienceContains(root, _options.ClientId))
        {
            throw new InvalidOperationException("The Stitch identity token audience is invalid.");
        }

        if (root.TryGetProperty("iss", out var issuerElement) &&
            Uri.TryCreate(issuerElement.GetString(), UriKind.Absolute, out var issuer) &&
            Uri.TryCreate(_options.AuthorizationEndpoint, UriKind.Absolute, out var authorizationEndpoint) &&
            !string.Equals(issuer.Host, authorizationEndpoint.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The Stitch identity token issuer is invalid.");
        }

        return new StitchIdentity(subject, nonce);
    }

    private static bool AudienceContains(JsonElement root, string clientId)
    {
        if (!root.TryGetProperty("aud", out var audience))
        {
            return false;
        }

        return audience.ValueKind switch
        {
            JsonValueKind.String => string.Equals(audience.GetString(), clientId, StringComparison.Ordinal),
            JsonValueKind.Array => audience.EnumerateArray().Any(
                item => string.Equals(item.GetString(), clientId, StringComparison.Ordinal)),
            _ => false,
        };
    }

    private void EnsureEnabled()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Stitch is disabled in provider configuration.");
        }
    }

    private static string GenerateBase64Url(int byteLength) =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(byteLength));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
        return Convert.FromBase64String(normalized);
    }

    private static string SafeBody(string body) =>
        string.IsNullOrWhiteSpace(body)
            ? "No response body."
            : body.Length <= 400 ? body : body[..400];

    private sealed record CachedClientToken(string AccessToken, DateTimeOffset ExpiresAt);
    private sealed record StitchIdentity(string Subject, string? Nonce);

    private sealed class StitchTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }
    }
}
