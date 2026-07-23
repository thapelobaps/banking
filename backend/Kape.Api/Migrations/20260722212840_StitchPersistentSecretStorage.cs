using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kape.Api.Migrations
{
    /// <inheritdoc />
    public partial class StitchPersistentSecretStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StitchAuthorizationRequests",
                columns: table => new
                {
                    State = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProtectedPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StitchAuthorizationRequests", x => x.State);
                });

            migrationBuilder.CreateTable(
                name: "StitchConnectionSecrets",
                columns: table => new
                {
                    ExternalConnectionId = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccessTokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StitchConnectionSecrets", x => x.ExternalConnectionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StitchAuthorizationRequests_ExpiresAt",
                table: "StitchAuthorizationRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_StitchAuthorizationRequests_User_CreatedAt",
                table: "StitchAuthorizationRequests",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StitchConnectionSecrets_AccessTokenExpiresAt",
                table: "StitchConnectionSecrets",
                column: "AccessTokenExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_StitchConnectionSecrets_UpdatedAt",
                table: "StitchConnectionSecrets",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StitchAuthorizationRequests");

            migrationBuilder.DropTable(
                name: "StitchConnectionSecrets");
        }
    }
}
