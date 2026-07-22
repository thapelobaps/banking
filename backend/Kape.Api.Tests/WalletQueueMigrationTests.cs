using System.Reflection;
using Kape.Api.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Kape.Api.Tests;

public sealed class WalletQueueMigrationTests
{
    [Fact]
    public void ReadPastCompatibilityMigration_ForcesLockBasedReadCommittedAccess()
    {
        var migration = new WalletQueueReadPastCompatibility();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var upMethod = typeof(WalletQueueReadPastCompatibility).GetMethod(
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

        Assert.Contains("SET TRANSACTION ISOLATION LEVEL READ COMMITTED", sql);
        Assert.Contains("READPAST", sql);
        Assert.Contains("READCOMMITTEDLOCK", sql);
        Assert.Contains("UPDLOCK", sql);
        Assert.Contains("ROWLOCK", sql);
    }
}
