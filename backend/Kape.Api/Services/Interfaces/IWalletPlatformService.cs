using Kape.Api.Domain;
using Kape.Api.DTOs.WalletPlatform;

namespace Kape.Api.Services.Interfaces;

public interface IWalletPlatformService
{
    Task<BankLinkSessionResponseDto> CreateBankLinkSessionAsync(Guid userId, CreateBankLinkSessionRequestDto request, CancellationToken cancellationToken);
    Task<BankConnectionResponseDto> CompleteBankLinkAsync(Guid userId, CompleteBankLinkRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<BankConnectionResponseDto>> GetBankConnectionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<BankConnectionResponseDto> GetBankConnectionAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken);
    Task<BankConnectionSyncResponseDto> SyncBankConnectionAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken);
    Task DeleteBankConnectionAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LinkedAccountResponseDto>> GetLinkedAccountsAsync(Guid userId, CancellationToken cancellationToken);
    Task<LinkedAccountResponseDto> GetLinkedAccountAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);
    Task<LinkedAccountBalanceResponseDto> GetLinkedAccountBalanceAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);
    Task<PageResponseDto<LinkedTransactionResponseDto>> GetLinkedAccountTransactionsAsync(Guid userId, Guid accountId, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<DebitOrderResponseDto>> GetLinkedAccountDebitOrdersAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);

    Task<PaymentMethodSetupResponseDto> CreatePaymentMethodSetupAsync(Guid userId, CreatePaymentMethodSetupRequestDto request, CancellationToken cancellationToken);
    Task<PaymentMethodResponseDto> ConfirmPaymentMethodAsync(Guid userId, ConfirmPaymentMethodRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentMethodResponseDto>> GetPaymentMethodsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PaymentMethodResponseDto> SetDefaultPaymentMethodAsync(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken);
    Task DeletePaymentMethodAsync(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken);

    Task<WalletResponseDto> GetWalletAsync(Guid userId, CancellationToken cancellationToken);
    Task<WalletBalanceResponseDto> GetWalletBalanceAsync(Guid userId, CancellationToken cancellationToken);
    Task<PageResponseDto<WalletTransactionResponseDto>> GetWalletTransactionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<WalletOperationPreviewResponseDto> PreviewTopUpAsync(Guid userId, WalletFundingPreviewRequestDto request, CancellationToken cancellationToken);
    Task<WalletTransactionResponseDto> CreateTopUpAsync(Guid userId, WalletFundingRequestDto request, CancellationToken cancellationToken);
    Task<WalletOperationPreviewResponseDto> PreviewWithdrawalAsync(Guid userId, WalletFundingPreviewRequestDto request, CancellationToken cancellationToken);
    Task<WalletTransactionResponseDto> CreateWithdrawalAsync(Guid userId, WalletFundingRequestDto request, CancellationToken cancellationToken);
    Task<WalletOperationPreviewResponseDto> PreviewTransferAsync(Guid userId, WalletTransferPreviewRequestDto request, CancellationToken cancellationToken);
    Task<WalletTransactionResponseDto> CreateTransferAsync(Guid userId, WalletTransferRequestDto request, CancellationToken cancellationToken);
    Task<WalletTransactionResponseDto> SendMoneyAsync(Guid userId, SendWalletMoneyRequestDto request, CancellationToken cancellationToken);
    Task<WalletTransactionResponseDto> ReverseWalletPurchaseAsync(Guid userId, Guid walletTransactionId, string reason, string idempotencyKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<LedgerAccountResponseDto>> GetLedgerAccountsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PageResponseDto<LedgerEntryResponseDto>> GetLedgerEntriesAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<LedgerEntryResponseDto> GetLedgerEntryAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);
    Task<LedgerReconciliationResponseDto> ReconcileLedgerAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<VoucherCategoryResponseDto>> GetVoucherCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<VoucherProviderResponseDto>> GetVoucherProvidersAsync(CancellationToken cancellationToken);
    Task<PageResponseDto<VoucherProductResponseDto>> GetVoucherProductsAsync(Guid? categoryId, int page, int pageSize, CancellationToken cancellationToken);
    Task<VoucherProductResponseDto> GetVoucherProductAsync(Guid productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<VoucherDenominationResponseDto>> GetVoucherDenominationsAsync(Guid productId, CancellationToken cancellationToken);
    Task<PageResponseDto<VoucherProductResponseDto>> SearchVouchersAsync(string query, int page, int pageSize, CancellationToken cancellationToken);
    Task<WalletOperationPreviewResponseDto> QuoteVoucherAsync(Guid userId, VoucherQuoteRequestDto request, CancellationToken cancellationToken);
    Task<VoucherOrderResponseDto> CreateVoucherOrderAsync(Guid userId, CreateVoucherOrderRequestDto request, CancellationToken cancellationToken);
    Task<PageResponseDto<VoucherOrderResponseDto>> GetVoucherOrdersAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<VoucherOrderResponseDto> GetVoucherOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PrepaidOperatorResponseDto>> GetPrepaidOperatorsAsync(string? productType, CancellationToken cancellationToken);
    Task<IReadOnlyList<PrepaidProductResponseDto>> GetPrepaidProductsAsync(Guid? operatorId, string? productType, CancellationToken cancellationToken);
    Task<ValidatePrepaidRecipientResponseDto> ValidatePrepaidRecipientAsync(ValidatePrepaidRecipientRequestDto request, CancellationToken cancellationToken);
    Task<WalletOperationPreviewResponseDto> QuotePrepaidAsync(Guid userId, PrepaidQuoteRequestDto request, CancellationToken cancellationToken);
    Task<PrepaidOrderResponseDto> CreatePrepaidOrderAsync(Guid userId, CreatePrepaidOrderRequestDto request, CancellationToken cancellationToken);
    Task<PageResponseDto<PrepaidOrderResponseDto>> GetPrepaidOrdersAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<PrepaidOrderResponseDto> GetPrepaidOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken);

    Task<ResolvedKapeUserResponseDto> ResolveKapeUserAsync(Guid currentUserId, ResolveKapeUserRequestDto request, CancellationToken cancellationToken);
    Task<PaymentRequestResponseDto> CreatePaymentRequestAsync(Guid userId, CreatePaymentRequestDto request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentRequestResponseDto>> GetPaymentRequestsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PaymentRequestResponseDto> PayPaymentRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken);
    Task<PaymentRequestResponseDto> DeclinePaymentRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken);

    Task<WebhookAcceptedResponseDto> AcceptWebhookAsync(string providerType, ProviderWebhookRequestDto request, string signature, CancellationToken cancellationToken);
    Task ProcessQueueMessageAsync(QueueMessage message, CancellationToken cancellationToken);
}
