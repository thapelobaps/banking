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

    public async Task<RecipientPreviewResponseDto> GetRecipientPreviewAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var virtualRecipient = DemoRecipientDirectory.Find(accountId);
        if (virtualRecipient is not null)
        {
            return new RecipientPreviewResponseDto(
                virtualRecipient.Id,
                virtualRecipient.BankName,
                virtualRecipient.AccountMask,
                virtualRecipient.AccountType,
                virtualRecipient.Currency,
                true);
        }

        var account = await bankAccountRepository.GetDemoAccountAsync(
            accountId,
            cancellationToken)
            ?? throw new NotFoundApiException("The recipient demo account could not be found.");

        var accountMask = account.AccountNumber.Length <= 4
            ? account.AccountNumber
            : account.AccountNumber[^4..];

        return new RecipientPreviewResponseDto(
            account.Id,
            account.BankName,
            accountMask,
            account.AccountType,
            account.Currency,
            account.IsDemo);
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
