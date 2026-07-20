using Kape.Api.Domain;
using Kape.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Kape.Api.Repositories;

public sealed class UserRepository(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IUserRepository
{
    public Task<ApplicationUser?> GetByEmailAsync(string email) =>
        userManager.FindByEmailAsync(email);

    public Task<ApplicationUser?> GetByIdAsync(Guid userId) =>
        userManager.FindByIdAsync(userId.ToString());

    public Task<IdentityResult> CreateAsync(ApplicationUser user, string password) =>
        userManager.CreateAsync(user, password);

    public Task<SignInResult> CheckPasswordAsync(ApplicationUser user, string password) =>
        signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
}
