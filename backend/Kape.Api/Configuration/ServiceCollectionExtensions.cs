using System.Text;
using Kape.Api.Data;
using Kape.Api.Domain;
using Kape.Api.Repositories;
using Kape.Api.Repositories.Interfaces;
using Kape.Api.Services;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kape.Api.Configuration;

public static class ServiceCollectionExtensions
{
    private const string FrontendCorsPolicy = "KapeFrontend";

    public static IServiceCollection AddKapeApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddIdentity(services);
        AddJwtAuthentication(services, configuration);
        AddProviderConfiguration(services, configuration);
        AddApplicationServices(services);
        AddCors(services, configuration);

        services.AddControllers();
        services.AddAuthorization();
        services.AddProblemDetails();
        services.AddDataProtection();
        services.AddMemoryCache(options => options.SizeLimit = 10_000);

        return services;
    }

    public static IApplicationBuilder UseKapeCors(this IApplicationBuilder app) =>
        app.UseCors(FrontendCorsPolicy);

    private static void AddDatabase(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:SqlServer is required.");

        services.AddDbContext<KapeDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.CommandTimeout(30)));
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<KapeDbContext>()
            .AddSignInManager();
    }

    private static void AddJwtAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("JWT signing key is required.");

        if (signingKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 characters.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "Kape.Api",
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"] ?? "Kape.Web",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });
    }

    private static void AddProviderConfiguration(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<BankAggregationProviderOptions>()
            .Bind(configuration.GetSection(BankAggregationProviderOptions.SectionName))
            .Validate(
                options => options.ActiveProvider.Trim().ToLowerInvariant() is "demo" or "demo-bank-aggregator" or "stitch",
                "Providers:BankAggregation:ActiveProvider must be demo-bank-aggregator or stitch.")
            .ValidateOnStart();

        services
            .AddOptions<StitchIntegrationOptions>()
            .Bind(configuration.GetSection(StitchIntegrationOptions.SectionName))
            .Validate(
                options => !options.Enabled || options.HasRequiredCredentials,
                "Stitch is enabled but ClientId, ClientSecret, or an absolute HTTPS RedirectUri is missing.")
            .Validate(
                options => !options.Enabled || options.HasValidStorageEncryptionKey,
                "Stitch is enabled but StorageEncryptionKey is not a base64-encoded 256-bit key.")
            .Validate(
                options => !options.Enabled || options.HasRequiredUserScopes,
                "Stitch user scopes must include openid, offline_access, accounts, balances, and transactions.")
            .Validate(
                options => options.ClientTokenRefreshSkewSeconds is >= 30 and <= 600,
                "Stitch client-token refresh skew must be between 30 and 600 seconds.")
            .ValidateOnStart();

        services.AddHttpClient("Stitch", (_, client) =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Kape-Stitch-Adapter/1.0");
        });
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IWalletPlatformRepository, WalletPlatformRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IWalletPlatformService, WalletPlatformService>();
        services.AddScoped<IWalletQueue, SqlWalletQueue>();
        services.AddSingleton<IWalletCache, MemoryWalletCache>();
        services.AddSingleton<IVoucherCodeProtector, DataProtectionVoucherCodeProtector>();
        services.AddSingleton<IWebhookSignatureValidator, WebhookSignatureValidator>();

        services.AddSingleton<IStitchSecretProtector, AesGcmStitchSecretProtector>();
        services.AddScoped<IStitchAuthorizationRequestStore, SqlStitchAuthorizationRequestStore>();
        services.AddScoped<IStitchConnectionSecretStore, SqlStitchConnectionSecretStore>();
        services.AddSingleton<IStitchOAuthClient, StitchOAuthClient>();
        services.AddSingleton<IStitchFinancialDataClient, StitchFinancialDataClient>();

        services.AddSingleton<IBankingProvider, SouthAfricanDemoBankingProvider>();
        services.AddSingleton<DemoBankAggregationProvider>();
        services.AddScoped<StitchBankAggregationProvider>();
        services.AddScoped<IBankAggregationProvider>(serviceProvider =>
        {
            var configuredProvider = serviceProvider
                .GetRequiredService<IOptions<BankAggregationProviderOptions>>()
                .Value
                .ActiveProvider
                .Trim()
                .ToLowerInvariant();

            return configuredProvider switch
            {
                "demo" or "demo-bank-aggregator" => serviceProvider.GetRequiredService<DemoBankAggregationProvider>(),
                "stitch" => ResolveStitchProvider(serviceProvider),
                _ => throw new InvalidOperationException($"Bank aggregation provider '{configuredProvider}' is not supported."),
            };
        });
        services.AddSingleton<IPaymentTokenizationProvider, DemoPaymentTokenizationProvider>();
        services.AddSingleton<IPayInProvider, DemoPayInProvider>();
        services.AddSingleton<IDigitalProductProvider, DemoDigitalProductProvider>();

        services.AddHostedService<WalletQueueWorker>();
    }

    private static StitchBankAggregationProvider ResolveStitchProvider(IServiceProvider serviceProvider)
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<StitchIntegrationOptions>>()
            .Value;
        if (!options.Enabled)
        {
            throw new InvalidOperationException(
                "The Stitch provider is selected but Providers:Stitch:Enabled is false.");
        }

        return serviceProvider.GetRequiredService<StitchBankAggregationProvider>();
    }

    private static void AddCors(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:3000"];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
    }
}
