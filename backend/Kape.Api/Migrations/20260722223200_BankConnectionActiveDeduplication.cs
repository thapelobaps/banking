using Kape.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kape.Api.Migrations;

[DbContext(typeof(KapeDbContext))]
[Migration("20260722223200_BankConnectionActiveDeduplication")]
public sealed class BankConnectionActiveDeduplication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DECLARE @SupersededConnections TABLE
            (
                Id uniqueidentifier NOT NULL PRIMARY KEY
            );

            ;WITH RankedLiveConnections AS
            (
                SELECT
                    connection.Id,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY connection.UserId, connection.ProviderId, connection.InstitutionId
                        ORDER BY connection.UpdatedAt DESC, connection.CreatedAt DESC, connection.Id DESC
                    ) AS LiveRank
                FROM dbo.BankConnections AS connection
                WHERE connection.IsDeleted = 0
                  AND connection.Status IN ('active', 'syncing')
                  AND connection.InstitutionId <> 'pending'
            )
            INSERT INTO @SupersededConnections (Id)
            SELECT ranked.Id
            FROM RankedLiveConnections AS ranked
            WHERE ranked.LiveRank > 1;

            UPDATE account
            SET account.IsActive = 0
            FROM dbo.LinkedBankAccounts AS account
            INNER JOIN @SupersededConnections AS superseded
                ON superseded.Id = account.BankConnectionId;

            UPDATE connection
            SET connection.IsDeleted = 1,
                connection.Status = 'superseded',
                connection.UpdatedAt = SYSUTCDATETIME()
            FROM dbo.BankConnections AS connection
            INNER JOIN @SupersededConnections AS superseded
                ON superseded.Id = connection.Id;
            """);

        migrationBuilder.Sql(
            """
            CREATE OR ALTER TRIGGER dbo.TR_BankConnections_KeepSingleLiveInstitution
            ON dbo.BankConnections
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @SupersededConnections TABLE
                (
                    Id uniqueidentifier NOT NULL PRIMARY KEY
                );

                ;WITH AffectedInstitutions AS
                (
                    SELECT DISTINCT
                        inserted.UserId,
                        inserted.ProviderId,
                        inserted.InstitutionId
                    FROM inserted
                    WHERE inserted.IsDeleted = 0
                      AND inserted.Status IN ('active', 'syncing')
                      AND inserted.InstitutionId <> 'pending'
                ),
                RankedLiveConnections AS
                (
                    SELECT
                        connection.Id,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY connection.UserId, connection.ProviderId, connection.InstitutionId
                            ORDER BY connection.UpdatedAt DESC, connection.CreatedAt DESC, connection.Id DESC
                        ) AS LiveRank
                    FROM dbo.BankConnections AS connection
                    INNER JOIN AffectedInstitutions AS affected
                        ON affected.UserId = connection.UserId
                       AND affected.ProviderId = connection.ProviderId
                       AND affected.InstitutionId = connection.InstitutionId
                    WHERE connection.IsDeleted = 0
                      AND connection.Status IN ('active', 'syncing')
                )
                INSERT INTO @SupersededConnections (Id)
                SELECT ranked.Id
                FROM RankedLiveConnections AS ranked
                WHERE ranked.LiveRank > 1;

                UPDATE account
                SET account.IsActive = 0
                FROM dbo.LinkedBankAccounts AS account
                INNER JOIN @SupersededConnections AS superseded
                    ON superseded.Id = account.BankConnectionId;

                UPDATE connection
                SET connection.IsDeleted = 1,
                    connection.Status = 'superseded',
                    connection.UpdatedAt = SYSUTCDATETIME()
                FROM dbo.BankConnections AS connection
                INNER JOIN @SupersededConnections AS superseded
                    ON superseded.Id = connection.Id;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS dbo.TR_BankConnections_KeepSingleLiveInstitution;
            """);
    }
}
