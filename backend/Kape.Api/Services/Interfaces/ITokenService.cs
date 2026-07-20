using Kape.Api.Domain;

namespace Kape.Api.Services.Interfaces;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(ApplicationUser user);
}
