using Kape.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kape.Api.Migrations;

[DbContext(typeof(KapeDbContext))]
[Migration("20260723110500_PaymentOrchestrationFoundation")]
public sealed class PaymentOrchestrationFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PaymentAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VoucherOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                PrepaidOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LinkedBankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                WalletTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ProviderId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                PaymentSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                ExternalPaymentId = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                FeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Scenario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Reference = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                RedirectUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                FailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentAttempts", item => item.Id);
                table.ForeignKey(
                    name: "FK_PaymentAttempts_LinkedBankAccounts_LinkedBankAccountId",
                    column: item => item.LinkedBankAccountId,
                    principalTable: "LinkedBankAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PaymentAttempts_PrepaidOrders_PrepaidOrderId",
                    column: item => item.PrepaidOrderId,
                    principalTable: "PrepaidOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PaymentAttempts_VoucherOrders_VoucherOrderId",
                    column: item => item.VoucherOrderId,
                    principalTable: "VoucherOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PaymentAttempts_WalletTransactions_WalletTransactionId",
                    column: item => item.WalletTransactionId,
                    principalTable: "WalletTransactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PaymentAttempts_Wallets_WalletId",
                    column: item => item.WalletId,
                    principalTable: "Wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PaymentReconciliationRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Scope = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                CheckedPayments = table.Column<int>(type: "int", nullable: false),
                MatchedPayments = table.Column<int>(type: "int", nullable: false),
                IssueCount = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_PaymentReconciliationRuns", item => item.Id));

        migrationBuilder.CreateTable(
            name: "PaymentRefunds",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProviderId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                ExternalRefundId = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentRefunds", item => item.Id);
                table.ForeignKey(
                    name: "FK_PaymentRefunds_PaymentAttempts_PaymentAttemptId",
                    column: item => item.PaymentAttemptId,
                    principalTable: "PaymentAttempts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PaymentStatusHistory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PreviousStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ExternalEventId = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentStatusHistory", item => item.Id);
                table.ForeignKey(
                    name: "FK_PaymentStatusHistory_PaymentAttempts_PaymentAttemptId",
                    column: item => item.PaymentAttemptId,
                    principalTable: "PaymentAttempts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WalletReservations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WalletReservations", item => item.Id);
                table.ForeignKey(
                    name: "FK_WalletReservations_PaymentAttempts_PaymentAttemptId",
                    column: item => item.PaymentAttemptId,
                    principalTable: "PaymentAttempts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_WalletReservations_Wallets_WalletId",
                    column: item => item.WalletId,
                    principalTable: "Wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PaymentReconciliationIssues",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReconciliationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                IssueType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentReconciliationIssues", item => item.Id);
                table.ForeignKey(
                    name: "FK_PaymentReconciliationIssues_PaymentAttempts_PaymentAttemptId",
                    column: item => item.PaymentAttemptId,
                    principalTable: "PaymentAttempts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PaymentReconciliationIssues_PaymentReconciliationRuns_ReconciliationRunId",
                    column: item => item.ReconciliationRunId,
                    principalTable: "PaymentReconciliationRuns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttempts_LinkedBankAccountId",
            table: "PaymentAttempts",
            column: "LinkedBankAccountId");
        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttempts_PrepaidOrder",
            table: "PaymentAttempts",
            column: "PrepaidOrderId",
            filter: "[PrepaidOrderId] IS NOT NULL");
        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttempts_User_Status_CreatedAt",
            table: "PaymentAttempts",
            columns: new[] { "UserId", "Status", "CreatedAt" });
        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttempts_VoucherOrder",
            table: "PaymentAttempts",
            column: "VoucherOrderId",
            filter: "[VoucherOrderId] IS NOT NULL");
        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttempts_WalletId",
            table: "PaymentAttempts",
            column: "WalletId");
        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttempts_WalletTransactionId",
            table: "PaymentAttempts",
            column: "WalletTransactionId");
        migrationBuilder.CreateIndex(
            name: "UX_PaymentAttempts_Provider_External",
            table: "PaymentAttempts",
            columns: new[] { "ProviderId", "ExternalPaymentId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "UX_PaymentAttempts_User_Idempotency",
            table: "PaymentAttempts",
            columns: new[] { "UserId", "IdempotencyKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentReconciliationIssues_PaymentAttemptId",
            table: "PaymentReconciliationIssues",
            column: "PaymentAttemptId");
        migrationBuilder.CreateIndex(
            name: "IX_PaymentReconciliationIssues_Run_ResolvedAt",
            table: "PaymentReconciliationIssues",
            columns: new[] { "ReconciliationRunId", "ResolvedAt" });
        migrationBuilder.CreateIndex(
            name: "IX_PaymentReconciliationRuns_User_StartedAt",
            table: "PaymentReconciliationRuns",
            columns: new[] { "UserId", "StartedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentRefunds_User_Status_CreatedAt",
            table: "PaymentRefunds",
            columns: new[] { "UserId", "Status", "CreatedAt" });
        migrationBuilder.CreateIndex(
            name: "UX_PaymentRefunds_Attempt_Idempotency",
            table: "PaymentRefunds",
            columns: new[] { "PaymentAttemptId", "IdempotencyKey" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "UX_PaymentRefunds_Provider_External",
            table: "PaymentRefunds",
            columns: new[] { "ProviderId", "ExternalRefundId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentStatusHistory_Attempt_CreatedAt",
            table: "PaymentStatusHistory",
            columns: new[] { "PaymentAttemptId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_WalletReservations_User_Status_CreatedAt",
            table: "WalletReservations",
            columns: new[] { "UserId", "Status", "CreatedAt" });
        migrationBuilder.CreateIndex(
            name: "UX_WalletReservations_PaymentAttempt",
            table: "WalletReservations",
            column: "PaymentAttemptId",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "UX_WalletReservations_User_Idempotency",
            table: "WalletReservations",
            columns: new[] { "UserId", "IdempotencyKey" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PaymentReconciliationIssues");
        migrationBuilder.DropTable(name: "PaymentRefunds");
        migrationBuilder.DropTable(name: "PaymentStatusHistory");
        migrationBuilder.DropTable(name: "WalletReservations");
        migrationBuilder.DropTable(name: "PaymentReconciliationRuns");
        migrationBuilder.DropTable(name: "PaymentAttempts");
    }
}
