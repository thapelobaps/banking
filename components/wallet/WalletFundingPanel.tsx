'use client';

import { useMemo, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowDownToLine, ArrowUpFromLine, Calculator, Loader2 } from 'lucide-react';

import { previewWalletFunding, submitWalletFunding } from '@/lib/actions/wallet.actions';
import { formatAmount } from '@/lib/utils';
import type {
  LinkedBankAccount,
  PaymentMethod,
  WalletFundingOperation,
  WalletFundingQuote,
} from '@/types/wallet';

type WalletFundingPanelProps = {
  paymentMethods: PaymentMethod[];
  linkedAccounts: LinkedBankAccount[];
};

const createIdempotencyKey = (operation: WalletFundingOperation) => {
  const id = typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  return `wallet-ui-${operation}-${id}`;
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
  const [source, setSource] = useState(() => {
    const defaultMethod = paymentMethods.find((method) => method.isDefault) ?? paymentMethods[0];
    if (defaultMethod) return `card:${defaultMethod.id}`;
    if (linkedAccounts[0]) return `bank:${linkedAccounts[0].id}`;
    return '';
  });
  const [quote, setQuote] = useState<WalletFundingQuote | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [isError, setIsError] = useState(false);
  const [idempotencyKey, setIdempotencyKey] = useState(() => createIdempotencyKey('top_up'));

  const sourceOptions = useMemo(
    () => [
      ...paymentMethods
        .filter((method) => method.status === 'active')
        .map((method) => ({
          value: `card:${method.id}`,
          label: `${method.bankName} ${method.brand} •••• ${method.last4}`,
        })),
      ...linkedAccounts
        .filter((account) => account.isActive)
        .map((account) => ({
          value: `bank:${account.id}`,
          label: `${account.institutionName} ${account.accountName} •••• ${account.accountNumberMask}`,
        })),
    ],
    [linkedAccounts, paymentMethods]
  );

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
      setMessage('Add a tokenised card or connect a bank account first.');
      return false;
    }
    if (!Number.isFinite(Number(amount)) || Number(amount) <= 0) {
      setIsError(true);
      setMessage('Enter an amount greater than zero.');
      return false;
    }
    return true;
  };

  const changeOperation = (nextOperation: WalletFundingOperation) => {
    setOperation(nextOperation);
    setQuote(null);
    setMessage(null);
    setIdempotencyKey(createIdempotencyKey(nextOperation));
  };

  const preview = () => {
    if (!validate()) return;

    setMessage(null);
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
      setMessage('Quote ready. Review the fee and confirm when you are satisfied.');
    });
  };

  const submit = () => {
    if (!validate()) return;

    setMessage(null);
    startTransition(async () => {
      const result = await submitWalletFunding(buildPayload());
      if (!result.ok) {
        setIsError(true);
        setMessage(result.error);
        return;
      }

      setIsError(false);
      setMessage(
        operation === 'top_up'
          ? `${formatAmount(result.data.amount)} was added to your Kape wallet.`
          : `${formatAmount(result.data.amount)} withdrawal completed.`
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
          <p>Preview every operation before it reaches the double-entry ledger.</p>
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
          <span>Funding source</span>
          <select
            value={source}
            onChange={(event) => {
              setSource(event.target.value);
              setQuote(null);
            }}
          >
            <option value="">Choose a card or linked account</option>
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
                setQuote(null);
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
            onChange={(event) => setReference(event.target.value)}
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

      {message ? (
        <p className={`wallet-feedback ${isError ? 'is-error' : 'is-success'}`} role="status">
          {message}
        </p>
      ) : null}

      <div className="wallet-panel__actions">
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
