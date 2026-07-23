using Kape.Api.Domain;
using Kape.Api.DTOs.Payments;
using Kape.Api.Exceptions;

namespace Kape.Api.Services;

public sealed partial class KapePayService
{
    public async Task<PaymentAttemptResponseDto> ReverseWalletPaymentAsync(
        Guid userId,
        Guid paymentAttemptId,
        CreateWalletPurchaseReversalRequestDto request,
        CancellationToken cancellationToken)
    {
        var payment = await GetOwnedPaymentAsync(userId, paymentAttemptId, true, cancellationToken);
        if (payment.PaymentSource != "wallet" || payment.WalletTransactionId is null)
        {
            throw new ConflictApiException("Only a Kape Demo Wallet purchase can use the wallet reversal workflow.");
        }

        if (payment.Status == "reversed")
        {
            return await MapPaymentAsync(payment, cancellationToken);
        }

        if (payment.Status != "completed")
        {
            throw new ConflictApiException("Only a completed wallet payment can be reversed.");
        }

        var reversal = await _walletPlatformService.ReverseWalletPurchaseAsync(
            userId,
            payment.WalletTransactionId.Value,
            request.Reason,
            RequireIdempotencyKey(request.IdempotencyKey),
            cancellationToken);

        var previousStatus = payment.Status;
        payment.Status = "reversed";
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        payment.CompletedAt = reversal.CompletedAt ?? DateTimeOffset.UtcNow;
        _repository.Add(new PaymentStatusHistory
        {
            PaymentAttemptId = payment.Id,
            PreviousStatus = previousStatus,
            Status = "reversed",
            Source = "wallet_reversal",
            Reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Synthetic wallet purchase reversed."
                : request.Reason.Trim(),
            ExternalEventId = reversal.ExternalReference,
        });
        await SetOrderStatusAsync(payment, "reversed", cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        payment = await GetOwnedPaymentAsync(userId, paymentAttemptId, false, cancellationToken);
        return await MapPaymentAsync(payment, cancellationToken);
    }
}
