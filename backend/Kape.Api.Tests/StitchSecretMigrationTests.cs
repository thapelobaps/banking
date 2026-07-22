using System.Reflection;
using Kape.Api.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Kape.Api.Tests;

public sealed class StitchSecretMigrationTests
{
    [Fact]
    public void DisconnectCleanupMigration_DeletesOnlyStitchConnectionSecrets()
    {
        var migration = new StitchSecretDisconnectCleanup();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var upMethod = typeof(StitchSecretDisconnectCleanup).GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(upMethod);
        upMethod!.Invoke(migration, new object[] { migrationBuilder });

        var sql = string.Join(
                Environment.NewLine,
                migrationBuilder.Operations
                    .OfType<SqlOperation>()
                    .Select(operation => operation.Sql))
            .ToUpperInvariant();

        Assert.Contains("TR_BANKCONNECTIONS_DELETESTITCHSECRETS", sql);
        Assert.Contains("STITCHCONNECTIONSECRETS", sql);
        Assert.Contains("PROVIDERID = 'STITCH'", sql);
        Assert.Contains("STATUS = 'DISCONNECTED'", sql);
        Assert.Contains("ISDELETED = 1", sql);
    }
}
