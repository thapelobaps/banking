using Kape.Api.DTOs.Banking;
using Kape.Api.Exceptions;
using Kape.Api.Mapping;
using Kape.Api.Repositories.Interfaces;
using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed class AccountService(
    IBankAccountRepository bankAccountRepository,
    ITransactionRepository transactionRepository) : IAccountService
{
    public async Task<IReadOnlyList<AccountResponseDto>> GetAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var accounts = await bankAccountRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        return accounts.Select(account => account.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<TransactionResponseDto>> GetTransactionsAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var accountExists = await bankAccountRepository.ExistsForUserAsync(
            accountId,
            userId,
            cancellationToken);

        if (!accountExists)
        {
            throw new NotFoundApiException("The account could not be found.");
        }

        var transactions = await transactionRepository.GetByAccountIdAsync(
            accountId,
            cancellationToken);

        return transactions.Select(transaction => transaction.ToDto()).ToList();
    }
}
