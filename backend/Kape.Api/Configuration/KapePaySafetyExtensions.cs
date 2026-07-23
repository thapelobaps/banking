namespace Kape.Api.Configuration;

public static class KapePaySafetyExtensions
{
    public static IServiceCollection AddKapePaySafety(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<KapePayOptions>()
            .Bind(configuration.GetSection(KapePayOptions.SectionName))
            .Validate(
                options => options.IsSupportedEnvironment,
                "Kape Pay supports demo and sandbox environments only in this portfolio build.")
            .Validate(
                options => options.IsSupportedProvider,
                "Kape Pay supports only the demo Pay by Bank provider in this portfolio build.")
            .Validate(
                options => !options.RealFundsEnabled,
                "Real funds must remain disabled in this portfolio build.")
            .Validate(
                options => options.IsSafePortfolioConfiguration,
                "The Kape Pay configuration is not safe for a demonstration environment.")
            .ValidateOnStart();

        return services;
    }
}
