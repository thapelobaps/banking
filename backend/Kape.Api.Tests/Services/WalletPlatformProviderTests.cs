using Kape.Api.Services;
using Xunit;

namespace Kape.Api.Tests.Services;

public sealed class WalletPlatformProviderTests
{
    [Fact]
    public async Task BankProviderSync_ReturnsAccountsTransactionsAndDebitOrders()
    {
        var provider = new DemoBankAggregationProvider();

        var result = await provider.SyncAsync("demo-connection-test", CancellationToken.None);

        Assert.Equal(2, result.Accounts.Count);
        Assert.NotEmpty(result.Transactions);
        Assert.NotEmpty(result.DebitOrders);
        Assert.All(result.Accounts, account => Assert.Equal("ZAR", account.Currency));
    }

    [Fact]
    public async Task PaymentProviderConfirm_TokenisesRawPaymentToken()
    {
        var provider = new DemoPaymentTokenizationProvider();
        const string rawToken = "raw-card-token-for-test";

        var result = await provider.ConfirmAsync(
            "setup-test",
            rawToken,
            "Mastercard",
            "Capitec",
            "3684",
            12,
            2030,
            CancellationToken.None);

        Assert.StartsWith("tok_", result.TokenReference);
        Assert.DoesNotContain(rawToken, result.TokenReference, StringComparison.Ordinal);
        Assert.Equal("3684", result.Last4);
    }

    [Fact]
    public async Task DigitalProductProvider_FulfilsVoucherAndPrepaidDemoOrders()
    {
        var provider = new DemoDigitalProductProvider();

        var voucher = await provider.FulfilVoucherAsync(
            "netflix",
            250m,
            "order-1",
            CancellationToken.None);
        var prepaid = await provider.FulfilPrepaidAsync(
            "vodacom-airtime",
            "0821234567",
            50m,
            "order-2",
            CancellationToken.None);

        Assert.StartsWith("voucher_", voucher.ExternalOrderId);
        Assert.StartsWith("KAPE-NETFLIX-", voucher.FulfilmentReference);
        Assert.StartsWith("prepaid_", prepaid.ExternalOrderId);
        Assert.StartsWith("KAPE-VODACOM-AIRTIME-", prepaid.FulfilmentReference);
    }
}
