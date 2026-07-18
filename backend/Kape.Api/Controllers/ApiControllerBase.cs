using System.Security.Claims;
using Kape.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Kape.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new UnauthorizedApiException();
        }
    }
}
