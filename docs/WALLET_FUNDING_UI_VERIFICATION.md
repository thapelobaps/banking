# Kape Wallet Funding UI Verification

This checklist verifies tokenised-card management, wallet top-ups, linked-bank withdrawals, double-entry ledger integrity and transaction-history visibility while the demo providers are active.

No real card details or real money should be used.

## Automated checks

```powershell
git fetch origin
git switch agent/kape-stitch-persistent-secrets
git pull --ff-only

npm ci
npm run typecheck
npm run lint
npm run build

dotnet restore backend/Kape.Api.Tests/Kape.Api.Tests.csproj
dotnet build backend/Kape.Api.Tests/Kape.Api.Tests.csproj --configuration Release --no-restore
dotnet test backend/Kape.Api.Tests/Kape.Api.Tests.csproj --configuration Release --no-build
```

## Start locally

API:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5000"
dotnet run --project backend/Kape.Api --no-launch-profile
```

Frontend:

```powershell
Remove-Item -Recurse -Force .next -ErrorAction SilentlyContinue
npm run dev
```

Open `http://localhost:3000/wallet`.

## Tokenised-card management

1. Confirm the first active card is marked **Default**.
2. Add a second fictional demo card with different masked details.
3. Make the second card default and confirm the first card loses its default status.
4. Try to add the exact same bank, network, last four digits and expiry again. The UI must reject the duplicate.
5. Remove a non-default card and confirm it disappears.
6. Remove the default card and confirm another active, non-expired card becomes the backend default.
7. Confirm historical wallet transactions remain visible after card removal.
8. Confirm an expired card is marked **Expired**, cannot become default and is not shown as a funding source.

## Top-up flow

1. Select **Add money**.
2. Choose an active tokenised card.
3. Enter `500.00` and a reference such as `Card top-up verification`.
4. Select **Preview** and verify the amount, card fee and total charged.
5. Select **Confirm top-up**.
6. Confirm the receipt displays a transaction reference.
7. Confirm ledger verification displays **Balanced**.
8. Confirm the wallet balance increases by `R500.00`.
9. Open **View transaction history** and confirm the top-up appears as a completed credit with its fee and external reference.

Repeat the top-up using a linked bank account. The linked-bank top-up quote should have no card fee.

## Withdrawal flow

1. Select **Withdraw**.
2. Confirm tokenised cards are not offered as destinations.
3. Choose an active linked bank account.
4. Enter `100.00` and a reference such as `Withdrawal verification`.
5. Select **Preview** and verify the withdrawal fee and total wallet debit.
6. Select **Confirm withdrawal**.
7. Confirm the receipt displays a transaction reference.
8. Confirm ledger verification displays **Balanced**.
9. Confirm the wallet balance decreases by the withdrawal amount plus the fee.
10. Open transaction history and confirm the withdrawal appears as a completed debit with its fee and external reference.

The API must reject a withdrawal request containing `paymentMethodId`, even when called outside the UI.

## Ledger verification

Call:

```text
GET /api/ledger/reconciliation
```

Expected result:

```json
{
  "difference": 0,
  "isBalanced": true
}
```

Run this query in SQL Server Management Studio to detect any unbalanced journal:

```sql
SELECT
    JournalId,
    SUM(CASE WHEN EntryType = 'debit' THEN Amount ELSE 0 END) AS TotalDebits,
    SUM(CASE WHEN EntryType = 'credit' THEN Amount ELSE 0 END) AS TotalCredits
FROM dbo.LedgerEntries
GROUP BY JournalId
HAVING
    SUM(CASE WHEN EntryType = 'debit' THEN Amount ELSE 0 END)
    <> SUM(CASE WHEN EntryType = 'credit' THEN Amount ELSE 0 END);
```

Expected result: **zero rows**.

## Final acceptance criteria

- Card removal preserves transaction history.
- Duplicate masked demo cards are rejected by the UI.
- Expired cards cannot fund the wallet.
- Top-ups accept active cards or linked bank accounts.
- Withdrawals accept linked bank accounts only.
- Every operation requires preview before confirmation.
- Every successful operation shows a receipt and external reference.
- Wallet balance and transaction history refresh after completion.
- Ledger reconciliation remains balanced with a zero difference.
- Repeating an API request with the same idempotency key does not create a second transaction.
