'use client';

import Link from 'next/link';
import { useEffect, useMemo, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import {
  ArrowDownToLine,
  ArrowUpFromLine,
  Calculator,
  CheckCircle2,
  Loader2,
  ReceiptText,
  Scale,
} from 'lucide-react';

import {
  getLedgerReconciliation,
  previewWalletFunding,
  submitWalletFunding,
} from '@/lib/actions/wallet.actions';
import { formatAmount } from '@/lib/utils';
import type {
  LedgerReconciliation,
  LinkedBankAccount,
  PaymentMethod,
  WalletFundingOperation,
  WalletFundingQuote,
  WalletTransaction,
} from '@/types/wallet';

type WalletFundingPanelProps = {
  paymentMethods: PaymentMethod[];
  linkedAccounts: LinkedBankAccount[];
};

type CompletedFunding = {
  transaction: WalletTransaction;
  reconciliation: LedgerReconciliation | null;
  sourceLabel: string;
};

const createIdempotencyKey = (operation: WalletFundingOperation) => {
  const id = typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  return `wallet-ui-${operation}-${id}`;
};

const isExpiredCard = (method: PaymentMethod) => {
  const now = new Date();
  const currentYear = now.getFullYear();
  const currentMonth = now.getMonth() + 1;
  return (
    method.status === 'expired' ||
    method.expiryYear < currentYear ||
    (method.expiryYear === currentYear && method.expiryMonth < currentMonth)
  );
};

const preferredSource = (
  operation: WalletFundingOperation,
  paymentMethods: PaymentMethod[],
  linkedAccounts: LinkedBankAccount[]
) => {
  const activeBanks = linkedAccounts.filter((account) => account.isActive);
  if (operation === 'withdrawal') {
    return activeBanks[0] ? `bank:${activeBanks[0].id}` : '';
  }

  const activeCards = paymentMethods.filter(
    (method) => method.status === 'active' && !isExpiredCard(method)
  );
  const defaultMethod = activeCards.find((method) => method.isDefault) ?? activeCards[0];
  if (defaultMethod) return `card:${defaultMethod.id}`;
  if (activeBanks[0]) return `bank:${activeBanks[0].id}`;
  return '';
};

export default function WalletFundingPanel({
  paymentMethods,
  linkedAccounts,
}: WalletFundingPanelProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [operation, setOperation] = useState<WalletFundingOperation>('top_up');
  const [amount, setAmount] = useState('');
  const [reference, setReference] = useState('');
  const [source, setSource] = useState(() => preferredSource('top_up', paymentMethods, linkedAccounts));
  const [quote, setQuote] = useState<WalletFundingQuote | null>(null);
  const [completedFunding, setCompletedFunding] = useState<CompletedFunding | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [isError, setIsError] = useState(false);
  const [idempotencyKey, setIdempotencyKey] = useState(() => createIdempotencyKey('top_up'));

  const sourceOptions = useMemo(() => {
    const cards = paymentMethods
      .filter((method) => method.status === 'active' && !isExpiredCard(method))
      .map((method) => ({
        value: `card:${method.id}`,
        label: `${method.bankName} ${method.brand} •••• ${method.last4}${method.isDefault ? ' — Default' : ''}`,
      }));

    const banks = linkedAccounts
      .filter((account) => account.isActive)
      .map((account) => ({
        value: `bank:${account.id}`,
        label: `${account.institutionName} ${account.accountName} •••• ${account.accountNumberMask}`,
      }));

    return operation === 'withdrawal' ? banks : [...cards, ...banks];
  }, [linkedAccounts, operation, paymentMethods]);

  useEffect(() => {
    if (source && sourceOptions.some((option) => option.value === source)) return;
    setSource(sourceOptions[0]?.value ?? '');
    setQuote(null);
  }, [source, sourceOptions]);

  const selectedSourceLabel =
    sourceOptions.find((option) => option.value === source)?.label ?? 'Funding source';

  const clearReview = () => {
    setQuote(null);
    setCompletedFunding(null);
    setMessage(null);
  };

  const buildPayload = () => {
    const numericAmount = Number(amount);
    const [sourceType, sourceId] = source.split(':');

    return {
      operation,
      amount: numericAmount,
      paymentMethodId: sourceType === 'card' ? sourceId : null,
      linkedBankAccountId: sourceType === 'bank' ? sourceId : null,
      reference: reference.trim() || null,
      idempotencyKey,
    };
  };

  const validate = () => {
    if (!source) {
      setIsError(true);
      setMessage(
        operation === 'withdrawal'
          ? 'Connect a bank account before making a withdrawal.'
          : 'Add a tokenised card or connect a bank account first.'
      );
      return false;
    }

    if (operation === 'withdrawal' && !source.startsWith('bank:')) {
      setIsError(true);
      setMessage('Withdrawals must be paid into a linked bank account.');
      return false;
    }

    if (!Number.isFinite(Number(amount)) || Number(amount) <= 0) {
      setIsError(true);
      setMessage('Enter an amount greater than zero.');
      return false;
    }

    const decimalPlaces = amount.includes('.') ? amount.split('.')[1]?.length ?? 0 : 0;
    if (decimalPlaces > 2) {
      setIsError(true);
      setMessage('Enter an amount with no more than two decimal places.');
      return false;
    }

    return true;
  };

  const changeOperation = (nextOperation: WalletFundingOperation) => {
    setOperation(nextOperation);
    setSource(preferredSource(nextOperation, paymentMethods, linkedAccounts));
    setQuote(null);
    setCompletedFunding(null);
    setMessage(null);
    setIdempotencyKey(createIdempotencyKey(nextOperation));
  };

  const preview = () => {
    if (!validate()) return;

    setMessage(null);
    setCompletedFunding(null);
    startTransition(async () => {
      const result = await previewWalletFunding(buildPayload());
      if (!result.ok) {
        setQuote(null);
        setIsError(true);
        setMessage(result.error);
        return;
      }

      setQuote(result.data);
      setIsError(false);
      setMessage('Quote ready. Review the amount and fee before confirming.');
    });
  };

  const submit = () => {
    if (!validate()) return;
    if (!quote || new Date(quote.expiresAt).getTime() <= Date.now()) {
      setQuote(null);
      setIsError(true);
      setMessage('This quote has expired. Preview the operation again.');
      return;
    }

    setMessage(null);
    startTransition(async () => {
      const result = await submitWalletFunding(buildPayload());
      if (!result.ok) {
        setIsError(true);
        setMessage(result.error);
        return;
      }

      const reconciliation = await getLedgerReconciliation();
      setCompletedFunding({
        transaction: result.data,
        reconciliation,
        sourceLabel: selectedSourceLabel,
      });
      setIsError(false);
      setMessage(
        reconciliation?.isBalanced
          ? 'Operation completed and the wallet ledger is balanced.'
          : 'Operation completed, but the ledger needs review.'
      );
      setAmount('');
      setReference('');
      setQuote(null);
      setIdempotencyKey(createIdempotencyKey(operation));
      router.refresh();
    });
  };

  return (
    <section className="wallet-panel wallet-funding-panel">
      <div className="wallet-panel__heading">
        <div>
          <span className="wallet-eyebrow">Move money</span>
          <h2>Fund or withdraw</h2>
          <p>Preview every operation before it reaches the SQL-backed double-entry ledger.</p>
        </div>
      </div>

      <div className="wallet-operation-switch" role="tablist" aria-label="Wallet operation">
        <button
          type="button"
          className={operation === 'top_up' ? 'is-active' : ''}
          onClick={() => changeOperation('top_up')}
        >
          <ArrowDownToLine size={16} />
          Add money
        </button>
        <button
          type="button"
          className={operation === 'withdrawal' ? 'is-active' : ''}
          onClick={() => changeOperation('withdrawal')}
        >
          <ArrowUpFromLine size={16} />
          Withdraw
        </button>
      </div>

      <div className="wallet-form-grid">
        <label className="wallet-field wallet-field--wide">
          <span>{operation === 'top_up' ? 'Funding source' : 'Withdrawal destination'}</span>
          <select
            value={source}
            onChange={(event) => {
              setSource(event.target.value);
              clearReview();
            }}
          >
            <option value="">
              {operation === 'top_up' ? 'Choose a card or linked account' : 'Choose a linked bank account'}
            </option>
            {sourceOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        <label className="wallet-field">
          <span>Amount</span>
          <div className="wallet-money-input">
            <strong>R</strong>
            <input
              inputMode="decimal"
              min="0.01"
              step="0.01"
              placeholder="500.00"
              value={amount}
              onChange={(event) => {
                setAmount(event.target.value);
                clearReview();
              }}
            />
          </div>
        </label>

        <label className="wallet-field">
          <span>Reference</span>
          <input
            maxLength={160}
            placeholder={operation === 'top_up' ? 'Wallet top-up' : 'Wallet withdrawal'}
            value={reference}
            onChange={(event) => {
              setReference(event.target.value);
              setQuote(null);
              setCompletedFunding(null);
            }}
          />
        </label>
      </div>

      {quote ? (
        <div className="wallet-quote">
          <div>
            <span>Amount</span>
            <strong>{formatAmount(quote.amount)}</strong>
          </div>
          <div>
            <span>Fee</span>
            <strong>{formatAmount(quote.feeAmount)}</strong>
          </div>
          <div className="wallet-quote__total">
            <span>{operation === 'top_up' ? 'Total charged' : 'Total wallet debit'}</span>
            <strong>{formatAmount(quote.totalAmount)}</strong>
          </div>
        </div>
      ) : null}

      {completedFunding ? (
        <div className="wallet-quote" role="status">
          <div>
            <span><ReceiptText size={13} /> Receipt</span>
            <strong>{formatAmount(completedFunding.transaction.amount)}</strong>
          </div>
          <div>
            <span>Transaction reference</span>
            <strong>{completedFunding.transaction.externalReference ?? completedFunding.transaction.id}</strong>
          </div>
          <div className="wallet-quote__total">
            <span><Scale size={13} /> Ledger verification</span>
            <strong>{completedFunding.reconciliation?.isBalanced ? 'Balanced' : 'Review required'}</strong>
          </div>
        </div>
      ) : null}

      {completedFunding ? (
        <p className="wallet-feedback is-success">
          <CheckCircle2 size={14} /> {completedFunding.sourceLabel}
        </p>
      ) : null}

      {message ? (
        <p className={`wallet-feedback ${isError ? 'is-error' : 'is-success'}`} role="status">
          {message}
        </p>
      ) : null}

      <div className="wallet-panel__actions">
        {completedFunding ? (
          <Link href="/transaction-history?source=wallet" className="wallet-button wallet-button--secondary">
            View transaction history
          </Link>
        ) : null}
        <button type="button" className="wallet-button wallet-button--secondary" onClick={preview} disabled={isPending}>
          {isPending ? <Loader2 size={16} className="animate-spin" /> : <Calculator size={16} />}
          Preview
        </button>
        <button
          type="button"
          className="wallet-button wallet-button--primary"
          onClick={submit}
          disabled={isPending || !quote}
        >
          {isPending ? <Loader2 size={16} className="animate-spin" /> : operation === 'top_up' ? <ArrowDownToLine size={16} /> : <ArrowUpFromLine size={16} />}
          Confirm {operation === 'top_up' ? 'top-up' : 'withdrawal'}
        </button>
      </div>
    </section>
  );
}
