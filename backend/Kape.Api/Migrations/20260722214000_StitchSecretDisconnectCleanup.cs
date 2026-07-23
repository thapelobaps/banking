using Kape.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kape.Api.Migrations;

[DbContext(typeof(KapeDbContext))]
[Migration("20260722214000_StitchSecretDisconnectCleanup")]
public sealed class StitchSecretDisconnectCleanup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR ALTER TRIGGER dbo.TR_BankConnections_DeleteStitchSecrets
            ON dbo.BankConnections
            AFTER UPDATE, DELETE
            AS
            BEGIN
                SET NOCOUNT ON;

                ;WITH DisconnectedStitchConnections AS
                (
                    SELECT inserted.ExternalConnectionId
                    FROM inserted
                    WHERE inserted.ProviderId = 'stitch'
                      AND (inserted.IsDeleted = 1 OR inserted.Status = 'disconnected')

                    UNION

                    SELECT deleted.ExternalConnectionId
                    FROM deleted
                    LEFT JOIN inserted ON inserted.Id = deleted.Id
                    WHERE inserted.Id IS NULL
                      AND deleted.ProviderId = 'stitch'
                )
                DELETE secret
                FROM dbo.StitchConnectionSecrets AS secret
                INNER JOIN DisconnectedStitchConnections AS disconnected
                    ON disconnected.ExternalConnectionId = secret.ExternalConnectionId;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS dbo.TR_BankConnections_DeleteStitchSecrets;
            """);
    }
}
