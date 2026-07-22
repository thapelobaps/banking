using System.Security.Cryptography;
using System.Text;
using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed class DemoBankAggregationProvider : IBankAggregationProvider
{
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
        var institutionId = string.IsNullOrWhiteSpace(state) ? "capitec" : state.Trim().ToLowerInvariant();
        var institutionName = institutionId switch
        {
            "standard-bank" => "Standard Bank",
            "fnb" => "FNB",
            "absa" => "Absa",
            "nedbank" => "Nedbank",
            _ => "Capitec",
        };

        return Task.FromResult(new BankProviderConnectionResult(
            $"demo-connection-{userId:N}-{authorizationCode.GetHashCode(StringComparison.Ordinal):X8}",
            institutionId,
            institutionName,
            DateTimeOffset.UtcNow.AddDays(90)));
    }

    public Task<BankProviderSyncResult> SyncAsync(
        string externalConnectionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var suffix = Math.Abs(externalConnectionId.GetHashCode(StringComparison.Ordinal));
        var capitecAccountId = $"linked-capitec-{suffix}";
        var standardAccountId = $"linked-standard-{suffix}";
        var now = DateTimeOffset.UtcNow;

        IReadOnlyList<ProviderLinkedAccount> accounts =
        [
            new(
                capitecAccountId,
                "Capitec",
                "Global One",
                "transaction",
                (1000 + suffix % 9000).ToString("D4"),
                "ZAR",
                18_684m,
                18_200m),
            new(
                standardAccountId,
                "Standard Bank",
                "MyMo Plus",
                "savings",
                (1000 + (suffix / 7) % 9000).ToString("D4"),
                "ZAR",
                7_500m,
                7_500m),
        ];

        IReadOnlyList<ProviderLinkedTransaction> transactions =
        [
            new(capitecAccountId, $"salary-{suffix}", "Salary payment", "Employer", 24_500m, "credit", "Income", "posted", now.AddDays(-5)),
            new(capitecAccountId, $"groceries-{suffix}", "Pick n Pay groceries", "Pick n Pay", 786.45m, "debit", "Groceries", "posted", now.AddDays(-3)),
            new(capitecAccountId, $"netflix-{suffix}", "Netflix subscription", "Netflix", 199m, "debit", "Entertainment", "posted", now.AddDays(-2)),
            new(standardAccountId, $"savings-{suffix}", "Monthly savings contribution", null, 1_250m, "credit", "Savings", "posted", now.AddDays(-1)),
        ];

        IReadOnlyList<ProviderDebitOrder> debitOrders =
        [
            new(capitecAccountId, $"vodacom-{suffix}", "Vodacom", 599m, "monthly", "active", now.AddDays(12), now.AddDays(-18)),
            new(capitecAccountId, $"netflix-do-{suffix}", "Netflix", 199m, "monthly", "active", now.AddDays(6), now.AddDays(-24)),
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
