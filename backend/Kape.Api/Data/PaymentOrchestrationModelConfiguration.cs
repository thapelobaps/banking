using Kape.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kape.Api.Data;

public static class PaymentOrchestrationModelConfiguration
{
    public static void ConfigurePaymentOrchestration(this ModelBuilder builder)
    {
        builder.Entity<PaymentAttempt>(entity =>
        {
            entity.ToTable("PaymentAttempts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProviderId).HasMaxLength(80).IsRequired();
            entity.Property(item => item.PaymentSource).HasMaxLength(30).IsRequired();
            entity.Property(item => item.ExternalPaymentId).HasMaxLength(180).IsRequired();
            entity.Property(item => item.Amount).HasPrecision(18, 2);
            entity.Property(item => item.FeeAmount).HasPrecision(18, 2);
            entity.Property(item => item.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Scenario).HasMaxLength(50).IsRequired();
            entity.Property(item => item.Reference).HasMaxLength(180).IsRequired();
            entity.Property(item => item.RedirectUrl).HasMaxLength(1000);
            entity.Property(item => item.FailureCode).HasMaxLength(100);
            entity.Property(item => item.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.HasIndex(item => new { item.UserId, item.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("UX_PaymentAttempts_User_Idempotency");
            entity.HasIndex(item => new { item.ProviderId, item.ExternalPaymentId })
                .IsUnique()
                .HasDatabaseName("UX_PaymentAttempts_Provider_External");
            entity.HasIndex(item => new { item.UserId, item.Status, item.CreatedAt })
                .HasDatabaseName("IX_PaymentAttempts_User_Status_CreatedAt")
                .IncludeProperties(item => new
                {
                    item.PaymentSource,
                    item.Amount,
                    item.Currency,
                    item.VoucherOrderId,
                    item.PrepaidOrderId,
                });
            entity.HasIndex(item => item.VoucherOrderId)
                .HasFilter("[VoucherOrderId] IS NOT NULL")
                .HasDatabaseName("IX_PaymentAttempts_VoucherOrder");
            entity.HasIndex(item => item.PrepaidOrderId)
                .HasFilter("[PrepaidOrderId] IS NOT NULL")
                .HasDatabaseName("IX_PaymentAttempts_PrepaidOrder");
            entity.HasOne<VoucherOrder>()
                .WithMany()
                .HasForeignKey(item => item.VoucherOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PrepaidOrder>()
                .WithMany()
                .HasForeignKey(item => item.PrepaidOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LinkedBankAccount>()
                .WithMany()
                .HasForeignKey(item => item.LinkedBankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Wallet>()
                .WithMany()
                .HasForeignKey(item => item.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WalletTransaction>()
                .WithMany()
                .HasForeignKey(item => item.WalletTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentStatusHistory>(entity =>
        {
            entity.ToTable("PaymentStatusHistory");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PreviousStatus).HasMaxLength(40);
            entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Source).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Reason).HasMaxLength(500);
            entity.Property(item => item.ExternalEventId).HasMaxLength(180);
            entity.HasIndex(item => new { item.PaymentAttemptId, item.CreatedAt })
                .HasDatabaseName("IX_PaymentStatusHistory_Attempt_CreatedAt")
                .IncludeProperties(item => new { item.Status, item.Source, item.ExternalEventId });
            entity.HasOne<PaymentAttempt>()
                .WithMany()
                .HasForeignKey(item => item.PaymentAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WalletReservation>(entity =>
        {
            entity.ToTable("WalletReservations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Amount).HasPrecision(18, 2);
            entity.Property(item => item.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(item => item.Status).HasMaxLength(30).IsRequired();
            entity.Property(item => item.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.HasIndex(item => item.PaymentAttemptId)
                .IsUnique()
                .HasDatabaseName("UX_WalletReservations_PaymentAttempt");
            entity.HasIndex(item => new { item.UserId, item.Status, item.CreatedAt })
                .HasDatabaseName("IX_WalletReservations_User_Status_CreatedAt")
                .IncludeProperties(item => new { item.WalletId, item.Amount, item.Currency });
            entity.HasIndex(item => new { item.UserId, item.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("UX_WalletReservations_User_Idempotency");
            entity.HasOne<PaymentAttempt>()
                .WithMany()
                .HasForeignKey(item => item.PaymentAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Wallet>()
                .WithMany()
                .HasForeignKey(item => item.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentRefund>(entity =>
        {
            entity.ToTable("PaymentRefunds");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProviderId).HasMaxLength(80).IsRequired();
            entity.Property(item => item.ExternalRefundId).HasMaxLength(180).IsRequired();
            entity.Property(item => item.Amount).HasPrecision(18, 2);
            entity.Property(item => item.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(item => item.Status).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Reason).HasMaxLength(500).IsRequired();
            entity.Property(item => item.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.HasIndex(item => new { item.ProviderId, item.ExternalRefundId })
                .IsUnique()
                .HasDatabaseName("UX_PaymentRefunds_Provider_External");
            entity.HasIndex(item => new { item.PaymentAttemptId, item.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("UX_PaymentRefunds_Attempt_Idempotency");
            entity.HasIndex(item => new { item.UserId, item.Status, item.CreatedAt })
                .HasDatabaseName("IX_PaymentRefunds_User_Status_CreatedAt");
            entity.HasOne<PaymentAttempt>()
                .WithMany()
                .HasForeignKey(item => item.PaymentAttemptId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentReconciliationRun>(entity =>
        {
            entity.ToTable("PaymentReconciliationRuns");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Scope).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(item => new { item.UserId, item.StartedAt })
                .HasDatabaseName("IX_PaymentReconciliationRuns_User_StartedAt")
                .IncludeProperties(item => new
                {
                    item.CheckedPayments,
                    item.MatchedPayments,
                    item.IssueCount,
                    item.Status,
                });
        });

        builder.Entity<PaymentReconciliationIssue>(entity =>
        {
            entity.ToTable("PaymentReconciliationIssues");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.IssueType).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.Severity).HasMaxLength(20).IsRequired();
            entity.HasIndex(item => new { item.ReconciliationRunId, item.ResolvedAt })
                .HasDatabaseName("IX_PaymentReconciliationIssues_Run_ResolvedAt")
                .IncludeProperties(item => new { item.PaymentAttemptId, item.IssueType, item.Severity });
            entity.HasOne<PaymentReconciliationRun>()
                .WithMany()
                .HasForeignKey(item => item.ReconciliationRunId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PaymentAttempt>()
                .WithMany()
                .HasForeignKey(item => item.PaymentAttemptId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
