using Kape.Api.DTOs.Banking;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kape.Api.Controllers;

[Authorize]
[Route("api/transfers")]
public sealed class TransfersController(ITransferService transferService) : ApiControllerBase
{
    [HttpPost("demo")]
    [ProducesResponseType<TransactionResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionResponseDto>> CreateDemoTransfer(
        [FromBody] DemoTransferRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await transferService.CreateDemoTransferAsync(
            CurrentUserId,
            request,
            cancellationToken));
}
