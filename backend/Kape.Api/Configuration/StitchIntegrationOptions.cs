namespace Kape.Api.Configuration;

public sealed class BankAggregationProviderOptions
{
    public const string SectionName = "Providers:BankAggregation";

    public string ActiveProvider { get; set; } = "demo-bank-aggregator";
}

public sealed class StitchIntegrationOptions
{
    public const string SectionName = "Providers:Stitch";

    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = "https://secure.stitch.money/connect/authorize";
    public string TokenEndpoint { get; set; } = "https://secure.stitch.money/connect/token";
    public string GraphQlEndpoint { get; set; } = "https://api.stitch.money/graphql";
    public string RestApiBaseUrl { get; set; } = "https://api.stitch.money/v2";
    public string[] UserScopes { get; set; } =
    [
        "openid",
        "offline_access",
        "accounts",
        "balances",
        "transactions",
        "accountholders",
    ];
    public string[] ClientScopes { get; set; } = ["client_paymentrequest"];
    public int ClientTokenRefreshSkewSeconds { get; set; } = 120;

    public bool HasRequiredCredentials =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        Uri.TryCreate(RedirectUri, UriKind.Absolute, out _);

    public bool HasRequiredUserScopes =>
        UserScopes.Contains("openid", StringComparer.Ordinal) &&
        UserScopes.Contains("accounts", StringComparer.Ordinal) &&
        UserScopes.Contains("balances", StringComparer.Ordinal) &&
        UserScopes.Contains("transactions", StringComparer.Ordinal) &&
        UserScopes.Contains("offline_access", StringComparer.Ordinal);
}
