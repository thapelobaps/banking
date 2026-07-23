using System.Data;
using Kape.Api.Domain;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Exceptions;

namespace Kape.Api.Services;

public sealed partial class WalletPlatformService
{
    public async Task<WalletTransactionResponseDto> ReverseWalletPurchaseAsync(
        Guid userId,
        Guid walletTransactionId,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var key = RequireIdempotencyKey(idempotencyKey);
        var existing = await FindWalletTransactionByIdempotencyKeyAsync(userId, key, cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        var original = await _repository.GetAsync<WalletTransaction>(
            item => item.Id == walletTransactionId && item.UserId == userId,
            cancellationToken: cancellationToken)
            ?? throw new NotFoundApiException("The wallet purchase transaction could not be found.");
        if (original.Status != "completed")
        {
            throw new ConflictApiException("Only a completed wallet purchase can be reversed.");
        }

        var settlementCode = original.Type switch
        {
            "voucher_purchase" => VoucherSettlementCode,
            "prepaid_purchase" => PrepaidSettlementCode,
            _ => throw new ConflictApiException("Only voucher and prepaid wallet purchases can use this reversal workflow."),
        };
        var externalReference = $"reversal_{original.Id:N}";
        var alreadyReversed = await _repository.GetAsync<WalletTransaction>(
            item => item.UserId == userId && item.ExternalReference == externalReference,
            cancellationToken: cancellationToken);
        if (alreadyReversed is not null)
        {
            return Map(alreadyReversed);
        }

        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        if (wallet.Id != original.WalletId)
        {
            throw new ConflictApiException("The wallet purchase belongs to another wallet.");
        }

        await using var databaseTransaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        existing = await FindWalletTransactionByIdempotencyKeyAsync(userId, key, cancellationToken);
        if (existing is not null)
        {
            await databaseTransaction.CommitAsync(cancellationToken);
            return Map(existing);
        }

        alreadyReversed = await _repository.GetAsync<WalletTransaction>(
            item => item.UserId == userId && item.ExternalReference == externalReference,
            cancellationToken: cancellationToken);
        if (alreadyReversed is not null)
        {
            await databaseTransaction.CommitAsync(cancellationToken);
            return Map(alreadyReversed);
        }

        var completedAt = DateTimeOffset.UtcNow;
        var reversal = new WalletTransaction
        {
            WalletId = original.WalletId,
            UserId = userId,
            Type = $"{original.Type}_reversal",
            Amount = original.Amount,
            FeeAmount = original.FeeAmount,
            NetAmount = original.NetAmount,
            Status = "completed",
            Reference = NormaliseReference(reason, $"Reversal of {original.Reference}"),
            ExternalReference = externalReference,
            IdempotencyKey = key,
            CompletedAt = completedAt,
        };

        _repository.Add(reversal);
        await AddJournalAsync(
            Guid.NewGuid(),
            reversal,
            settlementCode,
            WalletLedgerCode(wallet.Id),
            original.NetAmount,
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        InvalidateWallet(userId);
        return Map(reversal);
    }
}
