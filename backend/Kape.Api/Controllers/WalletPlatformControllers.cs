using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kape.Api.Controllers;

[Authorize]
[Route("api/bank-connections")]
public sealed class BankConnectionsController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpPost("link-session")]
    public async Task<ActionResult<BankLinkSessionResponseDto>> CreateLinkSession(
        [FromBody] CreateBankLinkSessionRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreateBankLinkSessionAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("callback")]
    public async Task<ActionResult<BankConnectionResponseDto>> CompleteLink(
        [FromBody] CompleteBankLinkRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.CompleteBankLinkAsync(CurrentUserId, request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankConnectionResponseDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetBankConnectionsAsync(CurrentUserId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankConnectionResponseDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetBankConnectionAsync(CurrentUserId, id, cancellationToken));

    [HttpPost("{id:guid}/sync")]
    public async Task<ActionResult<BankConnectionSyncResponseDto>> Sync(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.SyncBankConnectionAsync(CurrentUserId, id, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteBankConnectionAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}

[Authorize]
[Route("api/linked-accounts")]
public sealed class LinkedAccountsController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LinkedAccountResponseDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetLinkedAccountsAsync(CurrentUserId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LinkedAccountResponseDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetLinkedAccountAsync(CurrentUserId, id, cancellationToken));

    [HttpGet("{id:guid}/balances")]
    public async Task<ActionResult<LinkedAccountBalanceResponseDto>> GetBalance(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetLinkedAccountBalanceAsync(CurrentUserId, id, cancellationToken));

    [HttpGet("{id:guid}/transactions")]
    public async Task<ActionResult<PageResponseDto<LinkedTransactionResponseDto>>> GetTransactions(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetLinkedAccountTransactionsAsync(CurrentUserId, id, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}/debit-orders")]
    public async Task<ActionResult<IReadOnlyList<DebitOrderResponseDto>>> GetDebitOrders(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetLinkedAccountDebitOrdersAsync(CurrentUserId, id, cancellationToken));
}

[Authorize]
[Route("api/payment-methods")]
public sealed class PaymentMethodsController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpPost("setup")]
    public async Task<ActionResult<PaymentMethodSetupResponseDto>> Setup(
        [FromBody] CreatePaymentMethodSetupRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreatePaymentMethodSetupAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("confirm")]
    public async Task<ActionResult<PaymentMethodResponseDto>> Confirm(
        [FromBody] ConfirmPaymentMethodRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.ConfirmPaymentMethodAsync(CurrentUserId, request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentMethodResponseDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetPaymentMethodsAsync(CurrentUserId, cancellationToken));

    [HttpPatch("{id:guid}/default")]
    public async Task<ActionResult<PaymentMethodResponseDto>> SetDefault(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.SetDefaultPaymentMethodAsync(CurrentUserId, id, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeletePaymentMethodAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }
}

[Authorize]
[Route("api/wallet")]
public sealed class WalletController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WalletResponseDto>> Get(CancellationToken cancellationToken) =>
        Ok(await service.GetWalletAsync(CurrentUserId, cancellationToken));

    [HttpGet("balance")]
    public async Task<ActionResult<WalletBalanceResponseDto>> GetBalance(CancellationToken cancellationToken) =>
        Ok(await service.GetWalletBalanceAsync(CurrentUserId, cancellationToken));

    [HttpGet("transactions")]
    public async Task<ActionResult<PageResponseDto<WalletTransactionResponseDto>>> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetWalletTransactionsAsync(CurrentUserId, page, pageSize, cancellationToken));

    [HttpPost("top-ups/preview")]
    public async Task<ActionResult<WalletOperationPreviewResponseDto>> PreviewTopUp(
        [FromBody] WalletFundingPreviewRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.PreviewTopUpAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("top-ups")]
    public async Task<ActionResult<WalletTransactionResponseDto>> TopUp(
        [FromBody] WalletFundingRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreateTopUpAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("withdrawals/preview")]
    public async Task<ActionResult<WalletOperationPreviewResponseDto>> PreviewWithdrawal(
        [FromBody] WalletFundingPreviewRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.PreviewWithdrawalAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("withdrawals")]
    public async Task<ActionResult<WalletTransactionResponseDto>> Withdraw(
        [FromBody] WalletFundingRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreateWithdrawalAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("transfers/preview")]
    public async Task<ActionResult<WalletOperationPreviewResponseDto>> PreviewTransfer(
        [FromBody] WalletTransferPreviewRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.PreviewTransferAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("transfers")]
    public async Task<ActionResult<WalletTransactionResponseDto>> Transfer(
        [FromBody] WalletTransferRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreateTransferAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("send")]
    public async Task<ActionResult<WalletTransactionResponseDto>> Send(
        [FromBody] SendWalletMoneyRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.SendMoneyAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("request-money")]
    public async Task<ActionResult<PaymentRequestResponseDto>> RequestMoney(
        [FromBody] CreatePaymentRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreatePaymentRequestAsync(CurrentUserId, request, cancellationToken));

    [HttpGet("payment-requests")]
    public async Task<ActionResult<IReadOnlyList<PaymentRequestResponseDto>>> GetPaymentRequests(CancellationToken cancellationToken) =>
        Ok(await service.GetPaymentRequestsAsync(CurrentUserId, cancellationToken));

    [HttpPost("payment-requests/{id:guid}/pay")]
    public async Task<ActionResult<PaymentRequestResponseDto>> PayRequest(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.PayPaymentRequestAsync(CurrentUserId, id, cancellationToken));

    [HttpPost("payment-requests/{id:guid}/decline")]
    public async Task<ActionResult<PaymentRequestResponseDto>> DeclineRequest(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.DeclinePaymentRequestAsync(CurrentUserId, id, cancellationToken));
}

[Authorize]
[Route("api/ledger")]
public sealed class LedgerController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpGet("entries")]
    public async Task<ActionResult<PageResponseDto<LedgerEntryResponseDto>>> GetEntries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetLedgerEntriesAsync(CurrentUserId, page, pageSize, cancellationToken));

    [HttpGet("entries/{id:guid}")]
    public async Task<ActionResult<LedgerEntryResponseDto>> GetEntry(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetLedgerEntryAsync(CurrentUserId, id, cancellationToken));

    [HttpGet("accounts")]
    public async Task<ActionResult<IReadOnlyList<LedgerAccountResponseDto>>> GetAccounts(CancellationToken cancellationToken) =>
        Ok(await service.GetLedgerAccountsAsync(CurrentUserId, cancellationToken));

    [HttpGet("reconciliation")]
    public async Task<ActionResult<LedgerReconciliationResponseDto>> Reconcile(CancellationToken cancellationToken) =>
        Ok(await service.ReconcileLedgerAsync(CurrentUserId, cancellationToken));
}

[Authorize]
[Route("api/vouchers")]
public sealed class VouchersController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<VoucherCategoryResponseDto>>> GetCategories(CancellationToken cancellationToken) =>
        Ok(await service.GetVoucherCategoriesAsync(cancellationToken));

    [HttpGet("providers")]
    public async Task<ActionResult<IReadOnlyList<VoucherProviderResponseDto>>> GetProviders(CancellationToken cancellationToken) =>
        Ok(await service.GetVoucherProvidersAsync(cancellationToken));

    [HttpGet("products")]
    public async Task<ActionResult<PageResponseDto<VoucherProductResponseDto>>> GetProducts(
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetVoucherProductsAsync(categoryId, page, pageSize, cancellationToken));

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<VoucherProductResponseDto>> GetProduct(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetVoucherProductAsync(id, cancellationToken));

    [HttpGet("products/{id:guid}/denominations")]
    public async Task<ActionResult<IReadOnlyList<VoucherDenominationResponseDto>>> GetDenominations(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetVoucherDenominationsAsync(id, cancellationToken));

    [HttpGet("search")]
    public async Task<ActionResult<PageResponseDto<VoucherProductResponseDto>>> Search(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.SearchVouchersAsync(query, page, pageSize, cancellationToken));
}

[Authorize]
[Route("api/voucher-orders")]
public sealed class VoucherOrdersController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpPost("quote")]
    public async Task<ActionResult<WalletOperationPreviewResponseDto>> Quote(
        [FromBody] VoucherQuoteRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.QuoteVoucherAsync(CurrentUserId, request, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<VoucherOrderResponseDto>> Create(
        [FromBody] CreateVoucherOrderRequestDto request,
        CancellationToken cancellationToken) =>
        Accepted(await service.CreateVoucherOrderAsync(CurrentUserId, request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<PageResponseDto<VoucherOrderResponseDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetVoucherOrdersAsync(CurrentUserId, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VoucherOrderResponseDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetVoucherOrderAsync(CurrentUserId, id, cancellationToken));
}

[Authorize]
[Route("api/prepaid")]
public sealed class PrepaidController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpGet("operators")]
    public async Task<ActionResult<IReadOnlyList<PrepaidOperatorResponseDto>>> GetOperators(
        [FromQuery] string? productType,
        CancellationToken cancellationToken) =>
        Ok(await service.GetPrepaidOperatorsAsync(productType, cancellationToken));

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<PrepaidProductResponseDto>>> GetProducts(
        [FromQuery] Guid? operatorId,
        [FromQuery] string? productType,
        CancellationToken cancellationToken) =>
        Ok(await service.GetPrepaidProductsAsync(operatorId, productType, cancellationToken));

    [HttpPost("validate-recipient")]
    public async Task<ActionResult<ValidatePrepaidRecipientResponseDto>> ValidateRecipient(
        [FromBody] ValidatePrepaidRecipientRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.ValidatePrepaidRecipientAsync(request, cancellationToken));

    [HttpPost("orders/quote")]
    public async Task<ActionResult<WalletOperationPreviewResponseDto>> Quote(
        [FromBody] PrepaidQuoteRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.QuotePrepaidAsync(CurrentUserId, request, cancellationToken));

    [HttpPost("orders")]
    public async Task<ActionResult<PrepaidOrderResponseDto>> CreateOrder(
        [FromBody] CreatePrepaidOrderRequestDto request,
        CancellationToken cancellationToken) =>
        Accepted(await service.CreatePrepaidOrderAsync(CurrentUserId, request, cancellationToken));

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<PrepaidOrderResponseDto>> GetOrder(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetPrepaidOrderAsync(CurrentUserId, id, cancellationToken));
}

[Authorize]
[Route("api/kape-users")]
public sealed class KapeUsersController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpPost("resolve")]
    public async Task<ActionResult<ResolvedKapeUserResponseDto>> Resolve(
        [FromBody] ResolveKapeUserRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await service.ResolveKapeUserAsync(CurrentUserId, request, cancellationToken));
}

[AllowAnonymous]
[Route("api/webhooks")]
public sealed class WebhooksController(IWalletPlatformService service) : ApiControllerBase
{
    [HttpPost("banking-provider")]
    public Task<ActionResult<WebhookAcceptedResponseDto>> BankingProvider(
        [FromBody] ProviderWebhookRequestDto request,
        CancellationToken cancellationToken) =>
        Accept("banking-provider", request, cancellationToken);

    [HttpPost("payment-processor")]
    public Task<ActionResult<WebhookAcceptedResponseDto>> PaymentProcessor(
        [FromBody] ProviderWebhookRequestDto request,
        CancellationToken cancellationToken) =>
        Accept("payment-processor", request, cancellationToken);

    [HttpPost("voucher-provider")]
    public Task<ActionResult<WebhookAcceptedResponseDto>> VoucherProvider(
        [FromBody] ProviderWebhookRequestDto request,
        CancellationToken cancellationToken) =>
        Accept("voucher-provider", request, cancellationToken);

    [HttpPost("card-issuer")]
    public Task<ActionResult<WebhookAcceptedResponseDto>> CardIssuer(
        [FromBody] ProviderWebhookRequestDto request,
        CancellationToken cancellationToken) =>
        Accept("card-issuer", request, cancellationToken);

    private async Task<ActionResult<WebhookAcceptedResponseDto>> Accept(
        string providerType,
        ProviderWebhookRequestDto request,
        CancellationToken cancellationToken)
    {
        var signature = Request.Headers["X-Kape-Signature"].ToString();
        return Accepted(await service.AcceptWebhookAsync(providerType, request, signature, cancellationToken));
    }
}
