using Kape.Api.Data;
using Kape.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Kape.Api.Tests.Data;

public sealed class WalletPlatformModelTests
{
    private static KapeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KapeDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=KapeModelMetadataOnly;User Id=sa;Password=ModelMetadataOnly123!;TrustServerCertificate=True")
            .Options;
        return new KapeDbContext(options);
    }

    [Fact]
    public void WalletStorage_UsesExpectedFixedLengthSqlTypes()
    {
        using var context = CreateContext();

        var walletCurrency = context.Model
            .FindEntityType(typeof(Wallet))!
            .FindProperty(nameof(Wallet.Currency))!;
        var paymentMethodLast4 = context.Model
            .FindEntityType(typeof(PaymentMethod))!
            .FindProperty(nameof(PaymentMethod.Last4))!;
        var linkedAccountCurrency = context.Model
            .FindEntityType(typeof(LinkedBankAccount))!
            .FindProperty(nameof(LinkedBankAccount.Currency))!;

        Assert.Equal("char(3)", walletCurrency.GetColumnType());
        Assert.Equal("char(4)", paymentMethodLast4.GetColumnType());
        Assert.Equal("char(3)", linkedAccountCurrency.GetColumnType());
    }

    [Fact]
    public void QueueModel_UsesFilteredAtomicDequeueIndex()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(QueueMessage))!;
        var index = entity.GetIndexes()
            .Single(candidate => candidate.GetDatabaseName() == "IX_QueueMessages_Dequeue");

        Assert.Equal("[Status] = 'pending'", index.GetFilter());
        Assert.Equal(
            [
                nameof(QueueMessage.QueueName),
                nameof(QueueMessage.Status),
                nameof(QueueMessage.AvailableAt),
                nameof(QueueMessage.CreatedAt),
            ],
            index.Properties.Select(property => property.Name).ToArray());
    }

    [Fact]
    public void WalletModel_EnforcesOneWalletPerUser()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Wallet))!;
        var index = entity.GetIndexes()
            .Single(candidate => candidate.GetDatabaseName() == "UX_Wallets_UserId");

        Assert.True(index.IsUnique);
        Assert.Equal([nameof(Wallet.UserId)], index.Properties.Select(property => property.Name).ToArray());
    }
}
