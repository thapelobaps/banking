using Kape.Api.Services;
using Kape.Api.Services.Interfaces;
using Xunit;

namespace Kape.Api.Tests.Services;

public sealed class DemoPayInProviderTests
{
    [Theory]
    [InlineData("success", "completed", null)]
    [InlineData("pending", "pending", null)]
    [InlineData("cancelled", "cancelled", "customer_cancelled")]
    [InlineData("failed", "failed", "demo_declined")]
    [InlineData("insufficient_funds", "failed", "insufficient_funds")]
    [InlineData("unknown", "awaiting_approval", null)]
    public async Task CreatePayment_MapsDemoScenario(
        string scenario,
        string expectedStatus,
        string? expectedFailureCode)
    {
        var provider = new DemoPayInProvider();

        var session = await provider.CreatePaymentAsync(
            CreateRequest(scenario),
            CancellationToken.None);

        Assert.Equal("demo-pay-by-bank", session.ProviderId);
        Assert.StartsWith("demo_payin_", session.ExternalPaymentId);
        Assert.Equal(expectedStatus, session.Status);
        Assert.Equal(expectedFailureCode, session.FailureCode);
        Assert.NotNull(session.RedirectUrl);
        Assert.Contains(session.ExternalPaymentId, session.RedirectUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreatePayment_IsIdempotentForSameProviderKey()
    {
        var provider = new DemoPayInProvider();
        var request = CreateRequest("success", "pay-order-1");

        var first = await provider.CreatePaymentAsync(request, CancellationToken.None);
        var second = await provider.CreatePaymentAsync(request, CancellationToken.None);

        Assert.Equal(first.ExternalPaymentId, second.ExternalPaymentId);
        Assert.Equal(first.Status, second.Status);
    }

    [Fact]
    public async Task Refund_IsIdempotentAndUpdatesPaymentStatus()
    {
        var provider = new DemoPayInProvider();
        var payment = await provider.CreatePaymentAsync(
            CreateRequest("success"),
            CancellationToken.None);
        var request = new PayInProviderRefundRequest(
            payment.ExternalPaymentId,
            400m,
            "ZAR",
            "Demo fulfilment failed",
            "refund-order-1");

        var first = await provider.RefundAsync(request, CancellationToken.None);
        var second = await provider.RefundAsync(request, CancellationToken.None);
        var updatedPayment = await provider.GetPaymentAsync(payment.ExternalPaymentId, CancellationToken.None);

        Assert.Equal(first.ExternalRefundId, second.ExternalRefundId);
        Assert.Equal("completed", first.Status);
        Assert.Equal("refunded", updatedPayment.Status);
    }

    [Fact]
    public async Task Refund_RejectsIncompletePayment()
    {
        var provider = new DemoPayInProvider();
        var payment = await provider.CreatePaymentAsync(
            CreateRequest("pending"),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.RefundAsync(
            new PayInProviderRefundRequest(
                payment.ExternalPaymentId,
                400m,
                "ZAR",
                "Not completed",
                "refund-pending-1"),
            CancellationToken.None));
    }

    private static PayInProviderRequest CreateRequest(
        string scenario,
        string? idempotencyKey = null) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "voucher",
            Guid.NewGuid(),
            400m,
            "ZAR",
            "PnP demo voucher",
            scenario,
            idempotencyKey ?? Guid.NewGuid().ToString("N"),
            "https://kape.example/payments/return");
}
