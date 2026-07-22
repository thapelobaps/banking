'use client';

import { Check, Clipboard, Clock3, Loader2, Send, ShieldCheck, X } from 'lucide-react';
import { FormEvent, useMemo, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';

import {
  declinePaymentRequest,
  payPaymentRequest,
  submitPaymentRequest,
} from '@/lib/actions/marketplace.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';
import type { PaymentRequest } from '@/types/marketplace';

const defaultExpiry = () => {
  const date = new Date();
  date.setDate(date.getDate() + 7);
  return date.toISOString().slice(0, 10);
};

const replaceRequest = (items: PaymentRequest[], updated: PaymentRequest) =>
  items.map((item) => (item.id === updated.id ? updated : item));

export default function PaymentRequestsPanel({
  currentUserId,
  initialRequests,
  walletAvailable,
}: {
  currentUserId: string;
  initialRequests: PaymentRequest[];
  walletAvailable: number;
}) {
  const router = useRouter();
  const [requests, setRequests] = useState(initialRequests);
  const [view, setView] = useState<'all' | 'incoming' | 'outgoing'>('all');
  const [payerIdentifier, setPayerIdentifier] = useState('');
  const [amount, setAmount] = useState('250.00');
  const [message, setMessage] = useState('');
  const [expiresOn, setExpiresOn] = useState(defaultExpiry);
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const filteredRequests = useMemo(
    () => requests.filter((request) => {
      const outgoing = request.payeeUserId === currentUserId;
      if (view === 'incoming') return !outgoing;
      if (view === 'outgoing') return outgoing;
      return true;
    }),
    [currentUserId, requests, view]
  );

  const incomingPending = requests.filter(
    (request) => request.payeeUserId !== currentUserId && request.status === 'pending'
  ).length;
  const outgoingPending = requests.filter(
    (request) => request.payeeUserId === currentUserId && request.status === 'pending'
  ).length;

  const createRequest = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const numericAmount = Number(amount);
    if (!Number.isFinite(numericAmount) || numericAmount <= 0) {
      setError('Enter a valid amount.');
      return;
    }

    setError(null);
    setNotice(null);
    startTransition(() => {
      void submitPaymentRequest({
        payerIdentifier: payerIdentifier.trim() || null,
        amount: numericAmount,
        message: message.trim() || null,
        expiresAt: new Date(`${expiresOn}T23:59:00`).toISOString(),
      }).then((result) => {
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setRequests((items) => [result.data, ...items]);
        setPayerIdentifier('');
        setMessage('');
        setNotice('Payment request created successfully.');
        router.refresh();
      });
    });
  };

  const respond = (requestId: string, action: 'pay' | 'decline') => {
    setBusyId(requestId);
    setError(null);
    setNotice(null);
    startTransition(() => {
      const operation = action === 'pay' ? payPaymentRequest(requestId) : declinePaymentRequest(requestId);
      void operation.then((result) => {
        setBusyId(null);
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setRequests((items) => replaceRequest(items, result.data));
        setNotice(action === 'pay' ? 'Payment request paid from your Kape Wallet.' : 'Payment request declined.');
        router.refresh();
      });
    });
  };

  const copyRequest = async (requestId: string) => {
    await navigator.clipboard.writeText(requestId);
    setCopiedId(requestId);
    window.setTimeout(() => setCopiedId(null), 1800);
  };

  return (
    <section className="payment-request-layout">
      <form className="payment-request-create" onSubmit={createRequest}>
        <div className="marketplace-section-heading">
          <div>
            <span>Request money</span>
            <h2>Create a payment request</h2>
            <p>Address it to a Kape user or leave the payer blank to create an open request reference.</p>
          </div>
          <Send size={22} />
        </div>

        <label className="marketplace-field">
          <span>Payer email or South African mobile</span>
          <input
            value={payerIdentifier}
            placeholder="name@example.com or 082 123 4567"
            onChange={(event) => setPayerIdentifier(event.target.value)}
          />
          <small>Optional. A named payer can pay or decline directly.</small>
        </label>

        <div className="payment-request-form-grid">
          <label className="marketplace-field">
            <span>Amount</span>
            <div className="marketplace-money-input">
              <b>R</b>
              <input value={amount} inputMode="decimal" onChange={(event) => setAmount(event.target.value)} />
            </div>
          </label>
          <label className="marketplace-field">
            <span>Expires on</span>
            <input type="date" value={expiresOn} min={new Date().toISOString().slice(0, 10)} onChange={(event) => setExpiresOn(event.target.value)} />
          </label>
        </div>

        <label className="marketplace-field">
          <span>Message</span>
          <input value={message} maxLength={120} placeholder="Dinner, tickets, shared groceries..." onChange={(event) => setMessage(event.target.value)} />
        </label>

        {notice ? <p className="marketplace-message is-success">{notice}</p> : null}
        {error ? <p className="marketplace-message is-error">{error}</p> : null}

        <button className="marketplace-primary-button" type="submit" disabled={isPending}>
          {isPending && !busyId ? <Loader2 size={17} className="is-spinning" /> : <Send size={17} />}
          Create request
        </button>

        <div className="payment-request-security">
          <ShieldCheck size={18} />
          <span><strong>Wallet protected</strong><small>Incoming payments use the same idempotent double-entry transfer flow.</small></span>
        </div>
      </form>

      <div className="payment-request-list-shell">
        <div className="marketplace-section-heading">
          <div>
            <span>Request centre</span>
            <h2>Incoming and outgoing</h2>
            <p>Available wallet balance {formatAmount(walletAvailable)}.</p>
          </div>
          <span className="marketplace-wallet-pill">{incomingPending} to pay · {outgoingPending} waiting</span>
        </div>

        <div className="marketplace-category-tabs" role="tablist" aria-label="Payment request filters">
          {(['all', 'incoming', 'outgoing'] as const).map((item) => (
            <button key={item} type="button" className={view === item ? 'is-active' : ''} onClick={() => setView(item)}>
              {item}
            </button>
          ))}
        </div>

        <div className="payment-request-list">
          {filteredRequests.length ? filteredRequests.map((request) => {
            const outgoing = request.payeeUserId === currentUserId;
            const canRespond = !outgoing && request.status === 'pending';
            const created = formatDateTime(request.createdAt);
            const expires = formatDateTime(request.expiresAt);
            return (
              <article key={request.id} className={`payment-request-card payment-request-card--${request.status}`}>
                <div className="payment-request-card__top">
                  <span className={`payment-request-direction ${outgoing ? 'is-outgoing' : 'is-incoming'}`}>
                    {outgoing ? 'You requested' : 'Requested from you'}
                  </span>
                  <span className={`wallet-status wallet-status--${request.status}`}>{request.status}</span>
                </div>
                <strong>{formatAmount(request.amount)}</strong>
                <p>{request.message}</p>
                <div className="payment-request-card__meta">
                  <span><Clock3 size={14} /> Created {created.dateOnly}</span>
                  <span>Expires {expires.dateOnly}</span>
                </div>
                <div className="payment-request-card__actions">
                  <button type="button" className="is-quiet" onClick={() => copyRequest(request.id)}>
                    {copiedId === request.id ? <Check size={15} /> : <Clipboard size={15} />}
                    {copiedId === request.id ? 'Copied' : 'Copy reference'}
                  </button>
                  {canRespond ? (
                    <>
                      <button type="button" className="is-decline" disabled={isPending && busyId === request.id} onClick={() => respond(request.id, 'decline')}>
                        <X size={15} /> Decline
                      </button>
                      <button type="button" disabled={isPending && busyId === request.id} onClick={() => respond(request.id, 'pay')}>
                        {isPending && busyId === request.id ? <Loader2 size={15} className="is-spinning" /> : <Check size={15} />}
                        Pay
                      </button>
                    </>
                  ) : null}
                </div>
              </article>
            );
          }) : (
            <div className="kape-empty-state">
              <strong>No payment requests here</strong>
              <span>Create a request or change the selected filter.</span>
            </div>
          )}
        </div>
      </div>
    </section>
  );
}
