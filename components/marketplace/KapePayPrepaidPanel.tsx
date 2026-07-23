'use client';

import Link from 'next/link';
import { Bolt, Check, Clipboard, Landmark, Loader2, Phone, ReceiptText, RefreshCw, ShieldCheck, WalletCards } from 'lucide-react';
import { useEffect, useMemo, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';

import MarketplaceBrandMark from '@/components/marketplace/MarketplaceBrandMark';
import {
  previewKapePayPrepaid,
  refreshKapePayPayment,
  submitKapePayPrepaid,
} from '@/lib/actions/kape-pay.actions';
import { pollPrepaidOrder, validatePrepaidRecipient } from '@/lib/actions/marketplace.actions';
import { formatAmount } from '@/lib/utils';
import type { DemoPaymentScenario, KapePayQuote, KapePaySource, PaymentAttempt } from '@/types/kape-pay';
import type { PrepaidOperator, PrepaidOrder, PrepaidProduct } from '@/types/marketplace';
import type { LinkedBankAccount } from '@/types/wallet';

const activeFulfilmentStatuses = new Set(['pending', 'processing', 'payment_completed']);
const activePaymentStatuses = new Set(['created', 'awaiting_approval', 'pending']);

export default function KapePayPrepaidPanel({
  operators,
  products,
  walletAvailable,
  linkedAccounts,
}: {
  operators: PrepaidOperator[];
  products: PrepaidProduct[];
  walletAvailable: number;
  linkedAccounts: LinkedBankAccount[];
}) {
  const router = useRouter();
  const activeAccounts = linkedAccounts.filter((account) => account.isActive);
  const productTypes = useMemo(() => Array.from(new Set(products.map((product) => product.productType))), [products]);
  const [selectedType, setSelectedType] = useState(productTypes[0] ?? 'airtime');
  const filteredProducts = useMemo(() => products.filter((product) => product.productType === selectedType), [products, selectedType]);
  const [selectedProductId, setSelectedProductId] = useState(filteredProducts[0]?.id ?? products[0]?.id ?? '');
  const [recipient, setRecipient] = useState('');
  const [amount, setAmount] = useState('100.00');
  const [paymentSource, setPaymentSource] = useState<KapePaySource>(activeAccounts.length ? 'linked_bank' : 'wallet');
  const [linkedBankAccountId, setLinkedBankAccountId] = useState(activeAccounts[0]?.id ?? '');
  const [scenario, setScenario] = useState<DemoPaymentScenario>('success');
  const [quote, setQuote] = useState<KapePayQuote | null>(null);
  const [order, setOrder] = useState<PrepaidOrder | null>(null);
  const [payment, setPayment] = useState<PaymentAttempt | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [isPending, startTransition] = useTransition();

  const selectedProduct = products.find((product) => product.id === selectedProductId) ?? null;
  const selectedOperator = selectedProduct
    ? operators.find((operator) => operator.id === selectedProduct.operatorId) ?? null
    : null;
  const selectedAccount = activeAccounts.find((account) => account.id === linkedBankAccountId) ?? null;

  useEffect(() => {
    const firstVisible = filteredProducts[0];
    if (firstVisible && !filteredProducts.some((product) => product.id === selectedProductId)) {
      setSelectedProductId(firstVisible.id);
    }
  }, [filteredProducts, selectedProductId]);

  useEffect(() => {
    if (!selectedProduct) return;
    const suggestedAmount = selectedProduct.fixedAmount ?? Math.max(selectedProduct.minimumAmount, 100);
    setAmount(suggestedAmount.toFixed(2));
    setQuote(null);
    setOrder(null);
    setPayment(null);
    setError(null);
  }, [selectedProduct]);

  useEffect(() => {
    if (!order || !activeFulfilmentStatuses.has(order.status)) return;

    const timer = window.setInterval(() => {
      void pollPrepaidOrder(order.id).then((result) => {
        if (!result.ok) return;
        setOrder(result.data);
        if (result.data.status === 'fulfilled') {
          setMessage('Payment and fulfilment completed. Your demonstration token or reference is ready.');
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
    if (!selectedProduct) return;
    const numericAmount = Number(amount);
    if (!Number.isFinite(numericAmount) || numericAmount <= 0) {
      setError('Enter a valid amount.');
      return;
    }

    setError(null);
    setMessage(null);
    startTransition(() => {
      void (async () => {
        const validation = await validatePrepaidRecipient(selectedProduct.id, recipient);
        if (!validation.ok) {
          setQuote(null);
          setError(validation.error);
          return;
        }

        const result = await previewKapePayPrepaid({
          productId: selectedProduct.id,
          recipient: validation.data.normalisedRecipient,
          amount: numericAmount,
          paymentSource,
          linkedBankAccountId: paymentSource === 'linked_bank' ? linkedBankAccountId : null,
        });
        if (!result.ok) {
          setQuote(null);
          setError(result.error);
          return;
        }
        setRecipient(validation.data.normalisedRecipient);
        setQuote(result.data);
      })();
    });
  };

  const purchase = () => {
    if (!selectedProduct || !quote) return;
    setError(null);
    setMessage(null);
    startTransition(() => {
      void submitKapePayPrepaid({
        productId: selectedProduct.id,
        recipient,
        amount: Number(amount),
        paymentSource,
        linkedBankAccountId: paymentSource === 'linked_bank' ? linkedBankAccountId : null,
        scenario: paymentSource === 'linked_bank' ? scenario : 'success',
        returnUrl: `${window.location.origin}/prepaid`,
        idempotencyKey: `kape-pay-prepaid-${crypto.randomUUID()}`,
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
              ? 'Synthetic wallet payment completed. Kape is fulfilling the purchase.'
              : 'Demo Pay by Bank completed. Kape is fulfilling the purchase.'
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

  const copyReference = async () => {
    if (!order?.fulfilmentReference) return;
    await navigator.clipboard.writeText(order.fulfilmentReference);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1800);
  };

  const recipientLabel = selectedType === 'electricity' ? 'Prepaid meter number' : 'Mobile number';

  return (
    <section className="marketplace-purchase-shell">
      <div className="marketplace-catalogue">
        <div className="marketplace-section-heading">
          <div>
            <span>Kape Pay services</span>
            <h2>Choose a service</h2>
            <p>Pay directly from a linked sandbox bank or use the separate synthetic Kape Wallet ledger.</p>
          </div>
          <span className="marketplace-wallet-pill">Demo wallet {formatAmount(walletAvailable)}</span>
        </div>

        <div className="marketplace-category-tabs" role="tablist" aria-label="Prepaid product types">
          {productTypes.map((type) => (
            <button
              key={type}
              type="button"
              className={selectedType === type ? 'is-active' : ''}
              onClick={() => setSelectedType(type)}
            >
              {type === 'electricity' ? <Bolt size={15} /> : <Phone size={15} />}
              {type.replaceAll('_', ' ')}
            </button>
          ))}
        </div>

        <div className="marketplace-product-grid is-prepaid">
          {filteredProducts.map((product) => {
            const operator = operators.find((item) => item.id === product.operatorId);
            return (
              <button
                key={product.id}
                type="button"
                className={`marketplace-product-card${selectedProductId === product.id ? ' is-selected' : ''}`}
                onClick={() => setSelectedProductId(product.id)}
              >
                <MarketplaceBrandMark name={operator?.name ?? product.name} />
                <span>{product.name}</span>
                <small>
                  {product.fixedAmount
                    ? formatAmount(product.fixedAmount)
                    : `${formatAmount(product.minimumAmount)} – ${formatAmount(product.maximumAmount)}`}
                </small>
              </button>
            );
          })}
        </div>
      </div>

      <aside className="marketplace-checkout-card">
        <div className="marketplace-section-heading is-compact">
          <div>
            <span>Provider-neutral checkout</span>
            <h2>{selectedProduct?.name ?? 'Select a service'}</h2>
          </div>
          <ShieldCheck size={21} />
        </div>

        {selectedOperator ? <MarketplaceBrandMark name={selectedOperator.name} compact /> : null}

        <label className="marketplace-field">
          <span>{recipientLabel}</span>
          <input
            value={recipient}
            inputMode="numeric"
            placeholder={selectedType === 'electricity' ? 'Enter meter number' : '082 123 4567'}
            onChange={(event) => {
              setRecipient(event.target.value);
              resetCheckout();
            }}
          />
        </label>

        <label className="marketplace-field">
          <span>Amount</span>
          <div className="marketplace-money-input">
            <b>R</b>
            <input
              value={amount}
              inputMode="decimal"
              disabled={selectedProduct?.fixedAmount != null}
              onChange={(event) => {
                setAmount(event.target.value);
                resetCheckout();
              }}
            />
          </div>
          {selectedProduct ? (
            <small>Allowed range {formatAmount(selectedProduct.minimumAmount)} to {formatAmount(selectedProduct.maximumAmount)}</small>
          ) : null}
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
            <span><small>Purchase</small><strong>{formatAmount(quote.amount)}</strong></span>
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
              <span><strong>{order.status === 'fulfilled' ? 'Purchase ready' : `Order ${order.status}`}</strong><small>Order {order.id.slice(0, 8).toUpperCase()}</small></span>
            </div>
            {order.fulfilmentReference ? (
              <button type="button" onClick={copyReference}>
                <span>{order.fulfilmentReference}</span>
                {copied ? <Check size={16} /> : <Clipboard size={16} />}
              </button>
            ) : null}
            <Link href={`/receipts/prepaid/${order.id}`}><ReceiptText size={16} /> View receipt</Link>
          </div>
        ) : null}

        {message ? <p className="marketplace-message is-success">{message}</p> : null}
        {error ? <p className="marketplace-message is-error">{error}</p> : null}

        <div className="marketplace-checkout-actions">
          <button type="button" className="is-secondary" disabled={isPending || !selectedProduct || !recipient.trim() || (paymentSource === 'linked_bank' && !linkedBankAccountId)} onClick={preview}>
            {isPending ? <Loader2 size={17} className="is-spinning" /> : null} Validate and preview
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
