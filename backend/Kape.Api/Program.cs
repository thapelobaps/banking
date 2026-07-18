using System.Data;
using System.Security.Claims;
using System.Text;
using Kape.Api.Contracts;
using Kape.Api.Data;
using Kape.Api.Domain;
using Kape.Api.Services;
using Kape.Api.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is required.");
var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is required.");

if (signingKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
}

builder.Services.AddDbContext<KapeDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
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

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Kape.Api",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Kape.Web",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IDemoBankingService, DemoBankingService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new
{
    service = "Kape.Api",
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow,
}));

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager,
    KapeDbContext db,
    IDemoBankingService demoBanking,
    ITokenService tokenService) =>
{
    var validationErrors = SouthAfricanRegistrationValidator.Validate(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var email = request.Email.Trim().ToLowerInvariant();
    if (await userManager.FindByEmailAsync(email) is not null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["email"] = ["An account with this email address already exists."],
        });
    }

    var mobileNumber = SouthAfricanRegistrationValidator.NormaliseMobile(request.MobileNumber)!;
    var dateOfBirth = DateOnly.ParseExact(request.DateOfBirth, "yyyy-MM-dd");
    var now = DateTimeOffset.UtcNow;

    await using var transaction = await db.Database.BeginTransactionAsync();

    var user = new ApplicationUser
    {
        Id = Guid.NewGuid(),
        UserName = email,
        Email = email,
        FirstName = request.FirstName.Trim(),
        LastName = request.LastName.Trim(),
        MobileNumber = mobileNumber,
        PhoneNumber = mobileNumber,
        AddressLine1 = request.AddressLine1.Trim(),
        Suburb = request.Suburb.Trim(),
        City = request.City.Trim(),
        Province = request.Province,
        PostalCode = request.PostalCode.Trim(),
        DateOfBirth = dateOfBirth,
        Country = "South Africa",
        TermsAcceptedAt = now,
        PrivacyAcceptedAt = now,
        CreatedAt = now,
    };

    var createUserResult = await userManager.CreateAsync(user, request.Password);
    if (!createUserResult.Succeeded)
    {
        await transaction.RollbackAsync();
        return Results.ValidationProblem(createUserResult.Errors
            .GroupBy(error => error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                ? "password"
                : error.Code.Contains("Email", StringComparison.OrdinalIgnoreCase)
                    ? "email"
                    : "registration")
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray()));
    }

    var account = demoBanking.CreateDefaultAccount(user.Id, email);
    db.BankAccounts.Add(account);
    db.BankTransactions.AddRange(demoBanking.CreateStarterTransactions(account.Id));
    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    var (token, expiresAt) = tokenService.CreateAccessToken(user);
    return Results.Created("/api/auth/me", new AuthResponse(token, expiresAt, ToAuthUser(user)));
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await userManager.FindByEmailAsync(email);

    if (user is null)
    {
        return Results.Unauthorized();
    }

    var signInResult = await signInManager.CheckPasswordSignInAsync(
        user,
        request.Password,
        lockoutOnFailure: true);

    if (signInResult.IsLockedOut)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too many attempts",
            detail: "The account is temporarily locked. Please try again later.");
    }

    if (!signInResult.Succeeded)
    {
        return Results.Unauthorized();
    }

    var (token, expiresAt) = tokenService.CreateAccessToken(user);
    return Results.Ok(new AuthResponse(token, expiresAt, ToAuthUser(user)));
});

app.MapGet("/api/auth/me", async (
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var user = await userManager.FindByIdAsync(userId.Value.ToString());
    return user is null ? Results.Unauthorized() : Results.Ok(ToAuthUser(user));
}).RequireAuthorization();

app.MapGet("/api/accounts", async (
    ClaimsPrincipal principal,
    KapeDbContext db) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var accounts = await db.BankAccounts
        .AsNoTracking()
        .Where(account => account.UserId == userId.Value)
        .OrderBy(account => account.CreatedAt)
        .Select(account => ToAccountResponse(account))
        .ToListAsync();

    return Results.Ok(accounts);
}).RequireAuthorization();

app.MapGet("/api/accounts/{accountId:guid}/transactions", async (
    Guid accountId,
    ClaimsPrincipal principal,
    KapeDbContext db) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var accountExists = await db.BankAccounts
        .AnyAsync(account => account.Id == accountId && account.UserId == userId.Value);

    if (!accountExists)
    {
        return Results.NotFound();
    }

    var transactions = await db.BankTransactions
        .AsNoTracking()
        .Where(transaction => transaction.BankAccountId == accountId)
        .OrderByDescending(transaction => transaction.TransactionDate)
        .Select(transaction => ToTransactionResponse(transaction))
        .ToListAsync();

    return Results.Ok(transactions);
}).RequireAuthorization();

app.MapPost("/api/transfers/demo", async (
    DemoTransferRequest request,
    ClaimsPrincipal principal,
    KapeDbContext db) =>
{
    var userId = GetUserId(principal);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    if (request.Amount <= 0 || request.SenderBankAccountId == request.ReceiverBankAccountId)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["transfer"] = ["Use two different demo accounts and an amount greater than zero."],
        });
    }

    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

    var sender = await db.BankAccounts.SingleOrDefaultAsync(account =>
        account.Id == request.SenderBankAccountId && account.UserId == userId.Value && account.IsDemo);
    var receiver = await db.BankAccounts.SingleOrDefaultAsync(account =>
        account.Id == request.ReceiverBankAccountId && account.IsDemo);

    if (sender is null || receiver is null)
    {
        await transaction.RollbackAsync();
        return Results.NotFound(new { message = "A demo account could not be found." });
    }

    if (sender.AvailableBalance < request.Amount || sender.CurrentBalance < request.Amount)
    {
        await transaction.RollbackAsync();
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["amount"] = ["The demo account has insufficient available balance."],
        });
    }

    sender.CurrentBalance -= request.Amount;
    sender.AvailableBalance -= request.Amount;
    receiver.CurrentBalance += request.Amount;
    receiver.AvailableBalance += request.Amount;

    var reference = string.IsNullOrWhiteSpace(request.Reference)
        ? "Demo transfer"
        : request.Reference.Trim()[..Math.Min(request.Reference.Trim().Length, 120)];
    var transferDate = DateTimeOffset.UtcNow;

    var outgoing = new BankTransaction
    {
        BankAccountId = sender.Id,
        RelatedBankAccountId = receiver.Id,
        Name = reference,
        StatementDescription = "DEMO EFT SENT",
        Beneficiary = receiver.BankName,
        Amount = request.Amount,
        Direction = "debit",
        Category = "Transfer",
        Channel = "EFT",
        TransactionDate = transferDate,
    };

    var incoming = new BankTransaction
    {
        BankAccountId = receiver.Id,
        RelatedBankAccountId = sender.Id,
        Name = reference,
        StatementDescription = "DEMO EFT RECEIVED",
        Beneficiary = sender.BankName,
        Amount = request.Amount,
        Direction = "credit",
        Category = "Transfer",
        Channel = "EFT",
        TransactionDate = transferDate,
    };

    db.BankTransactions.AddRange(outgoing, incoming);
    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok(ToTransactionResponse(outgoing));
}).RequireAuthorization();

app.Run();

static Guid? GetUserId(ClaimsPrincipal principal)
{
    var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(value, out var userId) ? userId : null;
}

static AuthUserResponse ToAuthUser(ApplicationUser user) => new(
    user.Id,
    user.Email ?? string.Empty,
    user.FirstName,
    user.LastName,
    user.MobileNumber,
    user.Country);

static AccountResponse ToAccountResponse(BankAccount account) => new(
    account.Id,
    account.BankId,
    account.BankName,
    account.AccountNumber,
    account.BranchCode,
    account.AccountType,
    account.CurrentBalance,
    account.AvailableBalance,
    account.Currency,
    account.IsDemo);

static TransactionResponse ToTransactionResponse(BankTransaction transaction) => new(
    transaction.Id,
    transaction.BankAccountId,
    transaction.RelatedBankAccountId,
    transaction.Name,
    transaction.StatementDescription,
    transaction.Beneficiary,
    transaction.Amount,
    transaction.Direction,
    transaction.Category,
    transaction.Channel,
    transaction.Status,
    transaction.TransactionDate,
    transaction.IsDemo);
