using Kape.Api.DTOs.WalletPlatform;

namespace Kape.Api.DTOs.Payments;

public sealed record CreateWalletPurchaseReversalRequestDto(
    string Reason,
    string IdempotencyKey);

public sealed record WalletPurchaseReversalResponseDto(
    Guid OriginalTransactionId,
    WalletTransactionResponseDto Reversal,
    string Status,
    DateTimeOffset ReversedAt);
