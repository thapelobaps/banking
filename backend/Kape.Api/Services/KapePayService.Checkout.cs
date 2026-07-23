using System.Data;
using Kape.Api.Domain;
using Kape.Api.DTOs.Payments;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed partial class KapePayService
{
    public async Task<KapePayQuoteResponseDto> QuoteVoucherAsync(
        Guid userId,
        KapePayVoucherQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        var source = NormalisePaymentSource(request.PaymentSource);
        var product = await GetVoucherProductAsync(request.VoucherProductId, cancellationToken);
        var denomination = await GetVoucherDenominationAsync(product.Id, request.VoucherDenominationId, cancellationToken);

        if (source == "wallet")
        {
            var walletQuote = await _walletPlatformService.QuoteVoucherAsync(
                userId,
                new VoucherQuoteRequestDto(product.Id, denomination.Id),
                cancellationToken);
            return new KapePayQuoteResponseDto(
                "voucher",
                source,
                walletQuote.Amount,
                walletQuote.FeeAmount,
                walletQuote.TotalAmount,
                walletQuote.Currency,
                null,
                walletQuote.Status,
                walletQuote.ExpiresAt,
                DemoDisclaimer);
        }

        var account = await GetOwnedLinkedBankAccountAsync(userId, request.LinkedBankAccountId, cancellationToken);
        return new KapePayQuoteResponseDto(
            "voucher",
            source,
            denomination.Amount,
            denomination.FeeAmount,
            denomination.Amount + denomination.FeeAmount,
            "ZAR",
            account.Id,
            "quoted",
            DateTimeOffset.UtcNow.AddMinutes(5),
            DemoDisclaimer);
    }

    public async Task<KapePayVoucherCheckoutResponseDto> CreateVoucherOrderAsync(
        Guid userId,
        CreateKapePayVoucherOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var source = NormalisePaymentSource(request.PaymentSource);
        var idempotencyKey = RequireIdempotencyKey(request.IdempotencyKey);
        var existing = await FindIdempotentPaymentAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await GetVoucherCheckoutAsync(existing, cancellationToken);
        }

        if (source == "wallet")
        {
            return await CreateWalletVoucherOrderAsync(userId, request, idempotencyKey, cancellationToken);
        }

        return await CreateLinkedBankVoucherOrderAsync(userId, request, idempotencyKey, cancellationToken);
    }

    public async Task<KapePayQuoteResponseDto> QuotePrepaidAsync(
        Guid userId,
        KapePayPrepaidQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        var source = NormalisePaymentSource(request.PaymentSource);
        var product = await GetPrepaidProductAsync(request.ProductId, cancellationToken);
        var recipient = ValidatePrepaidRecipient(product, request.Recipient);
        ValidatePrepaidAmount(product, request.Amount);

        if (source == "wallet")
        {
            var walletQuote = await _walletPlatformService.QuotePrepaidAsync(
                userId,
                new PrepaidQuoteRequestDto(product.Id, recipient, request.Amount),
                cancellationToken);
            return new KapePayQuoteResponseDto(
                "prepaid",
                source,
                walletQuote.Amount,
                walletQuote.FeeAmount,
                walletQuote.TotalAmount,
                walletQuote.Currency,
                null,
                walletQuote.Status,
                walletQuote.ExpiresAt,
                DemoDisclaimer);
        }

        var account = await GetOwnedLinkedBankAccountAsync(userId, request.LinkedBankAccountId, cancellationToken);
        return new KapePayQuoteResponseDto(
            "prepaid",
            source,
            request.Amount,
            product.FeeAmount,
            request.Amount + product.FeeAmount,
            "ZAR",
            account.Id,
            "quoted",
            DateTimeOffset.UtcNow.AddMinutes(5),
            DemoDisclaimer);
    }

    public async Task<KapePayPrepaidCheckoutResponseDto> CreatePrepaidOrderAsync(
        Guid userId,
        CreateKapePayPrepaidOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var source = NormalisePaymentSource(request.PaymentSource);
        var idempotencyKey = RequireIdempotencyKey(request.IdempotencyKey);
        var existing = await FindIdempotentPaymentAsync(userId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await GetPrepaidCheckoutAsync(existing, cancellationToken);
        }

        if (source == "wallet")
        {
            return await CreateWalletPrepaidOrderAsync(userId, request, idempotencyKey, cancellationToken);
        }

        return await CreateLinkedBankPrepaidOrderAsync(userId, request, idempotencyKey, cancellationToken);
    }

    private async Task<KapePayVoucherCheckoutResponseDto> CreateWalletVoucherOrderAsync(
        Guid userId,
        CreateKapePayVoucherOrderRequestDto request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var orderResponse = await _walletPlatformService.CreateVoucherOrderAsync(
            userId,
            new CreateVoucherOrderRequestDto(
                request.VoucherProductId,
                request.VoucherDenominationId,
                idempotencyKey),
            cancellationToken);
        var order = await _repository.GetAsync<VoucherOrder>(
            item => item.Id == orderResponse.Id && item.UserId == userId,
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The wallet voucher order was not persisted.");

        var payment = await FindIdempotentPaymentAsync(userId, idempotencyKey, cancellationToken);
        if (payment is null)
        {
            payment = new PaymentAttempt
            {
                UserId = userId,
                VoucherOrderId = order.Id,
                WalletId = order.WalletId,
                WalletTransactionId = order.WalletTransactionId,
                ProviderId = DemoWalletProviderId,
                PaymentSource = "wallet",
                ExternalPaymentId = order.WalletTransactionId is null
                    ? $"wallet_order_{order.Id:N}"
                    : $"wallet_{order.WalletTransactionId.Value:N}",
                Amount = order.Amount,
                FeeAmount = order.FeeAmount,
                Status = "completed",
                Scenario = "success",
                Reference = $"Voucher order {order.Id:N}",
                IdempotencyKey = idempotencyKey,
                CompletedAt = order.CreatedAt,
            };
            _repository.Add(payment);
            _repository.Add(new PaymentStatusHistory
            {
                PaymentAttemptId = payment.Id,
                Status = "completed",
                Source = "wallet",
                Reason = "Synthetic Kape Wallet debit completed.",
            });
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return new KapePayVoucherCheckoutResponseDto(
            MapVoucherOrder(order),
            await MapPaymentAsync(payment, cancellationToken));
    }

    private async Task<KapePayPrepaidCheckoutResponseDto> CreateWalletPrepaidOrderAsync(
        Guid userId,
        CreateKapePayPrepaidOrderRequestDto request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var product = await GetPrepaidProductAsync(request.ProductId, cancellationToken);
        var recipient = ValidatePrepaidRecipient(product, request.Recipient);
        ValidatePrepaidAmount(product, request.Amount);
        var orderResponse = await _walletPlatformService.CreatePrepaidOrderAsync(
            userId,
            new CreatePrepaidOrderRequestDto(product.Id, recipient, request.Amount, idempotencyKey),
            cancellationToken);
        var order = await _repository.GetAsync<PrepaidOrder>(
            item => item.Id == orderResponse.Id && item.UserId == userId,
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The wallet prepaid order was not persisted.");

        var payment = await FindIdempotentPaymentAsync(userId, idempotencyKey, cancellationToken);
        if (payment is null)
        {
            payment = new PaymentAttempt
            {
                UserId = userId,
                PrepaidOrderId = order.Id,
                WalletId = order.WalletId,
                WalletTransactionId = order.WalletTransactionId,
                ProviderId = DemoWalletProviderId,
                PaymentSource = "wallet",
                ExternalPaymentId = order.WalletTransactionId is null
                    ? $"wallet_order_{order.Id:N}"
                    : $"wallet_{order.WalletTransactionId.Value:N}",
                Amount = order.Amount,
                FeeAmount = order.FeeAmount,
                Status = "completed",
                Scenario = "success",
                Reference = $"Prepaid order {order.Id:N}",
                IdempotencyKey = idempotencyKey,
                CompletedAt = order.CreatedAt,
            };
            _repository.Add(payment);
            _repository.Add(new PaymentStatusHistory
            {
                PaymentAttemptId = payment.Id,
                Status = "completed",
                Source = "wallet",
                Reason = "Synthetic Kape Wallet debit completed.",
            });
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return new KapePayPrepaidCheckoutResponseDto(
            MapPrepaidOrder(order),
            await MapPaymentAsync(payment, cancellationToken));
    }

    private async Task<KapePayVoucherCheckoutResponseDto> CreateLinkedBankVoucherOrderAsync(
        Guid userId,
        CreateKapePayVoucherOrderRequestDto request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var product = await GetVoucherProductAsync(request.VoucherProductId, cancellationToken);
        var denomination = await GetVoucherDenominationAsync(product.Id, request.VoucherDenominationId, cancellationToken);
        var account = await GetOwnedLinkedBankAccountAsync(userId, request.LinkedBankAccountId, cancellationToken);
        var wallet = await _walletPlatformService.GetWalletAsync(userId, cancellationToken);

        var order = new VoucherOrder
        {
            UserId = userId,
            WalletId = wallet.Id,
            VoucherProductId = product.Id,
            Amount = denomination.Amount,
            FeeAmount = denomination.FeeAmount,
            Status = "created",
            IdempotencyKey = idempotencyKey,
        };
        var payment = CreatePendingProviderPayment(
            userId,
            order.Id,
            null,
            wallet.Id,
            account.Id,
            order.Amount,
            order.FeeAmount,
            idempotencyKey,
            request.Scenario,
            $"Voucher order {order.Id:N}");
        order.Status = payment.Status;

        await using (var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
        {
            var duplicate = await FindIdempotentPaymentAsync(userId, idempotencyKey, cancellationToken);
            if (duplicate is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return await GetVoucherCheckoutAsync(duplicate, cancellationToken);
            }

            _repository.Add(order);
            _repository.Add(payment);
            _repository.Add(new PaymentStatusHistory
            {
                PaymentAttemptId = payment.Id,
                Status = "created",
                Source = "system",
                Reason = "Direct linked-bank payment attempt created.",
            });
            await _repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        var session = await _payInProvider.CreatePaymentAsync(
            new PayInProviderRequest(
                userId,
                account.Id,
                "voucher",
                order.Id,
                payment.Amount + payment.FeeAmount,
                payment.Currency,
                payment.Reference,
                payment.Scenario,
                idempotencyKey,
                request.ReturnUrl),
            cancellationToken);
        await ApplyProviderSessionAsync(payment, session, "provider", null, cancellationToken);

        order = await _repository.GetAsync<VoucherOrder>(
            item => item.Id == order.Id,
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The voucher order no longer exists.");
        payment = await GetOwnedPaymentAsync(userId, payment.Id, false, cancellationToken);
        return new KapePayVoucherCheckoutResponseDto(
            MapVoucherOrder(order),
            await MapPaymentAsync(payment, cancellationToken));
    }

    private async Task<KapePayPrepaidCheckoutResponseDto> CreateLinkedBankPrepaidOrderAsync(
        Guid userId,
        CreateKapePayPrepaidOrderRequestDto request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var product = await GetPrepaidProductAsync(request.ProductId, cancellationToken);
        var recipient = ValidatePrepaidRecipient(product, request.Recipient);
        ValidatePrepaidAmount(product, request.Amount);
        var account = await GetOwnedLinkedBankAccountAsync(userId, request.LinkedBankAccountId, cancellationToken);
        var wallet = await _walletPlatformService.GetWalletAsync(userId, cancellationToken);

        var order = new PrepaidOrder
        {
            UserId = userId,
            WalletId = wallet.Id,
            PrepaidProductId = product.Id,
            Recipient = recipient,
            Amount = request.Amount,
            FeeAmount = product.FeeAmount,
            Status = "created",
            IdempotencyKey = idempotencyKey,
        };
        var payment = CreatePendingProviderPayment(
            userId,
            null,
            order.Id,
            wallet.Id,
            account.Id,
            order.Amount,
            order.FeeAmount,
            idempotencyKey,
            request.Scenario,
            $"Prepaid order {order.Id:N}");
        order.Status = payment.Status;

        await using (var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
        {
            var duplicate = await FindIdempotentPaymentAsync(userId, idempotencyKey, cancellationToken);
            if (duplicate is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return await GetPrepaidCheckoutAsync(duplicate, cancellationToken);
            }

            _repository.Add(order);
            _repository.Add(payment);
            _repository.Add(new PaymentStatusHistory
            {
                PaymentAttemptId = payment.Id,
                Status = "created",
                Source = "system",
                Reason = "Direct linked-bank payment attempt created.",
            });
            await _repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        var session = await _payInProvider.CreatePaymentAsync(
            new PayInProviderRequest(
                userId,
                account.Id,
                "prepaid",
                order.Id,
                payment.Amount + payment.FeeAmount,
                payment.Currency,
                payment.Reference,
                payment.Scenario,
                idempotencyKey,
                request.ReturnUrl),
            cancellationToken);
        await ApplyProviderSessionAsync(payment, session, "provider", null, cancellationToken);

        order = await _repository.GetAsync<PrepaidOrder>(
            item => item.Id == order.Id,
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The prepaid order no longer exists.");
        payment = await GetOwnedPaymentAsync(userId, payment.Id, false, cancellationToken);
        return new KapePayPrepaidCheckoutResponseDto(
            MapPrepaidOrder(order),
            await MapPaymentAsync(payment, cancellationToken));
    }

    private PaymentAttempt CreatePendingProviderPayment(
        Guid userId,
        Guid? voucherOrderId,
        Guid? prepaidOrderId,
        Guid walletId,
        Guid linkedBankAccountId,
        decimal amount,
        decimal feeAmount,
        string idempotencyKey,
        string? scenario,
        string reference)
    {
        var payment = new PaymentAttempt
        {
            UserId = userId,
            VoucherOrderId = voucherOrderId,
            PrepaidOrderId = prepaidOrderId,
            LinkedBankAccountId = linkedBankAccountId,
            WalletId = walletId,
            ProviderId = _payInProvider.ProviderId,
            PaymentSource = "linked_bank",
            Amount = amount,
            FeeAmount = feeAmount,
            Status = "created",
            Scenario = string.IsNullOrWhiteSpace(scenario) ? "awaiting_approval" : scenario.Trim().ToLowerInvariant(),
            Reference = reference,
            IdempotencyKey = idempotencyKey,
        };
        payment.ExternalPaymentId = $"pending_{payment.Id:N}";
        return payment;
    }

    private async Task<KapePayVoucherCheckoutResponseDto> GetVoucherCheckoutAsync(
        PaymentAttempt payment,
        CancellationToken cancellationToken)
    {
        if (payment.VoucherOrderId is null)
        {
            throw Validation("idempotencyKey", "The idempotency key belongs to another payment type.");
        }

        var order = await _repository.GetAsync<VoucherOrder>(
            item => item.Id == payment.VoucherOrderId,
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The voucher order no longer exists.");
        return new KapePayVoucherCheckoutResponseDto(
            MapVoucherOrder(order),
            await MapPaymentAsync(payment, cancellationToken));
    }

    private async Task<KapePayPrepaidCheckoutResponseDto> GetPrepaidCheckoutAsync(
        PaymentAttempt payment,
        CancellationToken cancellationToken)
    {
        if (payment.PrepaidOrderId is null)
        {
            throw Validation("idempotencyKey", "The idempotency key belongs to another payment type.");
        }

        var order = await _repository.GetAsync<PrepaidOrder>(
            item => item.Id == payment.PrepaidOrderId,
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The prepaid order no longer exists.");
        return new KapePayPrepaidCheckoutResponseDto(
            MapPrepaidOrder(order),
            await MapPaymentAsync(payment, cancellationToken));
    }
}
