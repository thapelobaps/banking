using System.Text.Json;

namespace Kape.Api.DTOs.WalletPlatform;

public sealed record PageResponseDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);

public sealed record CreateBankLinkSessionRequestDto(
    string ProviderId,
    string? InstitutionId,
    string? ReturnUrl);

public sealed record BankLinkSessionResponseDto(
    Guid ConnectionId,
    string SessionId,
    string LinkUrl,
    DateTimeOffset ExpiresAt);

public sealed record CompleteBankLinkRequestDto(
    Guid ConnectionId,
    string AuthorizationCode,
    string State);

public sealed record BankConnectionResponseDto(
    Guid Id,
    string ProviderId,
    string InstitutionId,
    string InstitutionName,
    string Status,
    DateTimeOffset? ConsentExpiresAt,
    DateTimeOffset? LastSyncedAt,
    DateTimeOffset CreatedAt);

public sealed record BankConnectionSyncResponseDto(
    Guid ConnectionId,
    string Status,
    int LinkedAccounts,
    int ImportedTransactions,
    int ImportedDebitOrders,
    DateTimeOffset SyncedAt);

public sealed record LinkedAccountResponseDto(
    Guid Id,
    Guid BankConnectionId,
    string InstitutionName,
    string AccountName,
    string AccountType,
    string AccountNumberMask,
    string Currency,
    decimal CurrentBalance,
    decimal AvailableBalance,
    bool IsActive,
    DateTimeOffset? LastSyncedAt);

public sealed record LinkedAccountBalanceResponseDto(
    Guid AccountId,
    string Currency,
    decimal CurrentBalance,
    decimal AvailableBalance,
    DateTimeOffset? LastSyncedAt);

public sealed record LinkedTransactionResponseDto(
    Guid Id,
    Guid LinkedBankAccountId,
    string Description,
    string? MerchantName,
    decimal Amount,
    string Direction,
    string Category,
    string Status,
    DateTimeOffset PostedAt);

public sealed record DebitOrderResponseDto(
    Guid Id,
    Guid LinkedBankAccountId,
    string MerchantName,
    decimal? Amount,
    string Frequency,
    string Status,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastRunAt);

public sealed record CreatePaymentMethodSetupRequestDto(
    string ProviderId,
    string? ReturnUrl);

public sealed record PaymentMethodSetupResponseDto(
    string SetupSessionId,
    string ClientSecret,
    DateTimeOffset ExpiresAt);

public sealed record ConfirmPaymentMethodRequestDto(
    string SetupSessionId,
    string PaymentToken,
    string Brand,
    string BankName,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear);

public sealed record PaymentMethodResponseDto(
    Guid Id,
    string ProviderId,
    string Brand,
    string BankName,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    bool IsDefault,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? VerifiedAt);

public sealed record WalletResponseDto(
    Guid Id,
    string Currency,
    string Status,
    decimal Balance,
    decimal AvailableBalance,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record WalletBalanceResponseDto(
    Guid WalletId,
    string Currency,
    decimal Balance,
    decimal AvailableBalance,
    DateTimeOffset CalculatedAt);

public sealed record WalletTransactionResponseDto(
    Guid Id,
    string Type,
    decimal Amount,
    decimal FeeAmount,
    decimal NetAmount,
    string Status,
    string Reference,
    string? ExternalReference,
    Guid? RelatedUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record WalletFundingPreviewRequestDto(
    decimal Amount,
    Guid? PaymentMethodId,
    Guid? LinkedBankAccountId,
    string? Reference);

public sealed record WalletFundingRequestDto(
    decimal Amount,
    Guid? PaymentMethodId,
    Guid? LinkedBankAccountId,
    string? Reference,
    string? IdempotencyKey);

public sealed record WalletTransferPreviewRequestDto(
    Guid RecipientUserId,
    decimal Amount,
    string? Reference);

public sealed record WalletTransferRequestDto(
    Guid RecipientUserId,
    decimal Amount,
    string? Reference,
    string? IdempotencyKey);

public sealed record WalletOperationPreviewResponseDto(
    string Operation,
    decimal Amount,
    decimal FeeAmount,
    decimal TotalAmount,
    string Currency,
    string Status,
    DateTimeOffset ExpiresAt);

public sealed record LedgerAccountResponseDto(
    Guid Id,
    Guid? WalletId,
    string Code,
    string Name,
    string AccountType,
    string Currency,
    bool IsSystem,
    decimal Balance);

public sealed record LedgerEntryResponseDto(
    Guid Id,
    Guid JournalId,
    Guid LedgerAccountId,
    Guid? WalletTransactionId,
    string EntryType,
    decimal Amount,
    string Reference,
    DateTimeOffset OccurredAt);

public sealed record LedgerReconciliationResponseDto(
    Guid WalletId,
    decimal LedgerBalance,
    decimal PostedWalletTransactions,
    decimal Difference,
    bool IsBalanced,
    DateTimeOffset ReconciledAt);

public sealed record VoucherCategoryResponseDto(
    Guid Id,
    string Slug,
    string Name,
    int SortOrder);

public sealed record VoucherProviderResponseDto(
    Guid Id,
    string ProviderKey,
    string Name,
    string Status,
    DateTimeOffset? LastCatalogueSyncAt);

public sealed record VoucherProductResponseDto(
    Guid Id,
    Guid CategoryId,
    Guid ProviderId,
    string Slug,
    string BrandName,
    string ProductName,
    string Description,
    string Currency,
    string FulfilmentType,
    bool IsActive);

public sealed record VoucherDenominationResponseDto(
    Guid Id,
    Guid VoucherProductId,
    decimal Amount,
    decimal FeeAmount,
    bool IsActive);

public sealed record VoucherQuoteRequestDto(
    Guid VoucherProductId,
    Guid VoucherDenominationId);

public sealed record CreateVoucherOrderRequestDto(
    Guid VoucherProductId,
    Guid VoucherDenominationId,
    string? IdempotencyKey);

public sealed record VoucherOrderResponseDto(
    Guid Id,
    Guid VoucherProductId,
    decimal Amount,
    decimal FeeAmount,
    string Status,
    string? VoucherCode,
    string ExternalOrderId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FulfilledAt);

public sealed record PrepaidOperatorResponseDto(
    Guid Id,
    string OperatorKey,
    string Name,
    string ProductType,
    bool IsActive);

public sealed record PrepaidProductResponseDto(
    Guid Id,
    Guid OperatorId,
    string Name,
    string ProductType,
    decimal? FixedAmount,
    decimal MinimumAmount,
    decimal MaximumAmount,
    decimal FeeAmount,
    bool IsActive);

public sealed record ValidatePrepaidRecipientRequestDto(
    Guid ProductId,
    string Recipient);

public sealed record ValidatePrepaidRecipientResponseDto(
    bool IsValid,
    string NormalisedRecipient,
    string ProductType,
    string? Message);

public sealed record PrepaidQuoteRequestDto(
    Guid ProductId,
    string Recipient,
    decimal Amount);

public sealed record CreatePrepaidOrderRequestDto(
    Guid ProductId,
    string Recipient,
    decimal Amount,
    string? IdempotencyKey);

public sealed record PrepaidOrderResponseDto(
    Guid Id,
    Guid ProductId,
    string Recipient,
    decimal Amount,
    decimal FeeAmount,
    string Status,
    string ExternalOrderId,
    string? FulfilmentReference,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FulfilledAt);

public sealed record ResolveKapeUserRequestDto(string Identifier);

public sealed record ResolvedKapeUserResponseDto(
    Guid UserId,
    string DisplayName,
    string MaskedIdentifier);

public sealed record SendWalletMoneyRequestDto(
    string RecipientIdentifier,
    decimal Amount,
    string? Reference,
    string? IdempotencyKey);

public sealed record CreatePaymentRequestDto(
    string? PayerIdentifier,
    decimal Amount,
    string? Message,
    DateTimeOffset? ExpiresAt);

public sealed record PaymentRequestResponseDto(
    Guid Id,
    Guid PayeeUserId,
    Guid? PayerUserId,
    decimal Amount,
    string Currency,
    string Message,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt);

public sealed record ProviderWebhookRequestDto(
    string EventId,
    string EventType,
    JsonElement Payload);

public sealed record WebhookAcceptedResponseDto(
    Guid InboxId,
    string ProviderType,
    string EventId,
    string Status,
    DateTimeOffset ReceivedAt);
