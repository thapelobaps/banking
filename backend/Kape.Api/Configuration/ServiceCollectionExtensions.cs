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

        services.AddSingleton<IBankingProvider, SouthAfricanDemoBankingProvider>();
        services.AddSingleton<IBankAggregationProvider, DemoBankAggregationProvider>();
        services.AddSingleton<IPaymentTokenizationProvider, DemoPaymentTokenizationProvider>();
        services.AddSingleton<IDigitalProductProvider, DemoDigitalProductProvider>();

        services.AddHostedService<WalletQueueWorker>();
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
