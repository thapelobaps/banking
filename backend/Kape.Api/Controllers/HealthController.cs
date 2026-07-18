using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kape.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult Get() => Ok(new
    {
        service = "Kape.Api",
        status = "healthy",
        timestamp = DateTimeOffset.UtcNow,
    });
}
