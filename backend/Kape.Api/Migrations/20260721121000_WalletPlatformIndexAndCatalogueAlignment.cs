using Kape.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kape.Api.Migrations;

[DbContext(typeof(KapeDbContext))]
[Migration("20260721121000_WalletPlatformIndexAndCatalogueAlignment")]
public sealed class WalletPlatformIndexAndCatalogueAlignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID('dbo.LinkedBankAccounts')
                  AND name = 'IX_LinkedBankAccounts_User_Connection_SyncedAt')
            BEGIN
                CREATE NONCLUSTERED INDEX IX_LinkedBankAccounts_User_Connection_SyncedAt
                    ON dbo.LinkedBankAccounts(UserId, BankConnectionId, LastSyncedAt);
            END;

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID('dbo.LedgerEntries')
                  AND name = 'IX_LedgerEntries_WalletTransactionId')
            BEGIN
                CREATE NONCLUSTERED INDEX IX_LedgerEntries_WalletTransactionId
                    ON dbo.LedgerEntries(WalletTransactionId);
            END;
            """);

        migrationBuilder.Sql(
            """
            DECLARE @ProviderId uniqueidentifier = '10000000-0000-4000-8000-000000000001';

            IF NOT EXISTS (SELECT 1 FROM dbo.VoucherProviders WHERE Id = @ProviderId)
            BEGIN
                INSERT dbo.VoucherProviders(Id, ProviderKey, Name, Status, LastCatalogueSyncAt)
                VALUES (
                    @ProviderId,
                    'demo-digital-products',
                    'Kape Demo Digital Products',
                    'active',
                    SYSUTCDATETIME());
            END;

            ;WITH Categories(Id, Slug, Name, SortOrder) AS (
                SELECT CAST('20000000-0000-4000-8000-000000000001' AS uniqueidentifier), 'entertainment', 'Entertainment', 1 UNION ALL
                SELECT CAST('20000000-0000-4000-8000-000000000002' AS uniqueidentifier), 'gaming', 'Gaming', 2 UNION ALL
                SELECT CAST('20000000-0000-4000-8000-000000000003' AS uniqueidentifier), 'shopping', 'Shopping', 3 UNION ALL
                SELECT CAST('20000000-0000-4000-8000-000000000004' AS uniqueidentifier), 'transport', 'Transport and delivery', 4
            )
            INSERT dbo.VoucherCategories(Id, Slug, Name, SortOrder, IsActive)
            SELECT source.Id, source.Slug, source.Name, source.SortOrder, 1
            FROM Categories source
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.VoucherCategories target WHERE target.Id = source.Id);

            ;WITH Products(Id, CategoryId, ExternalProductId, Slug, BrandName, ProductName, Description) AS (
                SELECT CAST('30000000-0000-4000-8000-000000000001' AS uniqueidentifier), CAST('20000000-0000-4000-8000-000000000001' AS uniqueidentifier), 'netflix', 'netflix', 'Netflix', 'Netflix Gift Card', 'Stream films and series.' UNION ALL
                SELECT CAST('30000000-0000-4000-8000-000000000002' AS uniqueidentifier), CAST('20000000-0000-4000-8000-000000000001' AS uniqueidentifier), 'spotify', 'spotify', 'Spotify', 'Spotify Premium', 'Music and podcast voucher.' UNION ALL
                SELECT CAST('30000000-0000-4000-8000-000000000003' AS uniqueidentifier), CAST('20000000-0000-4000-8000-000000000003' AS uniqueidentifier), 'amazon', 'amazon', 'Amazon', 'Amazon Gift Card', 'Digital shopping gift card.' UNION ALL
                SELECT CAST('30000000-0000-4000-8000-000000000004' AS uniqueidentifier), CAST('20000000-0000-4000-8000-000000000003' AS uniqueidentifier), 'google-play', 'google-play', 'Google Play', 'Google Play Gift Code', 'Apps, games and digital content.' UNION ALL
                SELECT CAST('30000000-0000-4000-8000-000000000005' AS uniqueidentifier), CAST('20000000-0000-4000-8000-000000000004' AS uniqueidentifier), 'uber', 'uber', 'Uber', 'Uber and Uber Eats', 'Rides and food delivery credit.' UNION ALL
                SELECT CAST('30000000-0000-4000-8000-000000000006' AS uniqueidentifier), CAST('20000000-0000-4000-8000-000000000002' AS uniqueidentifier), 'playstation', 'playstation', 'PlayStation', 'PlayStation Store', 'Games and add-ons.' UNION ALL
                SELECT CAST('30000000-0000-4000-8000-000000000007' AS uniqueidentifier), CAST('20000000-0000-4000-8000-000000000002' AS uniqueidentifier), 'xbox', 'xbox', 'Xbox', 'Xbox Gift Card', 'Games, subscriptions and add-ons.' UNION ALL
                SELECT CAST('30000000-0000-4000-8000-000000000008' AS uniqueidentifier), CAST('20000000-0000-4000-8000-000000000002' AS uniqueidentifier), 'steam', 'steam', 'Steam', 'Steam Wallet', 'PC games and digital content.'
            )
            INSERT dbo.VoucherProducts(
                Id,
                CategoryId,
                ProviderId,
                ExternalProductId,
                Slug,
                BrandName,
                ProductName,
                Description,
                Currency,
                FulfilmentType,
                IsActive,
                UpdatedAt)
            SELECT
                source.Id,
                source.CategoryId,
                @ProviderId,
                source.ExternalProductId,
                source.Slug,
                source.BrandName,
                source.ProductName,
                source.Description,
                'ZAR',
                'pin',
                1,
                SYSUTCDATETIME()
            FROM Products source
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.VoucherProducts target WHERE target.Id = source.Id);

            INSERT dbo.VoucherDenominations(Id, VoucherProductId, Amount, FeeAmount, IsActive)
            SELECT NEWID(), product.Id, denomination.Amount, 0, 1
            FROM dbo.VoucherProducts product
            CROSS APPLY (VALUES
                (CAST(100 AS decimal(18,2))),
                (CAST(250 AS decimal(18,2))),
                (CAST(500 AS decimal(18,2))),
                (CAST(1000 AS decimal(18,2)))) denomination(Amount)
            WHERE product.ProviderId = @ProviderId
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.VoucherDenominations existing
                  WHERE existing.VoucherProductId = product.Id
                    AND existing.Amount = denomination.Amount);
            """);

        migrationBuilder.Sql(
            """
            ;WITH Operators(Id, OperatorKey, Name, ProductType) AS (
                SELECT CAST('40000000-0000-4000-8000-000000000001' AS uniqueidentifier), 'vodacom', 'Vodacom', 'airtime' UNION ALL
                SELECT CAST('40000000-0000-4000-8000-000000000002' AS uniqueidentifier), 'mtn', 'MTN', 'airtime' UNION ALL
                SELECT CAST('40000000-0000-4000-8000-000000000003' AS uniqueidentifier), 'telkom', 'Telkom', 'airtime' UNION ALL
                SELECT CAST('40000000-0000-4000-8000-000000000004' AS uniqueidentifier), 'cell-c', 'Cell C', 'airtime' UNION ALL
                SELECT CAST('40000000-0000-4000-8000-000000000005' AS uniqueidentifier), 'prepaid-electricity', 'Prepaid Electricity', 'electricity'
            )
            INSERT dbo.PrepaidOperators(Id, OperatorKey, Name, ProductType, IsActive)
            SELECT source.Id, source.OperatorKey, source.Name, source.ProductType, 1
            FROM Operators source
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.PrepaidOperators target WHERE target.Id = source.Id);

            ;WITH Products(Id, OperatorId, ExternalProductId, Name, ProductType, MinimumAmount, MaximumAmount) AS (
                SELECT CAST('50000000-0000-4000-8000-000000000001' AS uniqueidentifier), CAST('40000000-0000-4000-8000-000000000001' AS uniqueidentifier), 'vodacom-airtime', 'Vodacom Airtime', 'airtime', CAST(5 AS decimal(18,2)), CAST(1000 AS decimal(18,2)) UNION ALL
                SELECT CAST('50000000-0000-4000-8000-000000000002' AS uniqueidentifier), CAST('40000000-0000-4000-8000-000000000002' AS uniqueidentifier), 'mtn-airtime', 'MTN Airtime', 'airtime', CAST(5 AS decimal(18,2)), CAST(1000 AS decimal(18,2)) UNION ALL
                SELECT CAST('50000000-0000-4000-8000-000000000003' AS uniqueidentifier), CAST('40000000-0000-4000-8000-000000000003' AS uniqueidentifier), 'telkom-airtime', 'Telkom Airtime', 'airtime', CAST(5 AS decimal(18,2)), CAST(1000 AS decimal(18,2)) UNION ALL
                SELECT CAST('50000000-0000-4000-8000-000000000004' AS uniqueidentifier), CAST('40000000-0000-4000-8000-000000000004' AS uniqueidentifier), 'cellc-airtime', 'Cell C Airtime', 'airtime', CAST(5 AS decimal(18,2)), CAST(1000 AS decimal(18,2)) UNION ALL
                SELECT CAST('50000000-0000-4000-8000-000000000005' AS uniqueidentifier), CAST('40000000-0000-4000-8000-000000000005' AS uniqueidentifier), 'electricity', 'Prepaid Electricity', 'electricity', CAST(20 AS decimal(18,2)), CAST(5000 AS decimal(18,2))
            )
            INSERT dbo.PrepaidProducts(
                Id,
                OperatorId,
                ExternalProductId,
                Name,
                ProductType,
                FixedAmount,
                MinimumAmount,
                MaximumAmount,
                FeeAmount,
                IsActive)
            SELECT
                source.Id,
                source.OperatorId,
                source.ExternalProductId,
                source.Name,
                source.ProductType,
                NULL,
                source.MinimumAmount,
                source.MaximumAmount,
                0,
                1
            FROM Products source
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.PrepaidProducts target WHERE target.Id = source.Id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS IX_LedgerEntries_WalletTransactionId ON dbo.LedgerEntries;
            DROP INDEX IF EXISTS IX_LinkedBankAccounts_User_Connection_SyncedAt ON dbo.LinkedBankAccounts;

            DELETE denomination
            FROM dbo.VoucherDenominations denomination
            INNER JOIN dbo.VoucherProducts product ON product.Id = denomination.VoucherProductId
            WHERE product.ProviderId = '10000000-0000-4000-8000-000000000001'
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.VoucherOrders orders
                  WHERE orders.VoucherDenominationId = denomination.Id);

            DELETE product
            FROM dbo.VoucherProducts product
            WHERE product.ProviderId = '10000000-0000-4000-8000-000000000001'
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.VoucherOrders orders
                  WHERE orders.VoucherProductId = product.Id);

            DELETE category
            FROM dbo.VoucherCategories category
            WHERE category.Id IN (
                '20000000-0000-4000-8000-000000000001',
                '20000000-0000-4000-8000-000000000002',
                '20000000-0000-4000-8000-000000000003',
                '20000000-0000-4000-8000-000000000004')
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.VoucherProducts product
                  WHERE product.CategoryId = category.Id);

            DELETE provider
            FROM dbo.VoucherProviders provider
            WHERE provider.Id = '10000000-0000-4000-8000-000000000001'
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.VoucherProducts product
                  WHERE product.ProviderId = provider.Id);

            DELETE product
            FROM dbo.PrepaidProducts product
            WHERE product.Id IN (
                '50000000-0000-4000-8000-000000000001',
                '50000000-0000-4000-8000-000000000002',
                '50000000-0000-4000-8000-000000000003',
                '50000000-0000-4000-8000-000000000004',
                '50000000-0000-4000-8000-000000000005')
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.PrepaidOrders orders
                  WHERE orders.PrepaidProductId = product.Id);

            DELETE operator
            FROM dbo.PrepaidOperators operator
            WHERE operator.Id IN (
                '40000000-0000-4000-8000-000000000001',
                '40000000-0000-4000-8000-000000000002',
                '40000000-0000-4000-8000-000000000003',
                '40000000-0000-4000-8000-000000000004',
                '40000000-0000-4000-8000-000000000005')
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.PrepaidProducts product
                  WHERE product.OperatorId = operator.Id);
            """);
    }
}
