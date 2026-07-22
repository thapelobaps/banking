using Kape.Api.Data;
using Kape.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kape.Api.Tests.Data;

public sealed class StitchPersistenceModelTests
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
    public void AuthorizationRequest_UsesStatePrimaryKeyAndExpiryIndex()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(StitchAuthorizationRequestRecord))!;

        Assert.Equal("StitchAuthorizationRequests", entity.GetTableName());
        Assert.Equal(
            [nameof(StitchAuthorizationRequestRecord.State)],
            entity.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray());
        Assert.Contains(
            entity.GetIndexes(),
            index => index.GetDatabaseName() == "IX_StitchAuthorizationRequests_ExpiresAt");
    }

    [Fact]
    public void ConnectionSecret_StoresOnlyProtectedPayloadAndOperationalMetadata()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(StitchConnectionSecretRecord))!;
        var propertyNames = entity.GetProperties().Select(property => property.Name).ToArray();

        Assert.Equal("StitchConnectionSecrets", entity.GetTableName());
        Assert.Contains(nameof(StitchConnectionSecretRecord.ProtectedPayload), propertyNames);
        Assert.DoesNotContain("AccessToken", propertyNames);
        Assert.DoesNotContain("RefreshToken", propertyNames);
        Assert.DoesNotContain("ClientSecret", propertyNames);
    }
}
