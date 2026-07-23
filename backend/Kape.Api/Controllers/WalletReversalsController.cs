using Kape.Api.DTOs.Payments;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kape.Api.Controllers;

[Authorize]
[Route("api/wallet/transactions")]
public sealed class WalletReversalsController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpPost("{transactionId:guid}/reversals")]
    public async Task<ActionResult<WalletPurchaseReversalResponseDto>> ReversePurchase(
        Guid transactionId,
        [FromBody] CreateWalletPurchaseReversalRequestDto request,
        CancellationToken cancellationToken)
    {
        var reversal = await service.ReverseWalletPurchaseAsync(
            CurrentUserId,
            transactionId,
            request.Reason,
            request.IdempotencyKey,
            cancellationToken);
        return Accepted(new WalletPurchaseReversalResponseDto(
            transactionId,
            reversal,
            "completed",
            reversal.CompletedAt ?? DateTimeOffset.UtcNow));
    }
}
