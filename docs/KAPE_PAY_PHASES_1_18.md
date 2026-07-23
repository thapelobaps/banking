# Kape Pay phases 1–18

Kape is a production-shaped portfolio platform. The hosted build must use synthetic wallet value, demo providers, or Stitch sandbox only. It must never accept real banking credentials or enable real funds.

## Current implementation map

| Phase | Capability | Status | Repository implementation |
|---|---|---:|---|
| 1 | Current branch stabilisation | Complete in PR #8 | Linked-bank cleanup, institution-scoped demo sync, encrypted Stitch persistence, wallet funding regression fixes |
| 2 | Environment and safety modes | Complete | `KapePayOptions`, startup validation, `RealFundsEnabled=false` |
| 3 | Provider architecture | Complete foundation | Bank aggregation, tokenisation, pay-in and digital-product provider boundaries |
| 4 | Financial transaction state engine | Complete foundation | `PaymentAttempt`, `PaymentStatusHistory`, validated provider transitions |
| 5 | Product catalogue | Already present | Voucher categories/products/denominations and prepaid operators/products |
| 6 | Orders | Already present and extended | Voucher/prepaid orders linked to payment attempts and status history |
| 7 | Direct checkout | Complete demo path | Linked-bank or Kape Demo Wallet selection for vouchers and prepaid products |
| 8 | Demo payment scenario engine | Complete | Success, awaiting approval, pending, failed, cancelled and insufficient-funds scenarios |
| 9 | Webhook inbox | Complete foundation | Signature validation, durable inbox, duplicate event prevention and queue processing |
| 10 | Stitch Financial Data sandbox | Adapter implemented; activation pending | OAuth, encrypted tokens, refresh and account/transaction sync require sandbox credentials and HTTPS callback testing |
| 11 | Stitch Pay by Bank sandbox | Provider boundary ready; adapter pending | `IPayInProvider` and checkout lifecycle are ready for a Stitch sandbox implementation |
| 12 | Demo voucher fulfilment | Already present | Durable queue and protected demo voucher codes |
| 13 | Airtime, prepaid and electricity | Already present and extended | Direct-bank and wallet checkout for prepaid products |
| 14 | Demo Wallet separation | Complete in UI | Linked-bank balances and synthetic wallet balances remain visibly separate |
| 15 | Wallet reservations | Persistence and balance guard complete | Reservation entities/indexes and active-reservation-aware spend checks; asynchronous reservation capture remains future work |
| 16 | Direct bank and wallet payments | Complete demo path | Provider-orchestrated direct payments never credit the wallet; wallet payments use the ledger |
| 17 | Refunds and reversals | Complete demo foundation | Idempotent provider refunds and immutable opposite wallet ledger entries |
| 18 | Reconciliation | Complete foundation | Payment/order/source/fulfilment mismatch detection persisted per run |

## Safe architecture

```text
Linked sandbox bank
        |
        | provider payment attempt
        v
Kape Pay order ----> durable payment state ----> demo fulfilment

Kape Demo Wallet
        |
        | immutable double-entry ledger debit
        v
Kape Pay order ----> durable fulfilment
```

The two payment sources are intentionally separate:

- A linked-bank payment is attached directly to the order and never credits the Kape Wallet.
- A wallet payment uses synthetic ledger funds and can be reversed through a new opposite journal entry.

## Provider replacement boundary

The core checkout and order logic depends on `IPayInProvider` rather than Stitch-specific code.

```text
DemoPayInProvider
        |
        v
IPayInProvider <---- future StitchSandboxPayInProvider
        |
        v
KapePayService
```

A future Stitch sandbox adapter must implement payment creation, status lookup and refunds without changing the order, payment, webhook or reconciliation models.

## Production boundary

This portfolio build rejects unsafe payment configuration at startup. Supported configuration is limited to:

```json
{
  "Environment": "demo or sandbox",
  "ActiveProvider": "demo-pay-by-bank",
  "RealFundsEnabled": false
}
```

Live payment providers, real funds, settlement accounts and production credentials are deliberately outside this branch.

## Remaining external dependencies

The following cannot be completed without external sandbox configuration:

1. Stitch sandbox client credentials.
2. A public HTTPS OAuth callback.
3. A public HTTPS webhook endpoint.
4. Stitch sandbox Pay by Bank API/schema confirmation.
5. Stitch webhook signing configuration.

No real credentials or secrets belong in GitHub.
