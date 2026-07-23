using System.Text.Json;
using Kape.Api.DTOs.WalletPlatform;

namespace Kape.Api.DTOs.Payments;

public sealed record KapePayVoucherQuoteRequestDto(
    Guid VoucherProductId,
    Guid VoucherDenominationId,
    string PaymentSource,
    Guid? LinkedBankAccountId);

public sealed record CreateKapePayVoucherOrderRequestDto(
    Guid VoucherProductId,
    Guid VoucherDenominationId,
    string PaymentSource,
    Guid? LinkedBankAccountId,
    string? Scenario,
    string? ReturnUrl,
    string IdempotencyKey);

public sealed record KapePayPrepaidQuoteRequestDto(
    Guid ProductId,
    string Recipient,
    decimal Amount,
    string PaymentSource,
    Guid? LinkedBankAccountId);

public sealed record CreateKapePayPrepaidOrderRequestDto(
    Guid ProductId,
    string Recipient,
    decimal Amount,
    string PaymentSource,
    Guid? LinkedBankAccountId,
    string? Scenario,
    string? ReturnUrl,
    string IdempotencyKey);

public sealed record KapePayQuoteResponseDto(
    string OrderType,
    string PaymentSource,
    decimal Amount,
    decimal FeeAmount,
    decimal TotalAmount,
    string Currency,
    Guid? LinkedBankAccountId,
    string Status,
    DateTimeOffset ExpiresAt,
    string Disclaimer);

public sealed record PaymentStatusHistoryResponseDto(
    Guid Id,
    string? PreviousStatus,
    string Status,
    string Source,
    string? Reason,
    string? ExternalEventId,
    DateTimeOffset CreatedAt);

public sealed record PaymentAttemptResponseDto(
    Guid Id,
    string OrderType,
    Guid OrderId,
    string ProviderId,
    string PaymentSource,
    string ExternalPaymentId,
    decimal Amount,
    decimal FeeAmount,
    string Currency,
    string Status,
    string Scenario,
    string Reference,
    Guid? LinkedBankAccountId,
    Guid? WalletId,
    Guid? WalletTransactionId,
    string? RedirectUrl,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<PaymentStatusHistoryResponseDto> History);

public sealed record KapePayVoucherCheckoutResponseDto(
    VoucherOrderResponseDto Order,
    PaymentAttemptResponseDto Payment);

public sealed record KapePayPrepaidCheckoutResponseDto(
    PrepaidOrderResponseDto Order,
    PaymentAttemptResponseDto Payment);

public sealed record PaymentProviderWebhookRequestDto(
    string EventId,
    string EventType,
    string ExternalPaymentId,
    string Status,
    string? FailureCode,
    JsonElement Payload);

public sealed record CreatePaymentRefundRequestDto(
    decimal Amount,
    string Reason,
    string IdempotencyKey);

public sealed record PaymentRefundResponseDto(
    Guid Id,
    Guid PaymentAttemptId,
    string ProviderId,
    string ExternalRefundId,
    decimal Amount,
    string Currency,
    string Status,
    string Reason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record PaymentReconciliationIssueResponseDto(
    Guid Id,
    Guid? PaymentAttemptId,
    string IssueType,
    string Description,
    string Severity,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

public sealed record PaymentReconciliationResponseDto(
    Guid RunId,
    int CheckedPayments,
    int MatchedPayments,
    int IssueCount,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    IReadOnlyList<PaymentReconciliationIssueResponseDto> Issues);
