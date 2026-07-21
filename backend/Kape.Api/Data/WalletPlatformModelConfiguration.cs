using Kape.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kape.Api.Data;

public static class WalletPlatformModelConfiguration
{
    public static void ConfigureWalletPlatform(this ModelBuilder builder)
    {
        builder.Entity<BankConnection>(entity =>
        {
            entity.ToTable("BankConnections");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProviderId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.InstitutionId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.InstitutionName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ExternalConnectionId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.Status });
            entity.HasIndex(x => new { x.ProviderId, x.ExternalConnectionId }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.InstitutionId }).HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<LinkedBankAccount>(entity =>
        {
            entity.ToTable("LinkedBankAccounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalAccountId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.InstitutionName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AccountName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AccountType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.AccountNumberMask).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            entity.Property(x => x.AvailableBalance).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.UserId, x.IsActive });
            entity.HasIndex(x => new { x.BankConnectionId, x.ExternalAccountId }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.BankConnectionId, x.LastSyncedAt });
            entity.HasOne<BankConnection>().WithMany().HasForeignKey(x => x.BankConnectionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LinkedBankTransaction>(entity =>
        {
            entity.ToTable("LinkedBankTransactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalTransactionId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(200).IsRequired();
            entity.Property(x => x.MerchantName).HasMaxLength(120);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Direction).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.LinkedBankAccountId, x.PostedAt });
            entity.HasIndex(x => new { x.UserId, x.Category, x.PostedAt });
            entity.HasIndex(x => new { x.LinkedBankAccountId, x.ExternalTransactionId }).IsUnique();
            entity.HasOne<LinkedBankAccount>().WithMany().HasForeignKey(x => x.LinkedBankAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DebitOrder>(entity =>
        {
            entity.ToTable("DebitOrders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalDebitOrderId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.MerchantName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Frequency).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.LinkedBankAccountId, x.Status, x.NextRunAt });
            entity.HasIndex(x => new { x.LinkedBankAccountId, x.ExternalDebitOrderId }).IsUnique();
            entity.HasOne<LinkedBankAccount>().WithMany().HasForeignKey(x => x.LinkedBankAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("PaymentMethods");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProviderId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.TokenReference).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Brand).HasMaxLength(30).IsRequired();
            entity.Property(x => x.BankName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Last4).HasMaxLength(4).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.Status });
            entity.HasIndex(x => new { x.ProviderId, x.TokenReference }).IsUnique();
            entity.HasIndex(x => x.UserId).HasFilter("[IsDefault] = 1").IsUnique();
        });

        builder.Entity<Wallet>(entity =>
        {
            entity.ToTable("Wallets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<WalletTransaction>(entity =>
        {
            entity.ToTable("WalletTransactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.FeeAmount).HasPrecision(18, 2);
            entity.Property(x => x.NetAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ExternalReference).HasMaxLength(160);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100);
            entity.HasIndex(x => new { x.WalletId, x.CreatedAt });
            entity.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt });
            entity.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
            entity.HasOne<Wallet>().WithMany().HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LedgerAccount>(entity =>
        {
            entity.ToTable("LedgerAccounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AccountType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.WalletId, x.AccountType });
        });

        builder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("LedgerEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntryType).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Reference).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => new { x.LedgerAccountId, x.OccurredAt });
            entity.HasIndex(x => x.JournalId);
            entity.HasIndex(x => x.WalletTransactionId);
            entity.HasOne<LedgerAccount>().WithMany().HasForeignKey(x => x.LedgerAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WalletTransaction>().WithMany().HasForeignKey(x => x.WalletTransactionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<VoucherCategory>(entity =>
        {
            entity.ToTable("VoucherCategories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.SortOrder });
        });

        builder.Entity<VoucherProvider>(entity =>
        {
            entity.ToTable("VoucherProviders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProviderKey).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.ProviderKey).IsUnique();
        });

        builder.Entity<VoucherProduct>(entity =>
        {
            entity.ToTable("VoucherProducts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalProductId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            entity.Property(x => x.BrandName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ProductName).HasMaxLength(140).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(600).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.FulfilmentType).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.ProviderId, x.CategoryId, x.IsActive });
            entity.HasIndex(x => new { x.BrandName, x.ProductName });
            entity.HasOne<VoucherCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<VoucherProvider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<VoucherDenomination>(entity =>
        {
            entity.ToTable("VoucherDenominations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.FeeAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.VoucherProductId, x.Amount }).IsUnique();
            entity.HasOne<VoucherProduct>().WithMany().HasForeignKey(x => x.VoucherProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<VoucherOrder>(entity =>
        {
            entity.ToTable("VoucherOrders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.FeeAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ExternalOrderId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.EncryptedVoucherCode).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        builder.Entity<PrepaidOperator>(entity =>
        {
            entity.ToTable("PrepaidOperators");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorKey).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ProductType).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.OperatorKey).IsUnique();
            entity.HasIndex(x => new { x.ProductType, x.IsActive });
        });

        builder.Entity<PrepaidProduct>(entity =>
        {
            entity.ToTable("PrepaidProducts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalProductId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(140).IsRequired();
            entity.Property(x => x.ProductType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.FixedAmount).HasPrecision(18, 2);
            entity.Property(x => x.MinimumAmount).HasPrecision(18, 2);
            entity.Property(x => x.MaximumAmount).HasPrecision(18, 2);
            entity.Property(x => x.FeeAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.OperatorId, x.ExternalProductId }).IsUnique();
            entity.HasIndex(x => new { x.ProductType, x.IsActive });
            entity.HasOne<PrepaidOperator>().WithMany().HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PrepaidOrder>(entity =>
        {
            entity.ToTable("PrepaidOrders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Recipient).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.FeeAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ExternalOrderId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.FulfilmentReference).HasMaxLength(240);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        builder.Entity<PaymentRequest>(entity =>
        {
            entity.ToTable("PaymentRequests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.PayeeUserId, x.Status, x.CreatedAt });
            entity.HasIndex(x => new { x.PayerUserId, x.Status, x.ExpiresAt });
        });

        builder.Entity<WebhookInbox>(entity =>
        {
            entity.ToTable("WebhookInbox");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProviderType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ExternalEventId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Signature).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(1000);
            entity.HasIndex(x => new { x.ProviderType, x.ExternalEventId }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.ReceivedAt });
        });

        builder.Entity<QueueMessage>(entity =>
        {
            entity.ToTable("QueueMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.QueueName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MessageType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.LockedBy).HasMaxLength(100);
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex(x => new { x.QueueName, x.Status, x.AvailableAt });
            entity.HasIndex(x => new { x.Status, x.LockedAt });
        });
    }
}
