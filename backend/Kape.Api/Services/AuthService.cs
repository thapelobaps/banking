using System.Globalization;
using Kape.Api.Domain;
using Kape.Api.DTOs.Auth;
using Kape.Api.Exceptions;
using Kape.Api.Mapping;
using Kape.Api.Repositories.Interfaces;
using Kape.Api.Services.Interfaces;
using Kape.Api.Validation;

namespace Kape.Api.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IBankAccountRepository bankAccountRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork,
    IBankingProvider bankingProvider,
    ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationErrors = SouthAfricanRegistrationValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            throw new ValidationApiException(validationErrors);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await userRepository.GetByEmailAsync(email) is not null)
        {
            throw new ConflictApiException("An account with this email address already exists.");
        }

        var mobileNumber = SouthAfricanRegistrationValidator.NormaliseMobile(request.MobileNumber)!;
        var dateOfBirth = DateOnly.ParseExact(
            request.DateOfBirth,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);
        var now = DateTimeOffset.UtcNow;

        await using var transaction = await unitOfWork.BeginTransactionAsync(
            cancellationToken: cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MobileNumber = mobileNumber,
            PhoneNumber = mobileNumber,
            AddressLine1 = request.AddressLine1.Trim(),
            Suburb = request.Suburb.Trim(),
            City = request.City.Trim(),
            Province = request.Province,
            PostalCode = request.PostalCode.Trim(),
            DateOfBirth = dateOfBirth,
            Country = "South Africa",
            TermsAcceptedAt = now,
            PrivacyAcceptedAt = now,
            CreatedAt = now,
        };

        var identityResult = await userRepository.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ValidationApiException(MapIdentityErrors(identityResult.Errors));
        }

        var primaryAccount = bankingProvider.CreateDefaultDemoAccount(user.Id, email);
        var secondaryAccount = bankingProvider.CreateSecondaryDemoAccount(user.Id, email);

        bankAccountRepository.Add(primaryAccount);
        bankAccountRepository.Add(secondaryAccount);
        transactionRepository.AddRange(
            bankingProvider.CreateStarterTransactions(primaryAccount.Id));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var (token, expiresAt) = tokenService.CreateAccessToken(user);
        return new AuthResponseDto(token, expiresAt, user.ToDto());
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(email)
            ?? throw new UnauthorizedApiException("Invalid email or password.");

        var signInResult = await userRepository.CheckPasswordAsync(user, request.Password);
        if (signInResult.IsLockedOut)
        {
            throw new TooManyRequestsApiException(
                "The account is temporarily locked. Please try again later.");
        }

        if (!signInResult.Succeeded)
        {
            throw new UnauthorizedApiException("Invalid email or password.");
        }

        await EnsureTwoDemoAccountsAsync(user, email, cancellationToken);

        var (token, expiresAt) = tokenService.CreateAccessToken(user);
        return new AuthResponseDto(token, expiresAt, user.ToDto());
    }

    public async Task<UserResponseDto> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedApiException();

        return user.ToDto();
    }

    private async Task EnsureTwoDemoAccountsAsync(
        ApplicationUser user,
        string email,
        CancellationToken cancellationToken)
    {
        var accounts = await bankAccountRepository.GetByUserIdAsync(
            user.Id,
            cancellationToken);

        if (accounts.Count >= 2)
        {
            return;
        }

        if (accounts.Count == 0)
        {
            var primaryAccount = bankingProvider.CreateDefaultDemoAccount(user.Id, email);
            var secondaryAccount = bankingProvider.CreateSecondaryDemoAccount(user.Id, email);

            bankAccountRepository.Add(primaryAccount);
            bankAccountRepository.Add(secondaryAccount);
            transactionRepository.AddRange(
                bankingProvider.CreateStarterTransactions(primaryAccount.Id));
        }
        else
        {
            var existingAccount = accounts[0];
            var missingAccountType = string.Equals(
                existingAccount.AccountType,
                "transaction",
                StringComparison.OrdinalIgnoreCase)
                ? "savings"
                : "transaction";
            var companionAccount = bankingProvider.CreateCompanionDemoAccount(
                user.Id,
                email,
                missingAccountType);

            bankAccountRepository.Add(companionAccount);

            if (missingAccountType == "transaction")
            {
                transactionRepository.AddRange(
                    bankingProvider.CreateStarterTransactions(companionAccount.Id));
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyDictionary<string, string[]> MapIdentityErrors(
        IEnumerable<Microsoft.AspNetCore.Identity.IdentityError> errors) =>
        errors
            .GroupBy(error =>
                error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                    ? "password"
                    : error.Code.Contains("Email", StringComparison.OrdinalIgnoreCase) ||
                      error.Code.Contains("UserName", StringComparison.OrdinalIgnoreCase)
                        ? "email"
                        : "registration")
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());
}
