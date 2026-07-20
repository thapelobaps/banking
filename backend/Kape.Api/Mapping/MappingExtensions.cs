using Kape.Api.Domain;
using Kape.Api.DTOs.Auth;
using Kape.Api.DTOs.Banking;

namespace Kape.Api.Mapping;

public static class MappingExtensions
{
    public static UserResponseDto ToDto(this ApplicationUser user) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.FirstName,
        user.LastName,
        user.MobileNumber,
        user.Country);

    public static AccountResponseDto ToDto(this BankAccount account) => new(
        account.Id,
        account.BankId,
        account.BankName,
        account.AccountNumber,
        account.BranchCode,
        account.AccountType,
        account.CurrentBalance,
        account.AvailableBalance,
        account.Currency,
        account.IsDemo);

    public static TransactionResponseDto ToDto(this BankTransaction transaction) => new(
        transaction.Id,
        transaction.BankAccountId,
        transaction.RelatedBankAccountId,
        transaction.Name,
        transaction.StatementDescription,
        transaction.Beneficiary,
        transaction.Amount,
        transaction.Direction,
        transaction.Category,
        transaction.Channel,
        transaction.Status,
        transaction.TransactionDate,
        transaction.IsDemo);
}
