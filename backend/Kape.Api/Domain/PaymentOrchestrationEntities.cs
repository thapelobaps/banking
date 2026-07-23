namespace Kape.Api.Domain;

public sealed class PaymentAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? VoucherOrderId { get; set; }
    public Guid? PrepaidOrderId { get; set; }
    public Guid? LinkedBankAccountId { get; set; }
    public Guid? WalletId { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string PaymentSource { get; set; } = "linked_bank";
    public string ExternalPaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Status { get; set; } = "created";
    public string Scenario { get; set; } = "awaiting_approval";
    public string Reference { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public string? FailureCode { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class PaymentStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentAttemptId { get; set; }
    public string? PreviousStatus { get; set; }
    public string Status { get; set; } = "created";
    public string Source { get; set; } = "system";
    public string? Reason { get; set; }
    public string? ExternalEventId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WalletReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public Guid PaymentAttemptId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Status { get; set; } = "active";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CapturedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
}

public sealed class PaymentRefund
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PaymentAttemptId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string ExternalRefundId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Status { get; set; } = "created";
    public string Reason { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class PaymentReconciliationRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Scope { get; set; } = "user";
    public int CheckedPayments { get; set; }
    public int MatchedPayments { get; set; }
    public int IssueCount { get; set; }
    public string Status { get; set; } = "completed";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PaymentReconciliationIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReconciliationRunId { get; set; }
    public Guid? PaymentAttemptId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}
