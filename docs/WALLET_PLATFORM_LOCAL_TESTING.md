# Kape Wallet Platform — Local Verification

This phase uses demo providers, SQL Server, a double-entry ledger, tokenised demo payment methods, an encrypted voucher-code store, and a durable SQL queue. No real bank is contacted and no real money moves.

## 1. Switch to the Phase 3 branch

```powershell
git fetch origin
git switch agent/kape-phase-3-wallet-platform
git pull --ff-only origin agent/kape-phase-3-wallet-platform
git status
git log -1 --oneline
```

## 2. Stop running applications

Stop the Next.js and API terminals with `Ctrl+C` before applying the migration.

## 3. Install or update EF Core CLI

```powershell
dotnet tool update --global dotnet-ef --version 10.0.9
```

When the tool is not installed yet:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.9
```

Verify:

```powershell
dotnet ef --version
```

## 4. Build and test

```powershell
dotnet restore backend/Kape.Api.Tests/Kape.Api.Tests.csproj

dotnet build backend/Kape.Api.Tests/Kape.Api.Tests.csproj `
  --configuration Release `
  --no-restore

dotnet test backend/Kape.Api.Tests/Kape.Api.Tests.csproj `
  --configuration Release `
  --no-build

npm ci
npm run typecheck
npm run lint
npm run build
```

## 5. Review the pending migration

```powershell
dotnet ef migrations list `
  --project backend/Kape.Api/Kape.Api.csproj `
  --startup-project backend/Kape.Api/Kape.Api.csproj
```

The list must contain:

```text
20260721120000_WalletPlatformFoundation
```

Create a SQL script before changing the database:

```powershell
dotnet ef migrations script `
  --idempotent `
  --project backend/Kape.Api/Kape.Api.csproj `
  --startup-project backend/Kape.Api/Kape.Api.csproj `
  --output wallet-platform-migration.sql
```

Review `wallet-platform-migration.sql`. It creates the wallet-platform tables, constraints, indexes, SQL functions, stored procedures, and queue procedure.

## 6. Back up the local development database

This migration adds tables and SQL objects but always take a backup before applying schema changes to a database containing useful development data.

The default database from `backend/Kape.Api/appsettings.json` is:

```text
KapeApp
```

## 7. Apply the migration

```powershell
dotnet ef database update `
  --project backend/Kape.Api/Kape.Api.csproj `
  --startup-project backend/Kape.Api/Kape.Api.csproj
```

Verify the applied migration:

```powershell
dotnet ef migrations list `
  --project backend/Kape.Api/Kape.Api.csproj `
  --startup-project backend/Kape.Api/Kape.Api.csproj
```

## 8. Configure local development values

Do not commit secrets. Set them in the API terminal or use .NET user secrets.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5000"
$env:Jwt__SigningKey = "replace-with-a-local-key-longer-than-thirty-two-characters"
$env:Webhooks__SharedSecret = "replace-with-a-local-webhook-secret"
```

The webhook secret is only needed when testing provider webhook endpoints.

## 9. Start the API and frontend

API terminal:

```powershell
dotnet run --project backend/Kape.Api --no-launch-profile
```

Frontend terminal:

```powershell
npm run dev
```

Open:

```text
Frontend: http://localhost:3000
Swagger:  http://localhost:5000/swagger
```

## 10. Swagger verification order

### Authentication

1. Run `POST /api/auth/login`.
2. Copy the access token.
3. Select **Authorize** in Swagger and paste the token.

### Wallet creation and balance

1. `GET /api/wallet`
2. `GET /api/wallet/balance`
3. `GET /api/ledger/accounts`
4. `GET /api/ledger/reconciliation`

A newly created wallet starts at `R0.00` and must reconcile with a zero difference.

### Demo bank connection

Create a session:

```json
{
  "providerId": "demo",
  "institutionId": "capitec",
  "returnUrl": "http://localhost:3000/my-banks"
}
```

Use the returned `connectionId` with `POST /api/bank-connections/callback`:

```json
{
  "connectionId": "PASTE_CONNECTION_ID",
  "authorizationCode": "demo-authorised",
  "state": "capitec"
}
```

Then verify:

1. `GET /api/bank-connections`
2. `GET /api/linked-accounts`
3. `GET /api/linked-accounts/{id}/balances`
4. `GET /api/linked-accounts/{id}/transactions`
5. `GET /api/linked-accounts/{id}/debit-orders`
6. `POST /api/bank-connections/{id}/sync`

The demo sync creates linked Capitec and Standard Bank accounts with fictional data.

### Tokenised demo payment method

Create setup:

```json
{
  "providerId": "demo",
  "returnUrl": "http://localhost:3000/my-banks"
}
```

Confirm using fictional values only:

```json
{
  "setupSessionId": "PASTE_SETUP_SESSION_ID",
  "paymentToken": "demo-processor-token-001",
  "brand": "Mastercard",
  "bankName": "Capitec",
  "last4": "3684",
  "expiryMonth": 12,
  "expiryYear": 2030
}
```

Kape hashes the demo processor token and stores only a token reference and masked metadata. Never enter a real card number or CVV.

### Wallet top-up

Use the returned payment-method ID:

Preview:

```json
{
  "amount": 2000,
  "paymentMethodId": "PASTE_PAYMENT_METHOD_ID",
  "linkedBankAccountId": null,
  "reference": "Demo wallet funding"
}
```

Create:

```json
{
  "amount": 2000,
  "paymentMethodId": "PASTE_PAYMENT_METHOD_ID",
  "linkedBankAccountId": null,
  "reference": "Demo wallet funding",
  "idempotencyKey": "local-topup-001"
}
```

Repeat the same request with the same idempotency key. It must return the original transaction instead of creating a duplicate.

Then verify:

1. `GET /api/wallet/balance`
2. `GET /api/wallet/transactions`
3. `GET /api/ledger/entries`
4. `GET /api/ledger/reconciliation`

### Voucher order

1. `GET /api/vouchers/categories`
2. `GET /api/vouchers/products`
3. `GET /api/vouchers/products/{id}/denominations`
4. `POST /api/voucher-orders/quote`
5. `POST /api/voucher-orders`
6. Poll `GET /api/voucher-orders/{id}` until status becomes `fulfilled`.

Example create request:

```json
{
  "voucherProductId": "PASTE_PRODUCT_ID",
  "voucherDenominationId": "PASTE_DENOMINATION_ID",
  "idempotencyKey": "local-voucher-001"
}
```

The background worker fulfils the demo order through the SQL queue. Voucher codes are protected with ASP.NET Core Data Protection before storage.

### Prepaid order

1. `GET /api/prepaid/operators`
2. `GET /api/prepaid/products`
3. `POST /api/prepaid/validate-recipient`
4. `POST /api/prepaid/orders/quote`
5. `POST /api/prepaid/orders`
6. Poll `GET /api/prepaid/orders/{id}` until status becomes `fulfilled`.

Use a fictional South African-format number such as `0821234567`; do not use another person's real number.

### User-to-user wallet transfer

Create a second demo Kape user, fund that user's wallet, and resolve the recipient through:

```json
{
  "identifier": "second-demo-user@example.com"
}
```

Then test:

1. `POST /api/wallet/transfers/preview`
2. `POST /api/wallet/transfers`
3. `POST /api/wallet/request-money`
4. `GET /api/wallet/payment-requests`
5. `POST /api/wallet/payment-requests/{id}/pay`
6. `POST /api/wallet/payment-requests/{id}/decline`

Use a unique idempotency key for each intended transfer.

## 11. SQL Server verification

Run these queries in SQL Server Management Studio:

```sql
SELECT MigrationId
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;

SELECT name, type_desc
FROM sys.indexes
WHERE object_id IN (
    OBJECT_ID('dbo.WalletTransactions'),
    OBJECT_ID('dbo.LedgerEntries'),
    OBJECT_ID('dbo.QueueMessages'),
    OBJECT_ID('dbo.LinkedBankTransactions'))
  AND name IS NOT NULL
ORDER BY object_id, name;

SELECT name, type_desc
FROM sys.objects
WHERE name IN (
    'fn_WalletBalance',
    'fn_NormaliseSouthAfricanMobile',
    'sp_GetWalletStatement',
    'sp_GetLinkedAccountTransactions',
    'sp_ReconcileWallet',
    'sp_DequeueWalletMessage',
    'sp_ExpirePaymentRequests')
ORDER BY name;

SELECT TOP (20)
    QueueName,
    MessageType,
    Status,
    Attempts,
    AvailableAt,
    LockedAt,
    ProcessedAt,
    LastError
FROM dbo.QueueMessages
ORDER BY CreatedAt DESC;
```

## 12. Expected safety properties

- Every protected query is scoped to the authenticated user.
- Payment methods store token references and masked metadata, not full card numbers or CVVs.
- Wallet writes use serializable SQL transactions where balances can change.
- Ledger journals create equal debit and credit entries.
- Payment and order idempotency keys prevent duplicate processing.
- Webhook events are unique per provider and external event ID.
- The queue uses an atomic stored procedure with `UPDLOCK`, `READPAST`, and row locking.
- Voucher codes are encrypted at rest through Data Protection.
- All current providers are demo adapters; real banking, processor, and voucher providers require separate credentials, compliance, and sandbox certification.
