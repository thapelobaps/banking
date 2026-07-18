namespace Kape.Api.DTOs.Banking;

public sealed record AccountResponseDto(
    Guid Id,
    string BankId,
    string BankName,
    string AccountNumber,
    string BranchCode,
    string AccountType,
    decimal CurrentBalance,
    decimal AvailableBalance,
    string Currency,
    bool IsDemo);

public sealed record TransactionResponseDto(
    Guid Id,
    Guid BankAccountId,
    Guid? RelatedBankAccountId,
    string Name,
    string StatementDescription,
    string? Beneficiary,
    decimal Amount,
    string Direction,
    string Category,
    string Channel,
    string Status,
    DateTimeOffset TransactionDate,
    bool IsDemo);

public sealed record DemoTransferRequestDto(
    Guid SenderBankAccountId,
    Guid ReceiverBankAccountId,
    decimal Amount,
    string? Reference);
