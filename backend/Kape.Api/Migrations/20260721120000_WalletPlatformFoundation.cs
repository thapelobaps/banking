using Kape.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kape.Api.Migrations;

[DbContext(typeof(KapeDbContext))]
[Migration("20260721120000_WalletPlatformFoundation")]
public sealed class WalletPlatformFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE dbo.BankConnections (
                Id uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                ProviderId nvarchar(50) NOT NULL,
                InstitutionId nvarchar(100) NOT NULL,
                InstitutionName nvarchar(120) NOT NULL,
                ExternalConnectionId nvarchar(160) NOT NULL,
                Status nvarchar(30) NOT NULL,
                ConsentExpiresAt datetimeoffset(7) NULL,
                LastSyncedAt datetimeoffset(7) NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_BankConnections_CreatedAt DEFAULT SYSUTCDATETIME(),
                UpdatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_BankConnections_UpdatedAt DEFAULT SYSUTCDATETIME(),
                IsDeleted bit NOT NULL CONSTRAINT DF_BankConnections_IsDeleted DEFAULT 0,
                CONSTRAINT PK_BankConnections PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_BankConnections_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id)
            );

            CREATE TABLE dbo.LinkedBankAccounts (
                Id uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                BankConnectionId uniqueidentifier NOT NULL,
                ExternalAccountId nvarchar(160) NOT NULL,
                InstitutionName nvarchar(120) NOT NULL,
                AccountName nvarchar(120) NOT NULL,
                AccountType nvarchar(40) NOT NULL,
                AccountNumberMask nvarchar(12) NOT NULL,
                Currency char(3) NOT NULL,
                CurrentBalance decimal(18,2) NOT NULL,
                AvailableBalance decimal(18,2) NOT NULL,
                IsActive bit NOT NULL CONSTRAINT DF_LinkedBankAccounts_IsActive DEFAULT 1,
                LastSyncedAt datetimeoffset(7) NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_LinkedBankAccounts_CreatedAt DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_LinkedBankAccounts PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_LinkedBankAccounts_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id),
                CONSTRAINT FK_LinkedBankAccounts_BankConnections FOREIGN KEY (BankConnectionId) REFERENCES dbo.BankConnections(Id) ON DELETE CASCADE
            );

            CREATE TABLE dbo.LinkedBankTransactions (
                Id uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                LinkedBankAccountId uniqueidentifier NOT NULL,
                ExternalTransactionId nvarchar(160) NOT NULL,
                Description nvarchar(200) NOT NULL,
                MerchantName nvarchar(120) NULL,
                Amount decimal(18,2) NOT NULL,
                Direction nvarchar(10) NOT NULL,
                Category nvarchar(60) NOT NULL,
                Status nvarchar(30) NOT NULL,
                PostedAt datetimeoffset(7) NOT NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_LinkedBankTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_LinkedBankTransactions PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_LinkedBankTransactions_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id),
                CONSTRAINT FK_LinkedBankTransactions_LinkedBankAccounts FOREIGN KEY (LinkedBankAccountId) REFERENCES dbo.LinkedBankAccounts(Id) ON DELETE CASCADE,
                CONSTRAINT CK_LinkedBankTransactions_Direction CHECK (Direction IN ('credit','debit')),
                CONSTRAINT CK_LinkedBankTransactions_Amount CHECK (Amount >= 0)
            );

            CREATE TABLE dbo.DebitOrders (
                Id uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                LinkedBankAccountId uniqueidentifier NOT NULL,
                ExternalDebitOrderId nvarchar(160) NOT NULL,
                MerchantName nvarchar(120) NOT NULL,
                Amount decimal(18,2) NULL,
                Frequency nvarchar(30) NOT NULL,
                Status nvarchar(30) NOT NULL,
                NextRunAt datetimeoffset(7) NULL,
                LastRunAt datetimeoffset(7) NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_DebitOrders_CreatedAt DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_DebitOrders PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_DebitOrders_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id),
                CONSTRAINT FK_DebitOrders_LinkedBankAccounts FOREIGN KEY (LinkedBankAccountId) REFERENCES dbo.LinkedBankAccounts(Id) ON DELETE CASCADE,
                CONSTRAINT CK_DebitOrders_Amount CHECK (Amount IS NULL OR Amount >= 0)
            );

            CREATE TABLE dbo.PaymentMethods (
                Id uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                ProviderId nvarchar(50) NOT NULL,
                TokenReference nvarchar(240) NOT NULL,
                Brand nvarchar(30) NOT NULL,
                BankName nvarchar(100) NOT NULL,
                Last4 char(4) NOT NULL,
                ExpiryMonth int NOT NULL,
                ExpiryYear int NOT NULL,
                IsDefault bit NOT NULL CONSTRAINT DF_PaymentMethods_IsDefault DEFAULT 0,
                Status nvarchar(30) NOT NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_PaymentMethods_CreatedAt DEFAULT SYSUTCDATETIME(),
                VerifiedAt datetimeoffset(7) NULL,
                CONSTRAINT PK_PaymentMethods PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_PaymentMethods_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id),
                CONSTRAINT CK_PaymentMethods_ExpiryMonth CHECK (ExpiryMonth BETWEEN 1 AND 12),
                CONSTRAINT CK_PaymentMethods_Last4 CHECK (Last4 NOT LIKE '%[^0-9]%')
            );

            CREATE TABLE dbo.Wallets (
                Id uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                Currency char(3) NOT NULL,
                Status nvarchar(30) NOT NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_Wallets_CreatedAt DEFAULT SYSUTCDATETIME(),
                UpdatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_Wallets_UpdatedAt DEFAULT SYSUTCDATETIME(),
                RowVersion rowversion NOT NULL,
                CONSTRAINT PK_Wallets PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_Wallets_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id)
            );

            CREATE TABLE dbo.WalletTransactions (
                Id uniqueidentifier NOT NULL,
                WalletId uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                RelatedUserId uniqueidentifier NULL,
                PaymentMethodId uniqueidentifier NULL,
                Type nvarchar(40) NOT NULL,
                Amount decimal(18,2) NOT NULL,
                FeeAmount decimal(18,2) NOT NULL,
                NetAmount decimal(18,2) NOT NULL,
                Status nvarchar(30) NOT NULL,
                Reference nvarchar(160) NOT NULL,
                ExternalReference nvarchar(160) NULL,
                IdempotencyKey nvarchar(100) NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_WalletTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
                CompletedAt datetimeoffset(7) NULL,
                CONSTRAINT PK_WalletTransactions PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_WalletTransactions_Wallets FOREIGN KEY (WalletId) REFERENCES dbo.Wallets(Id),
                CONSTRAINT FK_WalletTransactions_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id),
                CONSTRAINT FK_WalletTransactions_PaymentMethods FOREIGN KEY (PaymentMethodId) REFERENCES dbo.PaymentMethods(Id),
                CONSTRAINT CK_WalletTransactions_Amounts CHECK (Amount > 0 AND FeeAmount >= 0 AND NetAmount > 0)
            );

            CREATE TABLE dbo.LedgerAccounts (
                Id uniqueidentifier NOT NULL,
                WalletId uniqueidentifier NULL,
                UserId uniqueidentifier NULL,
                Code nvarchar(80) NOT NULL,
                Name nvarchar(120) NOT NULL,
                AccountType nvarchar(30) NOT NULL,
                Currency char(3) NOT NULL,
                IsSystem bit NOT NULL CONSTRAINT DF_LedgerAccounts_IsSystem DEFAULT 0,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_LedgerAccounts_CreatedAt DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_LedgerAccounts PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_LedgerAccounts_Wallets FOREIGN KEY (WalletId) REFERENCES dbo.Wallets(Id),
                CONSTRAINT FK_LedgerAccounts_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id)
            );

            CREATE TABLE dbo.LedgerEntries (
                Id uniqueidentifier NOT NULL,
                JournalId uniqueidentifier NOT NULL,
                LedgerAccountId uniqueidentifier NOT NULL,
                WalletTransactionId uniqueidentifier NULL,
                EntryType nvarchar(10) NOT NULL,
                Amount decimal(18,2) NOT NULL,
                Reference nvarchar(160) NOT NULL,
                OccurredAt datetimeoffset(7) NOT NULL CONSTRAINT DF_LedgerEntries_OccurredAt DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_LedgerEntries PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_LedgerEntries_LedgerAccounts FOREIGN KEY (LedgerAccountId) REFERENCES dbo.LedgerAccounts(Id),
                CONSTRAINT FK_LedgerEntries_WalletTransactions FOREIGN KEY (WalletTransactionId) REFERENCES dbo.WalletTransactions(Id),
                CONSTRAINT CK_LedgerEntries_EntryType CHECK (EntryType IN ('debit','credit')),
                CONSTRAINT CK_LedgerEntries_Amount CHECK (Amount > 0)
            );

            CREATE TABLE dbo.VoucherCategories (
                Id uniqueidentifier NOT NULL,
                Slug nvarchar(80) NOT NULL,
                Name nvarchar(100) NOT NULL,
                SortOrder int NOT NULL,
                IsActive bit NOT NULL CONSTRAINT DF_VoucherCategories_IsActive DEFAULT 1,
                CONSTRAINT PK_VoucherCategories PRIMARY KEY CLUSTERED (Id)
            );

            CREATE TABLE dbo.VoucherProviders (
                Id uniqueidentifier NOT NULL,
                ProviderKey nvarchar(80) NOT NULL,
                Name nvarchar(120) NOT NULL,
                Status nvarchar(30) NOT NULL,
                LastCatalogueSyncAt datetimeoffset(7) NULL,
                CONSTRAINT PK_VoucherProviders PRIMARY KEY CLUSTERED (Id)
            );

            CREATE TABLE dbo.VoucherProducts (
                Id uniqueidentifier NOT NULL,
                CategoryId uniqueidentifier NOT NULL,
                ProviderId uniqueidentifier NOT NULL,
                ExternalProductId nvarchar(160) NOT NULL,
                Slug nvarchar(100) NOT NULL,
                BrandName nvarchar(100) NOT NULL,
                ProductName nvarchar(140) NOT NULL,
                Description nvarchar(600) NOT NULL,
                Currency char(3) NOT NULL,
                FulfilmentType nvarchar(30) NOT NULL,
                IsActive bit NOT NULL CONSTRAINT DF_VoucherProducts_IsActive DEFAULT 1,
                UpdatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_VoucherProducts_UpdatedAt DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_VoucherProducts PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_VoucherProducts_VoucherCategories FOREIGN KEY (CategoryId) REFERENCES dbo.VoucherCategories(Id),
                CONSTRAINT FK_VoucherProducts_VoucherProviders FOREIGN KEY (ProviderId) REFERENCES dbo.VoucherProviders(Id)
            );

            CREATE TABLE dbo.VoucherDenominations (
                Id uniqueidentifier NOT NULL,
                VoucherProductId uniqueidentifier NOT NULL,
                Amount decimal(18,2) NOT NULL,
                FeeAmount decimal(18,2) NOT NULL,
                IsActive bit NOT NULL CONSTRAINT DF_VoucherDenominations_IsActive DEFAULT 1,
                CONSTRAINT PK_VoucherDenominations PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_VoucherDenominations_VoucherProducts FOREIGN KEY (VoucherProductId) REFERENCES dbo.VoucherProducts(Id) ON DELETE CASCADE,
                CONSTRAINT CK_VoucherDenominations_Amounts CHECK (Amount > 0 AND FeeAmount >= 0)
            );

            CREATE TABLE dbo.VoucherOrders (
                Id uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                WalletId uniqueidentifier NOT NULL,
                VoucherProductId uniqueidentifier NOT NULL,
                VoucherDenominationId uniqueidentifier NOT NULL,
                WalletTransactionId uniqueidentifier NULL,
                Amount decimal(18,2) NOT NULL,
                FeeAmount decimal(18,2) NOT NULL,
                Status nvarchar(30) NOT NULL,
                ExternalOrderId nvarchar(160) NOT NULL,
                EncryptedVoucherCode nvarchar(1000) NOT NULL,
                IdempotencyKey nvarchar(100) NOT NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_VoucherOrders_CreatedAt DEFAULT SYSUTCDATETIME(),
                FulfilledAt datetimeoffset(7) NULL,
                CONSTRAINT PK_VoucherOrders PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_VoucherOrders_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id),
                CONSTRAINT FK_VoucherOrders_Wallets FOREIGN KEY (WalletId) REFERENCES dbo.Wallets(Id),
                CONSTRAINT FK_VoucherOrders_VoucherProducts FOREIGN KEY (VoucherProductId) REFERENCES dbo.VoucherProducts(Id),
                CONSTRAINT FK_VoucherOrders_VoucherDenominations FOREIGN KEY (VoucherDenominationId) REFERENCES dbo.VoucherDenominations(Id),
                CONSTRAINT FK_VoucherOrders_WalletTransactions FOREIGN KEY (WalletTransactionId) REFERENCES dbo.WalletTransactions(Id)
            );

            CREATE TABLE dbo.PrepaidOperators (
                Id uniqueidentifier NOT NULL,
                OperatorKey nvarchar(80) NOT NULL,
                Name nvarchar(120) NOT NULL,
                ProductType nvarchar(40) NOT NULL,
                IsActive bit NOT NULL CONSTRAINT DF_PrepaidOperators_IsActive DEFAULT 1,
                CONSTRAINT PK_PrepaidOperators PRIMARY KEY CLUSTERED (Id)
            );

            CREATE TABLE dbo.PrepaidProducts (
                Id uniqueidentifier NOT NULL,
                OperatorId uniqueidentifier NOT NULL,
                ExternalProductId nvarchar(160) NOT NULL,
                Name nvarchar(140) NOT NULL,
                ProductType nvarchar(40) NOT NULL,
                FixedAmount decimal(18,2) NULL,
                MinimumAmount decimal(18,2) NOT NULL,
                MaximumAmount decimal(18,2) NOT NULL,
                FeeAmount decimal(18,2) NOT NULL,
                IsActive bit NOT NULL CONSTRAINT DF_PrepaidProducts_IsActive DEFAULT 1,
                CONSTRAINT PK_PrepaidProducts PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_PrepaidProducts_PrepaidOperators FOREIGN KEY (OperatorId) REFERENCES dbo.PrepaidOperators(Id) ON DELETE CASCADE,
                CONSTRAINT CK_PrepaidProducts_Amounts CHECK (MinimumAmount >= 0 AND MaximumAmount >= MinimumAmount AND FeeAmount >= 0)
            );

            CREATE TABLE dbo.PrepaidOrders (
                Id uniqueidentifier NOT NULL,
                UserId uniqueidentifier NOT NULL,
                WalletId uniqueidentifier NOT NULL,
                PrepaidProductId uniqueidentifier NOT NULL,
                WalletTransactionId uniqueidentifier NULL,
                Recipient nvarchar(80) NOT NULL,
                Amount decimal(18,2) NOT NULL,
                FeeAmount decimal(18,2) NOT NULL,
                Status nvarchar(30) NOT NULL,
                ExternalOrderId nvarchar(160) NOT NULL,
                FulfilmentReference nvarchar(240) NULL,
                IdempotencyKey nvarchar(100) NOT NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_PrepaidOrders_CreatedAt DEFAULT SYSUTCDATETIME(),
                FulfilledAt datetimeoffset(7) NULL,
                CONSTRAINT PK_PrepaidOrders PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_PrepaidOrders_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id),
                CONSTRAINT FK_PrepaidOrders_Wallets FOREIGN KEY (WalletId) REFERENCES dbo.Wallets(Id),
                CONSTRAINT FK_PrepaidOrders_PrepaidProducts FOREIGN KEY (PrepaidProductId) REFERENCES dbo.PrepaidProducts(Id),
                CONSTRAINT FK_PrepaidOrders_WalletTransactions FOREIGN KEY (WalletTransactionId) REFERENCES dbo.WalletTransactions(Id)
            );

            CREATE TABLE dbo.PaymentRequests (
                Id uniqueidentifier NOT NULL,
                PayeeUserId uniqueidentifier NOT NULL,
                PayerUserId uniqueidentifier NULL,
                Amount decimal(18,2) NOT NULL,
                Currency char(3) NOT NULL,
                Message nvarchar(200) NOT NULL,
                Status nvarchar(30) NOT NULL,
                WalletTransactionId uniqueidentifier NULL,
                ExpiresAt datetimeoffset(7) NOT NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_PaymentRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
                RespondedAt datetimeoffset(7) NULL,
                CONSTRAINT PK_PaymentRequests PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT FK_PaymentRequests_Payee FOREIGN KEY (PayeeUserId) REFERENCES dbo.AspNetUsers(Id),
                CONSTRAINT FK_PaymentRequests_Payer FOREIGN KEY (PayerUserId) REFERENCES dbo.AspNetUsers(Id),
                CONSTRAINT FK_PaymentRequests_WalletTransactions FOREIGN KEY (WalletTransactionId) REFERENCES dbo.WalletTransactions(Id),
                CONSTRAINT CK_PaymentRequests_Amount CHECK (Amount > 0)
            );

            CREATE TABLE dbo.WebhookInbox (
                Id uniqueidentifier NOT NULL,
                ProviderType nvarchar(50) NOT NULL,
                ExternalEventId nvarchar(160) NOT NULL,
                EventType nvarchar(100) NOT NULL,
                Payload nvarchar(max) NOT NULL,
                Signature nvarchar(300) NOT NULL,
                Status nvarchar(30) NOT NULL,
                ReceivedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_WebhookInbox_ReceivedAt DEFAULT SYSUTCDATETIME(),
                ProcessedAt datetimeoffset(7) NULL,
                LastError nvarchar(1000) NULL,
                CONSTRAINT PK_WebhookInbox PRIMARY KEY CLUSTERED (Id)
            );

            CREATE TABLE dbo.QueueMessages (
                Id uniqueidentifier NOT NULL,
                QueueName nvarchar(80) NOT NULL,
                MessageType nvarchar(100) NOT NULL,
                Payload nvarchar(max) NOT NULL,
                Status nvarchar(30) NOT NULL,
                Attempts int NOT NULL CONSTRAINT DF_QueueMessages_Attempts DEFAULT 0,
                AvailableAt datetimeoffset(7) NOT NULL CONSTRAINT DF_QueueMessages_AvailableAt DEFAULT SYSUTCDATETIME(),
                LockedAt datetimeoffset(7) NULL,
                LockedBy nvarchar(100) NULL,
                ProcessedAt datetimeoffset(7) NULL,
                LastError nvarchar(2000) NULL,
                CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_QueueMessages_CreatedAt DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_QueueMessages PRIMARY KEY CLUSTERED (Id),
                CONSTRAINT CK_QueueMessages_Attempts CHECK (Attempts >= 0)
            );

            CREATE UNIQUE NONCLUSTERED INDEX UX_BankConnections_Provider_External ON dbo.BankConnections(ProviderId, ExternalConnectionId);
            CREATE NONCLUSTERED INDEX IX_BankConnections_User_Status ON dbo.BankConnections(UserId, Status) INCLUDE (InstitutionName, LastSyncedAt, ConsentExpiresAt);
            CREATE NONCLUSTERED INDEX IX_BankConnections_User_Institution_Active ON dbo.BankConnections(UserId, InstitutionId) WHERE IsDeleted = 0;

            CREATE UNIQUE NONCLUSTERED INDEX UX_LinkedBankAccounts_Connection_External ON dbo.LinkedBankAccounts(BankConnectionId, ExternalAccountId);
            CREATE NONCLUSTERED INDEX IX_LinkedBankAccounts_User_Active ON dbo.LinkedBankAccounts(UserId, IsActive) INCLUDE (InstitutionName, AccountName, CurrentBalance, AvailableBalance, Currency, LastSyncedAt);

            CREATE UNIQUE NONCLUSTERED INDEX UX_LinkedBankTransactions_Account_External ON dbo.LinkedBankTransactions(LinkedBankAccountId, ExternalTransactionId);
            CREATE NONCLUSTERED INDEX IX_LinkedBankTransactions_Account_PostedAt ON dbo.LinkedBankTransactions(LinkedBankAccountId, PostedAt DESC) INCLUDE (Amount, Direction, Category, Status, MerchantName, Description);
            CREATE NONCLUSTERED INDEX IX_LinkedBankTransactions_User_Category_PostedAt ON dbo.LinkedBankTransactions(UserId, Category, PostedAt DESC) INCLUDE (Amount, Direction);

            CREATE UNIQUE NONCLUSTERED INDEX UX_DebitOrders_Account_External ON dbo.DebitOrders(LinkedBankAccountId, ExternalDebitOrderId);
            CREATE NONCLUSTERED INDEX IX_DebitOrders_Account_Status_NextRun ON dbo.DebitOrders(LinkedBankAccountId, Status, NextRunAt) INCLUDE (MerchantName, Amount, Frequency);

            CREATE UNIQUE NONCLUSTERED INDEX UX_PaymentMethods_Provider_Token ON dbo.PaymentMethods(ProviderId, TokenReference);
            CREATE UNIQUE NONCLUSTERED INDEX UX_PaymentMethods_User_Default ON dbo.PaymentMethods(UserId) WHERE IsDefault = 1;
            CREATE NONCLUSTERED INDEX IX_PaymentMethods_User_Status ON dbo.PaymentMethods(UserId, Status) INCLUDE (BankName, Brand, Last4, ExpiryMonth, ExpiryYear, IsDefault);

            CREATE UNIQUE NONCLUSTERED INDEX UX_Wallets_UserId ON dbo.Wallets(UserId);
            CREATE UNIQUE NONCLUSTERED INDEX UX_WalletTransactions_User_Idempotency ON dbo.WalletTransactions(UserId, IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
            CREATE NONCLUSTERED INDEX IX_WalletTransactions_Wallet_CreatedAt ON dbo.WalletTransactions(WalletId, CreatedAt DESC) INCLUDE (Type, Amount, FeeAmount, NetAmount, Status, Reference, RelatedUserId);
            CREATE NONCLUSTERED INDEX IX_WalletTransactions_User_Status_CreatedAt ON dbo.WalletTransactions(UserId, Status, CreatedAt DESC) INCLUDE (Type, NetAmount);

            CREATE UNIQUE NONCLUSTERED INDEX UX_LedgerAccounts_Code ON dbo.LedgerAccounts(Code);
            CREATE NONCLUSTERED INDEX IX_LedgerAccounts_Wallet_Type ON dbo.LedgerAccounts(WalletId, AccountType) INCLUDE (Code, Currency);
            CREATE NONCLUSTERED INDEX IX_LedgerEntries_Account_OccurredAt ON dbo.LedgerEntries(LedgerAccountId, OccurredAt DESC) INCLUDE (JournalId, WalletTransactionId, EntryType, Amount, Reference);
            CREATE NONCLUSTERED INDEX IX_LedgerEntries_JournalId ON dbo.LedgerEntries(JournalId) INCLUDE (LedgerAccountId, EntryType, Amount);

            CREATE UNIQUE NONCLUSTERED INDEX UX_VoucherCategories_Slug ON dbo.VoucherCategories(Slug);
            CREATE NONCLUSTERED INDEX IX_VoucherCategories_Active_Sort ON dbo.VoucherCategories(IsActive, SortOrder) INCLUDE (Name, Slug);
            CREATE UNIQUE NONCLUSTERED INDEX UX_VoucherProviders_Key ON dbo.VoucherProviders(ProviderKey);
            CREATE UNIQUE NONCLUSTERED INDEX UX_VoucherProducts_Slug ON dbo.VoucherProducts(Slug);
            CREATE NONCLUSTERED INDEX IX_VoucherProducts_Provider_Category_Active ON dbo.VoucherProducts(ProviderId, CategoryId, IsActive) INCLUDE (BrandName, ProductName, Currency, FulfilmentType);
            CREATE NONCLUSTERED INDEX IX_VoucherProducts_Brand_Product ON dbo.VoucherProducts(BrandName, ProductName) INCLUDE (CategoryId, ProviderId, IsActive);
            CREATE UNIQUE NONCLUSTERED INDEX UX_VoucherDenominations_Product_Amount ON dbo.VoucherDenominations(VoucherProductId, Amount);
            CREATE UNIQUE NONCLUSTERED INDEX UX_VoucherOrders_User_Idempotency ON dbo.VoucherOrders(UserId, IdempotencyKey);
            CREATE NONCLUSTERED INDEX IX_VoucherOrders_User_CreatedAt ON dbo.VoucherOrders(UserId, CreatedAt DESC) INCLUDE (VoucherProductId, Amount, FeeAmount, Status, FulfilledAt);
            CREATE NONCLUSTERED INDEX IX_VoucherOrders_Status_CreatedAt ON dbo.VoucherOrders(Status, CreatedAt) INCLUDE (UserId, VoucherProductId);

            CREATE UNIQUE NONCLUSTERED INDEX UX_PrepaidOperators_Key ON dbo.PrepaidOperators(OperatorKey);
            CREATE NONCLUSTERED INDEX IX_PrepaidOperators_Type_Active ON dbo.PrepaidOperators(ProductType, IsActive) INCLUDE (Name);
            CREATE UNIQUE NONCLUSTERED INDEX UX_PrepaidProducts_Operator_External ON dbo.PrepaidProducts(OperatorId, ExternalProductId);
            CREATE NONCLUSTERED INDEX IX_PrepaidProducts_Type_Active ON dbo.PrepaidProducts(ProductType, IsActive) INCLUDE (OperatorId, Name, FixedAmount, MinimumAmount, MaximumAmount, FeeAmount);
            CREATE UNIQUE NONCLUSTERED INDEX UX_PrepaidOrders_User_Idempotency ON dbo.PrepaidOrders(UserId, IdempotencyKey);
            CREATE NONCLUSTERED INDEX IX_PrepaidOrders_User_CreatedAt ON dbo.PrepaidOrders(UserId, CreatedAt DESC) INCLUDE (PrepaidProductId, Recipient, Amount, FeeAmount, Status);
            CREATE NONCLUSTERED INDEX IX_PrepaidOrders_Status_CreatedAt ON dbo.PrepaidOrders(Status, CreatedAt) INCLUDE (UserId, PrepaidProductId);

            CREATE NONCLUSTERED INDEX IX_PaymentRequests_Payee_Status_CreatedAt ON dbo.PaymentRequests(PayeeUserId, Status, CreatedAt DESC) INCLUDE (PayerUserId, Amount, ExpiresAt, Message);
            CREATE NONCLUSTERED INDEX IX_PaymentRequests_Payer_Status_ExpiresAt ON dbo.PaymentRequests(PayerUserId, Status, ExpiresAt) INCLUDE (PayeeUserId, Amount, Message);
            CREATE UNIQUE NONCLUSTERED INDEX UX_WebhookInbox_Provider_Event ON dbo.WebhookInbox(ProviderType, ExternalEventId);
            CREATE NONCLUSTERED INDEX IX_WebhookInbox_Status_ReceivedAt ON dbo.WebhookInbox(Status, ReceivedAt) INCLUDE (ProviderType, EventType);
            CREATE NONCLUSTERED INDEX IX_QueueMessages_Dequeue ON dbo.QueueMessages(QueueName, Status, AvailableAt, CreatedAt) INCLUDE (MessageType, Attempts) WHERE Status = 'pending';
            CREATE NONCLUSTERED INDEX IX_QueueMessages_ProcessingLock ON dbo.QueueMessages(Status, LockedAt) INCLUDE (QueueName, LockedBy, Attempts) WHERE Status = 'processing';
            """);

        migrationBuilder.Sql(
            """
            CREATE OR ALTER FUNCTION dbo.fn_WalletBalance(@WalletId uniqueidentifier)
            RETURNS decimal(18,2)
            AS
            BEGIN
                DECLARE @Balance decimal(18,2);
                SELECT @Balance = COALESCE(SUM(CASE WHEN e.EntryType = 'credit' THEN e.Amount ELSE -e.Amount END), 0)
                FROM dbo.LedgerEntries e
                INNER JOIN dbo.LedgerAccounts a ON a.Id = e.LedgerAccountId
                WHERE a.WalletId = @WalletId AND a.AccountType = 'liability';
                RETURN COALESCE(@Balance, 0);
            END
            """);

        migrationBuilder.Sql(
            """
            CREATE OR ALTER FUNCTION dbo.fn_NormaliseSouthAfricanMobile(@Value nvarchar(32))
            RETURNS nvarchar(16)
            AS
            BEGIN
                DECLARE @Result nvarchar(32) = REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Value)), ' ', ''), '-', ''), '(', ''), ')', '');
                IF LEFT(@Result, 1) = '0' SET @Result = '+27' + SUBSTRING(@Result, 2, 31);
                IF LEFT(@Result, 2) = '27' SET @Result = '+' + @Result;
                RETURN LEFT(@Result, 16);
            END
            """);

        migrationBuilder.Sql(
            """
            CREATE OR ALTER PROCEDURE dbo.sp_GetWalletStatement
                @WalletId uniqueidentifier,
                @From datetimeoffset(7) = NULL,
                @To datetimeoffset(7) = NULL,
                @Offset int = 0,
                @PageSize int = 100
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT Id, WalletId, UserId, RelatedUserId, Type, Amount, FeeAmount, NetAmount, Status, Reference, ExternalReference, CreatedAt, CompletedAt
                FROM dbo.WalletTransactions
                WHERE WalletId = @WalletId
                  AND (@From IS NULL OR CreatedAt >= @From)
                  AND (@To IS NULL OR CreatedAt < @To)
                ORDER BY CreatedAt DESC
                OFFSET CASE WHEN @Offset < 0 THEN 0 ELSE @Offset END ROWS
                FETCH NEXT CASE WHEN @PageSize BETWEEN 1 AND 500 THEN @PageSize ELSE 100 END ROWS ONLY;
            END
            """);

        migrationBuilder.Sql(
            """
            CREATE OR ALTER PROCEDURE dbo.sp_GetLinkedAccountTransactions
                @LinkedBankAccountId uniqueidentifier,
                @From datetimeoffset(7) = NULL,
                @To datetimeoffset(7) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT Id, LinkedBankAccountId, Description, MerchantName, Amount, Direction, Category, Status, PostedAt
                FROM dbo.LinkedBankTransactions
                WHERE LinkedBankAccountId = @LinkedBankAccountId
                  AND (@From IS NULL OR PostedAt >= @From)
                  AND (@To IS NULL OR PostedAt < @To)
                ORDER BY PostedAt DESC;
            END
            """);

        migrationBuilder.Sql(
            """
            CREATE OR ALTER PROCEDURE dbo.sp_ReconcileWallet @WalletId uniqueidentifier
            AS
            BEGIN
                SET NOCOUNT ON;
                DECLARE @LedgerBalance decimal(18,2) = dbo.fn_WalletBalance(@WalletId);
                DECLARE @TransactionBalance decimal(18,2);
                SELECT @TransactionBalance = COALESCE(SUM(
                    CASE WHEN Type IN ('top_up','transfer_in','refund') THEN NetAmount ELSE -NetAmount END), 0)
                FROM dbo.WalletTransactions
                WHERE WalletId = @WalletId AND Status = 'completed';

                SELECT @WalletId AS WalletId,
                       @LedgerBalance AS LedgerBalance,
                       @TransactionBalance AS PostedWalletTransactions,
                       @LedgerBalance - @TransactionBalance AS Difference,
                       CASE WHEN @LedgerBalance = @TransactionBalance THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsBalanced,
                       SYSUTCDATETIME() AS ReconciledAt;
            END
            """);

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

        migrationBuilder.Sql(
            """
            CREATE OR ALTER PROCEDURE dbo.sp_ExpirePaymentRequests
            AS
            BEGIN
                SET NOCOUNT ON;
                UPDATE dbo.PaymentRequests
                SET Status = 'expired', RespondedAt = SYSUTCDATETIME()
                WHERE Status = 'pending' AND ExpiresAt <= SYSUTCDATETIME();
                SELECT @@ROWCOUNT AS ExpiredCount;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ExpirePaymentRequests;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_DequeueWalletMessage;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_ReconcileWallet;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetLinkedAccountTransactions;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_GetWalletStatement;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.fn_NormaliseSouthAfricanMobile;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.fn_WalletBalance;");

        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS dbo.QueueMessages;
            DROP TABLE IF EXISTS dbo.WebhookInbox;
            DROP TABLE IF EXISTS dbo.PaymentRequests;
            DROP TABLE IF EXISTS dbo.PrepaidOrders;
            DROP TABLE IF EXISTS dbo.PrepaidProducts;
            DROP TABLE IF EXISTS dbo.PrepaidOperators;
            DROP TABLE IF EXISTS dbo.VoucherOrders;
            DROP TABLE IF EXISTS dbo.VoucherDenominations;
            DROP TABLE IF EXISTS dbo.VoucherProducts;
            DROP TABLE IF EXISTS dbo.VoucherProviders;
            DROP TABLE IF EXISTS dbo.VoucherCategories;
            DROP TABLE IF EXISTS dbo.LedgerEntries;
            DROP TABLE IF EXISTS dbo.LedgerAccounts;
            DROP TABLE IF EXISTS dbo.WalletTransactions;
            DROP TABLE IF EXISTS dbo.Wallets;
            DROP TABLE IF EXISTS dbo.PaymentMethods;
            DROP TABLE IF EXISTS dbo.DebitOrders;
            DROP TABLE IF EXISTS dbo.LinkedBankTransactions;
            DROP TABLE IF EXISTS dbo.LinkedBankAccounts;
            DROP TABLE IF EXISTS dbo.BankConnections;
            """);
    }
}
