using Kape.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kape.Api.Migrations;

[DbContext(typeof(KapeDbContext))]
[Migration("20260722223800_DemoInstitutionScopedAccounts")]
public sealed class DemoInstitutionScopedAccounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE connection
            SET connection.ExternalConnectionId = CONCAT(
                    connection.ExternalConnectionId,
                    '|institution=',
                    connection.InstitutionId),
                connection.UpdatedAt = SYSUTCDATETIME()
            FROM dbo.BankConnections AS connection
            WHERE connection.ProviderId = 'demo-bank-aggregator'
              AND CHARINDEX('|institution=', connection.ExternalConnectionId) = 0;
            """);

        migrationBuilder.Sql(
            """
            UPDATE account
            SET account.IsActive = 0
            FROM dbo.LinkedBankAccounts AS account
            INNER JOIN dbo.BankConnections AS connection
                ON connection.Id = account.BankConnectionId
            WHERE connection.ProviderId = 'demo-bank-aggregator'
              AND connection.IsDeleted = 0
              AND account.IsActive = 1
              AND LOWER(LTRIM(RTRIM(account.InstitutionName)))
                  <> LOWER(LTRIM(RTRIM(connection.InstitutionName)));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE connection
            SET connection.ExternalConnectionId = LEFT(
                    connection.ExternalConnectionId,
                    CHARINDEX('|institution=', connection.ExternalConnectionId) - 1),
                connection.UpdatedAt = SYSUTCDATETIME()
            FROM dbo.BankConnections AS connection
            WHERE connection.ProviderId = 'demo-bank-aggregator'
              AND CHARINDEX('|institution=', connection.ExternalConnectionId) > 0;
            """);
    }
}
