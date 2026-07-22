using Kape.Api.Domain;
using Kape.Api.DTOs.WalletPlatform;

namespace Kape.Api.Services;

public sealed partial class WalletPlatformService
{
    public async Task<PageResponseDto<PrepaidOrderResponseDto>> GetPrepaidOrdersAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = NormalisePage(page);
        pageSize = NormalisePageSize(pageSize);

        var total = await _repository.CountAsync<PrepaidOrder>(
            item => item.UserId == userId,
            cancellationToken);
        var orders = await _repository.ListAsync<PrepaidOrder>(
            item => item.UserId == userId,
            query => query.OrderByDescending(item => item.CreatedAt),
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);

        return Page(orders.Select(Map).ToArray(), page, pageSize, total);
    }
}
