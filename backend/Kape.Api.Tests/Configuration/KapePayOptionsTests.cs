using Kape.Api.Configuration;
using Xunit;

namespace Kape.Api.Tests.Configuration;

public sealed class KapePayOptionsTests
{
    [Theory]
    [InlineData("demo")]
    [InlineData("sandbox")]
    public void DemoAndSandboxConfigurationsAreSafe(string environment)
    {
        var options = new KapePayOptions
        {
            Environment = environment,
            ActiveProvider = "demo-pay-by-bank",
            RealFundsEnabled = false,
        };

        Assert.True(options.IsSupportedEnvironment);
        Assert.True(options.IsSupportedProvider);
        Assert.True(options.IsSafePortfolioConfiguration);
    }

    [Fact]
    public void ProductionEnvironmentIsBlocked()
    {
        var options = new KapePayOptions
        {
            Environment = "production",
            ActiveProvider = "demo-pay-by-bank",
            RealFundsEnabled = false,
        };

        Assert.False(options.IsSupportedEnvironment);
        Assert.False(options.IsSafePortfolioConfiguration);
    }

    [Fact]
    public void RealFundsAreBlocked()
    {
        var options = new KapePayOptions
        {
            Environment = "sandbox",
            ActiveProvider = "demo-pay-by-bank",
            RealFundsEnabled = true,
        };

        Assert.False(options.IsSafePortfolioConfiguration);
    }

    [Fact]
    public void UnimplementedLiveProviderIsBlocked()
    {
        var options = new KapePayOptions
        {
            Environment = "sandbox",
            ActiveProvider = "stitch-live",
            RealFundsEnabled = false,
        };

        Assert.False(options.IsSupportedProvider);
        Assert.False(options.IsSafePortfolioConfiguration);
    }
}
