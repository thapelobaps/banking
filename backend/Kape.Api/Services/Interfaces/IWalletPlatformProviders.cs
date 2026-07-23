namespace Kape.Api.Services.Interfaces;

public sealed record BankProviderLinkSession(
    string SessionId,
    string LinkUrl,
    DateTimeOffset ExpiresAt);

public sealed record BankProviderConnectionResult(
    string ExternalConnectionId,
    string InstitutionId,
    string InstitutionName,
    DateTimeOffset ConsentExpiresAt);

public sealed record ProviderLinkedAccount(
    string ExternalAccountId,
    string InstitutionName,
    string AccountName,
    string AccountType,
    string AccountNumberMask,
    string Currency,
    decimal CurrentBalance,
    decimal AvailableBalance);

public sealed record ProviderLinkedTransaction(
    string ExternalAccountId,
    string ExternalTransactionId,
    string Description,
    string? MerchantName,
    decimal Amount,
    string Direction,
    string Category,
    string Status,
    DateTimeOffset PostedAt);

public sealed record ProviderDebitOrder(
    string ExternalAccountId,
    string ExternalDebitOrderId,
    string MerchantName,
    decimal? Amount,
    string Frequency,
    string Status,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastRunAt);

public sealed record BankProviderSyncResult(
    IReadOnlyList<ProviderLinkedAccount> Accounts,
    IReadOnlyList<ProviderLinkedTransaction> Transactions,
    IReadOnlyList<ProviderDebitOrder> DebitOrders);

public interface IBankAggregationProvider
{
    string ProviderId { get; }

    Task<BankProviderLinkSession> CreateLinkSessionAsync(
        Guid userId,
        string? institutionId,
        string? returnUrl,
        CancellationToken cancellationToken);

    Task<BankProviderConnectionResult> CompleteLinkAsync(
        Guid userId,
        string authorizationCode,
        string state,
        CancellationToken cancellationToken);

    Task<BankProviderSyncResult> SyncAsync(
        string externalConnectionId,
        CancellationToken cancellationToken);

    Task DisconnectAsync(
        string externalConnectionId,
        CancellationToken cancellationToken);
}

public sealed record PaymentSetupSession(
    string SetupSessionId,
    string ClientSecret,
    DateTimeOffset ExpiresAt);

public sealed record ConfirmedPaymentToken(
    string ProviderId,
    string TokenReference,
    string Brand,
    string BankName,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear);

public interface IPaymentTokenizationProvider
{
    string ProviderId { get; }

    Task<PaymentSetupSession> CreateSetupSessionAsync(
        Guid userId,
        string? returnUrl,
        CancellationToken cancellationToken);

    Task<ConfirmedPaymentToken> ConfirmAsync(
        string setupSessionId,
        string paymentToken,
        string brand,
        string bankName,
        string last4,
        int expiryMonth,
        int expiryYear,
        CancellationToken cancellationToken);
}

public sealed record PayInProviderRequest(
    Guid UserId,
    Guid LinkedBankAccountId,
    string OrderType,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string Reference,
    string Scenario,
    string IdempotencyKey,
    string? ReturnUrl);

public sealed record PayInProviderSession(
    string ProviderId,
    string ExternalPaymentId,
    string Status,
    string? RedirectUrl,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? CompletedAt,
    string? FailureCode);

public sealed record PayInProviderRefundRequest(
    string ExternalPaymentId,
    decimal Amount,
    string Currency,
    string Reason,
    string IdempotencyKey);

public sealed record PayInProviderRefundResult(
    string ProviderId,
    string ExternalRefundId,
    string ExternalPaymentId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt);

public interface IPayInProvider
{
    string ProviderId { get; }

    Task<PayInProviderSession> CreatePaymentAsync(
        PayInProviderRequest request,
        CancellationToken cancellationToken);

    Task<PayInProviderSession> GetPaymentAsync(
        string externalPaymentId,
        CancellationToken cancellationToken);

    Task<PayInProviderRefundResult> RefundAsync(
        PayInProviderRefundRequest request,
        CancellationToken cancellationToken);
}

public sealed record DigitalFulfilmentResult(
    string ExternalOrderId,
    string FulfilmentReference,
    DateTimeOffset FulfilledAt);

public interface IDigitalProductProvider
{
    string ProviderId { get; }

    Task<DigitalFulfilmentResult> FulfilVoucherAsync(
        string externalProductId,
        decimal amount,
        string orderReference,
        CancellationToken cancellationToken);

    Task<DigitalFulfilmentResult> FulfilPrepaidAsync(
        string externalProductId,
        string recipient,
        decimal amount,
        string orderReference,
        CancellationToken cancellationToken);
}

public interface IWebhookSignatureValidator
{
    bool IsValid(string providerType, string payload, string signature);
}
