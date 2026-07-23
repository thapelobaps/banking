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

        var sql = GetSql(migrationBuilder);

        Assert.Contains("TR_BANKCONNECTIONS_DELETESTITCHSECRETS", sql);
        Assert.Contains("STITCHCONNECTIONSECRETS", sql);
        Assert.Contains("PROVIDERID = 'STITCH'", sql);
        Assert.Contains("STATUS = 'DISCONNECTED'", sql);
        Assert.Contains("ISDELETED = 1", sql);
    }

    [Fact]
    public void PendingCleanupMigration_KeepsOnlyTheNewestPendingConnectionPerProvider()
    {
        var migration = new BankConnectionPendingCleanup();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var upMethod = typeof(BankConnectionPendingCleanup).GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(upMethod);
        upMethod!.Invoke(migration, new object[] { migrationBuilder });

        var sql = GetSql(migrationBuilder);

        Assert.Contains("TR_BANKCONNECTIONS_KEEPSINGLEPENDING", sql);
        Assert.Contains("PARTITION BY CONNECTION.USERID, CONNECTION.PROVIDERID", sql);
        Assert.Contains("PENDINGRANK > 1", sql);
        Assert.Contains("STATUS = 'SUPERSEDED'", sql);
        Assert.Contains("ISDELETED = 1", sql);
    }

    [Fact]
    public void ActiveDeduplicationMigration_KeepsOneLiveConnectionPerInstitution()
    {
        var migration = new BankConnectionActiveDeduplication();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var upMethod = typeof(BankConnectionActiveDeduplication).GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(upMethod);
        upMethod!.Invoke(migration, new object[] { migrationBuilder });

        var sql = GetSql(migrationBuilder);

        Assert.Contains("TR_BANKCONNECTIONS_KEEPSINGLELIVEINSTITUTION", sql);
        Assert.Contains(
            "PARTITION BY CONNECTION.USERID, CONNECTION.PROVIDERID, CONNECTION.INSTITUTIONID",
            sql);
        Assert.Contains("STATUS IN ('ACTIVE', 'SYNCING')", sql);
        Assert.Contains("LIVERANK > 1", sql);
        Assert.Contains("LINKEDBANKACCOUNTS", sql);
        Assert.Contains("STATUS = 'SUPERSEDED'", sql);
        Assert.Contains("ISDELETED = 1", sql);
    }

    [Fact]
    public void DemoInstitutionMigration_ScopesConnectionsAndDeactivatesCrossBankAccounts()
    {
        var migration = new DemoInstitutionScopedAccounts();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var upMethod = typeof(DemoInstitutionScopedAccounts).GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(upMethod);
        upMethod!.Invoke(migration, new object[] { migrationBuilder });

        var sql = GetSql(migrationBuilder);

        Assert.Contains("|INSTITUTION=", sql);
        Assert.Contains("PROVIDERID = 'DEMO-BANK-AGGREGATOR'", sql);
        Assert.Contains("LINKEDBANKACCOUNTS", sql);
        Assert.Contains("ACCOUNT.ISACTIVE = 0", sql);
        Assert.Contains("ACCOUNT.INSTITUTIONNAME", sql);
        Assert.Contains("CONNECTION.INSTITUTIONNAME", sql);
    }

    private static string GetSql(MigrationBuilder migrationBuilder) =>
        string.Join(
                Environment.NewLine,
                migrationBuilder.Operations
                    .OfType<SqlOperation>()
                    .Select(operation => operation.Sql))
            .ToUpperInvariant();
}
