'use client';

import Link from 'next/link';
import { Check, Clipboard, Landmark, Loader2, ReceiptText, RefreshCw, ShieldCheck, WalletCards } from 'lucide-react';
import { useEffect, useMemo, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';

import MarketplaceBrandMark from '@/components/marketplace/MarketplaceBrandMark';
import {
  previewKapePayVoucher,
  refreshKapePayPayment,
  submitKapePayVoucher,
} from '@/lib/actions/kape-pay.actions';
import { getVoucherDenominations, pollVoucherOrder } from '@/lib/actions/marketplace.actions';
import { formatAmount } from '@/lib/utils';
import type { DemoPaymentScenario, KapePayQuote, KapePaySource, PaymentAttempt } from '@/types/kape-pay';
import type { VoucherCategory, VoucherDenomination, VoucherOrder, VoucherProduct } from '@/types/marketplace';
import type { LinkedBankAccount } from '@/types/wallet';

const activeFulfilmentStatuses = new Set(['pending', 'processing', 'payment_completed']);
const activePaymentStatuses = new Set(['created', 'awaiting_approval', 'pending']);

export default function KapePayVoucherPanel({
  categories,
  products,
  walletAvailable,
  linkedAccounts,
}: {
  categories: VoucherCategory[];
  products: VoucherProduct[];
  walletAvailable: number;
  linkedAccounts: LinkedBankAccount[];
}) {
  const router = useRouter();
  const activeAccounts = linkedAccounts.filter((account) => account.isActive);
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [selectedProductId, setSelectedProductId] = useState(products[0]?.id ?? '');
  const [denominations, setDenominations] = useState<VoucherDenomination[]>([]);
  const [selectedDenominationId, setSelectedDenominationId] = useState('');
  const [paymentSource, setPaymentSource] = useState<KapePaySource>(activeAccounts.length ? 'linked_bank' : 'wallet');
  const [linkedBankAccountId, setLinkedBankAccountId] = useState(activeAccounts[0]?.id ?? '');
  const [scenario, setScenario] = useState<DemoPaymentScenario>('success');
  const [quote, setQuote] = useState<KapePayQuote | null>(null);
  const [order, setOrder] = useState<VoucherOrder | null>(null);
  const [payment, setPayment] = useState<PaymentAttempt | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [isPending, startTransition] = useTransition();

  const filteredProducts = useMemo(
    () => products.filter((product) => selectedCategory === 'all' || product.categoryId === selectedCategory),
    [products, selectedCategory]
  );
  const selectedProduct = products.find((product) => product.id === selectedProductId) ?? null;
  const selectedDenomination = denominations.find((item) => item.id === selectedDenominationId) ?? null;
  const selectedAccount = activeAccounts.find((account) => account.id === linkedBankAccountId) ?? null;

  useEffect(() => {
    const firstVisible = filteredProducts[0];
    if (firstVisible && !filteredProducts.some((product) => product.id === selectedProductId)) {
      setSelectedProductId(firstVisible.id);
    }
  }, [filteredProducts, selectedProductId]);

  useEffect(() => {
    if (!selectedProductId) {
      setDenominations([]);
      setSelectedDenominationId('');
      return;
    }

    setQuote(null);
    setOrder(null);
    setPayment(null);
    setError(null);
    startTransition(() => {
      void getVoucherDenominations(selectedProductId).then((result) => {
        if (!result.ok) {
          setDenominations([]);
          setSelectedDenominationId('');
          setError(result.error);
          return;
        }
        setDenominations(result.data);
        setSelectedDenominationId(result.data[0]?.id ?? '');
      });
    });
  }, [selectedProductId]);

  useEffect(() => {
    if (!order || !activeFulfilmentStatuses.has(order.status)) return;

    const timer = window.setInterval(() => {
      void pollVoucherOrder(order.id).then((result) => {
        if (!result.ok) return;
        setOrder(result.data);
        if (result.data.status === 'fulfilled') {
          setMessage('Payment and fulfilment completed. Your demonstration voucher is ready.');
          router.refresh();
        }
      });
    }, 1500);

    return () => window.clearInterval(timer);
  }, [order, router]);

  const resetCheckout = () => {
    setQuote(null);
    setOrder(null);
    setPayment(null);
    setMessage(null);
    setError(null);
  };

  const preview = () => {
    if (!selectedProduct || !selectedDenomination) return;
    setError(null);
    setMessage(null);
    startTransition(() => {
      void previewKapePayVoucher({
        voucherProductId: selectedProduct.id,
        voucherDenominationId: selectedDenomination.id,
        paymentSource,
        linkedBankAccountId: paymentSource === 'linked_bank' ? linkedBankAccountId : null,
      }).then((result) => {
        if (!result.ok) {
          setQuote(null);
          setError(result.error);
          return;
        }
        setQuote(result.data);
      });
    });
  };

  const purchase = () => {
    if (!selectedProduct || !selectedDenomination || !quote) return;
    setError(null);
    setMessage(null);
    startTransition(() => {
      void submitKapePayVoucher({
        voucherProductId: selectedProduct.id,
        voucherDenominationId: selectedDenomination.id,
        paymentSource,
        linkedBankAccountId: paymentSource === 'linked_bank' ? linkedBankAccountId : null,
        scenario: paymentSource === 'linked_bank' ? scenario : 'success',
        returnUrl: `${window.location.origin}/vouchers`,
        idempotencyKey: `kape-pay-voucher-${crypto.randomUUID()}`,
      }).then((result) => {
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setOrder(result.data.order);
        setPayment(result.data.payment);
        setQuote(null);
        if (result.data.payment.status === 'completed') {
          setMessage(
            paymentSource === 'wallet'
              ? 'Synthetic wallet payment completed. Kape is fulfilling the voucher.'
              : 'Demo Pay by Bank completed. Kape is fulfilling the voucher.'
          );
        } else if (result.data.payment.status === 'failed') {
          setError(`Payment failed${result.data.payment.failureCode ? `: ${result.data.payment.failureCode}` : '.'}`);
        } else if (result.data.payment.status === 'cancelled') {
          setError('The demonstration payment was cancelled.');
        } else {
          setMessage('The demonstration payment is awaiting its next provider status.');
        }
        router.refresh();
      });
    });
  };

  const refreshPayment = () => {
    if (!payment) return;
    startTransition(() => {
      void refreshKapePayPayment(payment.id).then((result) => {
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setPayment(result.data);
        setMessage(`Payment status: ${result.data.status}.`);
        router.refresh();
      });
    });
  };

  const copyVoucherCode = async () => {
    if (!order?.voucherCode) return;
    await navigator.clipboard.writeText(order.voucherCode);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1800);
  };

  return (
    <section className="marketplace-purchase-shell">
      <div className="marketplace-catalogue">
        <div className="marketplace-section-heading">
          <div>
            <span>Kape Pay catalogue</span>
            <h2>Choose a voucher</h2>
            <p>Pay directly from a linked sandbox bank or use the separate synthetic Kape Wallet ledger.</p>
          </div>
          <span className="marketplace-wallet-pill">Demo wallet {formatAmount(walletAvailable)}</span>
        </div>

        <div className="marketplace-category-tabs" role="tablist" aria-label="Voucher categories">
          <button type="button" className={selectedCategory === 'all' ? 'is-active' : ''} onClick={() => setSelectedCategory('all')}>All</button>
          {categories.map((category) => (
            <button
              key={category.id}
              type="button"
              className={selectedCategory === category.id ? 'is-active' : ''}
              onClick={() => setSelectedCategory(category.id)}
            >
              {category.name}
            </button>
          ))}
        </div>

        <div className="marketplace-product-grid">
          {filteredProducts.map((product) => (
            <button
              key={product.id}
              type="button"
              className={`marketplace-product-card${selectedProductId === product.id ? ' is-selected' : ''}`}
              onClick={() => setSelectedProductId(product.id)}
            >
              <MarketplaceBrandMark name={product.brandName} />
              <span>{product.productName}</span>
              <small>{product.description}</small>
            </button>
          ))}
        </div>
      </div>

      <aside className="marketplace-checkout-card">
        <div className="marketplace-section-heading is-compact">
          <div>
            <span>Provider-neutral checkout</span>
            <h2>{selectedProduct?.productName ?? 'Select a voucher'}</h2>
          </div>
          <ShieldCheck size={21} />
        </div>

        {selectedProduct ? <MarketplaceBrandMark name={selectedProduct.brandName} compact /> : null}

        <label className="marketplace-field">
          <span>Voucher amount</span>
          <div className="marketplace-denominations">
            {denominations.map((denomination) => (
              <button
                key={denomination.id}
                type="button"
                className={selectedDenominationId === denomination.id ? 'is-active' : ''}
                onClick={() => {
                  setSelectedDenominationId(denomination.id);
                  resetCheckout();
                }}
              >
                {formatAmount(denomination.amount)}
              </button>
            ))}
          </div>
        </label>

        <label className="marketplace-field">
          <span>Payment source</span>
          <select
            value={paymentSource}
            onChange={(event) => {
              setPaymentSource(event.target.value as KapePaySource);
              resetCheckout();
            }}
          >
            {activeAccounts.length ? <option value="linked_bank">Pay directly from linked bank</option> : null}
            <option value="wallet">Kape Demo Wallet — synthetic funds</option>
          </select>
        </label>

        {paymentSource === 'linked_bank' ? (
          <>
            <label className="marketplace-field">
              <span>Linked sandbox account</span>
              <select
                value={linkedBankAccountId}
                onChange={(event) => {
                  setLinkedBankAccountId(event.target.value);
                  resetCheckout();
                }}
              >
                {activeAccounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.institutionName} {account.accountName} •••• {account.accountNumberMask}
                  </option>
                ))}
              </select>
            </label>
            {selectedAccount ? (
              <p className="marketplace-fine-print"><Landmark size={14} /> Available sandbox bank balance: {formatAmount(selectedAccount.availableBalance)}</p>
            ) : null}
            <label className="marketplace-field">
              <span>Demo provider outcome</span>
              <select value={scenario} onChange={(event) => setScenario(event.target.value as DemoPaymentScenario)}>
                <option value="success">Successful payment</option>
                <option value="awaiting_approval">Awaiting approval</option>
                <option value="pending">Processing</option>
                <option value="failed">Declined</option>
                <option value="cancelled">Customer cancelled</option>
                <option value="insufficient_funds">Insufficient funds</option>
              </select>
            </label>
          </>
        ) : (
          <p className="marketplace-fine-print"><WalletCards size={14} /> Synthetic available balance: {formatAmount(walletAvailable)}</p>
        )}

        {quote ? (
          <div className="marketplace-quote">
            <span><small>Voucher</small><strong>{formatAmount(quote.amount)}</strong></span>
            <span><small>Fee</small><strong>{formatAmount(quote.feeAmount)}</strong></span>
            <span className="is-total"><small>Total payment</small><strong>{formatAmount(quote.totalAmount)}</strong></span>
          </div>
        ) : null}

        {payment ? (
          <div className={`marketplace-fulfilment marketplace-fulfilment--${payment.status}`}>
            <div>
              {payment.status === 'completed' ? <Check size={20} /> : <RefreshCw size={20} className={activePaymentStatuses.has(payment.status) ? 'is-spinning' : ''} />}
              <span><strong>Payment {payment.status}</strong><small>{payment.providerId} · {payment.externalPaymentId.slice(-12)}</small></span>
            </div>
            {activePaymentStatuses.has(payment.status) ? (
              <button type="button" onClick={refreshPayment} disabled={isPending}><RefreshCw size={15} /> Refresh payment</button>
            ) : null}
          </div>
        ) : null}

        {order ? (
          <div className={`marketplace-fulfilment marketplace-fulfilment--${order.status}`}>
            <div>
              {order.status === 'fulfilled' ? <Check size={20} /> : <RefreshCw size={20} className={activeFulfilmentStatuses.has(order.status) ? 'is-spinning' : ''} />}
              <span><strong>{order.status === 'fulfilled' ? 'Voucher ready' : `Order ${order.status}`}</strong><small>Order {order.id.slice(0, 8).toUpperCase()}</small></span>
            </div>
            {order.voucherCode ? (
              <button type="button" onClick={copyVoucherCode}>
                <span>{order.voucherCode}</span>
                {copied ? <Check size={16} /> : <Clipboard size={16} />}
              </button>
            ) : null}
            <Link href={`/receipts/voucher/${order.id}`}><ReceiptText size={16} /> View receipt</Link>
          </div>
        ) : null}

        {message ? <p className="marketplace-message is-success">{message}</p> : null}
        {error ? <p className="marketplace-message is-error">{error}</p> : null}

        <div className="marketplace-checkout-actions">
          <button type="button" className="is-secondary" disabled={isPending || !selectedDenomination || (paymentSource === 'linked_bank' && !linkedBankAccountId)} onClick={preview}>
            {isPending ? <Loader2 size={17} className="is-spinning" /> : null} Preview
          </button>
          <button type="button" disabled={isPending || !quote} onClick={purchase}>
            {isPending ? <Loader2 size={17} className="is-spinning" /> : null} Pay and buy
          </button>
        </div>

        <p className="marketplace-fine-print">Demonstration only. Linked-bank payments use the Pay by Bank provider boundary and never credit the Kape Wallet. Wallet purchases use synthetic ledger funds.</p>
      </aside>
    </section>
  );
}
