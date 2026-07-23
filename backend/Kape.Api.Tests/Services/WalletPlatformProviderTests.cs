using Kape.Api.Services;
using Xunit;

namespace Kape.Api.Tests.Services;

public sealed class WalletPlatformProviderTests
{
    [Theory]
    [InlineData("capitec", "Capitec", "Global One")]
    [InlineData("standard-bank", "Standard Bank", "MyMo Plus")]
    [InlineData("fnb", "FNB", "Easy Zero")]
    [InlineData("absa", "Absa", "Transact Account")]
    [InlineData("nedbank", "Nedbank", "MiGoals")]
    public async Task BankProviderSync_ReturnsOnlySelectedInstitution(
        string institutionId,
        string institutionName,
        string accountName)
    {
        var provider = new DemoBankAggregationProvider();
        var connection = await provider.CompleteLinkAsync(
            Guid.NewGuid(),
            $"demo-{institutionId}",
            institutionId,
            CancellationToken.None);

        var result = await provider.SyncAsync(connection.ExternalConnectionId, CancellationToken.None);

        var account = Assert.Single(result.Accounts);
        Assert.Equal(institutionName, account.InstitutionName);
        Assert.Equal(accountName, account.AccountName);
        Assert.Equal("ZAR", account.Currency);
        Assert.NotEmpty(result.Transactions);
        Assert.NotEmpty(result.DebitOrders);
        Assert.All(result.Transactions, transaction => Assert.Equal(account.ExternalAccountId, transaction.ExternalAccountId));
        Assert.All(result.DebitOrders, debitOrder => Assert.Equal(account.ExternalAccountId, debitOrder.ExternalAccountId));
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
