using Kape.Api.DTOs.Banking;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kape.Api.Controllers;

[Authorize]
[Route("api/accounts")]
public sealed class AccountsController(IAccountService accountService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountResponseDto>>> GetAccounts(
        CancellationToken cancellationToken) =>
        Ok(await accountService.GetAccountsAsync(CurrentUserId, cancellationToken));

    [HttpPost("demo/ensure")]
    [ProducesResponseType<IReadOnlyList<AccountResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountResponseDto>>> EnsureDemoAccounts(
        CancellationToken cancellationToken) =>
        Ok(await accountService.EnsureDemoAccountsAsync(CurrentUserId, cancellationToken));

    [HttpGet("demo-recipient/{accountId:guid}")]
    [ProducesResponseType<RecipientPreviewResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipientPreviewResponseDto>> GetRecipientPreview(
        Guid accountId,
        CancellationToken cancellationToken) =>
        Ok(await accountService.GetRecipientPreviewAsync(accountId, cancellationToken));

    [HttpGet("{accountId:guid}/transactions")]
    [ProducesResponseType<IReadOnlyList<TransactionResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponseDto>>> GetTransactions(
        Guid accountId,
        CancellationToken cancellationToken) =>
        Ok(await accountService.GetTransactionsAsync(
            CurrentUserId,
            accountId,
            cancellationToken));
}
