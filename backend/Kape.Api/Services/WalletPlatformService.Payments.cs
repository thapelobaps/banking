using System.Text.Json;
using Kape.Api.Domain;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Exceptions;

namespace Kape.Api.Services;

public sealed partial class WalletPlatformService
{
    public async Task<ResolvedKapeUserResponseDto> ResolveKapeUserAsync(
        Guid currentUserId,
        ResolveKapeUserRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier))
        {
            throw Validation("identifier", "Enter an email address, mobile number, or Kape user ID.");
        }

        var user = await _repository.ResolveUserAsync(request.Identifier, cancellationToken)
            ?? throw new NotFoundApiException("The Kape user could not be found.");
        if (user.Id == currentUserId)
        {
            throw Validation("identifier", "Choose another Kape user.");
        }

        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return new ResolvedKapeUserResponseDto(user.Id, displayName, MaskIdentifier(request.Identifier));
    }

    public async Task<PaymentRequestResponseDto> CreatePaymentRequestAsync(
        Guid userId,
        CreatePaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidatePositiveAmount(request.Amount);
        Guid? payerUserId = null;
        if (!string.IsNullOrWhiteSpace(request.PayerIdentifier))
        {
            var payer = await ResolveKapeUserAsync(
                userId,
                new ResolveKapeUserRequestDto(request.PayerIdentifier),
                cancellationToken);
            payerUserId = payer.UserId;
        }

        var expiresAt = request.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7);
        if (expiresAt <= DateTimeOffset.UtcNow || expiresAt > DateTimeOffset.UtcNow.AddDays(30))
        {
            throw Validation("expiresAt", "The request expiry must be within the next 30 days.");
        }

        var paymentRequest = new PaymentRequest
        {
            PayeeUserId = userId,
            PayerUserId = payerUserId,
            Amount = request.Amount,
            Message = NormaliseReference(request.Message, "Kape payment request"),
            ExpiresAt = expiresAt,
        };
        _repository.Add(paymentRequest);
        await _repository.SaveChangesAsync(cancellationToken);
        _cache.RemoveByPrefix($"payment-requests:{userId:N}");
        if (payerUserId is not null)
        {
            _cache.RemoveByPrefix($"payment-requests:{payerUserId.Value:N}");
        }

        return Map(paymentRequest);
    }

    public Task<IReadOnlyList<PaymentRequestResponseDto>> GetPaymentRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync<IReadOnlyList<PaymentRequestResponseDto>>(
            $"payment-requests:{userId:N}",
            async () => (await _repository.ListAsync<PaymentRequest>(
                    item => item.PayeeUserId == userId || item.PayerUserId == userId,
                    query => query.OrderByDescending(item => item.CreatedAt),
                    cancellationToken: cancellationToken))
                .Select(Map)
                .ToArray(),
            TimeSpan.FromSeconds(20),
            cancellationToken);

    public async Task<PaymentRequestResponseDto> PayPaymentRequestAsync(
        Guid userId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var paymentRequest = await _repository.GetAsync<PaymentRequest>(
            item => item.Id == requestId &&
                    item.Status == "pending" &&
                    (item.PayerUserId == null || item.PayerUserId == userId),
            tracking: true,
            cancellationToken)
            ?? throw new NotFoundApiException("The pending payment request could not be found.");

        if (paymentRequest.PayeeUserId == userId)
        {
            throw new ConflictApiException("You cannot pay your own payment request.");
        }

        if (paymentRequest.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            paymentRequest.Status = "expired";
            paymentRequest.RespondedAt = DateTimeOffset.UtcNow;
            await _repository.SaveChangesAsync(cancellationToken);
            throw new ConflictApiException("The payment request has expired.");
        }

        var transfer = await CreateTransferAsync(
            userId,
            new WalletTransferRequestDto(
                paymentRequest.PayeeUserId,
                paymentRequest.Amount,
                paymentRequest.Message,
                $"payment-request:{paymentRequest.Id:N}"),
            cancellationToken);

        paymentRequest.PayerUserId = userId;
        paymentRequest.Status = "paid";
        paymentRequest.WalletTransactionId = transfer.Id;
        paymentRequest.RespondedAt = DateTimeOffset.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        _cache.RemoveByPrefix($"payment-requests:{userId:N}");
        _cache.RemoveByPrefix($"payment-requests:{paymentRequest.PayeeUserId:N}");
        return Map(paymentRequest);
    }

    public async Task<PaymentRequestResponseDto> DeclinePaymentRequestAsync(
        Guid userId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var paymentRequest = await _repository.GetAsync<PaymentRequest>(
            item => item.Id == requestId && item.PayerUserId == userId && item.Status == "pending",
            tracking: true,
            cancellationToken)
            ?? throw new NotFoundApiException("The pending payment request could not be found.");

        paymentRequest.Status = "declined";
        paymentRequest.RespondedAt = DateTimeOffset.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        _cache.RemoveByPrefix($"payment-requests:{userId:N}");
        _cache.RemoveByPrefix($"payment-requests:{paymentRequest.PayeeUserId:N}");
        return Map(paymentRequest);
    }

    public async Task<WebhookAcceptedResponseDto> AcceptWebhookAsync(
        string providerType,
        ProviderWebhookRequestDto request,
        string signature,
        CancellationToken cancellationToken)
    {
        var payload = request.Payload.GetRawText();
        if (!_webhookSignatureValidator.IsValid(providerType, payload, signature))
        {
            throw new UnauthorizedApiException("The webhook signature is invalid.");
        }

        var existing = await _repository.GetAsync<WebhookInbox>(
            item => item.ProviderType == providerType && item.ExternalEventId == request.EventId,
            cancellationToken: cancellationToken);
        if (existing is not null)
        {
            return new WebhookAcceptedResponseDto(existing.Id, existing.ProviderType, existing.ExternalEventId, existing.Status, existing.ReceivedAt);
        }

        var inbox = new WebhookInbox
        {
            ProviderType = providerType,
            ExternalEventId = request.EventId,
            EventType = request.EventType,
            Payload = payload,
            Signature = signature,
        };
        _repository.Add(inbox);
        await _repository.SaveChangesAsync(cancellationToken);
        await _queue.EnqueueAsync(QueueName, "webhook", new WebhookQueuePayload(inbox.Id), cancellationToken: cancellationToken);
        return new WebhookAcceptedResponseDto(inbox.Id, inbox.ProviderType, inbox.ExternalEventId, inbox.Status, inbox.ReceivedAt);
    }

    public async Task ProcessQueueMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        switch (message.MessageType)
        {
            case "bank-sync":
            {
                var payload = JsonSerializer.Deserialize<BankSyncQueuePayload>(message.Payload, SerializerOptions)
                    ?? throw new InvalidOperationException("The bank-sync queue payload is invalid.");
                await SyncBankConnectionAsync(payload.UserId, payload.ConnectionId, cancellationToken);
                break;
            }
            case "voucher-fulfil":
            {
                var payload = JsonSerializer.Deserialize<VoucherFulfilQueuePayload>(message.Payload, SerializerOptions)
                    ?? throw new InvalidOperationException("The voucher fulfilment payload is invalid.");
                await FulfilVoucherOrderAsync(payload.UserId, payload.OrderId, cancellationToken);
                break;
            }
            case "prepaid-fulfil":
            {
                var payload = JsonSerializer.Deserialize<PrepaidFulfilQueuePayload>(message.Payload, SerializerOptions)
                    ?? throw new InvalidOperationException("The prepaid fulfilment payload is invalid.");
                await FulfilPrepaidOrderAsync(payload.UserId, payload.OrderId, cancellationToken);
                break;
            }
            case "webhook":
            {
                var payload = JsonSerializer.Deserialize<WebhookQueuePayload>(message.Payload, SerializerOptions)
                    ?? throw new InvalidOperationException("The webhook queue payload is invalid.");
                await ProcessWebhookAsync(payload.InboxId, cancellationToken);
                break;
            }
            default:
                throw new InvalidOperationException($"Queue message type '{message.MessageType}' is not supported.");
        }
    }

    private async Task FulfilVoucherOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _repository.GetAsync<VoucherOrder>(
            item => item.Id == orderId && item.UserId == userId,
            tracking: true,
            cancellationToken)
            ?? throw new InvalidOperationException("The voucher order no longer exists.");
        if (order.Status == "fulfilled")
        {
            return;
        }

        var product = await _repository.GetAsync<VoucherProduct>(item => item.Id == order.VoucherProductId, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The voucher product no longer exists.");
        order.Status = "processing";
        await _repository.SaveChangesAsync(cancellationToken);

        var result = await _digitalProvider.FulfilVoucherAsync(
            product.ExternalProductId,
            order.Amount,
            order.Id.ToString("N"),
            cancellationToken);
        order.ExternalOrderId = result.ExternalOrderId;
        order.EncryptedVoucherCode = _voucherCodeProtector.Protect(result.FulfilmentReference);
        order.Status = "fulfilled";
        order.FulfilledAt = result.FulfilledAt;
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task FulfilPrepaidOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _repository.GetAsync<PrepaidOrder>(
            item => item.Id == orderId && item.UserId == userId,
            tracking: true,
            cancellationToken)
            ?? throw new InvalidOperationException("The prepaid order no longer exists.");
        if (order.Status == "fulfilled")
        {
            return;
        }

        var product = await _repository.GetAsync<PrepaidProduct>(item => item.Id == order.PrepaidProductId, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The prepaid product no longer exists.");
        order.Status = "processing";
        await _repository.SaveChangesAsync(cancellationToken);

        var result = await _digitalProvider.FulfilPrepaidAsync(
            product.ExternalProductId,
            order.Recipient,
            order.Amount,
            order.Id.ToString("N"),
            cancellationToken);
        order.ExternalOrderId = result.ExternalOrderId;
        order.FulfilmentReference = result.FulfilmentReference;
        order.Status = "fulfilled";
        order.FulfilledAt = result.FulfilledAt;
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessWebhookAsync(Guid inboxId, CancellationToken cancellationToken)
    {
        var inbox = await _repository.GetAsync<WebhookInbox>(
            item => item.Id == inboxId,
            tracking: true,
            cancellationToken)
            ?? throw new InvalidOperationException("The webhook inbox item no longer exists.");
        if (inbox.Status == "processed")
        {
            return;
        }

        inbox.Status = "processed";
        inbox.ProcessedAt = DateTimeOffset.UtcNow;
        inbox.LastError = null;
        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Processed {ProviderType} webhook {EventId} ({EventType})",
            inbox.ProviderType,
            inbox.ExternalEventId,
            inbox.EventType);
    }

    private static string MaskIdentifier(string identifier)
    {
        var value = identifier.Trim();
        var at = value.IndexOf('@');
        if (at > 1)
        {
            return $"{value[..2]}***{value[at..]}";
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length >= 4)
        {
            return $"******{digits[^4..]}";
        }

        return "Kape user";
    }
}
