using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kape.Api.Controllers;

[Authorize]
[Route("api/prepaid/orders")]
public sealed class PrepaidOrderHistoryController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResponseDto<PrepaidOrderResponseDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetPrepaidOrdersAsync(CurrentUserId, page, pageSize, cancellationToken));
}
