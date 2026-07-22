using System.Security.Cryptography;
using System.Text;
using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed class DemoBankAggregationProvider : IBankAggregationProvider
{
    private const string InstitutionMarker = "|institution=";

    public string ProviderId => "demo-bank-aggregator";

    public Task<BankProviderLinkSession> CreateLinkSessionAsync(
        Guid userId,
        string? institutionId,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sessionId = $"link_{Guid.NewGuid():N}";
        var redirect = string.IsNullOrWhiteSpace(returnUrl)
            ? "http://localhost:3000/my-banks"
            : returnUrl.Trim();
        var linkUrl = $"{redirect}?demoBankLink={sessionId}&institution={Uri.EscapeDataString(institutionId ?? "choose")}";

        return Task.FromResult(new BankProviderLinkSession(
            sessionId,
            linkUrl,
            DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    public Task<BankProviderConnectionResult> CompleteLinkAsync(
        Guid userId,
        string authorizationCode,
        string state,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var institution = ResolveInstitution(state);
        var baseConnectionId = $"demo-connection-{userId:N}-{authorizationCode.GetHashCode(StringComparison.Ordinal):X8}";

        return Task.FromResult(new BankProviderConnectionResult(
            $"{baseConnectionId}{InstitutionMarker}{institution.Id}",
            institution.Id,
            institution.Name,
            DateTimeOffset.UtcNow.AddDays(90)));
    }

    public Task<BankProviderSyncResult> SyncAsync(
        string externalConnectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (baseConnectionId, institutionId) = ParseConnectionId(externalConnectionId);
        var institution = ResolveInstitution(institutionId);
        var connectionHash = baseConnectionId.GetHashCode(StringComparison.Ordinal);
        var suffix = connectionHash == int.MinValue ? int.MaxValue : Math.Abs(connectionHash);
        var accountSuffix = institution.Id == "standard-bank" ? "standard" : institution.Id;
        var accountId = $"linked-{accountSuffix}-{suffix}";
        var accountMask = (1000 + suffix % 9000).ToString("D4");
        var now = DateTimeOffset.UtcNow;

        IReadOnlyList<ProviderLinkedAccount> accounts =
        [
            new(
                accountId,
                institution.Name,
                institution.AccountName,
                institution.AccountType,
                accountMask,
                "ZAR",
                institution.CurrentBalance,
                institution.AvailableBalance),
        ];

        IReadOnlyList<ProviderLinkedTransaction> transactions =
        [
            new(accountId, $"salary-{suffix}", "Salary payment", "Employer", institution.SalaryAmount, "credit", "Income", "posted", now.AddDays(-5)),
            new(accountId, $"groceries-{suffix}", "Pick n Pay groceries", "Pick n Pay", 786.45m, "debit", "Groceries", "posted", now.AddDays(-3)),
            new(accountId, $"subscription-{suffix}", "Netflix subscription", "Netflix", 199m, "debit", "Entertainment", "posted", now.AddDays(-2)),
        ];

        IReadOnlyList<ProviderDebitOrder> debitOrders =
        [
            new(accountId, $"vodacom-{suffix}", "Vodacom", 599m, "monthly", "active", now.AddDays(12), now.AddDays(-18)),
            new(accountId, $"netflix-do-{suffix}", "Netflix", 199m, "monthly", "active", now.AddDays(6), now.AddDays(-24)),
        ];

        return Task.FromResult(new BankProviderSyncResult(accounts, transactions, debitOrders));
    }

    public Task DisconnectAsync(
        string externalConnectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static (string BaseConnectionId, string InstitutionId) ParseConnectionId(string externalConnectionId)
    {
        var markerIndex = externalConnectionId.LastIndexOf(InstitutionMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return (externalConnectionId, "capitec");
        }

        var institutionId = externalConnectionId[(markerIndex + InstitutionMarker.Length)..];
        return (externalConnectionId[..markerIndex], ResolveInstitution(institutionId).Id);
    }

    private static DemoInstitution ResolveInstitution(string? institutionId) =>
        institutionId?.Trim().ToLowerInvariant() switch
        {
            "standard-bank" => new("standard-bank", "Standard Bank", "MyMo Plus", "transaction", 7_500m, 7_500m, 18_500m),
            "fnb" => new("fnb", "FNB", "Easy Zero", "transaction", 12_840m, 12_540m, 22_000m),
            "absa" => new("absa", "Absa", "Transact Account", "transaction", 9_320m, 8_920m, 19_750m),
            "nedbank" => new("nedbank", "Nedbank", "MiGoals", "transaction", 15_680m, 15_180m, 23_400m),
            _ => new("capitec", "Capitec", "Global One", "transaction", 18_684m, 18_200m, 24_500m),
        };

    private sealed record DemoInstitution(
        string Id,
        string Name,
        string AccountName,
        string AccountType,
        decimal CurrentBalance,
        decimal AvailableBalance,
        decimal SalaryAmount);
}

public sealed class DemoPaymentTokenizationProvider : IPaymentTokenizationProvider
{
    public string ProviderId => "demo-tokenizer";

    public Task<PaymentSetupSession> CreateSetupSessionAsync(
        Guid userId,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentSetupSession(
            $"setup_{Guid.NewGuid():N}",
            $"demo_secret_{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddMinutes(20)));
    }

    public Task<ConfirmedPaymentToken> ConfirmAsync(
        string setupSessionId,
        string paymentToken,
        string brand,
        string bankName,
        string last4,
        int expiryMonth,
        int expiryYear,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ConfirmedPaymentToken(
            ProviderId,
            $"tok_{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(paymentToken))).ToLowerInvariant()}",
            brand.Trim(),
            bankName.Trim(),
            last4.Trim(),
            expiryMonth,
            expiryYear));
    }
}

public sealed class DemoDigitalProductProvider : IDigitalProductProvider
{
    public string ProviderId => "demo-digital-products";

    public Task<DigitalFulfilmentResult> FulfilVoucherAsync(
        string externalProductId,
        decimal amount,
        string orderReference,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var code = $"KAPE-{externalProductId.ToUpperInvariant()}-{RandomNumberGenerator.GetInt32(1000, 9999)}-{RandomNumberGenerator.GetInt32(1000, 9999)}";
        return Task.FromResult(new DigitalFulfilmentResult(
            $"voucher_{Guid.NewGuid():N}",
            code,
            DateTimeOffset.UtcNow));
    }

    public Task<DigitalFulfilmentResult> FulfilPrepaidAsync(
        string externalProductId,
        string recipient,
        decimal amount,
        string orderReference,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new DigitalFulfilmentResult(
            $"prepaid_{Guid.NewGuid():N}",
            $"KAPE-{externalProductId.ToUpperInvariant()}-{RandomNumberGenerator.GetInt32(100000, 999999)}",
            DateTimeOffset.UtcNow));
    }
}

public sealed class WebhookSignatureValidator(IConfiguration configuration) : IWebhookSignatureValidator
{
    private readonly byte[] _secret = Encoding.UTF8.GetBytes(
        configuration["Webhooks:SharedSecret"] ?? "kape-local-demo-webhook-secret");

    public bool IsValid(string providerType, string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        using var hmac = new HMACSHA256(_secret);
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{providerType}:{payload}")))
            .ToLowerInvariant();
        var actualBytes = Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant());
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return actualBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
