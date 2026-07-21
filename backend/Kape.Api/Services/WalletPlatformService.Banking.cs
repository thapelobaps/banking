using Kape.Api.Domain;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Exceptions;

namespace Kape.Api.Services;

public sealed partial class WalletPlatformService
{
    public async Task<BankLinkSessionResponseDto> CreateBankLinkSessionAsync(
        Guid userId,
        CreateBankLinkSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ProviderId, _bankProvider.ProviderId, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.ProviderId, "demo", StringComparison.OrdinalIgnoreCase))
        {
            throw Validation("providerId", $"Provider '{request.ProviderId}' is not configured.");
        }

        var session = await _bankProvider.CreateLinkSessionAsync(
            userId,
            request.InstitutionId,
            request.ReturnUrl,
            cancellationToken);

        var connection = new BankConnection
        {
            UserId = userId,
            ProviderId = _bankProvider.ProviderId,
            InstitutionId = request.InstitutionId?.Trim().ToLowerInvariant() ?? "pending",
            InstitutionName = "Pending bank selection",
            ExternalConnectionId = $"pending:{session.SessionId}",
            Status = "pending",
            ConsentExpiresAt = session.ExpiresAt,
        };

        _repository.Add(connection);
        await _repository.SaveChangesAsync(cancellationToken);
        _cache.Remove($"bank-connections:{userId:N}");

        return new BankLinkSessionResponseDto(connection.Id, session.SessionId, session.LinkUrl, session.ExpiresAt);
    }

    public async Task<BankConnectionResponseDto> CompleteBankLinkAsync(
        Guid userId,
        CompleteBankLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        var connection = await _repository.GetAsync<BankConnection>(
            item => item.Id == request.ConnectionId && item.UserId == userId && !item.IsDeleted,
            tracking: true,
            cancellationToken)
            ?? throw new NotFoundApiException("The bank connection could not be found.");

        var result = await _bankProvider.CompleteLinkAsync(
            userId,
            request.AuthorizationCode,
            request.State,
            cancellationToken);

        connection.ExternalConnectionId = result.ExternalConnectionId;
        connection.InstitutionId = result.InstitutionId;
        connection.InstitutionName = result.InstitutionName;
        connection.Status = "active";
        connection.ConsentExpiresAt = result.ConsentExpiresAt;
        connection.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        _cache.Remove($"bank-connections:{userId:N}");

        await SyncBankConnectionAsync(userId, connection.Id, cancellationToken);
        return Map(connection);
    }

    public Task<IReadOnlyList<BankConnectionResponseDto>> GetBankConnectionsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync<IReadOnlyList<BankConnectionResponseDto>>(
            $"bank-connections:{userId:N}",
            async () => (await _repository.ListAsync<BankConnection>(
                    item => item.UserId == userId && !item.IsDeleted,
                    query => query.OrderByDescending(item => item.CreatedAt),
                    cancellationToken: cancellationToken))
                .Select(Map)
                .ToArray(),
            TimeSpan.FromSeconds(30),
            cancellationToken);

    public async Task<BankConnectionResponseDto> GetBankConnectionAsync(
        Guid userId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var connection = await _repository.GetAsync<BankConnection>(
            item => item.Id == connectionId && item.UserId == userId && !item.IsDeleted,
            cancellationToken: cancellationToken)
            ?? throw new NotFoundApiException("The bank connection could not be found.");
        return Map(connection);
    }

    public async Task<BankConnectionSyncResponseDto> SyncBankConnectionAsync(
        Guid userId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var connection = await _repository.GetAsync<BankConnection>(
            item => item.Id == connectionId && item.UserId == userId && !item.IsDeleted,
            tracking: true,
            cancellationToken)
            ?? throw new NotFoundApiException("The bank connection could not be found.");

        if (connection.Status is "disconnected" or "expired")
        {
            throw new ConflictApiException("Reconnect the bank before synchronising it.");
        }

        connection.Status = "syncing";
        connection.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);

        var providerResult = await _bankProvider.SyncAsync(connection.ExternalConnectionId, cancellationToken);
        var accountMap = new Dictionary<string, LinkedBankAccount>(StringComparer.Ordinal);

        foreach (var source in providerResult.Accounts)
        {
            var account = await _repository.GetAsync<LinkedBankAccount>(
                item => item.BankConnectionId == connection.Id && item.ExternalAccountId == source.ExternalAccountId,
                tracking: true,
                cancellationToken);

            if (account is null)
            {
                account = new LinkedBankAccount
                {
                    UserId = userId,
                    BankConnectionId = connection.Id,
                    ExternalAccountId = source.ExternalAccountId,
                };
                _repository.Add(account);
            }

            account.InstitutionName = source.InstitutionName;
            account.AccountName = source.AccountName;
            account.AccountType = source.AccountType;
            account.AccountNumberMask = source.AccountNumberMask;
            account.Currency = source.Currency;
            account.CurrentBalance = source.CurrentBalance;
            account.AvailableBalance = source.AvailableBalance;
            account.IsActive = true;
            account.LastSyncedAt = DateTimeOffset.UtcNow;
            accountMap[source.ExternalAccountId] = account;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var importedTransactions = 0;
        foreach (var source in providerResult.Transactions)
        {
            if (!accountMap.TryGetValue(source.ExternalAccountId, out var account))
            {
                continue;
            }

            if (await _repository.AnyAsync<LinkedBankTransaction>(
                    item => item.LinkedBankAccountId == account.Id && item.ExternalTransactionId == source.ExternalTransactionId,
                    cancellationToken))
            {
                continue;
            }

            _repository.Add(new LinkedBankTransaction
            {
                UserId = userId,
                LinkedBankAccountId = account.Id,
                ExternalTransactionId = source.ExternalTransactionId,
                Description = source.Description,
                MerchantName = source.MerchantName,
                Amount = source.Amount,
                Direction = source.Direction,
                Category = source.Category,
                Status = source.Status,
                PostedAt = source.PostedAt,
            });
            importedTransactions++;
        }

        var importedDebitOrders = 0;
        foreach (var source in providerResult.DebitOrders)
        {
            if (!accountMap.TryGetValue(source.ExternalAccountId, out var account))
            {
                continue;
            }

            var debitOrder = await _repository.GetAsync<DebitOrder>(
                item => item.LinkedBankAccountId == account.Id && item.ExternalDebitOrderId == source.ExternalDebitOrderId,
                tracking: true,
                cancellationToken);

            if (debitOrder is null)
            {
                debitOrder = new DebitOrder
                {
                    UserId = userId,
                    LinkedBankAccountId = account.Id,
                    ExternalDebitOrderId = source.ExternalDebitOrderId,
                };
                _repository.Add(debitOrder);
                importedDebitOrders++;
            }

            debitOrder.MerchantName = source.MerchantName;
            debitOrder.Amount = source.Amount;
            debitOrder.Frequency = source.Frequency;
            debitOrder.Status = source.Status;
            debitOrder.NextRunAt = source.NextRunAt;
            debitOrder.LastRunAt = source.LastRunAt;
        }

        connection.Status = "active";
        connection.LastSyncedAt = DateTimeOffset.UtcNow;
        connection.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);

        _cache.Remove($"bank-connections:{userId:N}");
        _cache.RemoveByPrefix($"linked-accounts:{userId:N}");

        return new BankConnectionSyncResponseDto(
            connection.Id,
            connection.Status,
            accountMap.Count,
            importedTransactions,
            importedDebitOrders,
            connection.LastSyncedAt.Value);
    }

    public async Task DeleteBankConnectionAsync(
        Guid userId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var connection = await _repository.GetAsync<BankConnection>(
            item => item.Id == connectionId && item.UserId == userId && !item.IsDeleted,
            tracking: true,
            cancellationToken)
            ?? throw new NotFoundApiException("The bank connection could not be found.");

        connection.IsDeleted = true;
        connection.Status = "disconnected";
        connection.UpdatedAt = DateTimeOffset.UtcNow;

        var linkedAccounts = await _repository.ListAsync<LinkedBankAccount>(
            item => item.BankConnectionId == connection.Id && item.UserId == userId,
            cancellationToken: cancellationToken);
        foreach (var item in linkedAccounts)
        {
            var tracked = await _repository.GetAsync<LinkedBankAccount>(
                account => account.Id == item.Id,
                tracking: true,
                cancellationToken);
            if (tracked is not null)
            {
                tracked.IsActive = false;
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
        _cache.Remove($"bank-connections:{userId:N}");
        _cache.RemoveByPrefix($"linked-accounts:{userId:N}");
    }

    public Task<IReadOnlyList<LinkedAccountResponseDto>> GetLinkedAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync<IReadOnlyList<LinkedAccountResponseDto>>(
            $"linked-accounts:{userId:N}",
            async () => (await _repository.ListAsync<LinkedBankAccount>(
                    item => item.UserId == userId && item.IsActive,
                    query => query.OrderBy(item => item.InstitutionName).ThenBy(item => item.AccountName),
                    cancellationToken: cancellationToken))
                .Select(Map)
                .ToArray(),
            TimeSpan.FromSeconds(30),
            cancellationToken);

    public async Task<LinkedAccountResponseDto> GetLinkedAccountAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await GetOwnedLinkedAccountAsync(userId, accountId, cancellationToken);
        return Map(account);
    }

    public async Task<LinkedAccountBalanceResponseDto> GetLinkedAccountBalanceAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await GetOwnedLinkedAccountAsync(userId, accountId, cancellationToken);
        return new LinkedAccountBalanceResponseDto(account.Id, account.Currency, account.CurrentBalance, account.AvailableBalance, account.LastSyncedAt);
    }

    public async Task<PageResponseDto<LinkedTransactionResponseDto>> GetLinkedAccountTransactionsAsync(
        Guid userId,
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await GetOwnedLinkedAccountAsync(userId, accountId, cancellationToken);
        page = NormalisePage(page);
        pageSize = NormalisePageSize(pageSize);
        var total = await _repository.CountAsync<LinkedBankTransaction>(item => item.LinkedBankAccountId == accountId && item.UserId == userId, cancellationToken);
        var items = await _repository.ListAsync<LinkedBankTransaction>(
            item => item.LinkedBankAccountId == accountId && item.UserId == userId,
            query => query.OrderByDescending(item => item.PostedAt),
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);
        return Page(items.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<IReadOnlyList<DebitOrderResponseDto>> GetLinkedAccountDebitOrdersAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await GetOwnedLinkedAccountAsync(userId, accountId, cancellationToken);
        return (await _repository.ListAsync<DebitOrder>(
                item => item.LinkedBankAccountId == accountId && item.UserId == userId,
                query => query.OrderBy(item => item.NextRunAt),
                cancellationToken: cancellationToken))
            .Select(Map)
            .ToArray();
    }

    private async Task<LinkedBankAccount> GetOwnedLinkedAccountAsync(Guid userId, Guid accountId, CancellationToken cancellationToken) =>
        await _repository.GetAsync<LinkedBankAccount>(
            item => item.Id == accountId && item.UserId == userId && item.IsActive,
            cancellationToken: cancellationToken)
        ?? throw new NotFoundApiException("The linked bank account could not be found.");

    public async Task<PaymentMethodSetupResponseDto> CreatePaymentMethodSetupAsync(
        Guid userId,
        CreatePaymentMethodSetupRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ProviderId, _paymentProvider.ProviderId, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.ProviderId, "demo", StringComparison.OrdinalIgnoreCase))
        {
            throw Validation("providerId", $"Provider '{request.ProviderId}' is not configured.");
        }

        var session = await _paymentProvider.CreateSetupSessionAsync(userId, request.ReturnUrl, cancellationToken);
        return new PaymentMethodSetupResponseDto(session.SetupSessionId, session.ClientSecret, session.ExpiresAt);
    }

    public async Task<PaymentMethodResponseDto> ConfirmPaymentMethodAsync(
        Guid userId,
        ConfirmPaymentMethodRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Last4.Length != 4 || request.Last4.Any(character => !char.IsDigit(character)))
        {
            throw Validation("last4", "Enter the final four numeric card digits.");
        }

        if (request.ExpiryMonth is < 1 or > 12 || request.ExpiryYear < DateTimeOffset.UtcNow.Year)
        {
            throw Validation("expiry", "The card expiry date is invalid.");
        }

        var token = await _paymentProvider.ConfirmAsync(
            request.SetupSessionId,
            request.PaymentToken,
            request.Brand,
            request.BankName,
            request.Last4,
            request.ExpiryMonth,
            request.ExpiryYear,
            cancellationToken);

        var alreadyExists = await _repository.GetAsync<PaymentMethod>(
            item => item.ProviderId == token.ProviderId && item.TokenReference == token.TokenReference,
            cancellationToken: cancellationToken);
        if (alreadyExists is not null)
        {
            if (alreadyExists.UserId != userId)
            {
                throw new ConflictApiException("This tokenised payment method is already linked to another profile.");
            }

            return Map(alreadyExists);
        }

        var hasPaymentMethod = await _repository.AnyAsync<PaymentMethod>(item => item.UserId == userId, cancellationToken);
        var paymentMethod = new PaymentMethod
        {
            UserId = userId,
            ProviderId = token.ProviderId,
            TokenReference = token.TokenReference,
            Brand = token.Brand,
            BankName = token.BankName,
            Last4 = token.Last4,
            ExpiryMonth = token.ExpiryMonth,
            ExpiryYear = token.ExpiryYear,
            IsDefault = !hasPaymentMethod,
            Status = "active",
            VerifiedAt = DateTimeOffset.UtcNow,
        };

        _repository.Add(paymentMethod);
        await _repository.SaveChangesAsync(cancellationToken);
        _cache.Remove($"payment-methods:{userId:N}");
        return Map(paymentMethod);
    }

    public Task<IReadOnlyList<PaymentMethodResponseDto>> GetPaymentMethodsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync<IReadOnlyList<PaymentMethodResponseDto>>(
            $"payment-methods:{userId:N}",
            async () => (await _repository.ListAsync<PaymentMethod>(
                    item => item.UserId == userId,
                    query => query.OrderByDescending(item => item.IsDefault).ThenByDescending(item => item.CreatedAt),
                    cancellationToken: cancellationToken))
                .Select(Map)
                .ToArray(),
            TimeSpan.FromMinutes(2),
            cancellationToken);

    public async Task<PaymentMethodResponseDto> SetDefaultPaymentMethodAsync(
        Guid userId,
        Guid paymentMethodId,
        CancellationToken cancellationToken)
    {
        var target = await _repository.GetAsync<PaymentMethod>(
            item => item.Id == paymentMethodId && item.UserId == userId,
            tracking: true,
            cancellationToken)
            ?? throw new NotFoundApiException("The payment method could not be found.");

        if (target.Status != "active")
        {
            throw new ConflictApiException("Only an active payment method can be made default.");
        }

        var current = await _repository.GetAsync<PaymentMethod>(
            item => item.UserId == userId && item.IsDefault && item.Id != paymentMethodId,
            tracking: true,
            cancellationToken);
        if (current is not null)
        {
            current.IsDefault = false;
        }

        target.IsDefault = true;
        await _repository.SaveChangesAsync(cancellationToken);
        _cache.Remove($"payment-methods:{userId:N}");
        return Map(target);
    }

    public async Task DeletePaymentMethodAsync(
        Guid userId,
        Guid paymentMethodId,
        CancellationToken cancellationToken)
    {
        var target = await _repository.GetAsync<PaymentMethod>(
            item => item.Id == paymentMethodId && item.UserId == userId,
            tracking: true,
            cancellationToken)
            ?? throw new NotFoundApiException("The payment method could not be found.");

        var wasDefault = target.IsDefault;
        _repository.Remove(target);
        await _repository.SaveChangesAsync(cancellationToken);

        if (wasDefault)
        {
            var replacement = (await _repository.ListAsync<PaymentMethod>(
                    item => item.UserId == userId && item.Status == "active",
                    query => query.OrderByDescending(item => item.CreatedAt),
                    take: 1,
                    cancellationToken: cancellationToken))
                .FirstOrDefault();
            if (replacement is not null)
            {
                var trackedReplacement = await _repository.GetAsync<PaymentMethod>(
                    item => item.Id == replacement.Id,
                    tracking: true,
                    cancellationToken);
                if (trackedReplacement is not null)
                {
                    trackedReplacement.IsDefault = true;
                    await _repository.SaveChangesAsync(cancellationToken);
                }
            }
        }

        _cache.Remove($"payment-methods:{userId:N}");
    }
}
