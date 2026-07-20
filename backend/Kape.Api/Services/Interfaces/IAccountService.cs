using Kape.Api.DTOs.Banking;

namespace Kape.Api.Services.Interfaces;

public interface IAccountService
{
    Task<IReadOnlyList<AccountResponseDto>> GetAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<RecipientPreviewResponseDto> GetRecipientPreviewAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionResponseDto>> GetTransactionsAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken);
}
