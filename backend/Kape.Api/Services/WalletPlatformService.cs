using System.Data;
using System.Text.Json;
using Kape.Api.Domain;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Exceptions;
using Kape.Api.Repositories.Interfaces;
using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed partial class WalletPlatformService : IWalletPlatformService
{
    private const string QueueName = "wallet-platform";
    private const string WalletLiabilityType = "liability";
    private const string CashClearingCode = "SYS:CASH_CLEARING:ZAR";
    private const string WithdrawalClearingCode = "SYS:WITHDRAWAL_CLEARING:ZAR";
    private const string VoucherSettlementCode = "SYS:VOUCHER_SETTLEMENT:ZAR";
    private const string PrepaidSettlementCode = "SYS:PREPAID_SETTLEMENT:ZAR";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IWalletPlatformRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletCache _cache;
    private readonly IWalletQueue _queue;
    private readonly IBankAggregationProvider _bankProvider;
    private readonly IPaymentTokenizationProvider _paymentProvider;
    private readonly IDigitalProductProvider _digitalProvider;
    private readonly IWebhookSignatureValidator _webhookSignatureValidator;
    private readonly IVoucherCodeProtector _voucherCodeProtector;
    private readonly ILogger<WalletPlatformService> _logger;

    public WalletPlatformService(
        IWalletPlatformRepository repository,
        IUnitOfWork unitOfWork,
        IWalletCache cache,
        IWalletQueue queue,
        IBankAggregationProvider bankProvider,
        IPaymentTokenizationProvider paymentProvider,
        IDigitalProductProvider digitalProvider,
        IWebhookSignatureValidator webhookSignatureValidator,
        IVoucherCodeProtector voucherCodeProtector,
        ILogger<WalletPlatformService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _queue = queue;
        _bankProvider = bankProvider;
        _paymentProvider = paymentProvider;
        _digitalProvider = digitalProvider;
        _webhookSignatureValidator = webhookSignatureValidator;
        _voucherCodeProtector = voucherCodeProtector;
        _logger = logger;
    }

    private static int NormalisePage(int page) => Math.Max(1, page);
    private static int NormalisePageSize(int pageSize) => Math.Clamp(pageSize, 1, 100);

    private static void ValidatePositiveAmount(decimal amount, string field = "amount")
    {
        if (amount <= 0m || decimal.Round(amount, 2) != amount)
        {
            throw Validation(field, "Enter an amount greater than zero with no more than two decimal places.");
        }
    }

    private static ValidationApiException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static PageResponseDto<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize, int total) =>
        new(items, page, pageSize, total, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));

    private async Task<Wallet> EnsureWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var wallet = await _repository.GetAsync<Wallet>(
            item => item.UserId == userId,
            tracking: true,
            cancellationToken);

        if (wallet is null)
        {
            wallet = new Wallet { UserId = userId };
            _repository.Add(wallet);
        }

        await EnsureLedgerAccountsWithinTransactionAsync(wallet, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return wallet;
    }

    private async Task EnsureLedgerAccountsWithinTransactionAsync(
        Wallet wallet,
        CancellationToken cancellationToken)
    {
        var walletCode = WalletLedgerCode(wallet.Id);
        var required = new[]
        {
            (walletCode, "Customer wallet liability", WalletLiabilityType, false, (Guid?)wallet.Id, (Guid?)wallet.UserId),
            (CashClearingCode, "Cash and card clearing", "asset", true, null, null),
            (WithdrawalClearingCode, "Withdrawal clearing", "asset", true, null, null),
            (VoucherSettlementCode, "Voucher settlement", "expense", true, null, null),
            (PrepaidSettlementCode, "Prepaid settlement", "expense", true, null, null),
        };

        foreach (var item in required)
        {
            if (await _repository.AnyAsync<LedgerAccount>(account => account.Code == item.Item1, cancellationToken))
            {
                continue;
            }

            _repository.Add(new LedgerAccount
            {
                Code = item.Item1,
                Name = item.Item2,
                AccountType = item.Item3,
                IsSystem = item.Item4,
                WalletId = item.Item5,
                UserId = item.Item6,
            });
        }
    }

    private static string WalletLedgerCode(Guid walletId) => $"WALLET:{walletId:N}:LIABILITY";

    private async Task<LedgerAccount> GetLedgerAccountAsync(string code, CancellationToken cancellationToken) =>
        await _repository.GetAsync<LedgerAccount>(account => account.Code == code, cancellationToken: cancellationToken)
        ?? throw new InvalidOperationException($"Ledger account {code} is not configured.");

    private async Task<decimal> GetWalletBalanceValueAsync(Wallet wallet, CancellationToken cancellationToken)
    {
        var account = await GetLedgerAccountAsync(WalletLedgerCode(wallet.Id), cancellationToken);
        return await _repository.GetLedgerAccountBalanceAsync(account.Id, cancellationToken);
    }

    private async Task EnsureSufficientFundsAsync(Wallet wallet, decimal required, CancellationToken cancellationToken)
    {
        var balance = await GetWalletBalanceValueAsync(wallet, cancellationToken);
        if (balance < required)
        {
            throw Validation("amount", $"The wallet balance is insufficient. Available: R {balance:N2}.");
        }
    }

    private async Task AddJournalAsync(
        Guid journalId,
        WalletTransaction transaction,
        string debitCode,
        string creditCode,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var debit = await GetLedgerAccountAsync(debitCode, cancellationToken);
        var credit = await GetLedgerAccountAsync(creditCode, cancellationToken);

        _repository.AddRange(
        [
            new LedgerEntry
            {
                JournalId = journalId,
                LedgerAccountId = debit.Id,
                WalletTransactionId = transaction.Id,
                EntryType = "debit",
                Amount = amount,
                Reference = transaction.Reference,
            },
            new LedgerEntry
            {
                JournalId = journalId,
                LedgerAccountId = credit.Id,
                WalletTransactionId = transaction.Id,
                EntryType = "credit",
                Amount = amount,
                Reference = transaction.Reference,
            },
        ]);
    }

    private async Task<WalletTransaction?> FindIdempotentTransactionAsync(
        Guid userId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        return await _repository.GetAsync<WalletTransaction>(
            transaction => transaction.UserId == userId && transaction.IdempotencyKey == idempotencyKey.Trim(),
            cancellationToken: cancellationToken);
    }

    private void InvalidateWallet(Guid userId)
    {
        _cache.RemoveByPrefix($"wallet:{userId:N}");
        _cache.RemoveByPrefix($"ledger:{userId:N}");
        _cache.RemoveByPrefix($"payment-requests:{userId:N}");
    }

    private static BankConnectionResponseDto Map(BankConnection item) =>
        new(item.Id, item.ProviderId, item.InstitutionId, item.InstitutionName, item.Status, item.ConsentExpiresAt, item.LastSyncedAt, item.CreatedAt);

    private static LinkedAccountResponseDto Map(LinkedBankAccount item) =>
        new(item.Id, item.BankConnectionId, item.InstitutionName, item.AccountName, item.AccountType, item.AccountNumberMask, item.Currency, item.CurrentBalance, item.AvailableBalance, item.IsActive, item.LastSyncedAt);

    private static LinkedTransactionResponseDto Map(LinkedBankTransaction item) =>
        new(item.Id, item.LinkedBankAccountId, item.Description, item.MerchantName, item.Amount, item.Direction, item.Category, item.Status, item.PostedAt);

    private static DebitOrderResponseDto Map(DebitOrder item) =>
        new(item.Id, item.LinkedBankAccountId, item.MerchantName, item.Amount, item.Frequency, item.Status, item.NextRunAt, item.LastRunAt);

    private static PaymentMethodResponseDto Map(PaymentMethod item) =>
        new(item.Id, item.ProviderId, item.Brand, item.BankName, item.Last4, item.ExpiryMonth, item.ExpiryYear, item.IsDefault, item.Status, item.CreatedAt, item.VerifiedAt);

    private static WalletTransactionResponseDto Map(WalletTransaction item) =>
        new(item.Id, item.Type, item.Amount, item.FeeAmount, item.NetAmount, item.Status, item.Reference, item.ExternalReference, item.RelatedUserId, item.CreatedAt, item.CompletedAt);

    private static LedgerEntryResponseDto Map(LedgerEntry item) =>
        new(item.Id, item.JournalId, item.LedgerAccountId, item.WalletTransactionId, item.EntryType, item.Amount, item.Reference, item.OccurredAt);

    private static VoucherCategoryResponseDto Map(VoucherCategory item) => new(item.Id, item.Slug, item.Name, item.SortOrder);
    private static VoucherProviderResponseDto Map(VoucherProvider item) => new(item.Id, item.ProviderKey, item.Name, item.Status, item.LastCatalogueSyncAt);
    private static VoucherProductResponseDto Map(VoucherProduct item) => new(item.Id, item.CategoryId, item.ProviderId, item.Slug, item.BrandName, item.ProductName, item.Description, item.Currency, item.FulfilmentType, item.IsActive);
    private static VoucherDenominationResponseDto Map(VoucherDenomination item) => new(item.Id, item.VoucherProductId, item.Amount, item.FeeAmount, item.IsActive);
    private static PrepaidOperatorResponseDto Map(PrepaidOperator item) => new(item.Id, item.OperatorKey, item.Name, item.ProductType, item.IsActive);
    private static PrepaidProductResponseDto Map(PrepaidProduct item) => new(item.Id, item.OperatorId, item.Name, item.ProductType, item.FixedAmount, item.MinimumAmount, item.MaximumAmount, item.FeeAmount, item.IsActive);
    private static PaymentRequestResponseDto Map(PaymentRequest item) => new(item.Id, item.PayeeUserId, item.PayerUserId, item.Amount, item.Currency, item.Message, item.Status, item.ExpiresAt, item.CreatedAt, item.RespondedAt);

    private VoucherOrderResponseDto Map(VoucherOrder item)
    {
        string? code = null;
        if (item.Status == "fulfilled" && !string.IsNullOrWhiteSpace(item.EncryptedVoucherCode))
        {
            code = _voucherCodeProtector.Unprotect(item.EncryptedVoucherCode);
        }

        return new VoucherOrderResponseDto(item.Id, item.VoucherProductId, item.Amount, item.FeeAmount, item.Status, code, item.ExternalOrderId, item.CreatedAt, item.FulfilledAt);
    }

    private static PrepaidOrderResponseDto Map(PrepaidOrder item) =>
        new(item.Id, item.PrepaidProductId, item.Recipient, item.Amount, item.FeeAmount, item.Status, item.ExternalOrderId, item.FulfilmentReference, item.CreatedAt, item.FulfilledAt);

    private sealed record BankSyncQueuePayload(Guid UserId, Guid ConnectionId);
    private sealed record VoucherFulfilQueuePayload(Guid UserId, Guid OrderId);
    private sealed record PrepaidFulfilQueuePayload(Guid UserId, Guid OrderId);
    private sealed record WebhookQueuePayload(Guid InboxId);
}
