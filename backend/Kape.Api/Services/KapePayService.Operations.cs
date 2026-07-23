using System.Text.Json;
using Kape.Api.Domain;
using Kape.Api.DTOs.Payments;
using Kape.Api.DTOs.WalletPlatform;
using Kape.Api.Exceptions;
using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed partial class KapePayService
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public async Task<PaymentAttemptResponseDto> GetPaymentAsync(
        Guid userId,
        Guid paymentAttemptId,
        CancellationToken cancellationToken)
    {
        var payment = await GetOwnedPaymentAsync(userId, paymentAttemptId, false, cancellationToken);
        return await MapPaymentAsync(payment, cancellationToken);
    }

    public async Task<PageResponseDto<PaymentAttemptResponseDto>> GetPaymentsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = NormalisePage(page);
        pageSize = NormalisePageSize(pageSize);
        var total = await _repository.CountAsync<PaymentAttempt>(
            item => item.UserId == userId,
            cancellationToken);
        var payments = await _repository.ListAsync<PaymentAttempt>(
            item => item.UserId == userId,
            query => query.OrderByDescending(item => item.CreatedAt),
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);
        var mapped = new List<PaymentAttemptResponseDto>(payments.Count);
        foreach (var payment in payments)
        {
            mapped.Add(await MapPaymentAsync(payment, cancellationToken));
        }

        return Page(mapped, page, pageSize, total);
    }

    public async Task<PaymentAttemptResponseDto> RefreshPaymentAsync(
        Guid userId,
        Guid paymentAttemptId,
        CancellationToken cancellationToken)
    {
        var payment = await GetOwnedPaymentAsync(userId, paymentAttemptId, false, cancellationToken);
        if (payment.PaymentSource == "wallet")
        {
            return await MapPaymentAsync(payment, cancellationToken);
        }

        var session = await _payInProvider.GetPaymentAsync(payment.ExternalPaymentId, cancellationToken);
        await ApplyProviderSessionAsync(payment, session, "status_poll", null, cancellationToken);
        payment = await GetOwnedPaymentAsync(userId, paymentAttemptId, false, cancellationToken);
        return await MapPaymentAsync(payment, cancellationToken);
    }

    public async Task<WebhookAcceptedResponseDto> AcceptPaymentWebhookAsync(
        PaymentProviderWebhookRequestDto request,
        string signature,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EventId) ||
            string.IsNullOrWhiteSpace(request.ExternalPaymentId) ||
            string.IsNullOrWhiteSpace(request.Status))
        {
            throw Validation("webhook", "Event ID, external payment ID and status are required.");
        }

        var signedPayload = request.Payload.GetRawText();
        if (!_webhookSignatureValidator.IsValid(_payInProvider.ProviderId, signedPayload, signature))
        {
            throw new UnauthorizedApiException("The payment webhook signature is invalid.");
        }

        var existing = await _repository.GetAsync<WebhookInbox>(
            item => item.ProviderType == _payInProvider.ProviderId &&
                    item.ExternalEventId == request.EventId,
            cancellationToken: cancellationToken);
        if (existing is not null)
        {
            return new WebhookAcceptedResponseDto(
                existing.Id,
                existing.ProviderType,
                existing.ExternalEventId,
                existing.Status,
                existing.ReceivedAt);
        }

        var inbox = new WebhookInbox
        {
            ProviderType = _payInProvider.ProviderId,
            ExternalEventId = request.EventId.Trim(),
            EventType = request.EventType.Trim(),
            Payload = JsonSerializer.Serialize(request, WebJson),
            Signature = signature.Trim(),
        };
        _repository.Add(inbox);
        await _repository.SaveChangesAsync(cancellationToken);
        await _queue.EnqueueAsync(
            PaymentQueueName,
            "payment-webhook",
            new PaymentWebhookQueuePayload(inbox.Id),
            cancellationToken: cancellationToken);

        return new WebhookAcceptedResponseDto(
            inbox.Id,
            inbox.ProviderType,
            inbox.ExternalEventId,
            inbox.Status,
            inbox.ReceivedAt);
    }

    public async Task ProcessQueueMessageAsync(
        QueueMessage message,
        CancellationToken cancellationToken)
    {
        if (message.MessageType != "payment-webhook")
        {
            throw new InvalidOperationException($"Kape Pay queue message type '{message.MessageType}' is not supported.");
        }

        var payload = JsonSerializer.Deserialize<PaymentWebhookQueuePayload>(message.Payload, WebJson)
            ?? throw new InvalidOperationException("The payment webhook queue payload is invalid.");
        await ProcessPaymentWebhookAsync(payload.InboxId, cancellationToken);
    }

    public async Task<PaymentRefundResponseDto> RefundAsync(
        Guid userId,
        Guid paymentAttemptId,
        CreatePaymentRefundRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidatePositiveAmount(request.Amount);
        var idempotencyKey = RequireIdempotencyKey(request.IdempotencyKey);
        var payment = await GetOwnedPaymentAsync(userId, paymentAttemptId, true, cancellationToken);
        if (payment.PaymentSource == "wallet")
        {
            throw new ConflictApiException(
                "Synthetic wallet refunds require a ledger reversal and must use the wallet reversal workflow.");
        }

        if (payment.Status is not ("completed" or "refunded"))
        {
            throw new ConflictApiException("Only a completed direct-bank payment can be refunded.");
        }

        if (request.Amount > payment.Amount + payment.FeeAmount)
        {
            throw Validation("amount", "The refund cannot exceed the completed payment total.");
        }

        var existing = await _repository.GetAsync<PaymentRefund>(
            item => item.PaymentAttemptId == payment.Id && item.IdempotencyKey == idempotencyKey,
            cancellationToken: cancellationToken);
        if (existing is not null)
        {
            return MapRefund(existing);
        }

        var providerRefund = await _payInProvider.RefundAsync(
            new PayInProviderRefundRequest(
                payment.ExternalPaymentId,
                request.Amount,
                payment.Currency,
                request.Reason,
                idempotencyKey),
            cancellationToken);
        var refund = new PaymentRefund
        {
            UserId = userId,
            PaymentAttemptId = payment.Id,
            ProviderId = providerRefund.ProviderId,
            ExternalRefundId = providerRefund.ExternalRefundId,
            Amount = providerRefund.Amount,
            Currency = providerRefund.Currency,
            Status = providerRefund.Status,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Kape Pay refund" : request.Reason.Trim(),
            IdempotencyKey = idempotencyKey,
            CreatedAt = providerRefund.CreatedAt,
            CompletedAt = providerRefund.Status == "completed" ? providerRefund.CreatedAt : null,
        };

        var previousStatus = payment.Status;
        payment.Status = providerRefund.Status == "completed" ? "refunded" : "refund_pending";
        payment.UpdatedAt = DateTimeOffset.UtcNow;
        payment.CompletedAt = providerRefund.Status == "completed" ? providerRefund.CreatedAt : payment.CompletedAt;
        _repository.Add(refund);
        _repository.Add(new PaymentStatusHistory
        {
            PaymentAttemptId = payment.Id,
            PreviousStatus = previousStatus,
            Status = payment.Status,
            Source = "refund",
            Reason = refund.Reason,
            ExternalEventId = providerRefund.ExternalRefundId,
        });
        await SetOrderStatusAsync(payment, payment.Status, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return MapRefund(refund);
    }

    public async Task<PaymentReconciliationResponseDto> ReconcileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var payments = await _repository.ListAsync<PaymentAttempt>(
            item => item.UserId == userId,
            query => query.OrderBy(item => item.CreatedAt),
            cancellationToken: cancellationToken);
        var run = new PaymentReconciliationRun
        {
            UserId = userId,
            CheckedPayments = payments.Count,
            StartedAt = startedAt,
        };
        var issues = new List<PaymentReconciliationIssue>();
        var paymentsWithIssues = new HashSet<Guid>();

        foreach (var payment in payments)
        {
            var orderStatus = await GetOrderStatusAsync(payment, cancellationToken);
            void AddIssue(string issueType, string description, string severity = "warning")
            {
                paymentsWithIssues.Add(payment.Id);
                issues.Add(new PaymentReconciliationIssue
                {
                    ReconciliationRunId = run.Id,
                    PaymentAttemptId = payment.Id,
                    IssueType = issueType,
                    Description = description,
                    Severity = severity,
                });
            }

            if ((payment.VoucherOrderId is null) == (payment.PrepaidOrderId is null))
            {
                AddIssue("invalid_order_link", "The payment must be linked to exactly one voucher or prepaid order.", "critical");
            }

            if (payment.PaymentSource == "linked_bank" && payment.LinkedBankAccountId is null)
            {
                AddIssue("missing_bank_source", "A linked-bank payment has no linked bank account.", "critical");
            }

            if (payment.PaymentSource == "wallet" && payment.WalletTransactionId is null)
            {
                AddIssue("missing_wallet_transaction", "A wallet payment has no wallet transaction.", "critical");
            }

            if (payment.Status == "completed" && orderStatus is "failed" or "cancelled" or "expired")
            {
                AddIssue("paid_order_not_fulfillable", $"Payment is completed while the order status is '{orderStatus}'.", "critical");
            }

            if (payment.Status is "failed" or "cancelled" &&
                orderStatus is "payment_completed" or "processing" or "fulfilled")
            {
                AddIssue("unpaid_order_fulfilled", $"Order status '{orderStatus}' is not compatible with payment status '{payment.Status}'.", "critical");
            }

            if (orderStatus == "fulfilled" && payment.Status is not ("completed" or "refunded"))
            {
                AddIssue("fulfilment_without_payment", "The order was fulfilled without a completed payment.", "critical");
            }
        }

        run.MatchedPayments = payments.Count - paymentsWithIssues.Count;
        run.IssueCount = issues.Count;
        run.Status = issues.Count == 0 ? "balanced" : "review";
        run.CompletedAt = DateTimeOffset.UtcNow;
        _repository.Add(run);
        _repository.AddRange(issues);
        await _repository.SaveChangesAsync(cancellationToken);

        return new PaymentReconciliationResponseDto(
            run.Id,
            run.CheckedPayments,
            run.MatchedPayments,
            run.IssueCount,
            run.Status,
            run.StartedAt,
            run.CompletedAt,
            issues.Select(item => new PaymentReconciliationIssueResponseDto(
                item.Id,
                item.PaymentAttemptId,
                item.IssueType,
                item.Description,
                item.Severity,
                item.CreatedAt,
                item.ResolvedAt)).ToArray());
    }

    private async Task ProcessPaymentWebhookAsync(Guid inboxId, CancellationToken cancellationToken)
    {
        var inbox = await _repository.GetAsync<WebhookInbox>(
            item => item.Id == inboxId,
            tracking: true,
            cancellationToken)
            ?? throw new InvalidOperationException("The payment webhook inbox item no longer exists.");
        if (inbox.Status == "processed")
        {
            return;
        }

        try
        {
            var request = JsonSerializer.Deserialize<PaymentProviderWebhookRequestDto>(inbox.Payload, WebJson)
                ?? throw new InvalidOperationException("The payment webhook payload is invalid.");
            var payment = await _repository.GetAsync<PaymentAttempt>(
                item => item.ProviderId == inbox.ProviderType &&
                        item.ExternalPaymentId == request.ExternalPaymentId,
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The webhook payment attempt could not be found.");
            var session = new PayInProviderSession(
                payment.ProviderId,
                payment.ExternalPaymentId,
                request.Status,
                payment.RedirectUrl,
                payment.ExpiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5),
                IsTerminalStatus(request.Status) ? DateTimeOffset.UtcNow : payment.CompletedAt,
                request.FailureCode);

            await ApplyProviderSessionAsync(
                payment,
                session,
                "webhook",
                request.EventId,
                cancellationToken);
            inbox.Status = "processed";
            inbox.ProcessedAt = DateTimeOffset.UtcNow;
            inbox.LastError = null;
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            inbox.LastError = exception.Message.Length <= 1000
                ? exception.Message
                : exception.Message[..1000];
            await _repository.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task ApplyProviderSessionAsync(
        PaymentAttempt payment,
        PayInProviderSession session,
        string source,
        string? externalEventId,
        CancellationToken cancellationToken)
    {
        var tracked = await _repository.GetAsync<PaymentAttempt>(
            item => item.Id == payment.Id,
            tracking: true,
            cancellationToken)
            ?? throw new InvalidOperationException("The payment attempt no longer exists.");
        var previousStatus = tracked.Status;
        var nextStatus = NormaliseProviderStatus(session.Status);
        EnsureValidTransition(previousStatus, nextStatus);

        tracked.ProviderId = session.ProviderId;
        tracked.ExternalPaymentId = session.ExternalPaymentId;
        tracked.Status = nextStatus;
        tracked.RedirectUrl = session.RedirectUrl;
        tracked.FailureCode = session.FailureCode;
        tracked.ExpiresAt = session.ExpiresAt;
        tracked.CompletedAt = session.CompletedAt;
        tracked.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.Equals(previousStatus, nextStatus, StringComparison.Ordinal))
        {
            _repository.Add(new PaymentStatusHistory
            {
                PaymentAttemptId = tracked.Id,
                PreviousStatus = previousStatus,
                Status = nextStatus,
                Source = source,
                Reason = session.FailureCode,
                ExternalEventId = externalEventId,
            });
        }

        await SetOrderStatusAsync(tracked, OrderStatusForPayment(nextStatus), cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        if (nextStatus == "completed" && previousStatus != "completed")
        {
            await EnqueueFulfilmentAsync(tracked, cancellationToken);
        }
    }

    private async Task SetOrderStatusAsync(
        PaymentAttempt payment,
        string status,
        CancellationToken cancellationToken)
    {
        if (payment.VoucherOrderId is not null)
        {
            var order = await _repository.GetAsync<VoucherOrder>(
                item => item.Id == payment.VoucherOrderId,
                tracking: true,
                cancellationToken);
            if (order is not null && order.Status != "fulfilled")
            {
                order.Status = status;
            }
        }

        if (payment.PrepaidOrderId is not null)
        {
            var order = await _repository.GetAsync<PrepaidOrder>(
                item => item.Id == payment.PrepaidOrderId,
                tracking: true,
                cancellationToken);
            if (order is not null && order.Status != "fulfilled")
            {
                order.Status = status;
            }
        }
    }

    private async Task<string?> GetOrderStatusAsync(
        PaymentAttempt payment,
        CancellationToken cancellationToken)
    {
        if (payment.VoucherOrderId is not null)
        {
            return (await _repository.GetAsync<VoucherOrder>(
                item => item.Id == payment.VoucherOrderId,
                cancellationToken: cancellationToken))?.Status;
        }

        if (payment.PrepaidOrderId is not null)
        {
            return (await _repository.GetAsync<PrepaidOrder>(
                item => item.Id == payment.PrepaidOrderId,
                cancellationToken: cancellationToken))?.Status;
        }

        return null;
    }

    private async Task EnqueueFulfilmentAsync(
        PaymentAttempt payment,
        CancellationToken cancellationToken)
    {
        if (payment.VoucherOrderId is not null)
        {
            await _queue.EnqueueAsync(
                WalletPlatformQueueName,
                "voucher-fulfil",
                new FulfilOrderQueuePayload(payment.UserId, payment.VoucherOrderId.Value),
                cancellationToken: cancellationToken);
        }

        if (payment.PrepaidOrderId is not null)
        {
            await _queue.EnqueueAsync(
                WalletPlatformQueueName,
                "prepaid-fulfil",
                new FulfilOrderQueuePayload(payment.UserId, payment.PrepaidOrderId.Value),
                cancellationToken: cancellationToken);
        }
    }

    private static string NormaliseProviderStatus(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "created" => "created",
            "awaiting_approval" or "requires_action" => "awaiting_approval",
            "pending" or "processing" => "pending",
            "completed" or "paid" or "success" => "completed",
            "cancelled" or "canceled" => "cancelled",
            "expired" => "expired",
            "failed" or "declined" => "failed",
            "refunded" => "refunded",
            "refund_pending" => "refund_pending",
            _ => throw Validation("status", $"Payment status '{status}' is not supported."),
        };

    private static string OrderStatusForPayment(string paymentStatus) =>
        paymentStatus switch
        {
            "completed" => "payment_completed",
            "refunded" => "refunded",
            "refund_pending" => "refund_pending",
            _ => paymentStatus,
        };

    private static bool IsTerminalStatus(string? status) =>
        NormaliseProviderStatus(status) is "completed" or "failed" or "cancelled" or "expired" or "refunded";

    private static void EnsureValidTransition(string current, string next)
    {
        if (current == next)
        {
            return;
        }

        var allowed = current switch
        {
            "created" => next is "awaiting_approval" or "pending" or "completed" or "failed" or "cancelled" or "expired",
            "awaiting_approval" => next is "pending" or "completed" or "failed" or "cancelled" or "expired",
            "pending" => next is "completed" or "failed" or "cancelled" or "expired",
            "completed" => next is "refund_pending" or "refunded",
            "refund_pending" => next is "refunded" or "completed" or "failed",
            _ => false,
        };
        if (!allowed)
        {
            throw new ConflictApiException($"Payment cannot move from '{current}' to '{next}'.");
        }
    }
}
