using Kape.Api.DTOs.Payments;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kape.Api.Controllers;

[Authorize]
[Route("api/kape-pay")]
public sealed class KapePayController(IKapePayService service) : ApiControllerBase
{
    [HttpPost("vouchers/quote")]
    public async Task<ActionResult<KapePayQuoteResponseDto>> QuoteVoucher(
        [FromBody] KapePayVoucherQuoteRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.QuoteVoucherAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("vouchers")]
    public async Task<ActionResult<KapePayVoucherCheckoutResponseDto>> CreateVoucherOrder(
        [FromBody] CreateKapePayVoucherOrderRequestDto request,
        CancellationToken cancellationToken) =>
        Accepted(await service.CreateVoucherOrderAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("prepaid/quote")]
    public async Task<ActionResult<KapePayQuoteResponseDto>> QuotePrepaid(
        [FromBody] KapePayPrepaidQuoteRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.QuotePrepaidAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("prepaid")]
    public async Task<ActionResult<KapePayPrepaidCheckoutResponseDto>> CreatePrepaidOrder(
        [FromBody] CreateKapePayPrepaidOrderRequestDto request,
        CancellationToken cancellationToken) =>
        Accepted(await service.CreatePrepaidOrderAsync(CurrentUserId, request, cancellationToken));

    [HttpGet("payments")]
    public async Task<ActionResult<PageResponseDto<PaymentAttemptResponseDto>>> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetPaymentsAsync(CurrentUserId, page, pageSize, cancellationToken));

    [HttpGet("payments/{id:guid}")]
    public async Task<ActionResult<PaymentAttemptResponseDto>> GetPayment(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await service.GetPaymentAsync(CurrentUserId, id, cancellationToken));

    [HttpPost("payments/{id:guid}/refresh")]
    public async Task<ActionResult<PaymentAttemptResponseDto>> RefreshPayment(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await service.RefreshPaymentAsync(CurrentUserId, id, cancellationToken));

    [HttpPost("payments/{id:guid}/refunds")]
    public async Task<ActionResult<PaymentRefundResponseDto>> Refund(
        Guid id,
        [FromBody] CreatePaymentRefundRequestDto request,
        CancellationToken cancellationToken) =>
        Accepted(await service.RefundAsync(CurrentUserId, id, request, cancellationToken));

    [HttpPost("reconciliation")]
    public async Task<ActionResult<PaymentReconciliationResponseDto>> Reconcile(
        CancellationToken cancellationToken) =>
        Ok(await service.ReconcileAsync(CurrentUserId, cancellationToken));
}

[AllowAnonymous]
[Route("api/kape-pay/webhooks")]
public sealed class KapePayWebhooksController(IKapePayService service) : ApiControllerBase
{
    [HttpPost("payments")]
    public async Task<ActionResult<WebhookAcceptedResponseDto>> Payments(
        [FromBody] PaymentProviderWebhookRequestDto request,
        CancellationToken cancellationToken)
    {
        var signature = Request.Headers["X-Kape-Signature"].FirstOrDefault() ?? string.Empty;
        return Accepted(await service.AcceptPaymentWebhookAsync(request, signature, cancellationToken));
    }
}
