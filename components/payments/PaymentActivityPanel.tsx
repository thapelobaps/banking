'use client';

import { AlertTriangle, CheckCircle2, Landmark, Loader2, RefreshCw, RotateCcw, Scale, WalletCards } from 'lucide-react';
import { useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';

import {
  reconcileKapePay,
  refreshKapePayPayment,
  refundKapePayPayment,
} from '@/lib/actions/kape-pay.actions';
import { reverseWalletPurchase } from '@/lib/actions/wallet-reversal.actions';
import { formatAmount } from '@/lib/utils';
import type { PaymentAttempt, PaymentReconciliation } from '@/types/kape-pay';

const activeStatuses = new Set(['created', 'awaiting_approval', 'pending']);

export default function PaymentActivityPanel({ initialPayments }: { initialPayments: PaymentAttempt[] }) {
  const router = useRouter();
  const [payments, setPayments] = useState(initialPayments);
  const [reconciliation, setReconciliation] = useState<PaymentReconciliation | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [activeAction, setActiveAction] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const updatePayment = (updated: PaymentAttempt) => {
    setPayments((current) => current.map((item) => (item.id === updated.id ? updated : item)));
  };

  const refreshPayment = (payment: PaymentAttempt) => {
    setActiveAction(`refresh:${payment.id}`);
    setError(null);
    startTransition(() => {
      void refreshKapePayPayment(payment.id).then((result) => {
        setActiveAction(null);
        if (!result.ok) {
          setError(result.error);
          return;
        }
        updatePayment(result.data);
        setMessage(`Payment ${payment.externalPaymentId} is ${result.data.status}.`);
        router.refresh();
      });
    });
  };

  const refundPayment = (payment: PaymentAttempt) => {
    const total = payment.amount + payment.feeAmount;
    const confirmed = window.confirm(
      `Simulate a full refund of ${formatAmount(total)} for this direct-bank payment?`
    );
    if (!confirmed) return;

    setActiveAction(`refund:${payment.id}`);
    setError(null);
    startTransition(() => {
      void refundKapePayPayment(payment.id, total, 'Demonstration payment refund').then((result) => {
        setActiveAction(null);
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setPayments((current) => current.map((item) => (
          item.id === payment.id ? { ...item, status: 'refunded' } : item
        )));
        setMessage(`Refund ${result.data.externalRefundId} completed.`);
        router.refresh();
      });
    });
  };

  const reverseWallet = (payment: PaymentAttempt) => {
    if (!payment.walletTransactionId) return;
    const confirmed = window.confirm(
      `Reverse this synthetic wallet purchase and restore ${formatAmount(payment.amount + payment.feeAmount)}?`
    );
    if (!confirmed) return;

    setActiveAction(`reverse:${payment.id}`);
    setError(null);
    startTransition(() => {
      void reverseWalletPurchase(payment.walletTransactionId!, 'Demonstration fulfilment reversal').then((result) => {
        setActiveAction(null);
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setPayments((current) => current.map((item) => (
          item.id === payment.id ? { ...item, status: 'reversed' } : item
        )));
        setMessage(`Wallet reversal ${result.data.reversal.externalReference ?? result.data.reversal.id} completed.`);
        router.refresh();
      });
    });
  };

  const reconcile = () => {
    setActiveAction('reconcile');
    setError(null);
    startTransition(() => {
      void reconcileKapePay().then((result) => {
        setActiveAction(null);
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setReconciliation(result.data);
        setMessage(
          result.data.issueCount === 0
            ? 'Payment records, orders and fulfilment are reconciled.'
            : `${result.data.issueCount} reconciliation issue${result.data.issueCount === 1 ? '' : 's'} need review.`
        );
      });
    });
  };

  return (
    <div className="marketplace-orders-section">
      <section className="wallet-metric-grid">
        <article>
          <div className="wallet-metric__icon"><Landmark size={18} /></div>
          <span>Direct bank payments</span>
          <strong>{payments.filter((item) => item.paymentSource === 'linked_bank').length}</strong>
          <small>Provider-orchestrated attempts</small>
        </article>
        <article>
          <div className="wallet-metric__icon"><WalletCards size={18} /></div>
          <span>Wallet payments</span>
          <strong>{payments.filter((item) => item.paymentSource === 'wallet').length}</strong>
          <small>Synthetic ledger purchases</small>
        </article>
        <article>
          <div className="wallet-metric__icon"><RefreshCw size={18} /></div>
          <span>In progress</span>
          <strong>{payments.filter((item) => activeStatuses.has(item.status)).length}</strong>
          <small>Awaiting provider completion</small>
        </article>
        <article>
          <div className="wallet-metric__icon"><Scale size={18} /></div>
          <span>Reconciliation</span>
          <strong>{reconciliation?.status ?? 'Not run'}</strong>
          <small>{reconciliation ? `${reconciliation.issueCount} issues` : 'Run a fresh comparison'}</small>
        </article>
      </section>

      <div className="marketplace-section-heading">
        <div>
          <span>Payment operations</span>
          <h2>Payment activity</h2>
          <p>Provider states, order links, refunds, wallet reversals and status history remain separate from bank balances.</p>
        </div>
        <button type="button" className="marketplace-header-link" onClick={reconcile} disabled={isPending}>
          {activeAction === 'reconcile' ? <Loader2 size={16} className="is-spinning" /> : <Scale size={16} />}
          Reconcile
        </button>
      </div>

      {message ? <p className="marketplace-message is-success">{message}</p> : null}
      {error ? <p className="marketplace-message is-error">{error}</p> : null}

      {reconciliation?.issues.length ? (
        <div className="marketplace-order-grid">
          {reconciliation.issues.map((issue) => (
            <article key={issue.id} className="marketplace-order-card marketplace-order-card--failed">
              <div><AlertTriangle size={18} /><span className="wallet-status wallet-status--failed">{issue.severity}</span></div>
              <h3>{issue.issueType.replaceAll('_', ' ')}</h3>
              <p>{issue.description}</p>
            </article>
          ))}
        </div>
      ) : null}

      <div className="marketplace-order-grid">
        {payments.length ? payments.map((payment) => {
          const actionPending = activeAction?.endsWith(payment.id) ?? false;
          const total = payment.amount + payment.feeAmount;
          return (
            <article key={payment.id} className={`marketplace-order-card marketplace-order-card--${payment.status}`}>
              <div>
                {payment.status === 'completed' || payment.status === 'refunded' || payment.status === 'reversed'
                  ? <CheckCircle2 size={18} />
                  : activeStatuses.has(payment.status)
                    ? <RefreshCw size={18} className="is-spinning" />
                    : <AlertTriangle size={18} />}
                <span className={`wallet-status wallet-status--${payment.status}`}>{payment.status}</span>
              </div>
              <h3>{payment.orderType === 'voucher' ? 'Voucher purchase' : 'Prepaid purchase'}</h3>
              <strong>{formatAmount(total)}</strong>
              <p>{payment.paymentSource === 'linked_bank' ? 'Direct linked-bank payment' : 'Kape Demo Wallet'} · {payment.providerId}</p>
              <small>{payment.externalPaymentId}</small>

              <div className="marketplace-order-card__footer">
                {activeStatuses.has(payment.status) ? (
                  <button type="button" onClick={() => refreshPayment(payment)} disabled={isPending}>
                    {actionPending ? <Loader2 size={15} className="is-spinning" /> : <RefreshCw size={15} />} Refresh
                  </button>
                ) : null}
                {payment.paymentSource === 'linked_bank' && payment.status === 'completed' ? (
                  <button type="button" onClick={() => refundPayment(payment)} disabled={isPending}>
                    {actionPending ? <Loader2 size={15} className="is-spinning" /> : <RotateCcw size={15} />} Refund
                  </button>
                ) : null}
                {payment.paymentSource === 'wallet' && payment.status === 'completed' && payment.walletTransactionId ? (
                  <button type="button" onClick={() => reverseWallet(payment)} disabled={isPending}>
                    {actionPending ? <Loader2 size={15} className="is-spinning" /> : <RotateCcw size={15} />} Reverse
                  </button>
                ) : null}
              </div>

              <div className="marketplace-fine-print">
                {payment.history.length} status event{payment.history.length === 1 ? '' : 's'} · Order {payment.orderId.slice(0, 8).toUpperCase()}
              </div>
            </article>
          );
        }) : (
          <div className="kape-empty-state"><strong>No Kape Pay activity</strong><span>Buy a voucher or prepaid product using a linked bank or the demo wallet.</span></div>
        )}
      </div>
    </div>
  );
}
