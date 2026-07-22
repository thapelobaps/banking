'use client';

import {
  ArrowLeft,
  ArrowRight,
  CheckCircle2,
  Download,
  Loader2,
  ReceiptText,
  Search,
  ShieldCheck,
  UserRoundCheck,
  WalletCards,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

import {
  previewWalletTransfer,
  resolveKapeUser,
  submitWalletTransfer,
} from '@/lib/actions/wallet.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';
import type {
  ResolvedKapeUser,
  WalletFundingQuote,
  WalletSummary,
  WalletTransaction,
} from '@/types/wallet';

type WalletTransferPanelProps = {
  wallet: WalletSummary;
  senderName: string;
};

type ReviewState = {
  recipient: ResolvedKapeUser;
  quote: WalletFundingQuote;
  amount: number;
  reference: string;
  idempotencyKey: string;
};

type Stage = 'details' | 'review' | 'receipt';

const createIdempotencyKey = () => {
  const suffix = typeof globalThis.crypto?.randomUUID === 'function'
    ? globalThis.crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  return `wallet-ui-transfer-${suffix}`;
};

export default function WalletTransferPanel({ wallet, senderName }: WalletTransferPanelProps) {
  const router = useRouter();
  const [stage, setStage] = useState<Stage>('details');
  const [recipientIdentifier, setRecipientIdentifier] = useState('');
  const [amount, setAmount] = useState('');
  const [reference, setReference] = useState('');
  const [review, setReview] = useState<ReviewState | null>(null);
  const [receipt, setReceipt] = useState<WalletTransaction | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const reviewTransfer = async () => {
    setError(null);
    const amountValue = Number(amount);

    if (!recipientIdentifier.trim()) {
      setError('Enter the recipient email address, mobile number, or Kape user ID.');
      return;
    }

    if (!Number.isFinite(amountValue) || amountValue <= 0 || !/^\d+(\.\d{1,2})?$/.test(amount.trim())) {
      setError('Enter a valid amount greater than R0.00 using no more than two decimal places.');
      return;
    }

    if (amountValue > wallet.availableBalance) {
      setError(`The amount is greater than your available Kape Wallet balance of ${formatAmount(wallet.availableBalance)}.`);
      return;
    }

    setIsLoading(true);
    try {
      const resolved = await resolveKapeUser(recipientIdentifier);
      if (!resolved.ok) {
        setError(resolved.error);
        return;
      }

      const quote = await previewWalletTransfer({
        recipientUserId: resolved.data.userId,
        amount: amountValue,
        reference: reference.trim() || 'Kape wallet transfer',
      });
      if (!quote.ok) {
        setError(quote.error);
        return;
      }

      setReview({
        recipient: resolved.data,
        quote: quote.data,
        amount: amountValue,
        reference: reference.trim() || 'Kape wallet transfer',
        idempotencyKey: createIdempotencyKey(),
      });
      setStage('review');
    } finally {
      setIsLoading(false);
    }
  };

  const confirmTransfer = async () => {
    if (!review) return;

    setError(null);
    setIsLoading(true);
    try {
      const result = await submitWalletTransfer({
        recipientUserId: review.recipient.userId,
        amount: review.amount,
        reference: review.reference,
        idempotencyKey: review.idempotencyKey,
      });

      if (!result.ok) {
        setError(result.error);
        return;
      }

      setReceipt(result.data);
      setStage('receipt');
      router.refresh();
    } finally {
      setIsLoading(false);
    }
  };

  const reset = () => {
    setStage('details');
    setRecipientIdentifier('');
    setAmount('');
    setReference('');
    setReview(null);
    setReceipt(null);
    setError(null);
  };

  const downloadReceipt = () => {
    if (!receipt || !review) return;

    const content = [
      'KAPE WALLET - PROOF OF TRANSFER',
      '--------------------------------',
      `Status: ${receipt.status}`,
      `Transaction reference: ${receipt.id}`,
      `External reference: ${receipt.externalReference ?? 'Not available'}`,
      `Date: ${formatDateTime(receipt.completedAt ?? receipt.createdAt).dateTime}`,
      `From: ${senderName} - Kape Wallet`,
      `To: ${review.recipient.displayName} - ${review.recipient.maskedIdentifier}`,
      `Amount: ${formatAmount(receipt.amount)}`,
      `Fee: ${formatAmount(receipt.feeAmount)}`,
      `Payment reference: ${receipt.reference}`,
      '',
      'This transfer was posted through the Kape double-entry wallet ledger.',
      'The current development environment does not move money through a live bank rail.',
    ].join('\n');

    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `kape-wallet-transfer-${receipt.id}.txt`;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  };

  if (stage === 'receipt' && receipt && review) {
    return (
      <section className="wallet-transfer-panel" aria-live="polite">
        <div className="wallet-transfer-success">
          <span><CheckCircle2 size={28} /></span>
          <p>Transfer completed</p>
          <strong>{formatAmount(receipt.amount)}</strong>
          <small>Sent to {review.recipient.displayName}</small>
        </div>

        <div className="wallet-transfer-receipt">
          <div className="wallet-transfer-receipt__heading">
            <ReceiptText size={18} />
            <h3>Wallet transfer receipt</h3>
          </div>
          <dl>
            <div><dt>Status</dt><dd>{receipt.status}</dd></div>
            <div><dt>Recipient</dt><dd>{review.recipient.displayName}</dd></div>
            <div><dt>Recipient identifier</dt><dd>{review.recipient.maskedIdentifier}</dd></div>
            <div><dt>Reference</dt><dd>{receipt.reference}</dd></div>
            <div><dt>Transaction ID</dt><dd>{receipt.id}</dd></div>
            <div><dt>External reference</dt><dd>{receipt.externalReference ?? 'Not available'}</dd></div>
            <div><dt>Fee</dt><dd>{formatAmount(receipt.feeAmount)}</dd></div>
            <div><dt>Completed</dt><dd>{formatDateTime(receipt.completedAt ?? receipt.createdAt).dateTime}</dd></div>
          </dl>
        </div>

        <div className="wallet-transfer-actions wallet-transfer-actions--receipt">
          <button type="button" className="wallet-button wallet-button--secondary" onClick={reset}>
            New transfer
          </button>
          <button type="button" className="wallet-button wallet-button--secondary" onClick={downloadReceipt}>
            <Download size={16} /> Download proof
          </button>
          <button type="button" className="wallet-button wallet-button--primary" onClick={() => router.push('/transaction-history?source=wallet')}>
            View wallet activity <ArrowRight size={16} />
          </button>
        </div>
      </section>
    );
  }

  if (stage === 'review' && review) {
    return (
      <section className="wallet-transfer-panel">
        <div className="wallet-transfer-notice">
          <ShieldCheck size={18} />
          <div>
            <strong>Review before confirming</strong>
            <span>The wallet balance changes only after final confirmation.</span>
          </div>
        </div>

        <div className="wallet-transfer-review-grid">
          <article>
            <span>From</span>
            <strong>Kape Wallet</strong>
            <small>{senderName} · Available {formatAmount(wallet.availableBalance)}</small>
          </article>
          <article>
            <span>Recipient</span>
            <strong>{review.recipient.displayName}</strong>
            <small>{review.recipient.maskedIdentifier}</small>
          </article>
        </div>

        <div className="wallet-transfer-review-amount">
          <span>Amount to send</span>
          <strong>{formatAmount(review.amount)}</strong>
          <div>
            <p>Fee <b>{formatAmount(review.quote.feeAmount)}</b></p>
            <p>Total wallet debit <b>{formatAmount(review.quote.totalAmount)}</b></p>
          </div>
          <small>Reference: {review.reference}</small>
        </div>

        {error ? <p className="wallet-feedback is-error" role="alert">{error}</p> : null}

        <div className="wallet-transfer-actions">
          <button type="button" className="wallet-button wallet-button--secondary" onClick={() => setStage('details')} disabled={isLoading}>
            <ArrowLeft size={16} /> Edit details
          </button>
          <button type="button" className="wallet-button wallet-button--primary" onClick={confirmTransfer} disabled={isLoading}>
            {isLoading ? <Loader2 size={16} className="animate-spin" /> : <ShieldCheck size={16} />}
            Confirm transfer
          </button>
        </div>
      </section>
    );
  }

  return (
    <section className="wallet-transfer-panel">
      <div className="wallet-transfer-source">
        <div>
          <span><WalletCards size={16} /> Sending from</span>
          <strong>Kape Wallet</strong>
          <small>SQL-backed double-entry wallet</small>
        </div>
        <div>
          <span>Available</span>
          <strong>{formatAmount(wallet.availableBalance)}</strong>
        </div>
      </div>

      <label className="wallet-field">
        <span>Recipient</span>
        <div className="wallet-transfer-input-icon">
          <Search size={16} />
          <input
            value={recipientIdentifier}
            onChange={(event) => setRecipientIdentifier(event.target.value)}
            placeholder="Email, South African mobile number, or Kape user ID"
            autoComplete="off"
          />
        </div>
        <small>Kape verifies the recipient before showing the confirmation screen.</small>
      </label>

      <label className="wallet-field">
        <span>Amount in rand</span>
        <div className="wallet-transfer-money-input">
          <b>R</b>
          <input
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            inputMode="decimal"
            placeholder="250.00"
          />
        </div>
      </label>

      <label className="wallet-field">
        <span>Payment reference</span>
        <input
          value={reference}
          onChange={(event) => setReference(event.target.value.slice(0, 160))}
          placeholder="e.g. Shared groceries"
          maxLength={160}
        />
        <small>{reference.length}/160 characters</small>
      </label>

      <div className="wallet-transfer-assurance">
        <UserRoundCheck size={17} />
        <div>
          <strong>Verified Kape recipients only</strong>
          <span>Transfers are fee-free in the current wallet configuration and posted to both users atomically.</span>
        </div>
      </div>

      {error ? <p className="wallet-feedback is-error" role="alert">{error}</p> : null}

      <button type="button" className="wallet-button wallet-button--primary wallet-transfer-review-button" onClick={reviewTransfer} disabled={isLoading}>
        {isLoading ? <Loader2 size={16} className="animate-spin" /> : <ShieldCheck size={16} />}
        Resolve recipient and review
      </button>
    </section>
  );
}
