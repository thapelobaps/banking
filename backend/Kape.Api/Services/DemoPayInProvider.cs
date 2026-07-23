using System.Collections.Concurrent;
using Kape.Api.Services.Interfaces;

namespace Kape.Api.Services;

public sealed class DemoPayInProvider : IPayInProvider
{
    private const string PaymentPrefix = "demo_payin_";
    private readonly ConcurrentDictionary<string, PayInProviderSession> _payments = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PayInProviderSession> _paymentRequests = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PayInProviderRefundResult> _refunds = new(StringComparer.Ordinal);

    public string ProviderId => "demo-pay-by-bank";

    public Task<PayInProviderSession> CreatePaymentAsync(
        PayInProviderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Amount <= 0m || decimal.Round(request.Amount, 2) != request.Amount)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "The payment amount must be positive with no more than two decimal places.");
        }

        var idempotencyKey = request.IdempotencyKey.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("A provider idempotency key is required.", nameof(request));
        }

        if (_paymentRequests.TryGetValue(idempotencyKey, out var existing))
        {
            return Task.FromResult(existing);
        }

        var scenario = NormaliseScenario(request.Scenario);
        var externalPaymentId = $"{PaymentPrefix}{scenario}_{Guid.NewGuid():N}";
        var session = BuildSession(
            externalPaymentId,
            scenario,
            request.ReturnUrl,
            DateTimeOffset.UtcNow);

        _payments[externalPaymentId] = session;
        _paymentRequests[idempotencyKey] = session;
        return Task.FromResult(session);
    }

    public Task<PayInProviderSession> GetPaymentAsync(
        string externalPaymentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = externalPaymentId.Trim();

        if (_payments.TryGetValue(id, out var session))
        {
            return Task.FromResult(session);
        }

        var scenario = ParseScenario(id)
            ?? throw new KeyNotFoundException("The demo payment could not be found.");
        session = BuildSession(id, scenario, returnUrl: null, DateTimeOffset.UtcNow);
        _payments[id] = session;
        return Task.FromResult(session);
    }

    public async Task<PayInProviderRefundResult> RefundAsync(
        PayInProviderRefundRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payment = await GetPaymentAsync(request.ExternalPaymentId, cancellationToken);

        if (payment.Status is not ("completed" or "refunded"))
        {
            throw new InvalidOperationException("Only a completed demo payment can be refunded.");
        }

        if (request.Amount <= 0m || decimal.Round(request.Amount, 2) != request.Amount)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "The refund amount must be positive with no more than two decimal places.");
        }

        var key = request.IdempotencyKey.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A refund idempotency key is required.", nameof(request));
        }

        if (_refunds.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var createdAt = DateTimeOffset.UtcNow;
        var result = new PayInProviderRefundResult(
            ProviderId,
            $"demo_refund_{Guid.NewGuid():N}",
            payment.ExternalPaymentId,
            request.Amount,
            request.Currency,
            "completed",
            createdAt);

        _refunds[key] = result;
        _payments[payment.ExternalPaymentId] = payment with
        {
            Status = "refunded",
            CompletedAt = createdAt,
            FailureCode = null,
        };

        return result;
    }

    private PayInProviderSession BuildSession(
        string externalPaymentId,
        string scenario,
        string? returnUrl,
        DateTimeOffset now)
    {
        var (status, failureCode, completedAt) = scenario switch
        {
            "success" => ("completed", (string?)null, (DateTimeOffset?)now),
            "pending" => ("pending", null, null),
            "cancelled" => ("cancelled", "customer_cancelled", now),
            "failed" => ("failed", "demo_declined", now),
            "insufficient_funds" => ("failed", "insufficient_funds", now),
            _ => ("awaiting_approval", null, null),
        };

        return new PayInProviderSession(
            ProviderId,
            externalPaymentId,
            status,
            BuildRedirectUrl(returnUrl, externalPaymentId, scenario),
            now.AddMinutes(15),
            completedAt,
            failureCode);
    }

    private static string NormaliseScenario(string? scenario) =>
        string.IsNullOrWhiteSpace(scenario)
            ? "awaiting_approval"
            : scenario.Trim().ToLowerInvariant() switch
            {
                "approved" or "completed" or "success" => "success",
                "processing" or "pending" => "pending",
                "cancelled" or "canceled" => "cancelled",
                "insufficient" or "insufficient_funds" => "insufficient_funds",
                "declined" or "failed" => "failed",
                _ => "awaiting_approval",
            };

    private static string? ParseScenario(string externalPaymentId)
    {
        if (!externalPaymentId.StartsWith(PaymentPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var value = externalPaymentId[PaymentPrefix.Length..];
        foreach (var scenario in new[]
                 {
                     "insufficient_funds",
                     "awaiting_approval",
                     "cancelled",
                     "pending",
                     "failed",
                     "success",
                 })
        {
            if (value.StartsWith($"{scenario}_", StringComparison.Ordinal))
            {
                return scenario;
            }
        }

        return null;
    }

    private static string? BuildRedirectUrl(
        string? returnUrl,
        string externalPaymentId,
        string scenario)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !Uri.TryCreate(returnUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        var separator = string.IsNullOrWhiteSpace(uri.Query) ? "?" : "&";
        return $"{uri}{separator}demoPayment={Uri.EscapeDataString(externalPaymentId)}&scenario={Uri.EscapeDataString(scenario)}";
    }
}
