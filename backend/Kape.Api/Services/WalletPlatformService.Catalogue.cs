using System.Data;
using Kape.Api.Domain;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Exceptions;

namespace Kape.Api.Services;

public sealed partial class WalletPlatformService
{
    private static readonly Guid DemoVoucherProviderId = Guid.Parse("10000000-0000-4000-8000-000000000001");
    private static readonly Guid EntertainmentCategoryId = Guid.Parse("20000000-0000-4000-8000-000000000001");
    private static readonly Guid GamingCategoryId = Guid.Parse("20000000-0000-4000-8000-000000000002");
    private static readonly Guid ShoppingCategoryId = Guid.Parse("20000000-0000-4000-8000-000000000003");
    private static readonly Guid TransportCategoryId = Guid.Parse("20000000-0000-4000-8000-000000000004");

    public async Task<IReadOnlyList<VoucherCategoryResponseDto>> GetVoucherCategoriesAsync(CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        return await _cache.GetOrCreateAsync<IReadOnlyList<VoucherCategoryResponseDto>>(
            "catalogue:voucher-categories",
            async () => (await _repository.ListAsync<VoucherCategory>(
                    item => item.IsActive,
                    query => query.OrderBy(item => item.SortOrder).ThenBy(item => item.Name),
                    cancellationToken: cancellationToken))
                .Select(Map)
                .ToArray(),
            TimeSpan.FromMinutes(10),
            cancellationToken);
    }

    public async Task<IReadOnlyList<VoucherProviderResponseDto>> GetVoucherProvidersAsync(CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        return await _cache.GetOrCreateAsync<IReadOnlyList<VoucherProviderResponseDto>>(
            "catalogue:voucher-providers",
            async () => (await _repository.ListAsync<VoucherProvider>(
                    item => item.Status == "active",
                    query => query.OrderBy(item => item.Name),
                    cancellationToken: cancellationToken))
                .Select(Map)
                .ToArray(),
            TimeSpan.FromMinutes(10),
            cancellationToken);
    }

    public async Task<PageResponseDto<VoucherProductResponseDto>> GetVoucherProductsAsync(
        Guid? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        page = NormalisePage(page);
        pageSize = NormalisePageSize(pageSize);
        var total = categoryId is null
            ? await _repository.CountAsync<VoucherProduct>(item => item.IsActive, cancellationToken)
            : await _repository.CountAsync<VoucherProduct>(item => item.IsActive && item.CategoryId == categoryId, cancellationToken);
        var products = categoryId is null
            ? await _repository.ListAsync<VoucherProduct>(
                item => item.IsActive,
                query => query.OrderBy(item => item.BrandName).ThenBy(item => item.ProductName),
                (page - 1) * pageSize,
                pageSize,
                cancellationToken)
            : await _repository.ListAsync<VoucherProduct>(
                item => item.IsActive && item.CategoryId == categoryId,
                query => query.OrderBy(item => item.BrandName).ThenBy(item => item.ProductName),
                (page - 1) * pageSize,
                pageSize,
                cancellationToken);
        return Page(products.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<VoucherProductResponseDto> GetVoucherProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        var product = await GetVoucherProductEntityAsync(productId, cancellationToken);
        return Map(product);
    }

    public async Task<IReadOnlyList<VoucherDenominationResponseDto>> GetVoucherDenominationsAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        await GetVoucherProductEntityAsync(productId, cancellationToken);
        return (await _repository.ListAsync<VoucherDenomination>(
                item => item.VoucherProductId == productId && item.IsActive,
                query => query.OrderBy(item => item.Amount),
                cancellationToken: cancellationToken))
            .Select(Map)
            .ToArray();
    }

    public async Task<PageResponseDto<VoucherProductResponseDto>> SearchVouchersAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        var search = query.Trim().ToLowerInvariant();
        if (search.Length < 2)
        {
            throw Validation("query", "Enter at least two characters.");
        }

        page = NormalisePage(page);
        pageSize = NormalisePageSize(pageSize);
        var total = await _repository.CountAsync<VoucherProduct>(
            item => item.IsActive &&
                    (item.BrandName.ToLower().Contains(search) ||
                     item.ProductName.ToLower().Contains(search) ||
                     item.Description.ToLower().Contains(search)),
            cancellationToken);
        var products = await _repository.ListAsync<VoucherProduct>(
            item => item.IsActive &&
                    (item.BrandName.ToLower().Contains(search) ||
                     item.ProductName.ToLower().Contains(search) ||
                     item.Description.ToLower().Contains(search)),
            items => items.OrderBy(item => item.BrandName).ThenBy(item => item.ProductName),
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);
        return Page(products.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<WalletOperationPreviewResponseDto> QuoteVoucherAsync(
        Guid userId,
        VoucherQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        var denomination = await GetVoucherDenominationEntityAsync(request.VoucherProductId, request.VoucherDenominationId, cancellationToken);
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        await EnsureSufficientFundsAsync(wallet, denomination.Amount + denomination.FeeAmount, cancellationToken);
        return Preview("voucher_purchase", denomination.Amount, denomination.FeeAmount);
    }

    public async Task<VoucherOrderResponseDto> CreateVoucherOrderAsync(
        Guid userId,
        CreateVoucherOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        var idempotencyKey = NormaliseRequiredIdempotencyKey(request.IdempotencyKey);
        var existing = await _repository.GetAsync<VoucherOrder>(
            item => item.UserId == userId && item.IdempotencyKey == idempotencyKey,
            cancellationToken: cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        var product = await GetVoucherProductEntityAsync(request.VoucherProductId, cancellationToken);
        var denomination = await GetVoucherDenominationEntityAsync(product.Id, request.VoucherDenominationId, cancellationToken);
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var totalDebit = denomination.Amount + denomination.FeeAmount;

        await using var databaseTransaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await EnsureSufficientFundsAsync(wallet, totalDebit, cancellationToken);

        var walletTransaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = userId,
            Type = "voucher_purchase",
            Amount = denomination.Amount,
            FeeAmount = denomination.FeeAmount,
            NetAmount = totalDebit,
            Status = "completed",
            Reference = $"{product.BrandName} voucher",
            ExternalReference = $"voucher_{Guid.NewGuid():N}",
            IdempotencyKey = $"voucher:{idempotencyKey}",
            CompletedAt = DateTimeOffset.UtcNow,
        };
        var order = new VoucherOrder
        {
            UserId = userId,
            WalletId = wallet.Id,
            VoucherProductId = product.Id,
            VoucherDenominationId = denomination.Id,
            WalletTransactionId = walletTransaction.Id,
            Amount = denomination.Amount,
            FeeAmount = denomination.FeeAmount,
            Status = "pending",
            ExternalOrderId = string.Empty,
            EncryptedVoucherCode = string.Empty,
            IdempotencyKey = idempotencyKey,
        };

        _repository.Add(walletTransaction);
        _repository.Add(order);
        await AddJournalAsync(Guid.NewGuid(), walletTransaction, WalletLedgerCode(wallet.Id), VoucherSettlementCode, totalDebit, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        await _queue.EnqueueAsync(QueueName, "voucher-fulfil", new VoucherFulfilQueuePayload(userId, order.Id), cancellationToken: cancellationToken);

        InvalidateWallet(userId);
        return Map(order);
    }

    public async Task<PageResponseDto<VoucherOrderResponseDto>> GetVoucherOrdersAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = NormalisePage(page);
        pageSize = NormalisePageSize(pageSize);
        var total = await _repository.CountAsync<VoucherOrder>(item => item.UserId == userId, cancellationToken);
        var orders = await _repository.ListAsync<VoucherOrder>(
            item => item.UserId == userId,
            query => query.OrderByDescending(item => item.CreatedAt),
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);
        return Page(orders.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<VoucherOrderResponseDto> GetVoucherOrderAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetAsync<VoucherOrder>(
            item => item.Id == orderId && item.UserId == userId,
            cancellationToken: cancellationToken)
            ?? throw new NotFoundApiException("The voucher order could not be found.");
        return Map(order);
    }

    public async Task<IReadOnlyList<PrepaidOperatorResponseDto>> GetPrepaidOperatorsAsync(
        string? productType,
        CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        var normalisedType = productType?.Trim().ToLowerInvariant();
        var operators = string.IsNullOrWhiteSpace(normalisedType)
            ? await _repository.ListAsync<PrepaidOperator>(item => item.IsActive, query => query.OrderBy(item => item.Name), cancellationToken: cancellationToken)
            : await _repository.ListAsync<PrepaidOperator>(item => item.IsActive && item.ProductType == normalisedType, query => query.OrderBy(item => item.Name), cancellationToken: cancellationToken);
        return operators.Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<PrepaidProductResponseDto>> GetPrepaidProductsAsync(
        Guid? operatorId,
        string? productType,
        CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        var normalisedType = productType?.Trim().ToLowerInvariant();
        var products = await _repository.ListAsync<PrepaidProduct>(
            item => item.IsActive &&
                    (operatorId == null || item.OperatorId == operatorId) &&
                    (normalisedType == null || item.ProductType == normalisedType),
            query => query.OrderBy(item => item.Name),
            cancellationToken: cancellationToken);
        return products.Select(Map).ToArray();
    }

    public async Task<ValidatePrepaidRecipientResponseDto> ValidatePrepaidRecipientAsync(
        ValidatePrepaidRecipientRequestDto request,
        CancellationToken cancellationToken)
    {
        var product = await GetPrepaidProductEntityAsync(request.ProductId, cancellationToken);
        var value = new string(request.Recipient.Where(char.IsDigit).ToArray());
        var valid = product.ProductType == "electricity"
            ? value.Length is >= 10 and <= 13
            : value.Length == 10 && value.StartsWith('0');
        return new ValidatePrepaidRecipientResponseDto(
            valid,
            value,
            product.ProductType,
            valid ? null : product.ProductType == "electricity" ? "Enter a valid prepaid meter number." : "Enter a valid South African mobile number.");
    }

    public async Task<WalletOperationPreviewResponseDto> QuotePrepaidAsync(
        Guid userId,
        PrepaidQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        var product = await ValidatePrepaidOrderAsync(request.ProductId, request.Recipient, request.Amount, cancellationToken);
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        await EnsureSufficientFundsAsync(wallet, request.Amount + product.FeeAmount, cancellationToken);
        return Preview("prepaid_purchase", request.Amount, product.FeeAmount);
    }

    public async Task<PrepaidOrderResponseDto> CreatePrepaidOrderAsync(
        Guid userId,
        CreatePrepaidOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureCatalogueSeededAsync(cancellationToken);
        var idempotencyKey = NormaliseRequiredIdempotencyKey(request.IdempotencyKey);
        var existing = await _repository.GetAsync<PrepaidOrder>(
            item => item.UserId == userId && item.IdempotencyKey == idempotencyKey,
            cancellationToken: cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        var product = await ValidatePrepaidOrderAsync(request.ProductId, request.Recipient, request.Amount, cancellationToken);
        var recipient = (await ValidatePrepaidRecipientAsync(new ValidatePrepaidRecipientRequestDto(product.Id, request.Recipient), cancellationToken)).NormalisedRecipient;
        var wallet = await EnsureWalletAsync(userId, cancellationToken);
        var totalDebit = request.Amount + product.FeeAmount;

        await using var databaseTransaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await EnsureSufficientFundsAsync(wallet, totalDebit, cancellationToken);

        var walletTransaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = userId,
            Type = "prepaid_purchase",
            Amount = request.Amount,
            FeeAmount = product.FeeAmount,
            NetAmount = totalDebit,
            Status = "completed",
            Reference = $"{product.Name} for {recipient}",
            ExternalReference = $"prepaid_{Guid.NewGuid():N}",
            IdempotencyKey = $"prepaid:{idempotencyKey}",
            CompletedAt = DateTimeOffset.UtcNow,
        };
        var order = new PrepaidOrder
        {
            UserId = userId,
            WalletId = wallet.Id,
            PrepaidProductId = product.Id,
            WalletTransactionId = walletTransaction.Id,
            Recipient = recipient,
            Amount = request.Amount,
            FeeAmount = product.FeeAmount,
            Status = "pending",
            ExternalOrderId = string.Empty,
            IdempotencyKey = idempotencyKey,
        };

        _repository.Add(walletTransaction);
        _repository.Add(order);
        await AddJournalAsync(Guid.NewGuid(), walletTransaction, WalletLedgerCode(wallet.Id), PrepaidSettlementCode, totalDebit, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        await _queue.EnqueueAsync(QueueName, "prepaid-fulfil", new PrepaidFulfilQueuePayload(userId, order.Id), cancellationToken: cancellationToken);

        InvalidateWallet(userId);
        return Map(order);
    }

    public async Task<PrepaidOrderResponseDto> GetPrepaidOrderAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetAsync<PrepaidOrder>(
            item => item.Id == orderId && item.UserId == userId,
            cancellationToken: cancellationToken)
            ?? throw new NotFoundApiException("The prepaid order could not be found.");
        return Map(order);
    }

    private async Task<VoucherProduct> GetVoucherProductEntityAsync(Guid productId, CancellationToken cancellationToken) =>
        await _repository.GetAsync<VoucherProduct>(item => item.Id == productId && item.IsActive, cancellationToken: cancellationToken)
        ?? throw new NotFoundApiException("The voucher product could not be found.");

    private async Task<VoucherDenomination> GetVoucherDenominationEntityAsync(
        Guid productId,
        Guid denominationId,
        CancellationToken cancellationToken) =>
        await _repository.GetAsync<VoucherDenomination>(
            item => item.Id == denominationId && item.VoucherProductId == productId && item.IsActive,
            cancellationToken: cancellationToken)
        ?? throw new NotFoundApiException("The voucher denomination could not be found.");

    private async Task<PrepaidProduct> GetPrepaidProductEntityAsync(Guid productId, CancellationToken cancellationToken) =>
        await _repository.GetAsync<PrepaidProduct>(item => item.Id == productId && item.IsActive, cancellationToken: cancellationToken)
        ?? throw new NotFoundApiException("The prepaid product could not be found.");

    private async Task<PrepaidProduct> ValidatePrepaidOrderAsync(
        Guid productId,
        string recipient,
        decimal amount,
        CancellationToken cancellationToken)
    {
        ValidatePositiveAmount(amount);
        var product = await GetPrepaidProductEntityAsync(productId, cancellationToken);
        var validation = await ValidatePrepaidRecipientAsync(new ValidatePrepaidRecipientRequestDto(product.Id, recipient), cancellationToken);
        if (!validation.IsValid)
        {
            throw Validation("recipient", validation.Message ?? "The recipient is invalid.");
        }

        if (product.FixedAmount is not null && amount != product.FixedAmount.Value)
        {
            throw Validation("amount", $"This product requires an amount of R {product.FixedAmount.Value:N2}.");
        }

        if (amount < product.MinimumAmount || amount > product.MaximumAmount)
        {
            throw Validation("amount", $"Enter an amount between R {product.MinimumAmount:N2} and R {product.MaximumAmount:N2}.");
        }

        return product;
    }

    private static string NormaliseRequiredIdempotencyKey(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? Guid.NewGuid().ToString("N")
            : key.Trim()[..Math.Min(key.Trim().Length, 100)];

    private async Task EnsureCatalogueSeededAsync(CancellationToken cancellationToken)
    {
        if (await _repository.AnyAsync<VoucherProvider>(item => item.Id == DemoVoucherProviderId, cancellationToken))
        {
            return;
        }

        _repository.Add(new VoucherProvider
        {
            Id = DemoVoucherProviderId,
            ProviderKey = _digitalProvider.ProviderId,
            Name = "Kape Demo Digital Products",
            Status = "active",
            LastCatalogueSyncAt = DateTimeOffset.UtcNow,
        });
        _repository.AddRange(
        [
            new VoucherCategory { Id = EntertainmentCategoryId, Slug = "entertainment", Name = "Entertainment", SortOrder = 1 },
            new VoucherCategory { Id = GamingCategoryId, Slug = "gaming", Name = "Gaming", SortOrder = 2 },
            new VoucherCategory { Id = ShoppingCategoryId, Slug = "shopping", Name = "Shopping", SortOrder = 3 },
            new VoucherCategory { Id = TransportCategoryId, Slug = "transport", Name = "Transport and delivery", SortOrder = 4 },
        ]);

        var products = new[]
        {
            Voucher("30000000-0000-4000-8000-000000000001", EntertainmentCategoryId, "netflix", "Netflix", "Netflix Gift Card", "Stream films and series."),
            Voucher("30000000-0000-4000-8000-000000000002", EntertainmentCategoryId, "spotify", "Spotify", "Spotify Premium", "Music and podcast voucher."),
            Voucher("30000000-0000-4000-8000-000000000003", ShoppingCategoryId, "amazon", "Amazon", "Amazon Gift Card", "Digital shopping gift card."),
            Voucher("30000000-0000-4000-8000-000000000004", ShoppingCategoryId, "google-play", "Google Play", "Google Play Gift Code", "Apps, games and digital content."),
            Voucher("30000000-0000-4000-8000-000000000005", TransportCategoryId, "uber", "Uber", "Uber and Uber Eats", "Rides and food delivery credit."),
            Voucher("30000000-0000-4000-8000-000000000006", GamingCategoryId, "playstation", "PlayStation", "PlayStation Store", "Games and add-ons."),
            Voucher("30000000-0000-4000-8000-000000000007", GamingCategoryId, "xbox", "Xbox", "Xbox Gift Card", "Games, subscriptions and add-ons."),
            Voucher("30000000-0000-4000-8000-000000000008", GamingCategoryId, "steam", "Steam", "Steam Wallet", "PC games and digital content."),
        };
        _repository.AddRange(products);

        var denominations = new List<VoucherDenomination>();
        foreach (var product in products)
        {
            foreach (var amount in new[] { 100m, 250m, 500m, 1000m })
            {
                denominations.Add(new VoucherDenomination
                {
                    Id = DeterministicGuid($"{product.Id:N}:{amount}"),
                    VoucherProductId = product.Id,
                    Amount = amount,
                    FeeAmount = 0m,
                });
            }
        }
        _repository.AddRange(denominations);

        var vodacomId = Guid.Parse("40000000-0000-4000-8000-000000000001");
        var mtnId = Guid.Parse("40000000-0000-4000-8000-000000000002");
        var telkomId = Guid.Parse("40000000-0000-4000-8000-000000000003");
        var cellCId = Guid.Parse("40000000-0000-4000-8000-000000000004");
        var electricityId = Guid.Parse("40000000-0000-4000-8000-000000000005");
        _repository.AddRange(
        [
            new PrepaidOperator { Id = vodacomId, OperatorKey = "vodacom", Name = "Vodacom", ProductType = "airtime" },
            new PrepaidOperator { Id = mtnId, OperatorKey = "mtn", Name = "MTN", ProductType = "airtime" },
            new PrepaidOperator { Id = telkomId, OperatorKey = "telkom", Name = "Telkom", ProductType = "airtime" },
            new PrepaidOperator { Id = cellCId, OperatorKey = "cell-c", Name = "Cell C", ProductType = "airtime" },
            new PrepaidOperator { Id = electricityId, OperatorKey = "prepaid-electricity", Name = "Prepaid Electricity", ProductType = "electricity" },
        ]);
        _repository.AddRange(
        [
            Prepaid("50000000-0000-4000-8000-000000000001", vodacomId, "vodacom-airtime", "Vodacom Airtime", "airtime", 5m, 1000m),
            Prepaid("50000000-0000-4000-8000-000000000002", mtnId, "mtn-airtime", "MTN Airtime", "airtime", 5m, 1000m),
            Prepaid("50000000-0000-4000-8000-000000000003", telkomId, "telkom-airtime", "Telkom Airtime", "airtime", 5m, 1000m),
            Prepaid("50000000-0000-4000-8000-000000000004", cellCId, "cellc-airtime", "Cell C Airtime", "airtime", 5m, 1000m),
            Prepaid("50000000-0000-4000-8000-000000000005", electricityId, "electricity", "Prepaid Electricity", "electricity", 20m, 5000m),
        ]);

        await _repository.SaveChangesAsync(cancellationToken);
        _cache.RemoveByPrefix("catalogue:");
    }

    private static VoucherProduct Voucher(string id, Guid categoryId, string slug, string brand, string name, string description) =>
        new()
        {
            Id = Guid.Parse(id),
            CategoryId = categoryId,
            ProviderId = DemoVoucherProviderId,
            ExternalProductId = slug,
            Slug = slug,
            BrandName = brand,
            ProductName = name,
            Description = description,
        };

    private static PrepaidProduct Prepaid(string id, Guid operatorId, string externalId, string name, string type, decimal minimum, decimal maximum) =>
        new()
        {
            Id = Guid.Parse(id),
            OperatorId = operatorId,
            ExternalProductId = externalId,
            Name = name,
            ProductType = type,
            MinimumAmount = minimum,
            MaximumAmount = maximum,
            FeeAmount = 0m,
        };

    private static Guid DeterministicGuid(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }
}
