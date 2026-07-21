using System.ComponentModel.DataAnnotations;

namespace Kape.Api.Domain;

public sealed class BankConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string InstitutionId { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string ExternalConnectionId { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTimeOffset? ConsentExpiresAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsDeleted { get; set; }
}

public sealed class LinkedBankAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid BankConnectionId { get; set; }
    public string ExternalAccountId { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountNumberMask { get; set; } = string.Empty;
    public string Currency { get; set; } = "ZAR";
    public decimal CurrentBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LinkedBankTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid LinkedBankAccountId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? MerchantName { get; set; }
    public decimal Amount { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Category { get; set; } = "Uncategorised";
    public string Status { get; set; } = "posted";
    public DateTimeOffset PostedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class DebitOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid LinkedBankAccountId { get; set; }
    public string ExternalDebitOrderId { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string Frequency { get; set; } = "monthly";
    public string Status { get; set; } = "active";
    public DateTimeOffset? NextRunAt { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PaymentMethod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string TokenReference { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? VerifiedAt { get; set; }
}

public sealed class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}

public sealed class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public Guid? RelatedUserId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = "pending";
    public string Reference { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class LedgerAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? WalletId { get; set; }
    public Guid? UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Currency { get; set; } = "ZAR";
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JournalId { get; set; }
    public Guid LedgerAccountId { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class VoucherCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VoucherProvider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProviderKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTimeOffset? LastCatalogueSyncAt { get; set; }
}

public sealed class VoucherProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public Guid ProviderId { get; set; }
    public string ExternalProductId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Currency { get; set; } = "ZAR";
    public string FulfilmentType { get; set; } = "pin";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class VoucherDenomination
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VoucherProductId { get; set; }
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VoucherOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public Guid VoucherProductId { get; set; }
    public Guid VoucherDenominationId { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public string Status { get; set; } = "pending";
    public string ExternalOrderId { get; set; } = string.Empty;
    public string EncryptedVoucherCode { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FulfilledAt { get; set; }
}

public sealed class PrepaidOperator
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OperatorKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PrepaidProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperatorId { get; set; }
    public string ExternalProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal? FixedAmount { get; set; }
    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PrepaidOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public Guid PrepaidProductId { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public string Status { get; set; } = "pending";
    public string ExternalOrderId { get; set; } = string.Empty;
    public string? FulfilmentReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FulfilledAt { get; set; }
}

public sealed class PaymentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PayeeUserId { get; set; }
    public Guid? PayerUserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public Guid? WalletTransactionId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }
}

public sealed class WebhookInbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProviderType { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Status { get; set; } = "received";
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? LastError { get; set; }
}

public sealed class QueueMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string QueueName { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public int Attempts { get; set; }
    public DateTimeOffset AvailableAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
