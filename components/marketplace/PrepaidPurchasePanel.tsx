'use client';

import Link from 'next/link';
import { Bolt, Check, Clipboard, Loader2, Phone, ReceiptText, RefreshCw, ShieldCheck } from 'lucide-react';
import { useEffect, useMemo, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';

import MarketplaceBrandMark from '@/components/marketplace/MarketplaceBrandMark';
import {
  pollPrepaidOrder,
  previewPrepaidPurchase,
  submitPrepaidPurchase,
  validatePrepaidRecipient,
} from '@/lib/actions/marketplace.actions';
import { formatAmount } from '@/lib/utils';
import type {
  MarketplaceQuote,
  PrepaidOperator,
  PrepaidOrder,
  PrepaidProduct,
} from '@/types/marketplace';

const activeFulfilmentStatuses = new Set(['pending', 'processing']);

export default function PrepaidPurchasePanel({
  operators,
  products,
  walletAvailable,
}: {
  operators: PrepaidOperator[];
  products: PrepaidProduct[];
  walletAvailable: number;
}) {
  const router = useRouter();
  const productTypes = useMemo(() => Array.from(new Set(products.map((product) => product.productType))), [products]);
  const [selectedType, setSelectedType] = useState(productTypes[0] ?? 'airtime');
  const filteredProducts = useMemo(() => products.filter((product) => product.productType === selectedType), [products, selectedType]);
  const [selectedProductId, setSelectedProductId] = useState(filteredProducts[0]?.id ?? products[0]?.id ?? '');
  const [recipient, setRecipient] = useState('');
  const [amount, setAmount] = useState('100.00');
  const [quote, setQuote] = useState<MarketplaceQuote | null>(null);
  const [order, setOrder] = useState<PrepaidOrder | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [isPending, startTransition] = useTransition();

  const selectedProduct = products.find((product) => product.id === selectedProductId) ?? null;
  const selectedOperator = selectedProduct
    ? operators.find((operator) => operator.id === selectedProduct.operatorId) ?? null
    : null;

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
    setError(null);
  }, [selectedProduct]);

  useEffect(() => {
    if (!order || !activeFulfilmentStatuses.has(order.status)) return;

    const timer = window.setInterval(() => {
      void pollPrepaidOrder(order.id).then((result) => {
        if (!result.ok) return;
        setOrder(result.data);
        if (result.data.status === 'fulfilled') {
          setMessage('Purchase fulfilled. Your token or provider reference is ready.');
          router.refresh();
        }
      });
    }, 1500);

    return () => window.clearInterval(timer);
  }, [order, router]);

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

        const result = await previewPrepaidPurchase({
          productId: selectedProduct.id,
          recipient: validation.data.normalisedRecipient,
          amount: numericAmount,
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
      void submitPrepaidPurchase({
        productId: selectedProduct.id,
        recipient,
        amount: Number(amount),
        idempotencyKey: `prepaid-ui-${crypto.randomUUID()}`,
      }).then((result) => {
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setOrder(result.data);
        setMessage('Payment completed. Kape is fulfilling the purchase through the provider queue.');
        setQuote(null);
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
            <span>Prepaid services</span>
            <h2>Choose a service</h2>
            <p>Validate the recipient, preview the debit and receive the provider reference after fulfilment.</p>
          </div>
          <span className="marketplace-wallet-pill">Available {formatAmount(walletAvailable)}</span>
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
            <span>Secure checkout</span>
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
              setQuote(null);
              setOrder(null);
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
                setQuote(null);
                setOrder(null);
              }}
            />
          </div>
          {selectedProduct ? (
            <small>Allowed range {formatAmount(selectedProduct.minimumAmount)} to {formatAmount(selectedProduct.maximumAmount)}</small>
          ) : null}
        </label>

        {quote ? (
          <div className="marketplace-quote">
            <span><small>Purchase</small><strong>{formatAmount(quote.amount)}</strong></span>
            <span><small>Fee</small><strong>{formatAmount(quote.feeAmount)}</strong></span>
            <span className="is-total"><small>Total debit</small><strong>{formatAmount(quote.totalAmount)}</strong></span>
          </div>
        ) : null}

        {order ? (
          <div className={`marketplace-fulfilment marketplace-fulfilment--${order.status}`}>
            <div>
              {order.status === 'fulfilled' ? <Check size={20} /> : <RefreshCw size={20} className="is-spinning" />}
              <span><strong>{order.status === 'fulfilled' ? 'Purchase ready' : 'Fulfilling purchase'}</strong><small>Order {order.id.slice(0, 8).toUpperCase()}</small></span>
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
          <button type="button" className="is-secondary" disabled={isPending || !selectedProduct || !recipient.trim()} onClick={preview}>
            {isPending ? <Loader2 size={17} className="is-spinning" /> : null} Validate and preview
          </button>
          <button type="button" disabled={isPending || !quote} onClick={purchase}>
            {isPending ? <Loader2 size={17} className="is-spinning" /> : null} Buy now
          </button>
        </div>

        <p className="marketplace-fine-print">Kape validates the recipient before debiting the wallet. Purchases use idempotency and durable SQL queue fulfilment.</p>
      </aside>
    </section>
  );
}
