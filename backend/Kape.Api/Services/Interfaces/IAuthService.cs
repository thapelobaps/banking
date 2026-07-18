using Kape.Api.DTOs.Auth;

namespace Kape.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken);

    Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken);

    Task<UserResponseDto> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
