using System.Data;
using Kape.Api.Domain;
using Kape.Api.DTOs.Banking;
using Kape.Api.Exceptions;
using Kape.Api.Mapping;
using Kape.Api.Repositories.Interfaces;
using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed class TransferService(
    IBankAccountRepository bankAccountRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork) : ITransferService
{
    public async Task<TransactionResponseDto> CreateDemoTransferAsync(
        Guid userId,
        DemoTransferRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationApiException(new Dictionary<string, string[]>
            {
                ["amount"] = ["The amount must be greater than zero."],
            });
        }

        if (request.SenderBankAccountId == request.ReceiverBankAccountId)
        {
            throw new ValidationApiException(new Dictionary<string, string[]>
            {
                ["receiverBankAccountId"] = ["Choose a different recipient account."],
            });
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var sender = await bankAccountRepository.GetOwnedDemoAccountAsync(
            request.SenderBankAccountId,
            userId,
            cancellationToken)
            ?? throw new NotFoundApiException("The sender demo account could not be found.");

        var receiver = await bankAccountRepository.GetDemoAccountAsync(
            request.ReceiverBankAccountId,
            cancellationToken)
            ?? throw new NotFoundApiException("The recipient demo account could not be found.");

        if (sender.AvailableBalance < request.Amount ||
            sender.CurrentBalance < request.Amount)
        {
            throw new ValidationApiException(new Dictionary<string, string[]>
            {
                ["amount"] = ["The demo account has insufficient available balance."],
            });
        }

        sender.CurrentBalance -= request.Amount;
        sender.AvailableBalance -= request.Amount;
        receiver.CurrentBalance += request.Amount;
        receiver.AvailableBalance += request.Amount;

        var trimmedReference = request.Reference?.Trim();
        var reference = string.IsNullOrWhiteSpace(trimmedReference)
            ? "Demo transfer"
            : trimmedReference[..Math.Min(trimmedReference.Length, 120)];
        var transferDate = DateTimeOffset.UtcNow;

        var outgoing = new BankTransaction
        {
            BankAccountId = sender.Id,
            RelatedBankAccountId = receiver.Id,
            Name = reference,
            StatementDescription = "DEMO EFT SENT",
            Beneficiary = receiver.BankName,
            Amount = request.Amount,
            Direction = "debit",
            Category = "Transfer",
            Channel = "EFT",
            TransactionDate = transferDate,
        };

        var incoming = new BankTransaction
        {
            BankAccountId = receiver.Id,
            RelatedBankAccountId = sender.Id,
            Name = reference,
            StatementDescription = "DEMO EFT RECEIVED",
            Beneficiary = sender.BankName,
            Amount = request.Amount,
            Direction = "credit",
            Category = "Transfer",
            Channel = "EFT",
            TransactionDate = transferDate,
        };

        transactionRepository.Add(outgoing);
        transactionRepository.Add(incoming);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return outgoing.ToDto();
    }
}
