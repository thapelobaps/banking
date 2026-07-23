namespace Kape.Api.Configuration;

public sealed class KapePayOptions
{
    public const string SectionName = "Providers:Payments";

    public string Environment { get; set; } = "demo";
    public string ActiveProvider { get; set; } = "demo-pay-by-bank";
    public bool RealFundsEnabled { get; set; }
    public bool ShowDemoScenarioSelector { get; set; } = true;

    public bool IsSupportedEnvironment =>
        Environment.Trim().ToLowerInvariant() is "demo" or "sandbox";

    public bool IsSupportedProvider =>
        ActiveProvider.Trim().ToLowerInvariant() is "demo" or "demo-pay-by-bank";

    public bool IsSafePortfolioConfiguration =>
        IsSupportedEnvironment && IsSupportedProvider && !RealFundsEnabled;
}
