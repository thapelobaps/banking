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
            entity.HasIndex(x => new { x.UserId, x.Status })
                .HasDatabaseName("IX_BankConnections_User_Status")
                .IncludeProperties(x => new { x.InstitutionName, x.LastSyncedAt, x.ConsentExpiresAt });
            entity.HasIndex(x => new { x.ProviderId, x.ExternalConnectionId })
                .IsUnique()
                .HasDatabaseName("UX_BankConnections_Provider_External");
            entity.HasIndex(x => new { x.UserId, x.InstitutionId })
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("IX_BankConnections_User_Institution_Active");
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
            entity.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            entity.Property(x => x.AvailableBalance).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.UserId, x.IsActive })
                .HasDatabaseName("IX_LinkedBankAccounts_User_Active")
                .IncludeProperties(x => new
                {
                    x.InstitutionName,
                    x.AccountName,
                    x.CurrentBalance,
                    x.AvailableBalance,
                    x.Currency,
                    x.LastSyncedAt,
                });
            entity.HasIndex(x => new { x.BankConnectionId, x.ExternalAccountId })
                .IsUnique()
                .HasDatabaseName("UX_LinkedBankAccounts_Connection_External");
            entity.HasIndex(x => new { x.UserId, x.BankConnectionId, x.LastSyncedAt })
                .HasDatabaseName("IX_LinkedBankAccounts_User_Connection_SyncedAt");
            entity.HasOne<BankConnection>()
                .WithMany()
                .HasForeignKey(x => x.BankConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.HasIndex(x => new { x.LinkedBankAccountId, x.PostedAt })
                .HasDatabaseName("IX_LinkedBankTransactions_Account_PostedAt")
                .IncludeProperties(x => new
                {
                    x.Amount,
                    x.Direction,
                    x.Category,
                    x.Status,
                    x.MerchantName,
                    x.Description,
                });
            entity.HasIndex(x => new { x.UserId, x.Category, x.PostedAt })
                .HasDatabaseName("IX_LinkedBankTransactions_User_Category_PostedAt")
                .IncludeProperties(x => new { x.Amount, x.Direction });
            entity.HasIndex(x => new { x.LinkedBankAccountId, x.ExternalTransactionId })
                .IsUnique()
                .HasDatabaseName("UX_LinkedBankTransactions_Account_External");
            entity.HasOne<LinkedBankAccount>()
                .WithMany()
                .HasForeignKey(x => x.LinkedBankAccountId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.HasIndex(x => new { x.LinkedBankAccountId, x.Status, x.NextRunAt })
                .HasDatabaseName("IX_DebitOrders_Account_Status_NextRun")
                .IncludeProperties(x => new { x.MerchantName, x.Amount, x.Frequency });
            entity.HasIndex(x => new { x.LinkedBankAccountId, x.ExternalDebitOrderId })
                .IsUnique()
                .HasDatabaseName("UX_DebitOrders_Account_External");
            entity.HasOne<LinkedBankAccount>()
                .WithMany()
                .HasForeignKey(x => x.LinkedBankAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("PaymentMethods");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProviderId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.TokenReference).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Brand).HasMaxLength(30).IsRequired();
            entity.Property(x => x.BankName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Last4).HasMaxLength(4).IsFixedLength().IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.Status })
                .HasDatabaseName("IX_PaymentMethods_User_Status")
                .IncludeProperties(x => new
                {
                    x.BankName,
                    x.Brand,
                    x.Last4,
                    x.ExpiryMonth,
                    x.ExpiryYear,
                    x.IsDefault,
                });
            entity.HasIndex(x => new { x.ProviderId, x.TokenReference })
                .IsUnique()
                .HasDatabaseName("UX_PaymentMethods_Provider_Token");
            entity.HasIndex(x => x.UserId)
                .HasFilter("[IsDefault] = 1")
                .IsUnique()
                .HasDatabaseName("UX_PaymentMethods_User_Default");
        });

        builder.Entity<Wallet>(entity =>
        {
            entity.ToTable("Wallets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.UserId)
                .IsUnique()
                .HasDatabaseName("UX_Wallets_UserId");
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
            entity.HasIndex(x => new { x.WalletId, x.CreatedAt })
                .HasDatabaseName("IX_WalletTransactions_Wallet_CreatedAt")
                .IncludeProperties(x => new
                {
                    x.Type,
                    x.Amount,
                    x.FeeAmount,
                    x.NetAmount,
                    x.Status,
                    x.Reference,
                    x.RelatedUserId,
                });
            entity.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt })
                .HasDatabaseName("IX_WalletTransactions_User_Status_CreatedAt")
                .IncludeProperties(x => new { x.Type, x.NetAmount });
            entity.HasIndex(x => new { x.UserId, x.IdempotencyKey })
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL")
                .HasDatabaseName("UX_WalletTransactions_User_Idempotency");
            entity.HasOne<Wallet>()
                .WithMany()
                .HasForeignKey(x => x.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LedgerAccount>(entity =>
        {
            entity.ToTable("LedgerAccounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AccountType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.HasIndex(x => x.Code)
                .IsUnique()
                .HasDatabaseName("UX_LedgerAccounts_Code");
            entity.HasIndex(x => new { x.WalletId, x.AccountType })
                .HasDatabaseName("IX_LedgerAccounts_Wallet_Type")
                .IncludeProperties(x => new { x.Code, x.Currency });
        });

        builder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("LedgerEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntryType).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Reference).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => new { x.LedgerAccountId, x.OccurredAt })
                .HasDatabaseName("IX_LedgerEntries_Account_OccurredAt")
                .IncludeProperties(x => new
                {
                    x.JournalId,
                    x.WalletTransactionId,
                    x.EntryType,
                    x.Amount,
                    x.Reference,
                });
            entity.HasIndex(x => x.JournalId)
                .HasDatabaseName("IX_LedgerEntries_JournalId")
                .IncludeProperties(x => new { x.LedgerAccountId, x.EntryType, x.Amount });
            entity.HasIndex(x => x.WalletTransactionId)
                .HasDatabaseName("IX_LedgerEntries_WalletTransactionId");
            entity.HasOne<LedgerAccount>()
                .WithMany()
                .HasForeignKey(x => x.LedgerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WalletTransaction>()
                .WithMany()
                .HasForeignKey(x => x.WalletTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<VoucherCategory>(entity =>
        {
            entity.ToTable("VoucherCategories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Slug)
                .IsUnique()
                .HasDatabaseName("UX_VoucherCategories_Slug");
            entity.HasIndex(x => new { x.IsActive, x.SortOrder })
                .HasDatabaseName("IX_VoucherCategories_Active_Sort")
                .IncludeProperties(x => new { x.Name, x.Slug });
        });

        builder.Entity<VoucherProvider>(entity =>
        {
            entity.ToTable("VoucherProviders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProviderKey).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.ProviderKey)
                .IsUnique()
                .HasDatabaseName("UX_VoucherProviders_Key");
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
            entity.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.FulfilmentType).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.Slug)
                .IsUnique()
                .HasDatabaseName("UX_VoucherProducts_Slug");
            entity.HasIndex(x => new { x.ProviderId, x.CategoryId, x.IsActive })
                .HasDatabaseName("IX_VoucherProducts_Provider_Category_Active")
                .IncludeProperties(x => new { x.BrandName, x.ProductName, x.Currency, x.FulfilmentType });
            entity.HasIndex(x => new { x.BrandName, x.ProductName })
                .HasDatabaseName("IX_VoucherProducts_Brand_Product")
                .IncludeProperties(x => new { x.CategoryId, x.ProviderId, x.IsActive });
            entity.HasOne<VoucherCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<VoucherProvider>()
                .WithMany()
                .HasForeignKey(x => x.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<VoucherDenomination>(entity =>
        {
            entity.ToTable("VoucherDenominations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.FeeAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.VoucherProductId, x.Amount })
                .IsUnique()
                .HasDatabaseName("UX_VoucherDenominations_Product_Amount");
            entity.HasOne<VoucherProduct>()
                .WithMany()
                .HasForeignKey(x => x.VoucherProductId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.HasIndex(x => new { x.UserId, x.CreatedAt })
                .HasDatabaseName("IX_VoucherOrders_User_CreatedAt")
                .IncludeProperties(x => new
                {
                    x.VoucherProductId,
                    x.Amount,
                    x.FeeAmount,
                    x.Status,
                    x.FulfilledAt,
                });
            entity.HasIndex(x => new { x.UserId, x.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("UX_VoucherOrders_User_Idempotency");
            entity.HasIndex(x => new { x.Status, x.CreatedAt })
                .HasDatabaseName("IX_VoucherOrders_Status_CreatedAt")
                .IncludeProperties(x => new { x.UserId, x.VoucherProductId });
        });

        builder.Entity<PrepaidOperator>(entity =>
        {
            entity.ToTable("PrepaidOperators");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorKey).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ProductType).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.OperatorKey)
                .IsUnique()
                .HasDatabaseName("UX_PrepaidOperators_Key");
            entity.HasIndex(x => new { x.ProductType, x.IsActive })
                .HasDatabaseName("IX_PrepaidOperators_Type_Active")
                .IncludeProperties(x => x.Name);
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
            entity.HasIndex(x => new { x.OperatorId, x.ExternalProductId })
                .IsUnique()
                .HasDatabaseName("UX_PrepaidProducts_Operator_External");
            entity.HasIndex(x => new { x.ProductType, x.IsActive })
                .HasDatabaseName("IX_PrepaidProducts_Type_Active")
                .IncludeProperties(x => new
                {
                    x.OperatorId,
                    x.Name,
                    x.FixedAmount,
                    x.MinimumAmount,
                    x.MaximumAmount,
                    x.FeeAmount,
                });
            entity.HasOne<PrepaidOperator>()
                .WithMany()
                .HasForeignKey(x => x.OperatorId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.HasIndex(x => new { x.UserId, x.CreatedAt })
                .HasDatabaseName("IX_PrepaidOrders_User_CreatedAt")
                .IncludeProperties(x => new
                {
                    x.PrepaidProductId,
                    x.Recipient,
                    x.Amount,
                    x.FeeAmount,
                    x.Status,
                });
            entity.HasIndex(x => new { x.UserId, x.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("UX_PrepaidOrders_User_Idempotency");
            entity.HasIndex(x => new { x.Status, x.CreatedAt })
                .HasDatabaseName("IX_PrepaidOrders_Status_CreatedAt")
                .IncludeProperties(x => new { x.UserId, x.PrepaidProductId });
        });

        builder.Entity<PaymentRequest>(entity =>
        {
            entity.ToTable("PaymentRequests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(x => x.Message).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.PayeeUserId, x.Status, x.CreatedAt })
                .HasDatabaseName("IX_PaymentRequests_Payee_Status_CreatedAt")
                .IncludeProperties(x => new { x.PayerUserId, x.Amount, x.ExpiresAt, x.Message });
            entity.HasIndex(x => new { x.PayerUserId, x.Status, x.ExpiresAt })
                .HasDatabaseName("IX_PaymentRequests_Payer_Status_ExpiresAt")
                .IncludeProperties(x => new { x.PayeeUserId, x.Amount, x.Message });
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
            entity.HasIndex(x => new { x.ProviderType, x.ExternalEventId })
                .IsUnique()
                .HasDatabaseName("UX_WebhookInbox_Provider_Event");
            entity.HasIndex(x => new { x.Status, x.ReceivedAt })
                .HasDatabaseName("IX_WebhookInbox_Status_ReceivedAt")
                .IncludeProperties(x => new { x.ProviderType, x.EventType });
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
            entity.HasIndex(x => new { x.QueueName, x.Status, x.AvailableAt, x.CreatedAt })
                .HasFilter("[Status] = 'pending'")
                .HasDatabaseName("IX_QueueMessages_Dequeue")
                .IncludeProperties(x => new { x.MessageType, x.Attempts });
            entity.HasIndex(x => new { x.Status, x.LockedAt })
                .HasFilter("[Status] = 'processing'")
                .HasDatabaseName("IX_QueueMessages_ProcessingLock")
                .IncludeProperties(x => new { x.QueueName, x.LockedBy, x.Attempts });
        });
    }
}
