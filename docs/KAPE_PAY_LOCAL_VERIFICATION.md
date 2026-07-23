# Kape Pay local verification

This verification uses synthetic value and demo providers only.

## 1. Pull the branch

```powershell
git fetch origin
git switch agent/kape-payment-orchestration
git pull --ff-only
```

## 2. Apply database migrations

```powershell
dotnet ef database update --project backend/Kape.Api --startup-project backend/Kape.Api
```

Expected new tables:

- `PaymentAttempts`
- `PaymentStatusHistory`
- `WalletReservations`
- `PaymentRefunds`
- `PaymentReconciliationRuns`
- `PaymentReconciliationIssues`

## 3. Run automated verification

```powershell
dotnet restore backend/Kape.Api.sln
dotnet build backend/Kape.Api.sln --configuration Release --no-restore
dotnet test backend/Kape.Api.sln --configuration Release --no-build

npm ci
npm run type-check
npm run lint
npm run build
```

## 4. Start the applications

Start the API using the repository's local SQL Server configuration, then start Next.js:

```powershell
npm run dev
```

Use the existing demo user or register a new user.

## 5. Direct linked-bank voucher purchase

1. Open **Linked Banks** and confirm at least one active demo account is present.
2. Open **Vouchers**.
3. Select **Pay directly from linked bank**.
4. Choose an active account.
5. Select **Successful payment**.
6. Preview and complete the purchase.

Expected:

- A `PaymentAttempt` is created with source `linked_bank`.
- The provider is `demo-pay-by-bank`.
- The payment becomes `completed`.
- The order moves to `payment_completed`, then `processing`, then `fulfilled`.
- A fictional voucher is generated once.
- The Kape Wallet balance does not increase or decrease.

## 6. Direct linked-bank failure scenarios

Repeat checkout with each scenario:

- Awaiting approval
- Processing
- Declined
- Customer cancelled
- Insufficient funds

Expected:

- Failed/cancelled payments do not trigger fulfilment.
- Awaiting/pending payments can be inspected on **Kape Pay**.
- Order and payment statuses remain consistent.

## 7. Synthetic wallet purchase

1. Open **Vouchers** or **Prepaid**.
2. Select **Kape Demo Wallet — synthetic funds**.
3. Complete a purchase.

Expected:

- The existing wallet ledger is debited once.
- A `PaymentAttempt` is recorded with source `wallet`.
- Fulfilment completes through the durable queue.
- Ledger reconciliation remains balanced.

## 8. Refund a direct-bank payment

1. Open **Kape Pay**.
2. Find a completed direct-bank payment.
3. Select **Refund** and confirm.

Expected:

- One provider refund is created.
- Repeating the same provider idempotency key would return the same refund.
- Payment status becomes `refunded`.
- The original payment record remains unchanged in history.

## 9. Reverse a synthetic wallet purchase

1. Open **Kape Pay**.
2. Find a completed wallet payment.
3. Select **Reverse** and confirm.

Expected:

- The original purchase transaction remains in history.
- A new `<purchase_type>_reversal` transaction is created.
- The reversal credits the wallet liability through an opposite journal.
- Repeating reversal for the same original transaction does not credit twice.
- Ledger reconciliation remains balanced.

## 10. Reconciliation

On **Kape Pay**, select **Reconcile**.

Expected for a clean environment:

```text
Status: balanced
Issue count: 0
Matched payments: checked payments
```

Examples that should be flagged:

- Linked-bank payment without a linked account.
- Wallet payment without a wallet transaction.
- Fulfilled order without completed payment.
- Completed payment attached to a failed order.

## 11. Database checks

### Duplicate external provider references

```sql
SELECT ProviderId, ExternalPaymentId, COUNT(*) AS DuplicateCount
FROM PaymentAttempts
GROUP BY ProviderId, ExternalPaymentId
HAVING COUNT(*) > 1;
```

Expected: zero rows.

### Duplicate payment idempotency keys

```sql
SELECT UserId, IdempotencyKey, COUNT(*) AS DuplicateCount
FROM PaymentAttempts
GROUP BY UserId, IdempotencyKey
HAVING COUNT(*) > 1;
```

Expected: zero rows.

### Duplicate wallet reversals

```sql
SELECT UserId, ExternalReference, COUNT(*) AS DuplicateCount
FROM WalletTransactions
WHERE ExternalReference LIKE 'reversal_%'
GROUP BY UserId, ExternalReference
HAVING COUNT(*) > 1;
```

Expected: zero rows.

### Unbalanced journals

Use the journal-balance query from `docs/WALLET_FUNDING_UI_VERIFICATION.md`.

Expected: zero rows.

## 12. Safety checks

Confirm `backend/Kape.Api/appsettings.json` contains:

```json
{
  "Environment": "demo",
  "ActiveProvider": "demo-pay-by-bank",
  "RealFundsEnabled": false
}
```

Changing the environment to `production`, selecting an unsupported live provider, or setting `RealFundsEnabled` to `true` must make application startup validation fail.
