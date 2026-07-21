using System.Data;
using Kape.Api.Domain;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Exceptions;

namespace Kape.Api.Services;

public sealed partial class WalletPlatformService
{
    public async Task<WalletResponseDto> GetWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var balance = await GetWalletBalanceValueAsync(wallet, cancellationToken);
        return new WalletResponseDto(wallet.Id, wallet.Currency, wallet.Status, balance, balance, wallet.CreatedAt, wallet.UpdatedAt);
    }

    public async Task<WalletBalanceResponseDto> GetWalletBalanceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        return await _cache.GetOrCreateAsync(
            $"wallet:{userId:N}:balance",
            async () =>
            {
                var balance = await GetWalletBalanceValueAsync(wallet, cancellationToken);
                return new WalletBalanceResponseDto(wallet.Id, wallet.Currency, balance, balance, DateTimeOffset.UtcNow);
            },
            TimeSpan.FromSeconds(15),
            cancellationToken);
    }

    public async Task<PageResponseDto<WalletTransactionResponseDto>> GetWalletTransactionsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        page = NormalisePage(page);
        pageSize = NormalisePageSize(pageSize);
        var total = await _repository.CountAsync<WalletTransaction>(
            item => item.WalletId == wallet.Id && item.UserId == userId,
            cancellationToken);
        var items = await _repository.ListAsync<WalletTransaction>(
            item => item.WalletId == wallet.Id && item.UserId == userId,
            query => query.OrderByDescending(item => item.CreatedAt),
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);
        return Page(items.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<WalletOperationPreviewResponseDto> PreviewTopUpAsync(
        Guid userId,
        WalletFundingPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidatePositiveAmount(request.Amount);
        await EnsureFundingSourceAsync(userId, request.PaymentMethodId, request.LinkedBankAccountId, cancellationToken);
        var fee = CalculateTopUpFee(request.Amount, request.PaymentMethodId);
        return Preview("top_up", request.Amount, fee);
    }

    public async Task<WalletTransactionResponseDto> CreateTopUpAsync(
        Guid userId,
        WalletFundingRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidatePositiveAmount(request.Amount);
        var idempotencyKey = RequireIdempotencyKey(request.IdempotencyKey);
        var existing = await FindWalletTransactionByIdempotencyKeyAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        await EnsureFundingSourceAsync(userId, request.PaymentMethodId, request.LinkedBankAccountId, cancellationToken);
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var fee = CalculateTopUpFee(request.Amount, request.PaymentMethodId);

        await using var databaseTransaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        existing = await FindWalletTransactionByIdempotencyKeyAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            await databaseTransaction.CommitAsync(cancellationToken);
            return Map(existing);
        }

        var transaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = userId,
            PaymentMethodId = request.PaymentMethodId,
            Type = "top_up",
            Amount = request.Amount,
            FeeAmount = fee,
            NetAmount = request.Amount,
            Status = "completed",
            Reference = NormaliseReference(request.Reference, "Wallet top-up"),
            ExternalReference = $"topup_{Guid.NewGuid():N}",
            IdempotencyKey = idempotencyKey,
            CompletedAt = DateTimeOffset.UtcNow,
        };

        _repository.Add(transaction);
        await AddTopUpJournalAsync(
            transaction,
            WalletLedgerCode(wallet.Id),
            request.Amount,
            fee,
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        InvalidateWallet(userId);
        return Map(transaction);
    }

    public async Task<WalletOperationPreviewResponseDto> PreviewWithdrawalAsync(
        Guid userId,
        WalletFundingPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidatePositiveAmount(request.Amount);
        await EnsureWithdrawalDestinationAsync(userId, request.PaymentMethodId, request.LinkedBankAccountId, cancellationToken);
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var fee = CalculateWithdrawalFee(request.Amount);
        await EnsureSufficientFundsAsync(wallet, request.Amount + fee, cancellationToken);
        return Preview("withdrawal", request.Amount, fee);
    }

    public async Task<WalletTransactionResponseDto> CreateWithdrawalAsync(
        Guid userId,
        WalletFundingRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidatePositiveAmount(request.Amount);
        var idempotencyKey = RequireIdempotencyKey(request.IdempotencyKey);
        var existing = await FindWalletTransactionByIdempotencyKeyAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        await EnsureWithdrawalDestinationAsync(userId, request.PaymentMethodId, request.LinkedBankAccountId, cancellationToken);
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var fee = CalculateWithdrawalFee(request.Amount);
        var totalDebit = request.Amount + fee;

        await using var databaseTransaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        existing = await FindWalletTransactionByIdempotencyKeyAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            await databaseTransaction.CommitAsync(cancellationToken);
            return Map(existing);
        }

        await EnsureSufficientFundsAsync(wallet, totalDebit, cancellationToken);

        var transaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = userId,
            PaymentMethodId = request.PaymentMethodId,
            Type = "withdrawal",
            Amount = request.Amount,
            FeeAmount = fee,
            NetAmount = totalDebit,
            Status = "completed",
            Reference = NormaliseReference(request.Reference, "Wallet withdrawal"),
            ExternalReference = $"withdrawal_{Guid.NewGuid():N}",
            IdempotencyKey = idempotencyKey,
            CompletedAt = DateTimeOffset.UtcNow,
        };

        _repository.Add(transaction);
        await AddWalletDebitJournalAsync(
            transaction,
            WalletLedgerCode(wallet.Id),
            WithdrawalClearingCode,
            request.Amount,
            fee,
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        InvalidateWallet(userId);
        return Map(transaction);
    }

    public async Task<WalletOperationPreviewResponseDto> PreviewTransferAsync(
        Guid userId,
        WalletTransferPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidatePositiveAmount(request.Amount);
        if (request.RecipientUserId == userId)
        {
            throw Validation("recipientUserId", "Choose another Kape user.");
        }

        var recipient = await _repository.ResolveUserAsync(request.RecipientUserId.ToString(), cancellationToken)
            ?? throw new NotFoundApiException("The recipient Kape user could not be found.");
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        await EnsureSufficientFundsAsync(wallet, request.Amount, cancellationToken);
        _ = recipient;
        return Preview("wallet_transfer", request.Amount, 0m);
    }

    public async Task<WalletTransactionResponseDto> CreateTransferAsync(
        Guid userId,
        WalletTransferRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidatePositiveAmount(request.Amount);
        if (request.RecipientUserId == userId)
        {
            throw Validation("recipientUserId", "Choose another Kape user.");
        }

        var idempotencyKey = RequireIdempotencyKey(request.IdempotencyKey);
        var existing = await FindWalletTransactionByIdempotencyKeyAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        var recipient = await _repository.ResolveUserAsync(request.RecipientUserId.ToString(), cancellationToken)
            ?? throw new NotFoundApiException("The recipient Kape user could not be found.");
        var senderWallet = await EnsureWalletAsync(userId, cancellationToken);
        var recipientWallet = await EnsureWalletAsync(recipient.Id, cancellationToken);

        await using var databaseTransaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        existing = await FindWalletTransactionByIdempotencyKeyAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            await databaseTransaction.CommitAsync(cancellationToken);
            return Map(existing);
        }

        await EnsureSufficientFundsAsync(senderWallet, request.Amount, cancellationToken);

        var reference = NormaliseReference(request.Reference, "Kape wallet transfer");
        var completedAt = DateTimeOffset.UtcNow;
        var senderTransaction = new WalletTransaction
        {
            WalletId = senderWallet.Id,
            UserId = userId,
            RelatedUserId = recipient.Id,
            Type = "transfer_out",
            Amount = request.Amount,
            NetAmount = request.Amount,
            Status = "completed",
            Reference = reference,
            ExternalReference = $"kape_{Guid.NewGuid():N}",
            IdempotencyKey = idempotencyKey,
            CompletedAt = completedAt,
        };
        var recipientTransaction = new WalletTransaction
        {
            WalletId = recipientWallet.Id,
            UserId = recipient.Id,
            RelatedUserId = userId,
            Type = "transfer_in",
            Amount = request.Amount,
            NetAmount = request.Amount,
            Status = "completed",
            Reference = reference,
            ExternalReference = senderTransaction.ExternalReference,
            CompletedAt = completedAt,
        };

        _repository.Add(senderTransaction);
        _repository.Add(recipientTransaction);
        await AddJournalAsync(
            Guid.NewGuid(),
            senderTransaction,
            WalletLedgerCode(senderWallet.Id),
            WalletLedgerCode(recipientWallet.Id),
            request.Amount,
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        InvalidateWallet(userId);
        InvalidateWallet(recipient.Id);
        return Map(senderTransaction);
    }

    public async Task<WalletTransactionResponseDto> SendMoneyAsync(
        Guid userId,
        SendWalletMoneyRequestDto request,
        CancellationToken cancellationToken)
    {
        var recipient = await ResolveKapeUserAsync(
            userId,
            new ResolveKapeUserRequestDto(request.RecipientIdentifier),
            cancellationToken);
        return await CreateTransferAsync(
            userId,
            new WalletTransferRequestDto(recipient.UserId, request.Amount, request.Reference, request.IdempotencyKey),
            cancellationToken);
    }

    public async Task<IReadOnlyList<LedgerAccountResponseDto>> GetLedgerAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var accounts = await _repository.ListAsync<LedgerAccount>(
            item => item.WalletId == wallet.Id && item.UserId == userId,
            query => query.OrderBy(item => item.Code),
            cancellationToken: cancellationToken);
        var result = new List<LedgerAccountResponseDto>(accounts.Count);
        foreach (var account in accounts)
        {
            var balance = await _repository.GetLedgerAccountBalanceAsync(account.Id, cancellationToken);
            result.Add(new LedgerAccountResponseDto(account.Id, account.WalletId, account.Code, account.Name, account.AccountType, account.Currency, account.IsSystem, balance));
        }

        return result;
    }

    public async Task<PageResponseDto<LedgerEntryResponseDto>> GetLedgerEntriesAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var account = await GetLedgerAccountAsync(WalletLedgerCode(wallet.Id), cancellationToken);
        page = NormalisePage(page);
        pageSize = NormalisePageSize(pageSize);
        var total = await _repository.CountAsync<LedgerEntry>(item => item.LedgerAccountId == account.Id, cancellationToken);
        var entries = await _repository.ListAsync<LedgerEntry>(
            item => item.LedgerAccountId == account.Id,
            query => query.OrderByDescending(item => item.OccurredAt),
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);
        return Page(entries.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<LedgerEntryResponseDto> GetLedgerEntryAsync(
        Guid userId,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var account = await GetLedgerAccountAsync(WalletLedgerCode(wallet.Id), cancellationToken);
        var entry = await _repository.GetAsync<LedgerEntry>(
            item => item.Id == entryId && item.LedgerAccountId == account.Id,
            cancellationToken: cancellationToken)
            ?? throw new NotFoundApiException("The ledger entry could not be found.");
        return Map(entry);
    }

    public async Task<LedgerReconciliationResponseDto> ReconcileLedgerAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var ledgerBalance = await GetWalletBalanceValueAsync(wallet, cancellationToken);
        var transactionBalance = await _repository.GetWalletPostedTransactionBalanceAsync(wallet.Id, cancellationToken);
        var difference = ledgerBalance - transactionBalance;
        return new LedgerReconciliationResponseDto(
            wallet.Id,
            ledgerBalance,
            transactionBalance,
            difference,
            difference == 0m,
            DateTimeOffset.UtcNow);
    }

    private async Task EnsureFundingSourceAsync(
        Guid userId,
        Guid? paymentMethodId,
        Guid? linkedBankAccountId,
        CancellationToken cancellationToken)
    {
        if (paymentMethodId is null && linkedBankAccountId is null)
        {
            throw Validation("fundingSource", "Choose a tokenised card or linked bank account.");
        }

        if (paymentMethodId is not null)
        {
            var paymentMethod = await _repository.GetAsync<PaymentMethod>(
                item => item.Id == paymentMethodId && item.UserId == userId && item.Status == "active",
                cancellationToken: cancellationToken)
                ?? throw new NotFoundApiException("The tokenised payment method could not be found.");
            _ = paymentMethod;
        }

        if (linkedBankAccountId is not null)
        {
            await GetOwnedLinkedAccountAsync(userId, linkedBankAccountId.Value, cancellationToken);
        }
    }

    private Task EnsureWithdrawalDestinationAsync(
        Guid userId,
        Guid? paymentMethodId,
        Guid? linkedBankAccountId,
        CancellationToken cancellationToken) =>
        EnsureFundingSourceAsync(userId, paymentMethodId, linkedBankAccountId, cancellationToken);

    private Task<WalletTransaction?> FindWalletTransactionByIdempotencyKeyAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        _repository.GetAsync<WalletTransaction>(
            transaction => transaction.UserId == userId && transaction.IdempotencyKey == idempotencyKey,
            cancellationToken: cancellationToken);

    private static string RequireIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw Validation("idempotencyKey", "An idempotency key is required for this financial operation.");
        }

        var value = key.Trim();
        if (value.Length > 100)
        {
            throw Validation("idempotencyKey", "The idempotency key must be 100 characters or fewer.");
        }

        return value;
    }

    private static decimal CalculateTopUpFee(decimal amount, Guid? paymentMethodId) =>
        paymentMethodId is null ? 0m : decimal.Round(amount * 0.015m, 2, MidpointRounding.AwayFromZero);

    private static decimal CalculateWithdrawalFee(decimal amount) => amount >= 100m ? 5m : 2m;

    private static WalletOperationPreviewResponseDto Preview(string operation, decimal amount, decimal fee) =>
        new(operation, amount, fee, amount + fee, "ZAR", "quoted", DateTimeOffset.UtcNow.AddMinutes(5));

    private static string NormaliseReference(string? reference, string fallback) =>
        string.IsNullOrWhiteSpace(reference) ? fallback : reference.Trim()[..Math.Min(reference.Trim().Length, 160)];

    private static string? NormaliseIdempotencyKey(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : key.Trim()[..Math.Min(key.Trim().Length, 100)];
}
