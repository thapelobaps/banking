using Kape.Api.DTOs.Banking;

namespace Kape.Api.Services.Interfaces;

public interface ITransferService
{
    Task<TransactionResponseDto> CreateDemoTransferAsync(
        Guid userId,
        DemoTransferRequestDto request,
        CancellationToken cancellationToken);
}
