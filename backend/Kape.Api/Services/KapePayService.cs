using Kape.Api.Domain;
using Kape.Api.DTOs.Payments;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Exceptions;
using Kape.Api.Repositories.Interfaces;
using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed partial class KapePayService : IKapePayService
{
    private const string PaymentQueueName = "kape-pay";
    private const string WalletPlatformQueueName = "wallet-platform";
    private const string DemoWalletProviderId = "kape-demo-wallet";
    private const string DemoDisclaimer = "Demonstration environment: no real funds are held or transferred.";

    private readonly IWalletPlatformRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletPlatformService _walletPlatformService;
    private readonly IWalletQueue _queue;
    private readonly IPayInProvider _payInProvider;
    private readonly IWebhookSignatureValidator _webhookSignatureValidator;
    private readonly IVoucherCodeProtector _voucherCodeProtector;
    private readonly ILogger<KapePayService> _logger;

    public KapePayService(
        IWalletPlatformRepository repository,
        IUnitOfWork unitOfWork,
        IWalletPlatformService walletPlatformService,
        IWalletQueue queue,
        IPayInProvider payInProvider,
        IWebhookSignatureValidator webhookSignatureValidator,
        IVoucherCodeProtector voucherCodeProtector,
        ILogger<KapePayService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _walletPlatformService = walletPlatformService;
        _queue = queue;
        _payInProvider = payInProvider;
        _webhookSignatureValidator = webhookSignatureValidator;
        _voucherCodeProtector = voucherCodeProtector;
        _logger = logger;
    }

    private static ValidationApiException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static void ValidatePositiveAmount(decimal amount, string field = "amount")
    {
        if (amount <= 0m || decimal.Round(amount, 2) != amount)
        {
            throw Validation(field, "Enter an amount greater than zero with no more than two decimal places.");
        }
    }

    private static string NormalisePaymentSource(string? source) =>
        source?.Trim().ToLowerInvariant() switch
        {
            "wallet" or "kape_wallet" or "demo_wallet" => "wallet",
            "bank" or "linked_bank" or "pay_by_bank" => "linked_bank",
            _ => throw Validation("paymentSource", "Choose Kape Demo Wallet or a linked bank account."),
        };

    private static string RequireIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw Validation("idempotencyKey", "An idempotency key is required.");
        }

        var value = key.Trim();
        if (value.Length > 100)
        {
            throw Validation("idempotencyKey", "The idempotency key must be 100 characters or fewer.");
        }

        return value;
    }

    private async Task<LinkedBankAccount> GetOwnedLinkedBankAccountAsync(
        Guid userId,
        Guid? linkedBankAccountId,
        CancellationToken cancellationToken)
    {
        if (linkedBankAccountId is null)
        {
            throw Validation("linkedBankAccountId", "Choose an active linked bank account.");
        }

        return await _repository.GetAsync<LinkedBankAccount>(
            item => item.Id == linkedBankAccountId && item.UserId == userId && item.IsActive,
            cancellationToken: cancellationToken)
            ?? throw new NotFoundApiException("The linked bank account could not be found.");
    }

    private async Task<VoucherProduct> GetVoucherProductAsync(
        Guid productId,
        CancellationToken cancellationToken) =>
        await _repository.GetAsync<VoucherProduct>(
            item => item.Id == productId && item.IsActive,
            cancellationToken: cancellationToken)
        ?? throw new NotFoundApiException("The voucher product could not be found.");

    private async Task<VoucherDenomination> GetVoucherDenominationAsync(
        Guid productId,
        Guid denominationId,
        CancellationToken cancellationToken) =>
        await _repository.GetAsync<VoucherDenomination>(
            item => item.Id == denominationId &&
                    item.VoucherProductId == productId &&
                    item.IsActive,
            cancellationToken: cancellationToken)
        ?? throw new NotFoundApiException("The voucher denomination could not be found.");

    private async Task<PrepaidProduct> GetPrepaidProductAsync(
        Guid productId,
        CancellationToken cancellationToken) =>
        await _repository.GetAsync<PrepaidProduct>(
            item => item.Id == productId && item.IsActive,
            cancellationToken: cancellationToken)
        ?? throw new NotFoundApiException("The prepaid product could not be found.");

    private static string ValidatePrepaidRecipient(PrepaidProduct product, string recipient)
    {
        var value = new string((recipient ?? string.Empty).Where(char.IsDigit).ToArray());
        var isValid = product.ProductType == "electricity"
            ? value.Length is >= 10 and <= 13
            : value.Length == 10 && value.StartsWith('0');

        if (!isValid)
        {
            throw Validation(
                "recipient",
                product.ProductType == "electricity"
                    ? "Enter a valid prepaid meter number."
                    : "Enter a valid South African mobile number.");
        }

        return value;
    }

    private static void ValidatePrepaidAmount(PrepaidProduct product, decimal amount)
    {
        ValidatePositiveAmount(amount);
        if (product.FixedAmount is not null && amount != product.FixedAmount.Value)
        {
            throw Validation("amount", $"This product requires an amount of R {product.FixedAmount.Value:N2}.");
        }

        if (amount < product.MinimumAmount || amount > product.MaximumAmount)
        {
            throw Validation("amount", $"Enter an amount between R {product.MinimumAmount:N2} and R {product.MaximumAmount:N2}.");
        }
    }

    private async Task<PaymentAttempt?> FindIdempotentPaymentAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        await _repository.GetAsync<PaymentAttempt>(
            item => item.UserId == userId && item.IdempotencyKey == idempotencyKey,
            cancellationToken: cancellationToken);

    private async Task<PaymentAttempt> GetOwnedPaymentAsync(
        Guid userId,
        Guid paymentAttemptId,
        bool tracking,
        CancellationToken cancellationToken) =>
        await _repository.GetAsync<PaymentAttempt>(
            item => item.Id == paymentAttemptId && item.UserId == userId,
            tracking,
            cancellationToken)
        ?? throw new NotFoundApiException("The payment attempt could not be found.");

    private async Task<PaymentAttemptResponseDto> MapPaymentAsync(
        PaymentAttempt payment,
        CancellationToken cancellationToken)
    {
        var history = await _repository.ListAsync<PaymentStatusHistory>(
            item => item.PaymentAttemptId == payment.Id,
            query => query.OrderBy(item => item.CreatedAt),
            cancellationToken: cancellationToken);
        var orderType = payment.VoucherOrderId is not null ? "voucher" : "prepaid";
        var orderId = payment.VoucherOrderId ?? payment.PrepaidOrderId
            ?? throw new InvalidOperationException("The payment attempt is not linked to an order.");

        return new PaymentAttemptResponseDto(
            payment.Id,
            orderType,
            orderId,
            payment.ProviderId,
            payment.PaymentSource,
            payment.ExternalPaymentId,
            payment.Amount,
            payment.FeeAmount,
            payment.Currency,
            payment.Status,
            payment.Scenario,
            payment.Reference,
            payment.LinkedBankAccountId,
            payment.WalletId,
            payment.WalletTransactionId,
            payment.RedirectUrl,
            payment.FailureCode,
            payment.CreatedAt,
            payment.UpdatedAt,
            payment.ExpiresAt,
            payment.CompletedAt,
            history.Select(MapHistory).ToArray());
    }

    private static PaymentStatusHistoryResponseDto MapHistory(PaymentStatusHistory item) =>
        new(
            item.Id,
            item.PreviousStatus,
            item.Status,
            item.Source,
            item.Reason,
            item.ExternalEventId,
            item.CreatedAt);

    private VoucherOrderResponseDto MapVoucherOrder(VoucherOrder order)
    {
        string? code = null;
        if (order.Status == "fulfilled" && !string.IsNullOrWhiteSpace(order.EncryptedVoucherCode))
        {
            code = _voucherCodeProtector.Unprotect(order.EncryptedVoucherCode);
        }

        return new VoucherOrderResponseDto(
            order.Id,
            order.VoucherProductId,
            order.Amount,
            order.FeeAmount,
            order.Status,
            code,
            order.ExternalOrderId,
            order.CreatedAt,
            order.FulfilledAt);
    }

    private static PrepaidOrderResponseDto MapPrepaidOrder(PrepaidOrder order) =>
        new(
            order.Id,
            order.PrepaidProductId,
            order.Recipient,
            order.Amount,
            order.FeeAmount,
            order.Status,
            order.ExternalOrderId,
            order.FulfilmentReference,
            order.CreatedAt,
            order.FulfilledAt);

    private static PaymentRefundResponseDto MapRefund(PaymentRefund refund) =>
        new(
            refund.Id,
            refund.PaymentAttemptId,
            refund.ProviderId,
            refund.ExternalRefundId,
            refund.Amount,
            refund.Currency,
            refund.Status,
            refund.Reason,
            refund.CreatedAt,
            refund.CompletedAt);

    private static PageResponseDto<T> Page<T>(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int total) =>
        new(items, page, pageSize, total, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));

    private static int NormalisePage(int page) => Math.Max(1, page);
    private static int NormalisePageSize(int pageSize) => Math.Clamp(pageSize, 1, 100);

    private sealed record FulfilOrderQueuePayload(Guid UserId, Guid OrderId);
    private sealed record PaymentWebhookQueuePayload(Guid InboxId);
}
