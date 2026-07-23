using Kape.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kape.Api.Migrations;

[DbContext(typeof(KapeDbContext))]
[Migration("20260722222000_BankConnectionPendingCleanup")]
public sealed class BankConnectionPendingCleanup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ;WITH RankedPendingConnections AS
            (
                SELECT
                    connection.Id,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY connection.UserId, connection.ProviderId
                        ORDER BY connection.CreatedAt DESC, connection.Id DESC
                    ) AS PendingRank
                FROM dbo.BankConnections AS connection
                WHERE connection.IsDeleted = 0
                  AND connection.Status = 'pending'
            )
            UPDATE connection
            SET connection.IsDeleted = 1,
                connection.Status = 'superseded',
                connection.UpdatedAt = SYSUTCDATETIME()
            FROM dbo.BankConnections AS connection
            INNER JOIN RankedPendingConnections AS ranked
                ON ranked.Id = connection.Id
            WHERE ranked.PendingRank > 1;
            """);

        migrationBuilder.Sql(
            """
            CREATE OR ALTER TRIGGER dbo.TR_BankConnections_KeepSinglePending
            ON dbo.BankConnections
            AFTER INSERT
            AS
            BEGIN
                SET NOCOUNT ON;

                ;WITH AffectedProviders AS
                (
                    SELECT DISTINCT inserted.UserId, inserted.ProviderId
                    FROM inserted
                    WHERE inserted.IsDeleted = 0
                      AND inserted.Status = 'pending'
                ),
                RankedPendingConnections AS
                (
                    SELECT
                        connection.Id,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY connection.UserId, connection.ProviderId
                            ORDER BY connection.CreatedAt DESC, connection.Id DESC
                        ) AS PendingRank
                    FROM dbo.BankConnections AS connection
                    INNER JOIN AffectedProviders AS affected
                        ON affected.UserId = connection.UserId
                       AND affected.ProviderId = connection.ProviderId
                    WHERE connection.IsDeleted = 0
                      AND connection.Status = 'pending'
                )
                UPDATE connection
                SET connection.IsDeleted = 1,
                    connection.Status = 'superseded',
                    connection.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.BankConnections AS connection
                INNER JOIN RankedPendingConnections AS ranked
                    ON ranked.Id = connection.Id
                WHERE ranked.PendingRank > 1;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS dbo.TR_BankConnections_KeepSinglePending;
            """);
    }
}
