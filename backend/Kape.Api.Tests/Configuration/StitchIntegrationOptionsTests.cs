using Kape.Api.Configuration;
using Xunit;

namespace Kape.Api.Tests.Configuration;

public sealed class StitchIntegrationOptionsTests
{
    [Fact]
    public void Defaults_AreSafeAndTargetOfficialEndpoints()
    {
        var options = new StitchIntegrationOptions();

        Assert.False(options.Enabled);
        Assert.False(options.HasRequiredCredentials);
        Assert.True(options.HasRequiredUserScopes);
        Assert.Equal("https://secure.stitch.money/connect/authorize", options.AuthorizationEndpoint);
        Assert.Equal("https://secure.stitch.money/connect/token", options.TokenEndpoint);
        Assert.Equal("https://api.stitch.money/graphql", options.GraphQlEndpoint);
        Assert.Contains("offline_access", options.UserScopes);
        Assert.Contains("client_paymentrequest", options.ClientScopes);
    }

    [Fact]
    public void Credentials_AreReadyOnlyWhenClientAndAbsoluteRedirectArePresent()
    {
        var options = new StitchIntegrationOptions
        {
            Enabled = true,
            ClientId = "test-client",
            ClientSecret = "test-secret",
            RedirectUri = "https://localhost:3000/return",
        };

        Assert.True(options.HasRequiredCredentials);

        options.RedirectUri = "/relative/callback";

        Assert.False(options.HasRequiredCredentials);
    }

    [Fact]
    public void UserScopes_RequireRefreshAndFinancialDataPermissions()
    {
        var options = new StitchIntegrationOptions
        {
            UserScopes = ["openid", "accounts", "balances", "transactions"],
        };

        Assert.False(options.HasRequiredUserScopes);

        options.UserScopes = ["openid", "offline_access", "accounts", "balances", "transactions"];

        Assert.True(options.HasRequiredUserScopes);
    }
}
