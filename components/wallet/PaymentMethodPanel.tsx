'use client';

import { useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { CreditCard, Loader2, Plus, ShieldCheck } from 'lucide-react';

import {
  createDemoPaymentMethod,
  setDefaultPaymentMethod,
} from '@/lib/actions/wallet.actions';
import type { DemoCardInput, PaymentMethod } from '@/types/wallet';

type PaymentMethodPanelProps = {
  paymentMethods: PaymentMethod[];
};

const currentYear = new Date().getFullYear();

export default function PaymentMethodPanel({ paymentMethods }: PaymentMethodPanelProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [showForm, setShowForm] = useState(paymentMethods.length === 0);
  const [bankName, setBankName] = useState<DemoCardInput['bankName']>('Capitec');
  const [brand, setBrand] = useState<DemoCardInput['brand']>('Mastercard');
  const [last4, setLast4] = useState('3684');
  const [expiryMonth, setExpiryMonth] = useState(12);
  const [expiryYear, setExpiryYear] = useState(currentYear + 2);
  const [message, setMessage] = useState<string | null>(null);
  const [isError, setIsError] = useState(false);

  const addCard = () => {
    if (!/^\d{4}$/.test(last4)) {
      setIsError(true);
      setMessage('Enter exactly four numeric card digits.');
      return;
    }

    startTransition(async () => {
      const result = await createDemoPaymentMethod({
        bankName,
        brand,
        last4,
        expiryMonth,
        expiryYear,
      });

      if (!result.ok) {
        setIsError(true);
        setMessage(result.error);
        return;
      }

      setIsError(false);
      setMessage(`${result.data.bankName} ${result.data.brand} ending ${result.data.last4} is ready.`);
      setShowForm(false);
      router.refresh();
    });
  };

  const makeDefault = (paymentMethodId: string) => {
    startTransition(async () => {
      const result = await setDefaultPaymentMethod(paymentMethodId);
      if (!result.ok) {
        setIsError(true);
        setMessage(result.error);
        return;
      }

      setIsError(false);
      setMessage('Default funding card updated.');
      router.refresh();
    });
  };

  return (
    <section className="wallet-panel payment-method-panel">
      <div className="wallet-panel__heading wallet-panel__heading--row">
        <div>
          <span className="wallet-eyebrow">Tokenised cards</span>
          <h2>Payment methods</h2>
          <p>Kape stores only a token and masked card metadata.</p>
        </div>
        <button
          type="button"
          className="wallet-icon-button"
          onClick={() => setShowForm((value) => !value)}
          aria-label="Add a demo payment method"
        >
          <Plus size={18} />
        </button>
      </div>

      <div className="payment-method-list">
        {paymentMethods.length ? (
          paymentMethods.map((method) => (
            <article key={method.id} className={`payment-method-card ${method.isDefault ? 'is-default' : ''}`}>
              <div className="payment-method-card__icon">
                <CreditCard size={20} />
              </div>
              <div className="payment-method-card__details">
                <div>
                  <strong>{method.bankName}</strong>
                  {method.isDefault ? <span>Default</span> : null}
                </div>
                <p>{method.brand} •••• {method.last4}</p>
                <small>Expires {String(method.expiryMonth).padStart(2, '0')}/{String(method.expiryYear).slice(-2)}</small>
              </div>
              {!method.isDefault && method.status === 'active' ? (
                <button type="button" onClick={() => makeDefault(method.id)} disabled={isPending}>
                  Make default
                </button>
              ) : (
                <ShieldCheck size={18} aria-label="Verified card" />
              )}
            </article>
          ))
        ) : (
          <div className="wallet-empty-inline">
            <CreditCard size={20} />
            <div>
              <strong>No tokenised cards</strong>
              <span>Add a safe demo card to test wallet funding.</span>
            </div>
          </div>
        )}
      </div>

      {showForm ? (
        <div className="payment-method-form">
          <label className="wallet-field">
            <span>Bank</span>
            <select value={bankName} onChange={(event) => setBankName(event.target.value as DemoCardInput['bankName'])}>
              <option>Capitec</option>
              <option>Standard Bank</option>
              <option>FNB</option>
              <option>Absa</option>
              <option>Nedbank</option>
            </select>
          </label>
          <label className="wallet-field">
            <span>Card network</span>
            <select value={brand} onChange={(event) => setBrand(event.target.value as DemoCardInput['brand'])}>
              <option>Mastercard</option>
              <option>Visa</option>
            </select>
          </label>
          <label className="wallet-field">
            <span>Last four digits</span>
            <input
              inputMode="numeric"
              maxLength={4}
              value={last4}
              onChange={(event) => setLast4(event.target.value.replace(/\D/g, '').slice(0, 4))}
            />
          </label>
          <label className="wallet-field">
            <span>Expiry month</span>
            <select value={expiryMonth} onChange={(event) => setExpiryMonth(Number(event.target.value))}>
              {Array.from({ length: 12 }, (_, index) => index + 1).map((month) => (
                <option key={month} value={month}>{String(month).padStart(2, '0')}</option>
              ))}
            </select>
          </label>
          <label className="wallet-field">
            <span>Expiry year</span>
            <select value={expiryYear} onChange={(event) => setExpiryYear(Number(event.target.value))}>
              {Array.from({ length: 8 }, (_, index) => currentYear + index).map((year) => (
                <option key={year} value={year}>{year}</option>
              ))}
            </select>
          </label>
          <button type="button" className="wallet-button wallet-button--primary" onClick={addCard} disabled={isPending}>
            {isPending ? <Loader2 size={16} className="animate-spin" /> : <Plus size={16} />}
            Tokenise demo card
          </button>
        </div>
      ) : null}

      {message ? (
        <p className={`wallet-feedback ${isError ? 'is-error' : 'is-success'}`} role="status">
          {message}
        </p>
      ) : null}
    </section>
  );
}
