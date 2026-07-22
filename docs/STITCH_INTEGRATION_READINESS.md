# Stitch integration readiness

This document defines the boundary between Kape's working demo banking provider and a future live Stitch integration.

The live integration must not replace wallet, ledger, transaction, queue, or UI logic. It should be implemented as a provider adapter behind the existing `IBankAggregationProvider` contract.

## Current readiness

Kape already provides:

- provider-neutral `BankConnection`, `LinkedBankAccount`, `LinkedBankTransaction`, and `DebitOrder` models;
- a bank-link session and callback flow;
- idempotent imported transactions using external transaction identifiers;
- durable SQL queue processing;
- a webhook inbox with duplicate-event protection;
- a provider-independent frontend;
- a demo provider that remains the default until live credentials and tests are ready.

This branch adds:

- strongly typed Stitch configuration;
- startup validation for required credentials and scopes when Stitch is enabled;
- a named `HttpClient` boundary for Stitch requests;
- OAuth, token-vault, and financial-data client contracts;
- explicit provider selection through configuration;
- a fail-fast guard that prevents accidentally enabling an unfinished live adapter.

## Configuration

Keep all credentials outside Git. Use environment variables or a managed secret store.

```text
Providers__BankAggregation__ActiveProvider=demo-bank-aggregator
Providers__Stitch__Enabled=false
Providers__Stitch__ClientId=
Providers__Stitch__ClientSecret=
Providers__Stitch__RedirectUri=https://your-domain.example/api/bank-connections/stitch/callback
Providers__Stitch__WebhookSecret=
```

When the adapter is complete and sandbox tests pass:

```text
Providers__BankAggregation__ActiveProvider=stitch
Providers__Stitch__Enabled=true
```

Never commit a Stitch client secret, user access token, refresh token, webhook secret, authorization code, code verifier, or raw bank-account number.

## Adapter implementation plan

Create `StitchBankAggregationProvider : IBankAggregationProvider` and compose it from the following boundaries:

1. `IStitchOAuthClient`
   - Generate state, nonce, and PKCE values.
   - Build the user authorization URL.
   - Exchange the one-time authorization code.
   - Refresh expired user tokens.
   - Cache client tokens until shortly before expiry.

2. `IStitchConnectionSecretStore`
   - Encrypt access and refresh tokens at rest.
   - Store tokens by Kape `BankConnection.Id`, not by email or account number.
   - Rotate the stored refresh token whenever Stitch returns a new one.
   - Delete or revoke stored tokens when a bank is disconnected.

3. `IStitchFinancialDataClient`
   - Query accounts, balances, transactions, account holders, and debit-order payments.
   - Follow GraphQL pagination cursors until the required sync window is complete.
   - Map positive transaction amounts to credits and negative amounts to debits.
   - Return only masked account numbers to Kape.
   - Preserve Stitch IDs as external IDs for idempotent imports.

## Authentication rules

- Financial-data queries use Stitch user tokens.
- Client-level operations such as Pay by Bank requests use client tokens.
- Request `offline_access` so a refresh token can be issued.
- Authorization codes are single-use.
- Validate callback state and nonce before exchanging a code.
- Use PKCE with `S256`.
- Refresh tokens on the server only; never expose them to Next.js or the browser.
- Cache client tokens and refresh them shortly before expiry instead of requesting one for every API call.

## Webhook rules

- Treat signed Stitch webhooks as the source of truth for final payment status.
- Do not update financial status from redirect query parameters.
- Verify the provider signature against the exact raw request body.
- Reject stale or replayed webhook messages.
- Store the external event ID in `WebhookInbox` before processing.
- Return a successful HTTP response only after the event has been durably accepted.
- Process accepted events through the SQL queue so retries are safe.

The current generic webhook validator must be replaced or extended with the verification method required by the active Stitch webhook product before live traffic is enabled.

## Mapping rules

| Stitch data | Kape field |
| --- | --- |
| Stitch user or consent connection | `BankConnection.ExternalConnectionId` |
| Bank ID | `BankConnection.InstitutionId` |
| Bank display name | `BankConnection.InstitutionName` |
| Stitch bank-account ID | `LinkedBankAccount.ExternalAccountId` |
| Last four digits only | `LinkedBankAccount.AccountNumberMask` |
| Stitch transaction ID | `LinkedBankTransaction.ExternalTransactionId` |
| Positive amount | `Direction = credit` |
| Negative amount | `Direction = debit`, stored amount is absolute |
| Stitch debit-order ID | `DebitOrder.ExternalDebitOrderId` |
| GraphQL end cursor | encrypted provider connection state |

Do not create new wallet balances from linked-bank balances. Linked accounts remain informational unless a separate, confirmed payment or top-up flow posts to the Kape ledger.

## Pay by Bank preparation

Pay by Bank should be added as a separate payment provider flow and must not be mixed with financial-data synchronization.

Required controls:

- a unique Kape external reference for every Stitch payment request;
- quote or confirmation before initiating payment;
- pending status until a verified webhook confirms the final result;
- idempotent webhook processing;
- reconciliation between Stitch payment IDs, Kape wallet transactions, and ledger journals;
- refund records linked to the original completed payment.

## Sandbox acceptance criteria

Before selecting `stitch` as the active provider:

1. Link each supported sandbox bank.
2. Cancel one consent flow and confirm no connection becomes active.
3. Exchange one authorization code and confirm it cannot be reused.
4. Restart the API and confirm encrypted refresh tokens still work.
5. Import accounts and confirm only masked account numbers are returned to the UI.
6. Run the same sync twice and confirm no duplicate transactions are inserted.
7. Refresh an expired access token and rotate the refresh token safely.
8. Disconnect a bank and confirm local access and stored tokens are removed.
9. Reject an invalid webhook signature.
10. Accept the same webhook twice and confirm it is processed once.
11. Simulate provider downtime and confirm retry/backoff behaviour.
12. Reconcile every confirmed payment against the Kape double-entry ledger.

## Production gate

Do not enable Stitch in production until all of the following are complete:

- sandbox certification and approved redirect URLs;
- HTTPS callback and webhook endpoints;
- managed secret storage;
- encrypted durable token storage;
- token refresh and revocation;
- provider-specific webhook signature verification;
- structured logs with all tokens and account numbers redacted;
- timeout, retry, circuit-breaker, and rate-limit handling;
- monitoring for failed syncs, expired consents, webhook backlog, and reconciliation differences;
- data-retention and consent-deletion procedures.
