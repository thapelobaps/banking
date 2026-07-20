using Kape.Api.Services;
using Xunit;

namespace Kape.Api.Tests.Services;

public sealed class SouthAfricanDemoBankingProviderTests
{
    private readonly SouthAfricanDemoBankingProvider _provider = new();

    [Fact]
    public void CreateAccounts_ForSameUser_ReturnsDistinctTransactionAndSavingsAccounts()
    {
        var userId = Guid.NewGuid();
        const string email = "thapelo@example.com";

        var primary = _provider.CreateDefaultDemoAccount(userId, email);
        var secondary = _provider.CreateSecondaryDemoAccount(userId, email);

        Assert.Equal(userId, primary.UserId);
        Assert.Equal(userId, secondary.UserId);
        Assert.Equal("transaction", primary.AccountType);
        Assert.Equal("savings", secondary.AccountType);
        Assert.NotEqual(primary.Id, secondary.Id);
        Assert.NotEqual(primary.BankId, secondary.BankId);
        Assert.NotEqual(primary.AccountNumber, secondary.AccountNumber);
        Assert.True(primary.IsDemo);
        Assert.True(secondary.IsDemo);
        Assert.Equal("ZAR", primary.Currency);
        Assert.Equal("ZAR", secondary.Currency);
    }

    [Fact]
    public void CreateSecondaryDemoAccount_ForSameEmail_IsDeterministicApartFromEntityId()
    {
        var first = _provider.CreateSecondaryDemoAccount(Guid.NewGuid(), "user@example.com");
        var second = _provider.CreateSecondaryDemoAccount(Guid.NewGuid(), "user@example.com");

        Assert.Equal(first.BankId, second.BankId);
        Assert.Equal(first.BankName, second.BankName);
        Assert.Equal(first.AccountNumber, second.AccountNumber);
        Assert.Equal(first.CurrentBalance, second.CurrentBalance);
        Assert.Equal(first.AvailableBalance, second.AvailableBalance);
    }
}
