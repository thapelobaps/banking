using Kape.Api.Domain;
using Microsoft.AspNetCore.Identity;

namespace Kape.Api.Repositories.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser?> GetByIdAsync(Guid userId);
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
    Task<SignInResult> CheckPasswordAsync(ApplicationUser user, string password);
}
