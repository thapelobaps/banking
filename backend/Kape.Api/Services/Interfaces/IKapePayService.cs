using Kape.Api.Domain;
using Kape.Api.DTOs.Payments;
using Kape.Api.DTOs.WalletPlatform;

namespace Kape.Api.Services.Interfaces;

public interface IKapePayService
{
    Task<KapePayQuoteResponseDto> QuoteVoucherAsync(
        Guid userId,
        KapePayVoucherQuoteRequestDto request,
        CancellationToken cancellationToken);

    Task<KapePayVoucherCheckoutResponseDto> CreateVoucherOrderAsync(
        Guid userId,
        CreateKapePayVoucherOrderRequestDto request,
        CancellationToken cancellationToken);

    Task<KapePayQuoteResponseDto> QuotePrepaidAsync(
        Guid userId,
        KapePayPrepaidQuoteRequestDto request,
        CancellationToken cancellationToken);

    Task<KapePayPrepaidCheckoutResponseDto> CreatePrepaidOrderAsync(
        Guid userId,
        CreateKapePayPrepaidOrderRequestDto request,
        CancellationToken cancellationToken);

    Task<PaymentAttemptResponseDto> GetPaymentAsync(
        Guid userId,
        Guid paymentAttemptId,
        CancellationToken cancellationToken);

    Task<PageResponseDto<PaymentAttemptResponseDto>> GetPaymentsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PaymentAttemptResponseDto> RefreshPaymentAsync(
        Guid userId,
        Guid paymentAttemptId,
        CancellationToken cancellationToken);

    Task<WebhookAcceptedResponseDto> AcceptPaymentWebhookAsync(
        PaymentProviderWebhookRequestDto request,
        string signature,
        CancellationToken cancellationToken);

    Task ProcessQueueMessageAsync(
        QueueMessage message,
        CancellationToken cancellationToken);

    Task<PaymentRefundResponseDto> RefundAsync(
        Guid userId,
        Guid paymentAttemptId,
        CreatePaymentRefundRequestDto request,
        CancellationToken cancellationToken);

    Task<PaymentReconciliationResponseDto> ReconcileAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
