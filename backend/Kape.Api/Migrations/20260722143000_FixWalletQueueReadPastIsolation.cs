using Kape.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kape.Api.Migrations;

[DbContext(typeof(KapeDbContext))]
[Migration("20260722143000_FixWalletQueueReadPastIsolation")]
public sealed class FixWalletQueueReadPastIsolation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR ALTER PROCEDURE dbo.sp_DequeueWalletMessage
                @QueueName nvarchar(80),
                @WorkerId nvarchar(100)
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;
                SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

                BEGIN TRANSACTION;

                ;WITH NextMessage AS (
                    SELECT TOP (1) *
                    FROM dbo.QueueMessages WITH (
                        UPDLOCK,
                        READPAST,
                        READCOMMITTEDLOCK,
                        ROWLOCK,
                        INDEX(IX_QueueMessages_Dequeue)
                    )
                    WHERE QueueName = @QueueName
                      AND Status = 'pending'
                      AND AvailableAt <= SYSUTCDATETIME()
                    ORDER BY AvailableAt, CreatedAt
                )
                UPDATE NextMessage
                SET Status = 'processing',
                    LockedAt = SYSUTCDATETIME(),
                    LockedBy = @WorkerId,
                    Attempts = Attempts + 1
                OUTPUT inserted.*;

                COMMIT TRANSACTION;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR ALTER PROCEDURE dbo.sp_DequeueWalletMessage
                @QueueName nvarchar(80),
                @WorkerId nvarchar(100)
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                ;WITH NextMessage AS (
                    SELECT TOP (1) *
                    FROM dbo.QueueMessages WITH (UPDLOCK, READPAST, ROWLOCK, INDEX(IX_QueueMessages_Dequeue))
                    WHERE QueueName = @QueueName
                      AND Status = 'pending'
                      AND AvailableAt <= SYSUTCDATETIME()
                    ORDER BY AvailableAt, CreatedAt
                )
                UPDATE NextMessage
                SET Status = 'processing',
                    LockedAt = SYSUTCDATETIME(),
                    LockedBy = @WorkerId,
                    Attempts = Attempts + 1
                OUTPUT inserted.*;

                COMMIT TRANSACTION;
            END
            """);
    }
}
