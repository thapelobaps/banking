using Kape.Api.Data;
using Kape.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Kape.Api.Tests.Data;

public sealed class PaymentOrchestrationModelTests
{
    private static KapeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KapeDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=KapePaymentModelMetadataOnly;User Id=sa;Password=ModelMetadataOnly123!;TrustServerCertificate=True")
            .Options;
        return new KapeDbContext(options);
    }

    [Fact]
    public void PaymentAttempt_EnforcesUserIdempotencyAndProviderReferenceUniqueness()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(PaymentAttempt))!;
        var idempotency = entity.GetIndexes()
            .Single(index => index.GetDatabaseName() == "UX_PaymentAttempts_User_Idempotency");
        var providerReference = entity.GetIndexes()
            .Single(index => index.GetDatabaseName() == "UX_PaymentAttempts_Provider_External");

        Assert.True(idempotency.IsUnique);
        Assert.Equal(
            [nameof(PaymentAttempt.UserId), nameof(PaymentAttempt.IdempotencyKey)],
            idempotency.Properties.Select(property => property.Name).ToArray());
        Assert.True(providerReference.IsUnique);
        Assert.Equal(
            [nameof(PaymentAttempt.ProviderId), nameof(PaymentAttempt.ExternalPaymentId)],
            providerReference.Properties.Select(property => property.Name).ToArray());
    }

    [Fact]
    public void PaymentHistory_CascadesOnlyWithItsPaymentAttempt()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(PaymentStatusHistory))!;
        var paymentForeignKey = entity.GetForeignKeys()
            .Single(candidate => candidate.PrincipalEntityType.ClrType == typeof(PaymentAttempt));

        Assert.Equal(DeleteBehavior.Cascade, paymentForeignKey.DeleteBehavior);
        Assert.Equal(
            [nameof(PaymentStatusHistory.PaymentAttemptId)],
            paymentForeignKey.Properties.Select(property => property.Name).ToArray());
    }

    [Fact]
    public void WalletReservation_AllowsOnlyOneReservationPerPaymentAttempt()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(WalletReservation))!;
        var index = entity.GetIndexes()
            .Single(candidate => candidate.GetDatabaseName() == "UX_WalletReservations_PaymentAttempt");

        Assert.True(index.IsUnique);
        Assert.Equal(
            [nameof(WalletReservation.PaymentAttemptId)],
            index.Properties.Select(property => property.Name).ToArray());
    }

    [Fact]
    public void ReconciliationIssue_KeepsPaymentAndRunReferences()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(PaymentReconciliationIssue))!;

        Assert.Contains(
            entity.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(PaymentAttempt) &&
                   key.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(
            entity.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(PaymentReconciliationRun) &&
                   key.DeleteBehavior == DeleteBehavior.Cascade);
    }
}
